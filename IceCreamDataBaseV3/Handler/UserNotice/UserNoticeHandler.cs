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

    private async void IncomingIrcEventsOnOnNewIrcUserNotice(int botuserid, IrcUserNotice ircUserNotice)
    {
        Console.WriteLine("-----------------------" + ircUserNotice.MessageId);
        UpdateBagsIfRequired();

        if (!_userNoticeResponses.ContainsKey(botuserid) ||
            !_userNoticeResponses[botuserid].Contains(ircUserNotice.RoomId)
           )
            return;

        await using IcdbDbContext dbContext = new IcdbDbContext();

        string? response = dbContext.UserNoticeResponses
            .FirstOrDefault(unr => unr.BotUserId == botuserid &&
                                   unr.RoomId == ircUserNotice.RoomId &&
                                   unr.MessageId == ircUserNotice.MessageId)
            ?.Response;

        if (string.IsNullOrEmpty(response))
            return;

        await _hub.OutgoingIrcEvents.SendPrivMsg(
            new PrivMsgToTwitch(
                botuserid,
                ircUserNotice.RoomName,
                UserNoticeParameterHelper.HandleUserNoticeParameters(ircUserNotice, response)
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
