using System.Diagnostics.CodeAnalysis;

namespace IceCreamDataBaseV3.AppSettingsConfiguration;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class TwitchIrcHub
{
    public string? AppIdKey { get; init; }
    public string? HubRootUri { get; init; }
}
