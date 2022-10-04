using Boudica.Commands;
using Boudica.MongoDB;
using Boudica.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    // setup our fields we assign later
    private readonly IConfiguration _config;
    private DiscordSocketClient _client;

    static void Main(string[] args)
    {
        new Program().MainAsync().GetAwaiter().GetResult();
    }

    public Program()
    {
        // create the configuration
        var _builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(path: "config.json");

        // build the configuration and assign to _config          
        _config = _builder.Build();
    }

    public async Task MainAsync()
    {
        // call ConfigureServices to create the ServiceCollection/Provider for passing around the services
        await using (var services = ConfigureServices())
        {
            // get the client and assign to client 
            // you get the services via GetRequiredService<T>
            _client = services.GetRequiredService<DiscordSocketClient>();

            // setup logging and the ready event
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            services.GetRequiredService<CommandService>().Log += LogAsync;

#if DEBUG
            await _client.LoginAsync(TokenType.Bot, _config["DebugToken"]);
#else
            await _client.LoginAsync(TokenType.Bot, _config["Token"]);
#endif

            await _client.StartAsync();

            await _client.SetGameAsync(";help for command list");

            // we get the CommandHandler class here and call the InitializeAsync method to start things up for the CommandHandler service
            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            await Task.Delay(-1);
        }
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private Task ReadyAsync()
    {
        Console.WriteLine($"Connected as -> [{_client.CurrentUser}] :)");
        return Task.CompletedTask;
    }

    // this method handles the ServiceCollection creation/configuration, and builds out the service provider we can call on later
    private ServiceProvider ConfigureServices()
    {
        var socketConfig = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            MessageCacheSize = 100
        };
        // this returns a ServiceProvider that is used later to call for those services
        // we can add types we have access to here, hence adding the new using statement:
        // using csharpi.Services;
        // the config we build is also added, which comes in handy for setting the command prefix!
        return new ServiceCollection()
            .AddSingleton(_config)
            .AddSingleton(socketConfig)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<CommandService>()
            .AddSingleton<CommandHandler>()
            .AddScoped<ActivityService>()
            .AddScoped<InsultService>()
            .AddScoped<GuardianService>()
            .AddScoped<AwardedGuardianService>()
            .AddScoped<SettingsService>()
            .AddSingleton<IMongoDBContext, MongoDBContext>()

            .BuildServiceProvider();
    }
}