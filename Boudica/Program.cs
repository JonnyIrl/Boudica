using Boudica.Commands;
using Boudica.MongoDB;
using Boudica.Services;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class BoudicaInstance
{
    public static DiscordSocketClient Client;
}

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
        using (var services = ConfigureServices())
        {
            // get the client and assign to client 
            // you get the services via GetRequiredService<T>
            var client = services.GetRequiredService<DiscordSocketClient>();
            var commands = services.GetRequiredService<InteractionService>();
            _client = client;
            _commands = commands;
            // setup logging and the ready event
            client.Log += LogAsync;
            commands.Log += LogAsync;
            client.Ready += ReadyAsync;
            client.Connected += ClientConnected;

#if DEBUG
            await client.LoginAsync(TokenType.Bot, _config["DebugToken"]);
#else
            await client.LoginAsync(TokenType.Bot, _config["Token"]);
#endif

            await client.StartAsync();
            await client.SetGameAsync("/help for command list");

            // we get the CommandHandler class here and call the InitializeAsync method to start things up for the CommandHandler service
            await services.GetRequiredService<CommandHandler>().InitializeAsync();
            await Task.Delay(Timeout.Infinite);
        }
    }

    private async Task ClientConnected()
    {
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

        SocketGuild guild = _client.GetGuild(GuildId);
        if (guild != null)
        {
            await guild.DownloadUsersAsync();
            Console.WriteLine("Downloaded Users");
        }
    }

    // this method handles the ServiceCollection creation/configuration, and builds out the service provider we can call on later
    private ServiceProvider ConfigureServices()
    {
        var socketConfig = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            MessageCacheSize = 100,
            GatewayIntents = GatewayIntents.GuildEmojis | 
            GatewayIntents.GuildMembers | 
            GatewayIntents.GuildMessageReactions | 
            GatewayIntents.GuildMessages | 
            GatewayIntents.GuildPresences | 
            GatewayIntents.Guilds | 
            GatewayIntents.GuildWebhooks
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
            .AddSingleton<APIService>()
            .AddScoped<DailyGiftService>()
            .AddScoped<HistoryService>()
            .AddScoped<UserChallengeService>()
            .BuildServiceProvider();
    }
}