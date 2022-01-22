using IceCreamDataBaseV3.Model;
using TwitchIrcHubClient;
using TwitchIrcHubClient.DataTypes.Parsed.FromTwitch;
using TwitchIrcHubClient.DataTypes.Parsed.ToTwitch;

namespace IceCreamDataBaseV3;

public class Bot
{
    private readonly IrcHubClient _hub;

    public Bot()
    {
        if (string.IsNullOrEmpty(Program.ConfigRoot.TwitchIrcHub.AppIdKey))
            throw new InvalidOperationException("No AppIdKey!");

        _hub = new IrcHubClient(Program.ConfigRoot.TwitchIrcHub.AppIdKey);
        _hub.IncomingIrcEvents.OnNewIrcPrivMsg += OnNewIrcPrivMsg;
        _hub.IncomingIrcEvents.OnConnId += OnConnId;
        var a = IcdbDbContext.Instance;
    }

    private void OnConnId(string connid)
    {
        _hub.Api.Connections.SetChannels(122425204, new List<int> { 38949074 });
    }

    private async void OnNewIrcPrivMsg(IrcPrivMsg ircPrivMsg)
    {
        Console.WriteLine(ircPrivMsg.Message);
        if (ircPrivMsg.RoomId == 38949074)
            if (ircPrivMsg.Message.StartsWith("<"))
            {
                await _hub.OutgoingIrcEvents.SendPrivMsg(new PrivMsgToTwitch(122425204, "icdb", ">"));
            }
            else if (ircPrivMsg.Message.StartsWith(">"))
            {
                Dictionary<string, string> dict = await _hub.Api.TwitchUsers.IdToLogin(new[] { 38949074 });
                await _hub.OutgoingIrcEvents.SendPrivMsg(new PrivMsgToTwitch(122425204, "icdb", "xd"));
            }
    }
}
