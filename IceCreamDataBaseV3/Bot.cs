using System.Diagnostics;
using IceCreamDataBaseV3.Handler.PrivMsg;
using IceCreamDataBaseV3.Model;
using TwitchIrcHubClient;

namespace IceCreamDataBaseV3;

public class Bot
{
    private readonly IrcHubClient _hub;
    private readonly PrivMsgHandler _privMsgHandler;

    public Bot()
    {
        if (string.IsNullOrEmpty(Program.ConfigRoot.TwitchIrcHub.AppIdKey))
            throw new InvalidOperationException("No AppIdKey!");
        if (string.IsNullOrEmpty(Program.ConfigRoot.TwitchIrcHub.HubRootUri))
            throw new InvalidOperationException("No HubRootUri!");

        _hub = new IrcHubClient(Program.ConfigRoot.TwitchIrcHub.AppIdKey, Program.ConfigRoot.TwitchIrcHub.HubRootUri);
        _hub.IncomingIrcEvents.OnConnId += OnConnId;

        _privMsgHandler = new PrivMsgHandler(_hub);
    }

    private void OnConnId(string connId)
    {
        Console.WriteLine($"Received connId: {connId}");
        Stopwatch sw = Stopwatch.StartNew();
        using IcdbDbContext dbContext = new IcdbDbContext();

        sw.Stop();
        Console.WriteLine($"Context creation: {sw.Elapsed.TotalMilliseconds} ms");
        sw = Stopwatch.StartNew();
        dbContext.Channels
            .Where(channel => channel.Enabled)
            .AsEnumerable()
            .GroupBy(channel => channel.BotUserId, channel => channel.RoomId)
            .ToList()
            .ForEach(grouping => _hub.Api.Connections.SetChannels(grouping.Key, grouping.ToList()));
        sw.Stop();
        Console.WriteLine($"Query execution and SetChannels: {sw.Elapsed.TotalMilliseconds} ms");
    }
}
