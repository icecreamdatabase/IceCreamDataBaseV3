using IceCreamDataBaseV3.AppSettingsConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace IceCreamDataBaseV3;

public class Program
{
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

        Program _ = new Program(args);

        //CancellationToken cancellationToken = new CancellationToken();
        //cancellationToken.WaitHandle.WaitOne();
    }

    private static void CurrentDomain_ProcessExit(object? sender, EventArgs e)
    {
        Console.WriteLine("Shutting down...");
        
        // Send shutdown command to IrcHubClient with a 2 second timeout
    }

    public static ConfigRoot ConfigRoot { get; private set; } = new();

    private Bot _bot;

    private Program(string[] args)
    {
        Console.WriteLine("Starting");

        using IHost host = Host.CreateDefaultBuilder(args).Build();

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", true)
            .AddEnvironmentVariables()
            .Build();

        config.Bind(ConfigRoot);

        _bot = new Bot();

        host.Run();
    }
}
