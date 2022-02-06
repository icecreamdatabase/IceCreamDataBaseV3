using System.Collections.Concurrent;
using IceCreamDataBaseV3.Model;
using TwitchIrcHubClient;
using TwitchIrcHubClient.DataTypes.Parsed.FromTwitch;
using TwitchIrcHubClient.DataTypes.Parsed.ToTwitch;

namespace IceCreamDataBaseV3.Handler.UserNotice;

public class UserNoticeHandler
{
    private readonly IrcHubClient _hub;

    private ConcurrentDictionary<int, List<int>> _userNoticeResponses = new();

    public UserNoticeHandler(IrcHubClient hub)
    {
        _hub = hub;
        _hub.IncomingIrcEvents.OnNewIrcUserNotice += IncomingIrcEventsOnOnNewIrcUserNotice;
    }

    private async void IncomingIrcEventsOnOnNewIrcUserNotice(int botUserId, IrcUserNotice ircUserNotice)
    {
        UpdateBagsIfRequired();

        if (!_userNoticeResponses.ContainsKey(botUserId) ||
            !_userNoticeResponses[botUserId].Contains(ircUserNotice.RoomId)
           )
            return;

        await using IcdbDbContext dbContext = new IcdbDbContext();

        string? response = dbContext.UserNoticeResponses
            .FirstOrDefault(unr => unr.BotUserId == botUserId &&
                                   unr.RoomId == ircUserNotice.RoomId &&
                                   unr.MessageId == ircUserNotice.MessageId)
            ?.Response;

        if (string.IsNullOrEmpty(response))
            return;

        string responseMessage = UserNoticeParameterHelper.HandleUserNoticeParameters(ircUserNotice, response);
        
        Console.WriteLine($"{botUserId} <-- #{ircUserNotice.RoomName}: {ircUserNotice.MessageId.ToString()}");
        Console.WriteLine($"{botUserId} --> #{ircUserNotice.RoomName}: {response}");

        await _hub.OutgoingIrcEvents.SendPrivMsg(
            new PrivMsgToTwitch(
                botUserId,
                ircUserNotice.RoomName,
                responseMessage
            )
        );
    }

    private const int CacheUpdateIntervalSeconds = 30;
    private DateTime _lastUpdate = DateTime.MinValue;

    private async void UpdateBagsIfRequired()
    {
        if ((DateTime.UtcNow - _lastUpdate).TotalSeconds < CacheUpdateIntervalSeconds)
            return;

        await using IcdbDbContext dbContext = new IcdbDbContext();

        Dictionary<int, List<int>> responses =
            dbContext.UserNoticeResponses
                .AsEnumerable()
                .GroupBy(unr => unr.BotUserId, unr => unr.RoomId)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());

        _userNoticeResponses = new ConcurrentDictionary<int, List<int>>(responses);

        _lastUpdate = DateTime.UtcNow;
    }
}
