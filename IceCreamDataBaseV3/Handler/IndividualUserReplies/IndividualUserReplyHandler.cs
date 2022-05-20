using System.Collections.Concurrent;
using IceCreamDataBaseV3.Model;
using IceCreamDataBaseV3.Model.Schema;
using Microsoft.EntityFrameworkCore;
using TwitchIrcHubClient;
using TwitchIrcHubClient.DataTypes.Parsed.FromTwitch;
using TwitchIrcHubClient.DataTypes.Parsed.ToTwitch;

namespace IceCreamDataBaseV3.Handler.IndividualUserReplies;

public class IndividualUserReplyHandler
{
    private readonly IrcHubClient _hub;

    private readonly ConcurrentDictionary<
        (int botUserId, int roomId, int triggerUserId),
        (string triggerPhrase, string triggerResponse)
    > _triggers = new();

    public IndividualUserReplyHandler(IrcHubClient hub)
    {
        _hub = hub;
        _hub.IncomingIrcEvents.OnNewIrcPrivMsg += OnNewIrcPrivMsg;
    }

    private async void OnNewIrcPrivMsg(int botUserId, IrcPrivMsg ircPrivMsg)
    {
        //Console.WriteLine($"{botUserId} <-- #{ircPrivMsg.RoomName} {ircPrivMsg.UserName}: {ircPrivMsg.Message}");

        UpdateBagsIfRequired();
        await CheckTriggers(botUserId, ircPrivMsg);
    }

    private const int CacheUpdateIntervalSeconds = 60;
    private DateTime _lastUpdate = DateTime.MinValue;

    private async void UpdateBagsIfRequired()
    {
        if ((DateTime.UtcNow - _lastUpdate).TotalSeconds < CacheUpdateIntervalSeconds)
            return;

        await using IcdbDbContext dbContext = new IcdbDbContext();

        List<Channel> channels = dbContext.Channels
            .Include(channel => channel.IndividualUserReplies)
            .ToList();

        _triggers.Clear();
        foreach (Channel channel in channels)
        {
            List<IndividualUserReply> replies = channel.IndividualUserReplies
                .Where(command => command.Enabled)
                .ToList();

            foreach (IndividualUserReply reply in replies)
            {
                _triggers[(channel.BotUserId, channel.RoomId, reply.TriggerUserId)] =
                    (triggerPhrase: reply.TriggerPhrase.Trim(), triggerResponse: reply.Response.Trim());
                //_triggers[channel.BotUserId][channel.RoomId][reply.TriggerUserId] = (reply.TriggerPhrase, reply.Response);
            }
        }

        _lastUpdate = DateTime.UtcNow;
    }

    private async Task CheckTriggers(int botUserId, IrcPrivMsg ircPrivMsg)
    {
        // Check
        if (!_triggers.ContainsKey((botUserId, ircPrivMsg.RoomId, ircPrivMsg.UserId)))
            return;

        (string triggerPhrase, string triggerResponse) trigger =
            _triggers[(botUserId, ircPrivMsg.RoomId, ircPrivMsg.UserId)];
        if (!ircPrivMsg.Message.Contains(trigger.triggerPhrase))
            return;

        if (!HasCooldownPassed(botUserId, ircPrivMsg, trigger.triggerPhrase))
        {
            return;
        }
        
        // Output
        Console.WriteLine($"{botUserId} <-- #{ircPrivMsg.RoomName} {ircPrivMsg.UserName}: {ircPrivMsg.Message}");
        Console.WriteLine($"{botUserId} --> #{ircPrivMsg.RoomName}: {trigger.triggerResponse}");

        string[] splitMessage = trigger.triggerResponse.Split("{nl}");

        foreach (string message in splitMessage)
        {
            await _hub.OutgoingIrcEvents.SendPrivMsg(
                new PrivMsgToTwitch(
                    botUserId,
                    ircPrivMsg.RoomName,
                    message,
                    null,
                    null,
                    splitMessage.Length > 1
                )
            );
        }
    }

    private static readonly Dictionary<(int botUserId, int roomId, int userId, string triggerPhrase), DateTime>
        CommandLastUsage = new();

    private const double CooldownSeconds = 5;

    private bool HasCooldownPassed(int botUserId, IrcPrivMsg ircPrivMsg, string triggerPhrase)
    {
        if (!Program.ConfigRoot.SpecialUsers.BotOwnerUserIds.Contains(ircPrivMsg.UserId) &&
            !Program.ConfigRoot.SpecialUsers.BotAdminUserIds.Contains(ircPrivMsg.UserId) &&
            CommandLastUsage.ContainsKey((botUserId, ircPrivMsg.RoomId, ircPrivMsg.UserId, triggerPhrase))
           )
        {
            TimeSpan timeSinceLastUsage =
                DateTime.UtcNow - CommandLastUsage[(botUserId, ircPrivMsg.RoomId, ircPrivMsg.UserId, triggerPhrase)];
            if (timeSinceLastUsage.TotalSeconds < CooldownSeconds)
                return false;
        }

        CommandLastUsage[(botUserId, ircPrivMsg.RoomId, ircPrivMsg.UserId, triggerPhrase)] = DateTime.UtcNow;
        return true;
    }
}
