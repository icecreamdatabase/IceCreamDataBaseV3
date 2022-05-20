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

    public DbSet<Channel> Channels { get; set; } = null!;
    public DbSet<Command> Commands { get; set; } = null!;
    public DbSet<CommandGroupLink> CommandGroupLinks { get; set; } = null!;
    public DbSet<CommandGroup> CommandGroups { get; set; } = null!;
    public DbSet<UserNoticeResponse> UserNoticeResponses { get; set; } = null!;
    public DbSet<IndividualUserReply> IndividualUserReplies { get; set; } = null!;

    private static bool _firstTime;

    public IcdbDbContext()
    {
        string? dbConString = Program.ConfigRoot.ConnectionStrings.IcdbV3Db;
        if (string.IsNullOrEmpty(dbConString))
            throw new InvalidOperationException("No MySql connection string!");
        _fullConString = dbConString + AdditionalMySqlConfigurationParameters;

        if (_firstTime) return;
        Database.EnsureCreated();
        _firstTime = true;
        //string createScript = Database.GenerateCreateScript();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMySQL(_fullConString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        Channel.BuildModel(modelBuilder);
        Command.BuildModel(modelBuilder);
        CommandGroupLink.BuildModel(modelBuilder);
        CommandGroup.BuildModel(modelBuilder);
        UserNoticeResponse.BuildModel(modelBuilder);
        IndividualUserReply.BuildModel(modelBuilder);
    }
}
