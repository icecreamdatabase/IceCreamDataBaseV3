using System.Diagnostics.CodeAnalysis;

namespace IceCreamDataBaseV3.AppSettingsConfiguration;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class ConnectionStrings
{
    private readonly string? _icdbV3Db;

    public string? IcdbV3Db
    {
        //Try env var first else use appsettings.json
        get => Environment.GetEnvironmentVariable(@"ICDBV3_CONNECTIONSTRINGS_DB") ?? _icdbV3Db;
        init => _icdbV3Db = value;
    }
}
