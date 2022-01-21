using IceCreamDataBaseV3.Model;
using TwitchIrcHubClient;
using TwitchIrcHubClient.DataTypes.Parsed.FromTwitch;
using TwitchIrcHubClient.DataTypes.Parsed.ToTwitch;
using TwitchIrcHubClient.InternalApi.Connections;
using TwitchIrcHubClient.InternalApi.TwitchUsers;

namespace IceCreamDataBaseV3;

public class Bot
{
    private readonly IrcHubClient _ircHubClient;

    public Bot()
    {
        if (string.IsNullOrEmpty(Program.ConfigRoot.TwitchIrcHub.AppIdKey))
            throw new InvalidOperationException("No AppIdKey!");
        GlobalDataSetup.AppIdKey = Program.ConfigRoot.TwitchIrcHub.AppIdKey;

        _ircHubClient = new IrcHubClient(Program.ConfigRoot.TwitchIrcHub.AppIdKey);
        _ircHubClient.IncomingIrcEvents.OnNewIrcPrivMsg += OnNewIrcPrivMsg;
        _ircHubClient.IncomingIrcEvents.OnConnId += OnConnId;
        var a = IcdbDbContext.Instance;
    }

    private void OnConnId(string connid)
    {
        Connections.SetChannels(122425204, new List<int> { 38949074 });
    }

    private async void OnNewIrcPrivMsg(IrcPrivMsg ircPrivMsg)
    {
        Console.WriteLine(ircPrivMsg.Message);
        if (ircPrivMsg.RoomId == 38949074)
            if (ircPrivMsg.Message.StartsWith("<"))
            {
                await _ircHubClient.OutgoingIrcEvents.SendPrivMsg(new PrivMsgToTwitch(122425204, "icdb", ">"));
            }
            else if (ircPrivMsg.Message.StartsWith(">"))
            {
                Dictionary<string, string> dict = await TwitchUsers.IdToLogin(new[] { 38949074 });
                await _ircHubClient.OutgoingIrcEvents.SendPrivMsg(new PrivMsgToTwitch(122425204, "icdb", "xd"));
            }
    }
}
