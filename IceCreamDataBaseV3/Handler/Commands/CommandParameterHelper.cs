using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using IceCreamDataBaseV3.Model.Schema;
using TwitchIrcHubClient;
using TwitchIrcHubClient.DataTypes.Parsed.FromTwitch;
using TwitchIrcHubClient.TwitchIrcHubApi.TwitchUsers;

namespace IceCreamDataBaseV3.Handler.Commands;

public class CommandParameterHelper
{
    // @formatter:off
    private static readonly Dictionary<string, string> IceCreamFacts = new()
    {
        { "Magnum Mini Almond Ice Cream Bar", "Most tasters could agree that, while the ice cream was \"creamy,\" they enjoyed the chocolate coating more. \"The crunchy milk chocolate\" has a \"hint of nuts\" which gave it good texture, according to our volunteers. Overall, the 160-calorie bar tastes as delicious as it looks." },
        { "Good Humor Mounds Ice Cream Bar", "This coconut ice cream bar covered in chocolate tasted \"unique\" and had a \"fluffy yet creamy texture.\" Many tasters also enjoyed the chocolate coating and its \"wonderful taste.\" It's no surprise this dessert was a hit: It has 190 calories, the most of all the treats in our taste test." },
        { "Blue Bunny Sweet Freedom No Sugar Added Raspberry and Vanilla Swirl Bar", "Our volunteers had mixed opinions about the chocolate and berry flavors in these Blue Bunny bars. While some noted the \"smooth and creamy texture,\" other tasters only liked the \"crunchy chocolate shell\" of this 70-calorie bar. Either way, all could agree this treat looked absolutely scrumptious." },
        { "Skinny Cow Oh Fudge Nuts Ice Cream Cone", "The nutty flavor and \"light sprinkling of peanuts\" on this ice cream come were enjoyable to tasters. On the other hand, most agreed the \"weird texture\" hinted to its light and low-cal nature. If indulging in the 150-calorie treat, eat quickly; our tasters found the cone \"breaks easily\" on the bottom." },
        { "Stonyfield Farm Nonfat After Dark Chocolate Frozen Yogurt Novelty Bar", "The combination of textures in this novelty bar makes it something special. Our volunteers thought the chocolate had a \"good crackle\" and \"didn't seem like it would shatter and fall off,\" while the ice cream was \"smooth.\" Unfortunately, some tasters could tell it was low-cal and \"not in a good way,\" with ice cream they described as \"thin\" and \"watery.\"" },
        { "Blue Bunny Sweet Freedom No Sugar Added Vanilla Ice Cream Cone", "After you've enjoyed its chocolate-and-peanut-toppings and the ice cream within, Blue Bunny's cones offer an extra little treat: a \"hefty amount\" of chocolate lining the inside of the cone. Many volunteers complained that the vanilla was \"more icy than creamy,\" and \"not tasty.\" At the least, you get a \"huge portion\" with only 160 calories." },
        { "Edy's/Dryer's Mango Fruit Bar Review", "A good summer treat, Edy's/Dryer's Mango Fruit Bars are \"refreshing and light.\" A \"simple, straightforward\" fruit bar, our volunteers thought they seemed \"healthy\" and \"fresh and summery.\" Unfortunately, some tasters found that sugariness cloying, while others complained that the bars did \"not offer enough flavor.\"" },
        { "Magnum Mini Classic Ice Cream Bar", "A good ratio of vanilla to chocolate makes this \"perfect sized\" ice cream bar delicious. Yet for some tasters, their evaluation might have been swayed by a price. Some complained that the vanilla ice cream was \"bland,\" but many still felt it was a \"cute modest size.\"" },
        { "Good Humor York Peppermint Pattie Ice Cream Bar", "Featuring the \"taste [of] a big peppermint patty,\" Good Humor's York Peppermint Pattie Ice Cream Bar had a \"strong\" flavor, according to our volunteers. Most approved of the \"delicious chocolate shell and light texture,\" but some felt the frozen treat was too messy because it had \"no stick or anything to hold it.\"" },
        { "Ciao Bella Blueberry Passion Sorbet Bar", "Often referred to as \"refreshing\" by tasters, this fruit bar seemed to be a hit among most. Featuring a real fruit flavor that packed a punch, volunteers thought it \"tasted like a fruit smoothie.\" The downsides: Some tasters described the color as \"not appetizing\" and the bars melted quickly." },
        { "Weight Watchers Divine Triple Chocolate Dessert Bar", "Our tasters loved the \"great flavor\" of this triple chocolate treat. With its chocolate bar filled with chocolate swirls and then coated in a chocolate shell, one volunteer declared this 110-calorie treat \"any chocolate lover's dream!\" Make sure you have napkins handy though; we found that it melts quickly and \"falls apart.\"" },
        { "Cadbury Vanilla Chocolate Ice Cream Bar", "Most tasters agreed that the Cadbury's chocolate coating passed the taste test but the ice cream did not. With a \"Cadbury Egg-tasting exterior\", the chocolate outside was \"rich and delicious\" and \"not fake like other products.\" The ice cream, though, had \"no distinctive flavor\" and left a \"funny aftertaste.\"" },
        { "Champ Snack Size Ice Cream Cones in Vanilla", "The chocolate and nuts are the real stars of these snack-size cones. Our volunteers loved the combination of textures and the \"crunchy\" cone. Unfortunately, many complained that the ice cream within was \"bland\" and \"extremely icy.\"" },
        { "H\u00E4agen-Dazs Snack Sized Vanilla Chocolate Cones", "Our tasters thought the nuts and chocolate of this cone were the \"perfect combination.\" Many loved the taste and \"good portion size,\" too. Volunteers were mixed on the overall sweetness however, with some loving the flavor and others calling it \"overpowering.\"" },
        { "Blue Bunny Premium All Natural Vanilla Ice Cream", "The \"thick and creamy\" texture of Blue Bunny All Natural Vanilla Ice Cream was well received by our volunteers. \"Sweet and milky,\" the \"good French vanilla flavor\" of this ice cream caused one taster to joke, \"Bring on the cone!\"" }
    };
    // @formatter:on

    private static readonly Regex RegexTargetOrUser = new(
        "\\${targetOrUser}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly Regex RegexUser = new(
        "\\${user}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly Regex RegexUserNoPing = new(
        "\\${userNoPing}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly Regex RegexChannel = new(
        "\\${channel}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly Regex RegexTimesUsed = new(
        "\\${timesUsed}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly Regex RegexUptime = new(
        "\\${uptime}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly Regex RegexIceCream = new(
        "\\${icecream}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly Regex RegexGdq = new(
        "\\${gdq}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly Regex RegexRandom = new(
        "\\$random{([^\\}]*)}",
        RegexOptions.Compiled & RegexOptions.CultureInvariant & RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(50)
    );

    private static readonly Random Rnd = new();


    private readonly IrcHubClient _hub;

    public CommandParameterHelper(IrcHubClient hub)
    {
        _hub = hub;
    }

    internal async Task<string> HandlePrivMsgParameters(IrcPrivMsg ircPrivMsg, Command command)
    {
        string response = command.Response;

        if (RegexTargetOrUser.IsMatch(response))
            response = RegexTargetOrUser.Replace(response, await GetTargetOrUser(ircPrivMsg));
        if (RegexUser.IsMatch(response))
            response = RegexUser.Replace(response, ircPrivMsg.UserName);
        if (RegexUserNoPing.IsMatch(response))
            response = RegexUserNoPing.Replace(response, string.Join("\U000E0000", ircPrivMsg.UserName.Split()));
        if (RegexChannel.IsMatch(response))
            response = RegexChannel.Replace(response, string.Join("\U000E0000", ircPrivMsg.RoomName.Split()));
        if (RegexTimesUsed.IsMatch(response))
            response = RegexTimesUsed.Replace(response, command.TimesUsed.ToString());
        if (RegexUptime.IsMatch(response))
            response = RegexUptime.Replace(response, GetHumanReadableProcessUptime());
        if (RegexIceCream.IsMatch(response))
        {
            (string? key, string? value) = IceCreamFacts.ElementAt(Rnd.Next(0, IceCreamFacts.Count));
            string iceCreamFact = $"{key} 🍨: {value}";
            response = RegexIceCream.Replace(response, iceCreamFact);
        }
        //if (RegexGdq.IsMatch(response))
        //    response = RegexGdq.Replace(response, );
        if (RegexRandom.IsMatch(response))
        {
            string[] options = RegexRandom.Match(response).Groups[1].Value.Split('|');
            string selection = options[Rnd.Next(0, options.Length)].Trim();
            response = RegexRandom.Replace(response, selection);
        }

        return response;
    }

    private static readonly List<string> KnownLogins = new();

    private async Task<string> GetTargetOrUser(IrcPrivMsg ircPrivMsg)
    {
        string[] messageSplit = ircPrivMsg.Message.Split(" ");
        if (messageSplit.Length < 2)
            return ircPrivMsg.UserName;

        string targetUserName = messageSplit[1];

        if (KnownLogins.Contains(targetUserName.ToLowerInvariant()))
            return targetUserName;

        //TODO: check is user in channel instead
        List<TwitchUsersResult> results = await _hub.Api.TwitchUsers.Users(null, new[] { targetUserName });
        if (results.Count == 0)
            return ircPrivMsg.UserName;

        KnownLogins.Add(results[0].Login.ToLowerInvariant());
        return targetUserName;
    }

    private static string GetHumanReadableProcessUptime()
    {
        TimeSpan uptime = DateTime.Now - Process.GetCurrentProcess().StartTime;
        StringBuilder sb = new StringBuilder();

        if (uptime.TotalDays >= 1d)
        {
            sb.Append(' ');
            sb.Append(uptime.Days);
            sb.Append('d');
        }

        if (uptime.TotalHours >= 1d)
        {
            sb.Append(' ');
            sb.Append(uptime.Hours);
            sb.Append('h');
        }

        if (uptime.TotalMinutes >= 1d)
        {
            sb.Append(' ');
            sb.Append(uptime.Minutes);
            sb.Append('m');
        }

        sb.Append(' ');
        sb.Append(uptime.Seconds);
        sb.Append('s');

        return sb.ToString().Trim();
    }
}
