using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Boudica.Classes;
using Boudica.Enums;
using Boudica.Helpers;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Boudica.Commands
{
    public class CommandHandler
    {
        #region Raid Modals
        public delegate Task<Result> EditRaidModalSubmitted(ITextChannel channel, string title, string description, int raidId);
        public delegate Task<Result> CreateRaidModalSubmitted(SocketModal modal, ITextChannel channel, string title, string description);
        public event CreateRaidModalSubmitted OnCreateRaidModalSubmitted;
        public event EditRaidModalSubmitted OnEditRaidModalSubmitted;
        #endregion

        #region Raid Buttons
        public delegate Task<Result> EditRaidButtonClicked(SocketMessageComponent component, int raidId);
        public delegate Task<Result> CloseRaidButtonClicked(SocketMessageComponent component, int raidId);
        public delegate Task<Result> AlertRaidButtonClicked(SocketMessageComponent component, int raidId);
        public event EditRaidButtonClicked OnEditRaidButtonClicked;
        public event CloseRaidButtonClicked OnCloseRaidButtonClicked;
        public event AlertRaidButtonClicked OnAlertRaidButtonClicked;
        #endregion

        #region Fireteam Modals
        public delegate Task<Result> EditFireteamModalSubmitted(ITextChannel channel, string title, string description, int fireteamId);
        public delegate Task<Result> CreateFireteamModalSubmitted(SocketModal modal, ITextChannel channel, string title, string description, string fireteamSize);
        public event CreateFireteamModalSubmitted OnCreateFireteamModalSubmitted;
        public event EditFireteamModalSubmitted OnEditFireteamModalSubmitted;
        #endregion

        #region Fireteam Buttons
        public delegate Task<Result> EditFireteamButtonClicked(SocketMessageComponent component, int raidId);
        public delegate Task<Result> CloseFireteamButtonClicked(SocketMessageComponent component, int fireteamId);
        public delegate Task<Result> AlertFireteamButtonClicked(SocketMessageComponent component, int fireteamId);
        public event EditFireteamButtonClicked OnEditFireteamButtonClicked;
        public event CloseFireteamButtonClicked OnCloseFireteamButtonClicked;
        public event AlertFireteamButtonClicked OnAlertFireteamButtonClicked;
        #endregion

        private const string RaidIsClosed = "This raid is now closed";
        private const string ActivityIsClosed = "This activity is now closed";
        // setup fields to be set later in the constructor
        private readonly IConfiguration _config;
        private readonly InteractionService _commands;
        private readonly DiscordSocketClient _client;
        private readonly IServiceProvider _services;
        private readonly ActivityService _activityService;
        private readonly GuardianService _guardianService;
        private readonly TrialsService _trialsService;
        private readonly HiringService _hiringService;
        private char Prefix = ';';

        private static List<ulong> _manualRemovedReactionList = new List<ulong>();
        private object _lock = new object();

        private Emoji _jEmoji = new Emoji("🇯");
        private Emoji _sEmoji = new Emoji("🇸");
        private Emote _glimmerEmote = null;
        private List<Emoji> _alphabetList;

        #region Ids
        private const ulong RaidChannel = 530529729321631785;
        private const string RaidRole = "Raid Fanatics";

        private const ulong DungeonChannel = 530529123349823528;
        private const string DungeonRole = "Dungeon Challengers";

        private const ulong VanguardChannel = 530530338099691530;
        private const string VanguardRole = "Nightfall Enthusiasts";

        private const ulong CrucibleChannel = 530529088620724246;
        private const string CrucibleRole = "Crucible Contenders";

        private const ulong GambitChannel = 552184673749696512;
        private const string GambitRole = "Gambit Hustlers";

        private const ulong MiscChannel = 530528672172736515;
        private const string MiscRole = "Activity Aficionados";

#if DEBUG
        private const ulong GeneralChannel = 958852217186713683;
#else
        private const ulong GeneralChannel = 530528343666458663;
#endif

        #endregion

#if DEBUG
        private const ulong glimmerId = 1009200271475347567;
        private const ulong GuildId = 958852217186713680;
#else
        private const ulong glimmerId = 728197708074188802;
        private const ulong GuildId = 530462081636368395;
#endif
        public CommandHandler(InteractionService interactionCommands, IServiceProvider services)
        {
            //var oauthHelper = new OAuthHelper(services);

            // juice up the fields with these services
            // since we passed the services in, we can use GetRequiredService to pass them into the fields set earlier
            _config = services.GetRequiredService<IConfiguration>();
            _commands = interactionCommands;
            _client = services.GetRequiredService<DiscordSocketClient>();
            _activityService = services.GetRequiredService<ActivityService>();
            _guardianService = services.GetRequiredService<GuardianService>();
            _trialsService = services.GetRequiredService<TrialsService>();
            _hiringService = services.GetRequiredService<HiringService>();
            _services = services;
            ConfigHelper.LoadConfig();
            Emote.TryParse($"<:misc_glimmer:{glimmerId}>", out _glimmerEmote);
            PopulateAlphabetList();

            // get prefix from the configuration file
            Prefix = Char.Parse(_config["Prefix"]);

            //Listen for Reactions
            _client.ReactionAdded += ReactionAddedAsync;
            _client.ReactionRemoved += ReactionRemovedAsync;

            _client.UserJoined += UserJoined;
            //_client.Connected += ClientConnected;

            //Listen for modals
            _client.ModalSubmitted += ModalSubmitted;

            _client.ButtonExecuted += ButtonExecuted;
            BoudicaInstance.Client = _client;
        }

        private async Task ButtonExecuted(SocketMessageComponent component)
        {
            // We can now check for our custom id
            string switchStatement = component.Data.CustomId.Substring(0, component.Data.CustomId.IndexOf("-"));
            if(int.TryParse(switchStatement, out int swap) == false)
            {
                await component.RespondAsync("Command failed", ephemeral: true);
                return;
            }
            if (int.TryParse(component.Data.CustomId.Replace(switchStatement + "-", string.Empty).Trim(), out int id) == false)
            {
                await component.RespondAsync("Command failed", ephemeral: true);
                return;
            }
            ButtonCustomId buttonClicked = (ButtonCustomId) swap;
            switch (buttonClicked)
            {
                case ButtonCustomId.Invalid:
                    await component.RespondAsync("Failed", ephemeral: true);
                    break;
                case ButtonCustomId.RaidAlert:
                    if (OnAlertRaidButtonClicked != null)
                    {
                        Result result = await OnAlertRaidButtonClicked.Invoke(component, id);
                        if (result.Success)
                        {
                            await component.RespondAsync("Successfully alerted raid", ephemeral: true);
                        }
                        else
                        {
                            await component.RespondAsync("Failed to alert raid - " + result.Message, ephemeral: true);
                        }
                        return;
                    }
                    await component.RespondAsync("Failed to alert raid", ephemeral: true);
                    break;
                case ButtonCustomId.FireteamAlert:
                    if (OnAlertFireteamButtonClicked != null)
                    {
                        Result result = await OnAlertFireteamButtonClicked.Invoke(component, id);
                        if (result.Success)
                        {
                            await component.RespondAsync("Successfully alerted fireteam", ephemeral: true);
                        }
                        else
                        {
                            await component.RespondAsync("Failed to alert fireteam - " + result.Message, ephemeral: true);
                        }
                        return;
                    }
                    await component.RespondAsync("Failed to alert fireteam", ephemeral: true);
                    break;
                case ButtonCustomId.EditRaid:
                    if (OnEditRaidButtonClicked != null)
                    {
                        Result result = await OnEditRaidButtonClicked.Invoke(component, id);
                        if (result.Success)
                        {

                        }
                        else
                        {
                            await component.RespondAsync("Failed to edit raid - " + result.Message, ephemeral: true);
                        }
                        return;
                    }
                    await component.RespondAsync("Failed to edit raid", ephemeral: true);
                    break;
                case ButtonCustomId.EditFireteam:
                    if (OnEditFireteamButtonClicked != null)
                    {
                        Result result = await OnEditFireteamButtonClicked.Invoke(component, id);
                        if (result.Success)
                        {

                        }
                        else
                        {
                            await component.RespondAsync("Failed to edit fireteam - " + result.Message, ephemeral: true);
                        }
                        return;
                    }
                    await component.RespondAsync("Failed to edit fireteam", ephemeral: true);
                    break;
                case ButtonCustomId.CloseRaid:
                    if (OnCloseRaidButtonClicked != null)
                    {
                        Result result = await OnCloseRaidButtonClicked.Invoke(component, id);
                        if (result.Success)
                        {

                        }
                        else
                        {
                            await component.RespondAsync("Failed to close raid - " + result.Message, ephemeral: true);
                        }
                        return;
                    }
                    await component.RespondAsync("Failed to close raid", ephemeral: true);
                    break;
                case ButtonCustomId.CloseFireteam:
                    if (OnCloseFireteamButtonClicked != null)
                    {
                        Result result = await OnCloseFireteamButtonClicked.Invoke(component, id);
                        if (result.Success)
                        {

                        }
                        else
                        {
                            await component.RespondAsync("Failed to close fireteam - " + result.Message, ephemeral: true);
                        }
                        return;
                    }
                    await component.RespondAsync("Failed to close fireteam", ephemeral: true);
                    break;
                default:
                    await component.RespondAsync("Failed", ephemeral: true);
                    break;
            }
            //await component.RespondAsync($"Button click. Type: {buttonClicked}, Id: {id}", ephemeral: true);
        }

        private async Task UserJoined(SocketGuildUser arg)
        {
            if(arg.Guild.Id != GuildId)
            {
                Console.WriteLine("GuildId not equal");
                return;
            }

            SocketGuild guild = _client.GetGuild(arg.Guild.Id);
            if(guild == null)
            {
                Console.WriteLine("Could not find guild that user joined");
                return;
            }

            ITextChannel channel = guild.GetTextChannel(GeneralChannel);
            if(channel == null)
            {
                Console.WriteLine("Could not find channel for id " + GeneralChannel);
                return;
            }

            await channel.SendMessageAsync($"<@{arg.Id}>!", embed: EmbedHelper.CreateInfoReply(GetUserJoinedMessage()).Build());
            await _guardianService.CreateGuardian(arg.Id, arg.Username);
        }

        private string GetUserJoinedMessage()
        {
            const ulong CrucibleChannel = 530529088620724246;
            const ulong RaidChannel = 530529729321631785;
            const ulong CheckpointChannel = 1009209094286094386;
            const ulong WeaponsChannel = 530530585538461698;
            const ulong AnnouncementsChannel = 557927181393592370;
            const ulong LoreChannel = 530529654306766848;
            const ulong WelcomeChannel = 530536477151592469;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Welcome to the best clan related decision you ever made.... We are the Deadly Viper Squad and now so are you!");
            sb.AppendLine("");
            sb.AppendLine($"There are a number of channels for you to be a part of and get involved in such as <#{CrucibleChannel}> and <#{RaidChannel}>, where we organise activities relating to those areas of Destiny. Additionally, we have some channels to enhance your gaming experience! An example being <#{CheckpointChannel}>, which allows you to avoid the hassle of trudging through an activity, when you want to focus on one specific part! Feel free to get stuck in with discussion around the <#{LoreChannel}> of destiny, as well as the best rolls for the must have <#{WeaponsChannel}>.");
            sb.AppendLine("");
            sb.AppendLine($"However, before any of that, you'll need to get yourself kitted out with roles in <#{WelcomeChannel}>, so please head on over and don't forget to read the clan rules too!");
            sb.AppendLine("");
            sb.AppendLine("We have a number of in-house robots we use to streamline the discord and give you the autonomy to organise your own fireteams, stay appraised of your stats and keep you up to date with all news destiny. You may recognise <@296023718839451649>, utilised by **/** commands, for example */daily* which shows the daily rotations (after you have registered with /register). But allow us to introduce you to <@994334270480986233>! Boudica lets you organise fireteams and earn our clan currency *Glimmer*. Feel free to use the /help command for a full list of what these bad bots are capable of!");
            sb.AppendLine("");
            sb.AppendLine($"I'll draw your attention to Seasonal Clan Events and the channel(s) in the category. As a clan we enjoy organising a wide range of events that allow clan members to test their mettle, their artistic flair and their fashionable expertise against one another, on a seasonal basis. *Glimmer* will be particularly useful during the upcoming event we are looking to debut during the next expansion Lightfall. Further news on these glorious events is released from <#{AnnouncementsChannel}>.");
            sb.AppendLine("");
            sb.AppendLine($"We hope we haven't bombarded you with too much info, all that is left is to welcome you to a community, over a **decade** in the making!");
            sb.AppendLine("");
            sb.AppendLine("We are most happy to have you and look forward to seeing you grow!");

            return sb.ToString();
        }

        public void AddPlayerToManualEmoteList(ulong userId)
        {
            lock (_lock)
            {
                _manualRemovedReactionList.Add(userId);
            }
        }

        private void PopulateAlphabetList()
        {
            _alphabetList = new List<Emoji>();
            _alphabetList.Add(new Emoji("🇦"));
            _alphabetList.Add(new Emoji("🇧"));
            _alphabetList.Add(new Emoji("🇨"));
            _alphabetList.Add(new Emoji("🇩"));
            _alphabetList.Add(new Emoji("🇪"));
            _alphabetList.Add(new Emoji("🇫"));
            _alphabetList.Add(new Emoji("🇬"));
            _alphabetList.Add(new Emoji("🇭"));
            _alphabetList.Add(new Emoji("🇮"));
            _alphabetList.Add(new Emoji("🇰"));
            _alphabetList.Add(new Emoji("🇱"));
            _alphabetList.Add(new Emoji("🇲"));
            _alphabetList.Add(new Emoji("🇳"));
            _alphabetList.Add(new Emoji("🇴"));
            _alphabetList.Add(new Emoji("🇵"));
            _alphabetList.Add(new Emoji("🇶"));
            _alphabetList.Add(new Emoji("🇷"));
            _alphabetList.Add(new Emoji("🇹"));
            _alphabetList.Add(new Emoji("🇺"));
            _alphabetList.Add(new Emoji("🇻"));
            _alphabetList.Add(new Emoji("🇼"));
            _alphabetList.Add(new Emoji("🇽"));
            _alphabetList.Add(new Emoji("🇾"));
            _alphabetList.Add(new Emoji("🇿"));
        }

        private async Task ModalSubmitted(SocketModal modal)
        {
            // We can now check for our custom id
            string switchStatement = modal.Data.CustomId.Substring(0, modal.Data.CustomId.IndexOf("-"));
            if (int.TryParse(switchStatement, out int swap) == false)
            {
                await modal.RespondAsync("Command failed", ephemeral: true);
                return;
            }
            if (int.TryParse(modal.Data.CustomId.Replace(switchStatement + "-", string.Empty).Trim(), out int id) == false)
            {
                await modal.RespondAsync("Command failed", ephemeral: true);
                return;
            }

            List<SocketMessageComponentData> components = modal.Data.Components.ToList();
            string title = string.Empty;
            string description = string.Empty;
            string fireteamSize = string.Empty;
            foreach (SocketMessageComponentData component in components)
            {
                if(int.TryParse(component.CustomId, out int modalInputType))
                {
                    ModalInputType type = (ModalInputType)modalInputType;
                    switch (type)
                    {
                        case ModalInputType.InputTitle:
                            title = component.Value;
                            break;
                        case ModalInputType.InputDescription:
                            description = component.Value;
                            break;
                        case ModalInputType.Select:
                            break;
                        case ModalInputType.FireteamSize:
                            fireteamSize = component.Value;
                            break;
                        default:
                            break;
                    }
                }
            }

            ButtonCustomId buttonClicked = (ButtonCustomId)swap;
            switch (buttonClicked)
            {
                case ButtonCustomId.Invalid:
                    break;
                case ButtonCustomId.EditRaid:
                    if (OnEditRaidModalSubmitted != null)
                    {
                        Result result = await OnEditRaidModalSubmitted.Invoke((ITextChannel)modal.Channel, title, description, id);
                        if (result.Success)
                        {
                            await modal.RespondAsync("Success", ephemeral: true);
                        }
                        else
                        {
                            await modal.RespondAsync("Failed to edit raid - " + result.Message, ephemeral: true);
                        }
                        return;
                    }
                    await modal.RespondAsync("Failed to edit raid", ephemeral: true);
                    break;
                case ButtonCustomId.EditFireteam:
                    if (OnEditFireteamModalSubmitted != null)
                    {
                        Result result = await OnEditFireteamModalSubmitted.Invoke((ITextChannel)modal.Channel, title, description, id);
                        if (result.Success)
                        {
                            await modal.RespondAsync("Success", ephemeral: true);
                        }
                        else
                        {
                            await modal.RespondAsync("Failed to edit fireteam - " + result.Message, ephemeral: true);
                        }
                        return;
                    }
                    await modal.RespondAsync("Failed to edit fireteam", ephemeral: true);
                    break;
                case ButtonCustomId.CreateRaid:
                    if (OnCreateRaidModalSubmitted != null)
                    {
                        Result result = await OnCreateRaidModalSubmitted.Invoke(modal, (ITextChannel)modal.Channel, title, description);
                        if (result.Success)
                        {
                            await modal.FollowupAsync("Successfully created raid", ephemeral: true);
                        }
                        else
                        {
                            await modal.RespondAsync("Failed to create raid - " + result.Message, ephemeral: true);
                        }
                        return;
                    }
                    await modal.RespondAsync("Failed to create raid", ephemeral: true);
                    break;
                case ButtonCustomId.CreateFireteam:
                    if (OnCreateFireteamModalSubmitted != null)
                    {
                        Result result = await OnCreateFireteamModalSubmitted.Invoke(modal, (ITextChannel)modal.Channel, title, description, fireteamSize);
                        if (result.Success)
                        {
                            await modal.FollowupAsync("Successfully created fireteam", ephemeral: true);
                        }
                        else
                        {
                            await modal.RespondAsync("Failed to create fireteam - " + result.Message, ephemeral: true);
                        }
                        return;
                    }
                    await modal.RespondAsync("Failed to create fireteam", ephemeral: true);
                    break;
                default:
                    await modal.RespondAsync("Failed", ephemeral: true);
                    break;
            }

        }

        public async Task InitializeAsync()
        {
            // register modules that are public and inherit ModuleBase<T>.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            //SocketGuild guild = _client.GetGuild(GuildId);
            //if (guild.IsSynced == false)
            //{
            //    await guild.DownloadUsersAsync();
            //}

            // process the InteractionCreated payloads to execute Interactions commands
            _client.InteractionCreated += HandleInteraction;

            // process the command execution results 
            _commands.SlashCommandExecuted += SlashCommandExecuted;
            _commands.ContextCommandExecuted += ContextCommandExecuted;
            _commands.ComponentCommandExecuted += ComponentCommandExecuted;
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                // create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new SocketInteractionContext(_client, arg);
                var data = arg.Data;
                switch (arg.Type)
                {
                    case InteractionType.Ping:
                        break;
                    case InteractionType.ApplicationCommand:
                        await _commands.ExecuteCommandAsync(ctx, _services);
                        break;
                    case InteractionType.MessageComponent:
                        break;
                    case InteractionType.ApplicationCommandAutocomplete:
                        break;
                    case InteractionType.ModalSubmit:
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                // if a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                {
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
                }
            }
        }

        private Task ComponentCommandExecuted(ComponentCommandInfo arg1, IInteractionContext arg2, IResult arg3)
        {
            if (!arg3.IsSuccess)
            {
                switch (arg3.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                    case InteractionCommandError.UnknownCommand:
                        // implement
                        break;
                    case InteractionCommandError.BadArgs:
                        // implement
                        break;
                    case InteractionCommandError.Exception:
                        // implement
                        break;
                    case InteractionCommandError.Unsuccessful:
                        // implement
                        break;
                    default:
                        break;
                }
            }

            return Task.CompletedTask;
        }
        private Task ContextCommandExecuted(ContextCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
        {
            if (!arg3.IsSuccess)
            {
                switch (arg3.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                    case InteractionCommandError.UnknownCommand:
                        // implement
                        break;
                    case InteractionCommandError.BadArgs:
                        // implement
                        break;
                    case InteractionCommandError.Exception:
                        // implement
                        break;
                    case InteractionCommandError.Unsuccessful:
                        // implement
                        break;
                    default:
                        break;
                }
            }

            return Task.CompletedTask;
        }
        private Task SlashCommandExecuted(SlashCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
        {
            if (!arg3.IsSuccess)
            {
                switch (arg3.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // implement
                        break;
                    case InteractionCommandError.UnknownCommand:
                        // implement
                        break;
                    case InteractionCommandError.BadArgs:
                        // implement
                        break;
                    case InteractionCommandError.Exception:
                        // implement
                        break;
                    case InteractionCommandError.Unsuccessful:
                        // implement
                        break;
                    default:
                        break;
                }
            }

            return Task.CompletedTask;
        }

        public async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (reaction.UserId == 1016602395528151050 || reaction.UserId == 994334270480986233) return;

            if (reaction.Emote.Name == "🇯")
            {
                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }
                ActivityResponse result = await AddPlayerToActivityV2(message, user);
                if (result.Success == false)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    lock (_lock)
                    {
                        _manualRemovedReactionList.Add(user.Id);
                    }
                    await originalMessage.RemoveReactionAsync(reaction.Emote, user);
                    if (result.IsFull)
                    {
                        await originalMessage.ReplyAsync(null, false, EmbedHelper.CreateFailedReply($"<@{user.Id}>, sorry " + result.FullMessage).Build());
                    }
                }
                else if (result.Success && result.PreviousReaction)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    lock (_lock)
                    {
                        _manualRemovedReactionList.Add(user.Id);
                    }
                    await originalMessage.RemoveReactionAsync(Emoji.Parse(":regional_indicator_s:"), user);
                }
                return;
            }

            else if (reaction.Emote.Name == "🇸")
            {
                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }
                ActivityResponse result = await AddSubstituteToActivityV2(message, user);
                if (result.Success == false)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    lock (_lock)
                    {
                        _manualRemovedReactionList.Add(user.Id);
                    }
                    await originalMessage.RemoveReactionAsync(reaction.Emote, user);
                }
                else if (result.Success && result.PreviousReaction)
                {
                    var originalMessage = await message.GetOrDownloadAsync();
                    lock (_lock)
                    {
                        _manualRemovedReactionList.Add(user.Id);
                    }
                    await originalMessage.RemoveReactionAsync(Emoji.Parse(":regional_indicator_j:"), user);
                }

                return;
            }

            else if (reaction.Emote.Name == "misc_glimmer")
            {
                var originalMessage = await message.GetOrDownloadAsync();
                if (originalMessage != null)
                {
                    ulong authorId = originalMessage.Author.Id;
                    //Stop person adding increasing their own
                    if (authorId != reaction.UserId)
                    {
                        var user = await reaction.Channel.GetUserAsync(authorId) as SocketGuildUser;
                        if (user == null || user.IsBot)
                        {
                            return;
                        }
                        await _guardianService.IncreaseGlimmerAsync(user.Id, user.DisplayName, 1);
                    }
                }
            }
            else if (reaction.Emote.Name == "✅")
            {
                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }

                await ActivityClosedReactionSuccess(message, user);
            }
            else if (reaction.Emote.Name == "❌")
            {
                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }

                await ActivityClosedReactionFail(message, user);
            }
            else if (_alphabetList.Contains(reaction.Emote))
            {
                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }

                var originalMessage = await message.GetOrDownloadAsync();
                if (originalMessage == null) return;
                if (originalMessage.Embeds == null || originalMessage.Embeds.Any() == false) return;
                if (originalMessage.Author.IsBot == false) return;
                var embed = originalMessage.Embeds.First();
                if (embed.Title.Contains("closed")) return;

                bool result = await _trialsService.AddPlayersVote(user.Id, user.Username, reaction.Emote.Name);
                if(result)
                    await originalMessage.ReplyAsync($"<@{user.Id}>, your vote has been counted and locked in.");
            }
        }
        public async Task ReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
        {
            if (reaction.Emote.Name == "🇯")
            {
                lock (_lock)
                {
                    if (_manualRemovedReactionList.Contains(reaction.UserId))
                    {
                        _manualRemovedReactionList.RemoveAll(x => x == reaction.UserId);
                        return;
                    }
                }

                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }
                ActivityResponse result = await RemovePlayerFromActivityV2(message, user);
                return;
            }

            else if (reaction.Emote.Name == "🇸")
            {
                lock (_lock)
                {
                    if (_manualRemovedReactionList.Contains(reaction.UserId))
                    {
                        _manualRemovedReactionList.RemoveAll(x => x == reaction.UserId);
                        return;
                    }
                }
                var user = await reaction.Channel.GetUserAsync(reaction.UserId) as SocketGuildUser;
                if (user == null || user.IsBot)
                {
                    return;
                }
                ActivityResponse result = await RemoveSubFromActivityV2(message, user);
                return;
            }

            else if (reaction.Emote.Name == "misc_glimmer")
            {
                var originalMessage = await message.GetOrDownloadAsync();
                if (originalMessage != null)
                {
                    ulong authorId = originalMessage.Author.Id;
                    //Stop person decreasing their own
                    if (authorId != reaction.UserId)
                        await _guardianService.RemoveGlimmerAsync(authorId, 1);
                }
            }

        }
        private async Task<ActivityResponse> AddPlayerToActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            if(embeds.Any() == false)
            {
                activityResponse.Success = true;
                return activityResponse;
            }
            var embed = embeds?.First();
            if (embed != null)
            {
                ActivityType activityType = new ActivityType();
                activityType.Parse(embed);
                if (activityType.Activity == ActivityTypes.Unknown)
                    return activityResponse;

                if (activityType.Activity == ActivityTypes.Raid)
                {
                    Raid existingRaid = await _activityService.GetMongoRaidAsync(activityType.Id);
                    if (existingRaid == null || existingRaid.AwardedGlimmer)
                    {
                        //Set to success to let the reaction go through
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    //Already joined
                    ActivityUser existingPlayer = existingRaid.Players.FirstOrDefault(x => x.UserId == user.Id);
                    if (existingPlayer != null)
                    {
                        //If the player hasn't already reacted then add them.
                        if (existingPlayer.Reacted == false)
                        {
                            existingPlayer.Reacted = true;
                            //Need to update the message to include the person has reacted.
                            await _activityService.UpdateRaidAsync(existingRaid);
                            await UpdateRaidMessage(originalMessage, embed, existingRaid, true);
                        }
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    //If the raid has been closed and new people try and join
                    if(existingRaid.DateTimeClosed != DateTime.MinValue)
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    if (existingRaid.MaxPlayerCount == existingRaid.Players.Count)
                    {
                        return activityResponse;
                    }

                    //Player is a Sub
                    ActivityUser subUser = existingRaid.Substitutes?.Find(x => x.UserId == user.Id);
                    if (subUser != null)
                    {
                        existingRaid.Substitutes?.Remove(subUser);
                        activityResponse.PreviousReaction = true;
                    }

                    //Now Add them to the Players list
                    existingRaid.Players.Add(new ActivityUser(user.Id, user.Username, true));
                    existingRaid = await _activityService.UpdateRaidAsync(existingRaid);
                    if (existingRaid.Players.Count == existingRaid.MaxPlayerCount)
                    {
                        activityResponse.IsFull = true;
                        activityResponse.FullMessage = $"Raid Id {existingRaid.Id} is now full. Feel free to Sub or watch if a slot becomes available!";
                    }

                    await UpdateRaidMessage(originalMessage, embed, existingRaid, false);

                    activityResponse.Success = true;
                    return activityResponse;
                }
                else if (activityType.Activity == ActivityTypes.Fireteam)
                {
                    Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(activityType.Id);
                    if (existingFireteam == null || existingFireteam.AwardedGlimmer)
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    //Already Joined
                    ActivityUser existingPlayer = existingFireteam.Players.FirstOrDefault(x => x.UserId == user.Id);
                    if (existingPlayer != null)
                    {
                        //If the player hasn't already reacted then add them.
                        if (existingPlayer.Reacted == false)
                        {
                            existingPlayer.Reacted = true;
                            await _activityService.UpdateFireteamAsync(existingFireteam);
                            //Need to update the message to include the person has reacted.
                            await UpdateFireteamMessage(originalMessage, embed, existingFireteam, true);
                        }
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    //If the Fireteam has been closed and new people try and join
                    if (existingFireteam.DateTimeClosed != DateTime.MinValue)
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    if (existingFireteam.MaxPlayerCount == existingFireteam.Players.Count)
                    {
                        return activityResponse;
                    }

                    //Player is a Sub
                    ActivityUser subUser = existingFireteam.Substitutes?.Find(x => x.UserId == user.Id);
                    if (subUser != null)
                    {
                        existingFireteam.Substitutes?.Remove(subUser);
                        activityResponse.PreviousReaction = true;
                    }
                    

                    //Now Add them to the Players list
                    existingFireteam.Players.Add(new ActivityUser(user.Id, user.Username, true));
                    existingFireteam = await _activityService.UpdateFireteamAsync(existingFireteam);
                    if (existingFireteam.Players.Count == existingFireteam.MaxPlayerCount)
                    {
                        activityResponse.IsFull = true;
                        activityResponse.FullMessage = $"Fireteam Id {existingFireteam.Id} is now full. Feel free to Sub or watch if a slot becomes available!";
                    }

                    await UpdateFireteamMessage(originalMessage, embed, existingFireteam, false);

                    activityResponse.Success = true;
                    return activityResponse;
                }

                return activityResponse;
            }
            return activityResponse;
        }

        private async Task UpdateRaidMessage(IUserMessage originalMessage, IEmbed? embed, Raid existingRaid, bool alreadyJoined)
        {
            var modifiedEmbed = new EmbedBuilder();
            AddActivityUsersField(modifiedEmbed, "Players", existingRaid.Players);
            AddActivityUsersField(modifiedEmbed, "Subs", existingRaid.Substitutes);

            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);

            await originalMessage.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            if (alreadyJoined == false && existingRaid.Players.Count == existingRaid.MaxPlayerCount)
            {
                try
                {
                    await originalMessage.ReplyAsync($"<@{existingRaid.CreatedByUserId}>, your Raid is now full!");
                }
                catch (Exception ex)
                {

                }
            }
        }

        private async Task UpdateFireteamMessage(IUserMessage originalMessage, IEmbed? embed, Fireteam existingFireteam, bool alreadyJoined)
        {
            var modifiedEmbed = new EmbedBuilder();
            AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
            AddActivityUsersField(modifiedEmbed, "Subs", existingFireteam.Substitutes);

            EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
            EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);

            await originalMessage.ModifyAsync(x =>
            {
                x.Embed = modifiedEmbed.Build();
            });

            if (alreadyJoined == false && existingFireteam.Players.Count == existingFireteam.MaxPlayerCount)
            {
                try
                {
                    await originalMessage.ReplyAsync($"<@{existingFireteam.CreatedByUserId}>, your Fireteam is now full!");
                }
                catch (Exception ex)
                {

                }
            }
        }

        private async Task<ActivityResponse> RemovePlayerFromActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            if (embeds.Any() == false)
            {
                activityResponse.Success = true;
                return activityResponse;
            }
            var embed = embeds?.First();
            if (embed != null)
            {
#region Check Closed
                if (embed.Title == RaidIsClosed || embed.Title == ActivityIsClosed)
                {
                    return activityResponse;
                }
#endregion


                ActivityType activityType = new ActivityType();
                activityType.Parse(embed);
                if (activityType.Activity == ActivityTypes.Unknown)
                    return activityResponse;

                if (activityType.Activity == ActivityTypes.Raid)
                {
                    MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(activityType.Id);
                    if (existingRaid == null)
                    {
                        return activityResponse;
                    }

                    //Player is a Sub
                    ActivityUser playerUser = existingRaid.Players?.Find(x => x.UserId == user.Id);
                    if (playerUser != null)
                    {
                        existingRaid.Players?.Remove(playerUser);
                    }
                    else
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    existingRaid = await _activityService.UpdateRaidAsync(existingRaid);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingRaid.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingRaid.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    if (existingRaid.Players.Count == existingRaid.MaxPlayerCount - 1)
                    {
                        try
                        {
                            IRole role = GetRoleForChannel(user, originalMessage.Channel.Id);
                            if (role != null)
                            {
                                await originalMessage.ReplyAsync(role.Mention + " A slot has now opened up!");
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    await originalMessage.ReplyAsync(null, false, EmbedHelper.CreateInfoReply($"<@{user.Id}> has left Raid Id {existingRaid.Id}").Build());

                    activityResponse.Success = true;
                    return activityResponse;
                }
                else if (activityType.Activity == ActivityTypes.Fireteam)
                {
                    MongoDB.Models.Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(activityType.Id);
                    if (existingFireteam == null)
                    {
                        return activityResponse;
                    }

                    ActivityUser playerUser = existingFireteam.Players?.Find(x => x.UserId == user.Id);
                    if (playerUser != null)
                    {
                        existingFireteam.Players?.Remove(playerUser);
                    }
                    //Player got stuck in bad loop where said was active but wasn't.
                    else
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }


                    existingFireteam = await _activityService.UpdateFireteamAsync(existingFireteam);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingFireteam.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    if (existingFireteam.Players.Count == existingFireteam.MaxPlayerCount - 1)
                    {
                        try
                        {
                            IRole role = GetRoleForChannel(user, originalMessage.Channel.Id);
                            if (role != null)
                            {
                                await originalMessage.ReplyAsync(role.Mention + " A slot has now opened up!");
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }

                    await originalMessage.ReplyAsync(null, false, EmbedHelper.CreateInfoReply($"<@{user.Id}> has left Fireteam Id {existingFireteam.Id}").Build());

                    activityResponse.Success = true;
                    return activityResponse;
                }

                return activityResponse;
            }
            return activityResponse;
        }
        private async Task<ActivityResponse> RemoveSubFromActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            if (embeds.Any() == false)
            {
                activityResponse.Success = true;
                return activityResponse;
            }
            var embed = embeds?.First();
            if (embed != null)
            {
#region Check Closed
                if (embed.Title == RaidIsClosed || embed.Title == ActivityIsClosed)
                {
                    return activityResponse;
                }
#endregion


                ActivityType activityType = new ActivityType();
                activityType.Parse(embed);
                if (activityType.Activity == ActivityTypes.Unknown)
                    return activityResponse;

                if (activityType.Activity == ActivityTypes.Raid)
                {
                    MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(activityType.Id);
                    if (existingRaid == null)
                    {
                        return activityResponse;
                    }

                    //Player is a Sub
                    ActivityUser subUser = existingRaid.Substitutes?.Find(x => x.UserId == user.Id);
                    if (subUser != null)
                    {
                        existingRaid.Substitutes?.Remove(subUser);
                    }
                    else
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    existingRaid = await _activityService.UpdateRaidAsync(existingRaid);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingRaid.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingRaid.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    activityResponse.Success = true;
                    return activityResponse;
                }
                else if (activityType.Activity == ActivityTypes.Fireteam)
                {
                    MongoDB.Models.Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(activityType.Id);
                    if (existingFireteam == null)
                    {
                        return activityResponse;
                    }

                    ActivityUser subUser = existingFireteam.Substitutes?.Find(x => x.UserId == user.Id);
                    if (subUser != null)
                    {
                        existingFireteam.Substitutes?.Remove(subUser);
                    }
                    else
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    existingFireteam = await _activityService.UpdateFireteamAsync(existingFireteam);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingFireteam.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    activityResponse.Success = true;
                    return activityResponse;
                }

                return activityResponse;
            }
            return activityResponse;
        }
        private async Task<ActivityResponse> AddSubstituteToActivityV2(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            ActivityResponse activityResponse = new ActivityResponse(false, false);
            var originalMessage = await message.GetOrDownloadAsync();
            var embeds = originalMessage.Embeds.ToList();
            if (embeds.Any() == false)
            {
                activityResponse.Success = true;
                return activityResponse;
            }
            var embed = embeds?.First();
            if (embed != null)
            {
#region Check Closed
                if (embed.Title == RaidIsClosed || embed.Title == ActivityIsClosed)
                {
                    return activityResponse;
                }
#endregion


                ActivityType activityType = new ActivityType();
                activityType.Parse(embed);
                if (activityType.Activity == ActivityTypes.Unknown)
                    return activityResponse;

                if (activityType.Activity == ActivityTypes.Raid)
                {
                    MongoDB.Models.Raid existingRaid = await _activityService.GetMongoRaidAsync(activityType.Id);
                    if (existingRaid == null)
                    {
                        return activityResponse;
                    }

                    //User is already a Player
                    ActivityUser playerUser = existingRaid.Players?.Find(x => x.UserId == user.Id);
                    if (playerUser != null)
                    {
                        existingRaid.Players?.Remove(playerUser);
                        activityResponse.PreviousReaction = true;
                    }
                    //Already Joined
                    else if (existingRaid.Substitutes.FirstOrDefault(x => x.UserId == user.Id) != null)
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    //Now Add them to the Substitues list
                    existingRaid.Substitutes.Add(new ActivityUser(user.Id, user.Username));
                    existingRaid = await _activityService.UpdateRaidAsync(existingRaid);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingRaid.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingRaid.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingRaid);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    activityResponse.Success = true;
                    return activityResponse;
                }
                else if (activityType.Activity == ActivityTypes.Fireteam)
                {
                    MongoDB.Models.Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(activityType.Id);
                    if (existingFireteam == null)
                    {
                        return activityResponse;
                    }

                    //User is already a Player
                    ActivityUser playerUser = existingFireteam.Players?.Find(x => x.UserId == user.Id);
                    if (playerUser != null)
                    {
                        existingFireteam.Players?.Remove(playerUser);
                        activityResponse.PreviousReaction = true;
                    }
                    //Already Joined
                    else if (existingFireteam.Substitutes.FirstOrDefault(x => x.UserId == user.Id) != null)
                    {
                        activityResponse.Success = true;
                        return activityResponse;
                    }

                    //Now Add them to the Substitues list
                    existingFireteam.Substitutes.Add(new ActivityUser(user.Id, user.Username));
                    existingFireteam = await _activityService.UpdateFireteamAsync(existingFireteam);

                    var modifiedEmbed = new EmbedBuilder();
                    AddActivityUsersField(modifiedEmbed, "Players", existingFireteam.Players);
                    AddActivityUsersField(modifiedEmbed, "Subs", existingFireteam.Substitutes);

                    EmbedHelper.UpdateAuthorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateDescriptionTitleColorOnEmbed(modifiedEmbed, embed);
                    EmbedHelper.UpdateFooterOnEmbed(modifiedEmbed, existingFireteam);

                    await originalMessage.ModifyAsync(x =>
                    {
                        x.Embed = modifiedEmbed.Build();
                    });

                    activityResponse.Success = true;
                    return activityResponse;
                }

                return activityResponse;
            }
            return activityResponse;
        }
        private async Task ActivityClosedReactionSuccess(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            var originalMessage = await message.GetOrDownloadAsync();
            if (originalMessage.Author.IsBot == false) return;
            var embeds = originalMessage.Embeds.ToList();
            if (embeds.Any() == false)
            {
                return;
            }

            var embed = embeds?.First();
            if (embed != null)
            {
                string description = embed.Description;
                if (description.Contains("did this activity get completed") == false)
                    return;
                string[] split = description.Split("has");
                if (split.Length <= 0)
                {
                    return;
                }
                string[] newDescriptionSplit = description.Split("<@");
                //Raid
                if(split[0].Contains("Raid"))
                {
                    if (int.TryParse(split[0].Replace("Raid", string.Empty).Trim(), out int id))
                    {
                        Raid existingRaid = await _activityService.GetMongoRaidAsync(id);
                        if (existingRaid == null || existingRaid.DateTimeClosed == DateTime.MinValue || existingRaid.AwardedGlimmer || existingRaid.CreatedByUserId != user.Id) return;

                        Tuple<int, bool> glimmerResult = await CalculateGlimmerForActivity(existingRaid.Players, existingRaid.CreatedByUserId, true);
                        existingRaid.AwardedGlimmer = true;
                        await _activityService.UpdateRaidAsync(existingRaid);
                        StringBuilder sb = new StringBuilder();
                        sb.AppendJoin(", ", existingRaid.Players.Where(x => x.Reacted).Select(x => x.DisplayName));
                        sb.Append($" received {glimmerResult.Item1} Glimmer for completing this activity.");
                        if (glimmerResult.Item2 == false)
                        {
                            Console.WriteLine("glimmerResult.Item2 == false");
                            string creatorName = existingRaid.Players.FirstOrDefault(x => x.UserId == existingRaid.CreatedByUserId)?.DisplayName;
                            if (string.IsNullOrEmpty(creatorName) == false)
                                sb.Append($" {creatorName} received a first-time weekly bonus of 3 Glimmer for creating the activity");
                        }
                        var modifiedEmbed = new EmbedBuilder();
                        modifiedEmbed.Description = $"{newDescriptionSplit[0]} {sb.ToString()}";
                        modifiedEmbed.Color = embed.Color;
                        await originalMessage.ModifyAsync(x =>
                        {
                            x.Embed = modifiedEmbed.Build();
                        });
                    }
                }
                //Fireteam
                else
                {
                    if (int.TryParse(split[0].Replace("Fireteam", string.Empty).Trim(), out int id))
                    {
                        Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(id);
                        if (existingFireteam == null || existingFireteam.DateTimeClosed == DateTime.MinValue || existingFireteam.AwardedGlimmer || existingFireteam.CreatedByUserId != user.Id) return;
                        Tuple<int, bool> glimmerResult = await CalculateGlimmerForActivity(existingFireteam.Players, existingFireteam.CreatedByUserId, false);
                        existingFireteam.AwardedGlimmer = true;
                        await _activityService.UpdateFireteamAsync(existingFireteam);
                        StringBuilder sb = new StringBuilder();
                        sb.AppendJoin(", ", existingFireteam.Players.Where(x => x.Reacted).Select(x => x.DisplayName));
                        sb.Append($" received {glimmerResult.Item1} Glimmer for completing this activity.");
                        //AwardedThisWeek = false
                        if (glimmerResult.Item2 == false)
                        {
                            Console.WriteLine("glimmerResult.Item2 == false");
                            string creatorName = existingFireteam.Players.FirstOrDefault(x => x.UserId == existingFireteam.CreatedByUserId)?.DisplayName;
                            if (string.IsNullOrEmpty(creatorName) == false)
                                sb.Append($" {creatorName} received a first-time weekly bonus of 3 Glimmer for creating the activity");
                        }
                        var modifiedEmbed = new EmbedBuilder();
                        modifiedEmbed.Description = $"{newDescriptionSplit[0]} {sb.ToString()}";
                        modifiedEmbed.Color = embed.Color;
                        await originalMessage.ModifyAsync(x =>
                        {
                            x.Embed = modifiedEmbed.Build();
                        });
                    }
                }
            }
        }
        private async Task ActivityClosedReactionFail(Cacheable<IUserMessage, ulong> message, SocketGuildUser user)
        {
            var originalMessage = await message.GetOrDownloadAsync();
            if (originalMessage.Author.IsBot == false) return;
            var embeds = originalMessage.Embeds.ToList();
            if (embeds.Any() == false)
            {
                return;
            }

            var embed = embeds?.First();
            if (embed != null)
            {
                string description = embed.Description;
                if (description.Contains("did this activity get completed") == false)
                    return;
                string[] split = description.Split("has");
                if (split.Length <= 0)
                {
                    return;
                }
                string[] newDescriptionSplit = description.Split("<@");
                //Raid
                if (split[0].Contains("Raid"))
                {
                    if (int.TryParse(split[0].Replace("Raid", string.Empty).Trim(), out int id))
                    {
                        Raid existingRaid = await _activityService.GetMongoRaidAsync(id);
                        if (existingRaid == null || existingRaid.DateTimeClosed == DateTime.MinValue || existingRaid.AwardedGlimmer || existingRaid.CreatedByUserId != user.Id) return;
                        Task.Run(async () =>
                        {
                            existingRaid.AwardedGlimmer = true;
                            await _activityService.UpdateRaidAsync(existingRaid);
                            var modifiedEmbed = new EmbedBuilder();
                            modifiedEmbed.Description = $"{newDescriptionSplit[0]} No Glimmer has been awarded as this activity did not complete.";
                            modifiedEmbed.Color = embed.Color;
                            await originalMessage.ModifyAsync(x =>
                            {
                                x.Embed = modifiedEmbed.Build();
                            });
                        });
                    }
                }
                //Fireteam
                else
                {
                    if (int.TryParse(split[0].Replace("Fireteam", string.Empty).Trim(), out int id))
                    {
                        Fireteam existingFireteam = await _activityService.GetMongoFireteamAsync(id);
                        if (existingFireteam == null || existingFireteam.DateTimeClosed == DateTime.MinValue || existingFireteam.AwardedGlimmer || existingFireteam.CreatedByUserId != user.Id) return;
                        Task.Run(async () =>
                        {
                            existingFireteam.AwardedGlimmer = true;
                            await _activityService.UpdateFireteamAsync(existingFireteam);
                            var modifiedEmbed = new EmbedBuilder();
                            modifiedEmbed.Description = $"{newDescriptionSplit[0]} No Glimmer has been awarded as this activity did not complete.";
                            modifiedEmbed.Color = embed.Color;
                            await originalMessage.ModifyAsync(x =>
                            {
                                x.Embed = modifiedEmbed.Build();
                            });
                        });
                    }
                }
            }
        }
        private async Task<Tuple<int, bool>> CalculateGlimmerForActivity(List<ActivityUser> activityUsers, ulong creatorId, bool isRaid)
        {
            if (activityUsers == null) return new Tuple<int, bool>(-1, false);
            int increaseAmount = 1 * activityUsers.Count(x => x.Reacted);
            bool awardedThisWeek = false;
            foreach (ActivityUser user in activityUsers)
            {
                if (user.UserId == creatorId)
                {
                    await _hiringService.UpdateCreatedPost(user.UserId);

                    if (isRaid)
                        awardedThisWeek = await CreatedRaidThisWeek(creatorId);
                    else 
                        awardedThisWeek = await CreatedFireteamThisWeek(creatorId);

                    //Give bonus of 3 for first activity each week.
                    if (awardedThisWeek == false)
                    {
                        await _guardianService.IncreaseGlimmerAsync(user.UserId, user.DisplayName, increaseAmount + 3);
                        Console.WriteLine($"Increased Glimmer for {user.DisplayName} by {increaseAmount + 3}");
                    }
                    else
                    {
                        await _guardianService.IncreaseGlimmerAsync(user.UserId, user.DisplayName, increaseAmount);
                        Console.WriteLine($"Increased Glimmer for {user.DisplayName} by {increaseAmount}");
                    }
                }
                else if (user.Reacted)
                {
                    await _hiringService.UpdateJoinedPost(user.UserId, DateTime.UtcNow);
                    await _guardianService.IncreaseGlimmerAsync(user.UserId, user.DisplayName, increaseAmount);
                    Console.WriteLine($"Increased Glimmer for {user.DisplayName} by {increaseAmount}");
                }
            }

            return new Tuple<int, bool>(increaseAmount, awardedThisWeek);
        }

        private async Task<bool> CreatedRaidThisWeek(ulong userId)
        {
            return await _activityService.CreatedRaidThisWeek(userId);
        }

        private async Task<bool> CreatedFireteamThisWeek(ulong userId)
        {
            return await _activityService.CreatedFireteamThisWeek(userId);
        }

        private EmbedBuilder AddActivityUsersField(EmbedBuilder embed, string title, List<ActivityUser> activityUsers)
        {
            const string PlayersTitle = "Players";
            const string SubstitutesTitle = "Subs";
            if(activityUsers == null || activityUsers.Count == 0)
            {
                embed.AddField(title, "-");
                return embed;
            }

            StringBuilder sb = new StringBuilder();
            foreach(ActivityUser user in activityUsers)
            {
                if(_glimmerEmote != null && title != SubstitutesTitle && user.Reacted)
                    sb.AppendLine($"{_glimmerEmote} {user.DisplayName}");
                else 
                    sb.AppendLine(user.DisplayName);
            }

            embed.AddField(title, sb.ToString().Trim());
            return embed;
        }

        private IRole GetRoleForChannel(SocketGuildUser user, ulong channelId)
        {
            if (user == null || user.IsBot) 
                return null;

            switch (channelId)
            {
                case RaidChannel:
                    return user.Guild.Roles.FirstOrDefault(x => x.Name == RaidRole);
                case VanguardChannel:
                    return user.Guild.Roles.FirstOrDefault(x => x.Name == VanguardRole);
                case CrucibleChannel:
                    return user.Guild.Roles.FirstOrDefault(x => x.Name == CrucibleRole);
                case GambitChannel:
                    return user.Guild.Roles.FirstOrDefault(x => x.Name == GambitRole);
                case MiscChannel:
                    return user.Guild.Roles.FirstOrDefault(x => x.Name == MiscRole);
                case DungeonChannel:
                    return user.Guild.Roles.FirstOrDefault(x => x.Name == DungeonRole);
            }

            return null;
        }

    }

    public class ActivityType
    {
        public ActivityTypes Activity { get; set; }
        public int Id { get; set; }

        public void Parse(IEmbed embed)
        {
            if (embed == null) return;
            if (embed.Footer.HasValue == false) return;
            var footerText = embed.Footer.Value.Text;
            string[] split = footerText.Split("\n");
            if(split.Length == 0) return;
            string text = split[0];
            if (string.IsNullOrEmpty(text)) return;
            int.TryParse(text.Split(" ")[2], out int id);
            if (id <= 0) return;
            Id = id;
            if (text.Contains("Raid"))
            {
                Activity = ActivityTypes.Raid;
            }
            else if (text.Contains("Fireteam"))
            {
                Activity = ActivityTypes.Fireteam;
            }
        }
        
    }

    public enum ActivityTypes
    {
        Unknown = 0,
        Raid = 1,
        Fireteam = 2
    }
}
