using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using IceCreamDataBaseV3.Model;
using IceCreamDataBaseV3.Model.Schema;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using TwitchIrcHubClient;
using TwitchIrcHubClient.DataTypes.Parsed.FromTwitch;
using TwitchIrcHubClient.DataTypes.Parsed.ToTwitch;

namespace IceCreamDataBaseV3.Handler.PrivMsg;

public class PrivMsgHandler
{
    private readonly IrcHubClient _hub;
    private readonly PrivMsgParameterHelper _privMsgParameterHelper;

    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, ConcurrentBag<(int id, string phrase)>>>
        _commandTriggers = new();

    private readonly ConcurrentDictionary<int, ConcurrentDictionary<int, ConcurrentBag<(int id, Regex expression)>>>
        _commandTriggersRegex = new();

    public PrivMsgHandler(IrcHubClient hub)
    {
        _hub = hub;
        _hub.IncomingIrcEvents.OnNewIrcPrivMsg += OnNewIrcPrivMsg;
        _privMsgParameterHelper = new PrivMsgParameterHelper(_hub);
    }

    private async void OnNewIrcPrivMsg(int botUserId, IrcPrivMsg ircPrivMsg)
    {
        //Console.WriteLine($"{botUserId} <-- #{ircPrivMsg.RoomName} {ircPrivMsg.UserName}: {ircPrivMsg.Message}");

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
            .Distinct()
            .ToList();

        foreach (Channel channel in channels)
        {
            if (!_commandTriggers.ContainsKey(channel.BotUserId))
                _commandTriggers[channel.BotUserId] =
                    new ConcurrentDictionary<int, ConcurrentBag<(int id, string phrase)>>();

            if (!_commandTriggers[channel.BotUserId].ContainsKey(channel.RoomId))
                _commandTriggers[channel.BotUserId][channel.RoomId] =
                    new ConcurrentBag<(int id, string phrase)>();

            if (!_commandTriggersRegex.ContainsKey(channel.BotUserId))
                _commandTriggersRegex[channel.BotUserId] =
                    new ConcurrentDictionary<int, ConcurrentBag<(int id, Regex expression)>>();

            if (!_commandTriggersRegex[channel.BotUserId].ContainsKey(channel.RoomId))
                _commandTriggersRegex[channel.BotUserId][channel.RoomId] =
                    new ConcurrentBag<(int id, Regex expression)>();

            _commandTriggers[channel.BotUserId][channel.RoomId].Clear();
            _commandTriggersRegex[channel.BotUserId][channel.RoomId].Clear();

            List<Command> commands = channel.CommandGroupLinks
                .Select(cgl => cgl.CommandGroup)
                .SelectMany(cg => cg.Commands)
                .Where(command => command.Enabled)
                .ToList();

            foreach (Command command in commands)
            {
                if (command.IsRegex)
                {
                    _commandTriggersRegex[channel.BotUserId][channel.RoomId].Add((command.Id,
                        new Regex(
                            command.TriggerPhrase,
                            RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
                            TimeSpan.FromMilliseconds(100)
                        )));
                }
                else
                {
                    _commandTriggers[channel.BotUserId][channel.RoomId].Add((command.Id, command.TriggerPhrase + " "));
                }
            }
        }

        _lastUpdate = DateTime.UtcNow;
    }

    private async Task CheckTriggers(int botUserId, IrcPrivMsg ircPrivMsg)
    {
        if (!_commandTriggers.ContainsKey(botUserId) ||
            !_commandTriggers[botUserId].ContainsKey(ircPrivMsg.RoomId)
           )
            return;

        string inputMessage = ircPrivMsg.Message + " ";

        List<int> matchedIds = _commandTriggers[botUserId][ircPrivMsg.RoomId]
            .Where(trigger => inputMessage.ToLowerInvariant().StartsWith(trigger.phrase.ToLowerInvariant()))
            .Select(trigger => trigger.id)
            .ToList();

        await ExecuteCommandResponse(botUserId, ircPrivMsg, matchedIds);
    }

    private async Task CheckTriggersRegex(int botUserId, IrcPrivMsg ircPrivMsg)
    {
        if (!_commandTriggersRegex.ContainsKey(botUserId) ||
            !_commandTriggersRegex[botUserId].ContainsKey(ircPrivMsg.RoomId)
           )
            return;

        List<int> matchedIds = _commandTriggersRegex[botUserId][ircPrivMsg.RoomId]
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

        string responseMessage = await _privMsgParameterHelper.HandlePrivMsgParameters(ircPrivMsg, command);

        Console.WriteLine($"{botUserId} <-- #{ircPrivMsg.RoomName} {ircPrivMsg.UserName}: {ircPrivMsg.Message}");
        Console.WriteLine($"{botUserId} --> #{ircPrivMsg.RoomName}: {responseMessage}");

        string[] splitMessage = responseMessage.Split("{nl}");

        foreach (string message in splitMessage)
        {
            await _hub.OutgoingIrcEvents.SendPrivMsg(
                new PrivMsgToTwitch(
                    botUserId,
                    ircPrivMsg.RoomName,
                    message,
                    null,
                    command.ShouldReply ? ircPrivMsg.Id : null,
                    splitMessage.Length > 1
                )
            );
        }


        command.TimesUsed++;
        await dbContext.SaveChangesAsync();
    }

    private static bool CheckTriggerPermission(IrcPrivMsg ircPrivMsg, Command command)
    {
        bool isBotOwner = Program.ConfigRoot.SpecialUsers.BotOwnerUserIds.Contains(ircPrivMsg.UserId);
        bool isBotAdmin = Program.ConfigRoot.SpecialUsers.BotAdminUserIds.Contains(ircPrivMsg.UserId);
        bool isBroadCaster = ircPrivMsg.RoomId == ircPrivMsg.UserId;
        bool isMod = ircPrivMsg.Badges.ContainsKey("moderator");
        bool isVip = ircPrivMsg.Badges.ContainsKey("vip");
        bool isNormal = /* !isBotOwner && !isBotAdmin &&*/ !isBroadCaster && !isMod && !isVip;

        return command.TriggerBotOwner && isBotOwner ||
               command.TriggerBotAdmin && isBotAdmin ||
               command.TriggerBroadcaster && isBroadCaster ||
               command.TriggerMods && isMod ||
               command.TriggerVips && isVip ||
               command.TriggerNormal && isNormal;
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
