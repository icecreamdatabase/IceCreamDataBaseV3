using System.Text.RegularExpressions;
using TwitchIrcHubClient.DataTypes.Parsed.FromTwitch;

namespace IceCreamDataBaseV3.Handler.UserNotice;

public static class UserNoticeParameterHelper
{
    private static readonly Regex RegexUser = new(
        "\\${user}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly Regex RegexChannel = new(
        "\\${channel}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly Regex RegexMonths = new(
        "\\${months}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly Regex RegexMassGiftCount = new(
        "\\${massGiftCount}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly Regex RegexSecondUser = new(
        "\\${secondUser}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    internal static string HandleUserNoticeParameters(IrcUserNotice ircUserNotice, string response)
    {
        if (RegexUser.IsMatch(response))
            if (!string.IsNullOrEmpty(ircUserNotice.DisplayName))
                response = RegexUser.Replace(response, ircUserNotice.DisplayName);
            else if (!string.IsNullOrEmpty(ircUserNotice.Login))
                response = RegexUser.Replace(response, ircUserNotice.Login);
        if (RegexChannel.IsMatch(response))
            response = RegexChannel.Replace(response, string.Join("\U000E0000", ircUserNotice.RoomName.Split()));
        if (RegexMonths.IsMatch(response) && !string.IsNullOrEmpty(ircUserNotice.MsgParamCumulativeMonths))
            response = RegexMonths.Replace(response, ircUserNotice.MsgParamCumulativeMonths);
        if (RegexMassGiftCount.IsMatch(response) && !string.IsNullOrEmpty(ircUserNotice.MsgParamMassGiftCount))
            response = RegexMassGiftCount.Replace(response, ircUserNotice.MsgParamMassGiftCount);
        if (RegexSecondUser.IsMatch(response))
            if (!string.IsNullOrEmpty(ircUserNotice.MsgParamRecipientDisplayName))
                response = RegexSecondUser.Replace(response, ircUserNotice.MsgParamRecipientDisplayName);
            else if (!string.IsNullOrEmpty(ircUserNotice.MsgParamRecipientUserName))
                response = RegexSecondUser.Replace(response, ircUserNotice.MsgParamRecipientUserName);
            else if (!string.IsNullOrEmpty(ircUserNotice.MsgParamSenderName))
                response = RegexSecondUser.Replace(response, ircUserNotice.MsgParamSenderName);
            else if (!string.IsNullOrEmpty(ircUserNotice.MsgParamSenderLogin))
                response = RegexSecondUser.Replace(response, ircUserNotice.MsgParamSenderLogin);

        return response;
    }
}
