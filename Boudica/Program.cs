using Boudica.Commands;
using Boudica.MongoDB;
using Boudica.Services;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    // setup our fields we assign later
    private readonly IConfiguration _config;
    private DiscordSocketClient _client;
    private InteractionService _commands;

#if DEBUG
        private const ulong GuildId = 958852217186713680;
#else
    private const ulong GuildId = 530462081636368395;
#endif

    public static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync(string[] args)
    {

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
            _commands = services.GetRequiredService<InteractionService>();

            // setup logging and the ready event
            _client.Log += LogAsync;
            _commands.Log += LogAsync;
            _client.Ready += ReadyAsync;

#if DEBUG
            await _client.LoginAsync(TokenType.Bot, _config["DebugToken"]);
#else
            await _client.LoginAsync(TokenType.Bot, _config["Token"]);
#endif

            await _client.StartAsync();
            await _client.SetGameAsync(";help for command list");

            // we get the CommandHandler class here and call the InitializeAsync method to start things up for the CommandHandler service
            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            await Task.Delay(Timeout.Infinite);
        }
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        Console.WriteLine($"Adding commands to {GuildId}...");
        await _commands.RegisterCommandsToGuildAsync(GuildId);
        Console.WriteLine($"Connected as -> [{_client.CurrentUser}] :)");
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
            .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<CommandHandler>()
            .AddScoped<ActivityService>()
            .AddScoped<InsultService>()
            .AddScoped<GuardianService>()
            .AddScoped<AwardedGuardianService>()
            .AddScoped<SettingsService>()
            .AddSingleton<CronService>()
            .AddScoped<GifService>()
            .AddScoped<TrialsService>()
            .AddScoped<HiringService>()
            .AddSingleton<IMongoDBContext, MongoDBContext>()

            .BuildServiceProvider();
    }
}