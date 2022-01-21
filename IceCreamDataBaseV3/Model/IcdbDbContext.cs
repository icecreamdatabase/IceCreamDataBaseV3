using IceCreamDataBaseV3.Model.Schema;
using Microsoft.EntityFrameworkCore;

namespace IceCreamDataBaseV3.Model;

public sealed class IcdbDbContext : DbContext
{
    /// <summary>
    /// <c>TreatTinyAsBoolean=false</c> results in using bit(1) instead of tinyint(1) for <see cref="bool"/>.<br/>
    /// <br/>
    /// In 5.0.5 SSL was enabled by default. It isn't necessary for our usage.
    /// (We don't expose the DB to the internet.)
    /// https://stackoverflow.com/a/45108611
    /// </summary>
    private const string AdditionalMySqlConfigurationParameters = ";TreatTinyAsBoolean=false;SslMode=none";

    private readonly string _fullConString;

    private static IcdbDbContext? _instance;

    public static IcdbDbContext Instance => _instance ??= new IcdbDbContext();

    public DbSet<Channel> Channels { get; set; } = null!;
    public DbSet<Command> Commands { get; set; } = null!;
    public DbSet<CommandGroupLink> CommandGroupLinks { get; set; } = null!;
    public DbSet<CommandGroup> CommandGroups { get; set; } = null!;

    private IcdbDbContext()
    {
        if (_instance != null)
            throw new InvalidOperationException($"Only one instance of {nameof(IcdbDbContext)} can be created.");

        //Try env var first else use appsettings.json
        //string? dbConString = Environment.GetEnvironmentVariable(@"ICDBV3_CONNECTIONSTRINGS_DB");
        //if (string.IsNullOrEmpty(dbConString))
        string? dbConString = Program.ConfigRoot.ConnectionStrings.IcdbV3Db;
        if (string.IsNullOrEmpty(dbConString))
            throw new InvalidOperationException("No MySql connection string!");
        _fullConString = dbConString + AdditionalMySqlConfigurationParameters;

        _instance = this;

        Database.EnsureCreated();
        string createScript = Database.GenerateCreateScript();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySQL(_fullConString);
    }
}
