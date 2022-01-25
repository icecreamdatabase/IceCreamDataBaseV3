﻿using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using IceCreamDataBaseV3.Model;
using IceCreamDataBaseV3.Model.Schema;
using Microsoft.EntityFrameworkCore;
using TwitchIrcHubClient;
using TwitchIrcHubClient.DataTypes.Parsed.FromTwitch;
using TwitchIrcHubClient.DataTypes.Parsed.ToTwitch;

namespace IceCreamDataBaseV3.Handler.PrivMsg;

public class PrivMsgHandler
{
    private readonly IrcHubClient _hub;

    private readonly Dictionary<int, ConcurrentBag<(int id, string phrase)>> _commandTriggers = new();
    private readonly Dictionary<int, ConcurrentBag<(int id, Regex expression)>> _commandTriggersRegex = new();

    public PrivMsgHandler(IrcHubClient hub)
    {
        _hub = hub;
        _hub.IncomingIrcEvents.OnNewIrcPrivMsg += OnNewIrcPrivMsg;
    }

    private async void OnNewIrcPrivMsg(int botUserId, IrcPrivMsg ircPrivMsg)
    {
        if (ircPrivMsg.RoomId != 38949074) return;

        Console.WriteLine(ircPrivMsg.Message);
        UpdateBagsIfRequired();
        await CheckTriggers(botUserId, ircPrivMsg);
        await CheckTriggersRegex(botUserId, ircPrivMsg);

        // Store command triggers in cache
        // Next message every X minutes triggers a refresh in the background 
    }

    private const int CacheUpdateIntervalSeconds = 30;
    private DateTime _lastUpdate = DateTime.MinValue;

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

        await _hub.OutgoingIrcEvents.SendPrivMsg(
            new PrivMsgToTwitch(
                botUserId,
                ircPrivMsg.RoomName,
                command.Response,
                null,
                command.ShouldReply ? ircPrivMsg.Id : null
            )
        );
    }

    private static bool CheckTriggerPermission(IrcPrivMsg ircPrivMsg, Command? command)
    {
        return true;
    }
}
