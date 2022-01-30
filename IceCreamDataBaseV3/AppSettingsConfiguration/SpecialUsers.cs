using System.Diagnostics.CodeAnalysis;

namespace IceCreamDataBaseV3.AppSettingsConfiguration;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class SpecialUsers
{
    public int[] BotOwnerUserIds { get; init; }= Array.Empty<int>();
    public int[] BotAdminUserIds { get; init; }= Array.Empty<int>();
}
