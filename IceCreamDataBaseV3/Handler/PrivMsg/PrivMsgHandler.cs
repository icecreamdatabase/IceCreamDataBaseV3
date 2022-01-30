﻿using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using IceCreamDataBaseV3.Model;
using IceCreamDataBaseV3.Model.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.X509.Qualified;
using TwitchIrcHubClient;
using TwitchIrcHubClient.DataTypes.Parsed.FromTwitch;
using TwitchIrcHubClient.DataTypes.Parsed.ToTwitch;

namespace IceCreamDataBaseV3.Handler.PrivMsg;

public class PrivMsgHandler
{
    private readonly IrcHubClient _hub;
    private readonly MessageParameterHelper _messageParameterHelper;

    private readonly Dictionary<int, ConcurrentBag<(int id, string phrase)>> _commandTriggers = new();
    private readonly Dictionary<int, ConcurrentBag<(int id, Regex expression)>> _commandTriggersRegex = new();

    public PrivMsgHandler(IrcHubClient hub)
    {
        _hub = hub;
        _hub.IncomingIrcEvents.OnNewIrcPrivMsg += OnNewIrcPrivMsg;
        _messageParameterHelper = new MessageParameterHelper(_hub);
    }

    private async void OnNewIrcPrivMsg(int botUserId, IrcPrivMsg ircPrivMsg)
    {
        if (ircPrivMsg.RoomId != 38949074) return;

        Console.WriteLine(ircPrivMsg.Message);
        await CheckHardCoded(botUserId, ircPrivMsg);
        UpdateBagsIfRequired();
        await CheckTriggers(botUserId, ircPrivMsg);
        await CheckTriggersRegex(botUserId, ircPrivMsg);
    }

    private const int CacheUpdateIntervalSeconds = 30;
    private DateTime _lastUpdate = DateTime.MinValue;

    private async Task CheckHardCoded(int botUserId, IrcPrivMsg ircPrivMsg)
    {
        if (!Program.ConfigRoot.SpecialUsers.BotOwnerUserIds.Contains(ircPrivMsg.UserId))
            return;

        if (ircPrivMsg.Message.StartsWith("<shutdown") ||
            ircPrivMsg.Message.StartsWith("<sh")
           )
        {
            await _hub.OutgoingIrcEvents.SendPrivMsg(
                new PrivMsgToTwitch(
                    botUserId,
                    ircPrivMsg.RoomName,
                    "Shutting down gracefully..."
                )
            );
            await Task.Delay(500);
            _hub.Dispose();
            await Task.Delay(500);
            Environment.Exit(0);
        }
        else if (ircPrivMsg.Message.StartsWith("<eval "))
        {
            string input = ircPrivMsg.Message[6..];
            if (!string.IsNullOrEmpty(input))
            {
                string responseMessage;
                try
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.CancelAfter(2000);

                    object response = await CSharpScript.EvaluateAsync<object>(
                        input,
                        ScriptOptions.Default,
                        ircPrivMsg, // TODO: create parent "globals" object
                        typeof(IrcPrivMsg),
                        cts.Token // TODO: This has no effect. This token has to be handled by the script itself
                    );

                    responseMessage = response.ToString() ?? "⚠Result was null⚠";
                    if (string.IsNullOrEmpty(responseMessage))
                        responseMessage = "⚠Result was empty string⚠";
                }
                catch (OperationCanceledException)
                {
                    responseMessage = "⚠Eval timed out⚠";
                }
                catch (Exception e)
                {
                    responseMessage = $"⚠{e.Message}⚠";
                }

                await _hub.OutgoingIrcEvents.SendPrivMsg(
                    new PrivMsgToTwitch(
                        botUserId,
                        ircPrivMsg.RoomName,
                        responseMessage
                    )
                );
            }
        }
    }

    private async void UpdateBagsIfRequired()
    {
        if ((DateTime.UtcNow - _lastUpdate).TotalSeconds < CacheUpdateIntervalSeconds)
            return;

        await using IcdbDbContext dbContext = new IcdbDbContext();

        List<Channel> channels = dbContext.Channels
            .Include(channel => channel.CommandGroupLinks)
            .ThenInclude(cgl => cgl.CommandGroup)
            .ThenInclude(cg => cg.Commands)
            .ToList();

        foreach (Channel channel in channels)
        {
            if (!_commandTriggers.ContainsKey(channel.RoomId))
                _commandTriggers[channel.RoomId] = new ConcurrentBag<(int id, string phrase)>();
            if (!_commandTriggersRegex.ContainsKey(channel.RoomId))
                _commandTriggersRegex[channel.RoomId] = new ConcurrentBag<(int id, Regex expression)>();

            _commandTriggers[channel.RoomId].Clear();
            _commandTriggersRegex[channel.RoomId].Clear();

            List<Command> commands = channel.CommandGroupLinks
                .Select(cgl => cgl.CommandGroup)
                .SelectMany(cg => cg.Commands)
                .Where(command => command.Enabled)
                .ToList();

            foreach (Command command in commands)
            {
                if (command.IsRegex)
                {
                    _commandTriggersRegex[channel.RoomId].Add((command.Id,
                        new Regex(
                            command.TriggerPhrase,
                            RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
                            TimeSpan.FromMilliseconds(100)
                        )));
                }
                else
                {
                    _commandTriggers[channel.RoomId].Add((command.Id, command.TriggerPhrase + " "));
                }
            }
        }

        _lastUpdate = DateTime.UtcNow;
    }

    private async Task CheckTriggers(int botUserId, IrcPrivMsg ircPrivMsg)
    {
        if (!_commandTriggers.ContainsKey(ircPrivMsg.RoomId)) return;

        string inputMessage = ircPrivMsg.Message + " ";

        List<int> matchedIds = _commandTriggers[ircPrivMsg.RoomId]
            .Where(trigger => inputMessage.StartsWith(trigger.phrase))
            .Select(trigger => trigger.id)
            .ToList();

        await ExecuteCommandResponse(botUserId, ircPrivMsg, matchedIds);
    }

    private async Task CheckTriggersRegex(int botUserId, IrcPrivMsg ircPrivMsg)
    {
        if (!_commandTriggersRegex.ContainsKey(ircPrivMsg.RoomId)) return;

        List<int> matchedIds = _commandTriggersRegex[ircPrivMsg.RoomId]
            .Where(trigger => trigger.expression.IsMatch(ircPrivMsg.Message))
            .Select(trigger => trigger.id)
            .ToList();

        await ExecuteCommandResponse(botUserId, ircPrivMsg, matchedIds);
    }

    private async Task ExecuteCommandResponse(int botUserId, IrcPrivMsg ircPrivMsg, ICollection<int> matchedIds)
    {
        if (matchedIds.Count == 0) return;

        await using IcdbDbContext dbContext = new IcdbDbContext();
        List<Command> commands = dbContext.Commands
            .Where(command => matchedIds.Contains(command.Id))
            .ToList();

        Command? command = commands.FirstOrDefault(command => CheckTriggerPermission(ircPrivMsg, command));
        if (command == null) return;

        if (!HasCooldownPassed(ircPrivMsg, command))
            return;

        string responseMessage = await _messageParameterHelper.HandleMessageParameters(ircPrivMsg, command);

        await _hub.OutgoingIrcEvents.SendPrivMsg(
            new PrivMsgToTwitch(
                botUserId,
                ircPrivMsg.RoomName,
                responseMessage,
                null,
                command.ShouldReply ? ircPrivMsg.Id : null
            )
        );

        command.TimesUsed++;
        await dbContext.SaveChangesAsync();
    }

    private static bool CheckTriggerPermission(IrcPrivMsg ircPrivMsg, Command command)
    {
        return
            // Bot owner
            command.TriggerBotOwner && Program.ConfigRoot.SpecialUsers.BotOwnerUserIds.Contains(ircPrivMsg.UserId) ||
            // Bot admin
            command.TriggerBotAdmin && Program.ConfigRoot.SpecialUsers.BotAdminUserIds.Contains(ircPrivMsg.UserId) ||
            // Broadcaster
            command.TriggerBroadcaster && ircPrivMsg.RoomId == ircPrivMsg.UserId ||
            // Mod
            command.TriggerMods && ircPrivMsg.Badges.ContainsKey("asdf") ||
            // Vips
            command.TriggerVips && ircPrivMsg.Badges.ContainsKey("asdf") ||
            //Normal user
            command.TriggerNormal;
    }

    private static readonly Dictionary<(int roomId, int commandId), DateTime> CommandLastUsage = new();

    private static bool HasCooldownPassed(IrcPrivMsg ircPrivMsg, Command command)
    {
        if (!Program.ConfigRoot.SpecialUsers.BotOwnerUserIds.Contains(ircPrivMsg.UserId) &&
            !Program.ConfigRoot.SpecialUsers.BotAdminUserIds.Contains(ircPrivMsg.UserId) &&
            CommandLastUsage.ContainsKey((roomId: ircPrivMsg.RoomId, commandId: command.Id))
           )
        {
            TimeSpan timeSinceLastUsage = DateTime.UtcNow - CommandLastUsage[(ircPrivMsg.RoomId, command.Id)];
            if (timeSinceLastUsage.TotalSeconds < command.CooldownSeconds)
                return false;
        }

        CommandLastUsage[(roomId: ircPrivMsg.RoomId, commandId: command.Id)] = DateTime.UtcNow;
        return true;
    }
}