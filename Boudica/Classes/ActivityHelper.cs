using Boudica.Commands;
using Boudica.Helpers;
using Boudica.MongoDB.Models;
using Boudica.Services;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Boudica.Classes
{
    public class ActivityHelper : InteractionModuleBase<SocketInteractionContext>
    {
        public readonly ActivityService _activityService;
        public readonly GuardianService _guardianService;
        public delegate void AddRemoveReactionDelegate(ulong userId, bool addReaction);
        public event AddRemoveReactionDelegate OnAddRemoveReaction;

        public Emoji _jEmoji = new Emoji("🇯");
        public Emoji _sEmoji = new Emoji("🇸");
        public Emote _glimmerEmote;
        public List<Emoji> _successFailEmotes = null;

        public const ulong RaidChannel = 530529729321631785;
        public const string RaidRole = "Raid Fanatics";

        public const ulong DungeonChannel = 530529123349823528;
        public const string DungeonRole = "Dungeon Challengers";

        public const ulong VanguardChannel = 530530338099691530;
        public const string VanguardRole = "Nightfall Enthusiasts";

        public const ulong CrucibleChannel = 530529088620724246;
        public const string CrucibleRole = "Crucible Contenders";

        public const ulong GambitChannel = 552184673749696512;
        public const string GambitRole = "Gambit Hustlers";

        public const ulong MiscChannel = 530528672172736515;
        public const string MiscRole = "Activity Aficionados";


#if DEBUG
        public const ulong glimmerId = 1009200271475347567;
#else
        public const ulong glimmerId = 728197708074188802;
#endif

        public const int CreatorPoints = 5;
        public CommandHandler _commandHandler;
        public ActivityHelper(IServiceProvider services, CommandHandler handler)
        {
            _activityService = services.GetRequiredService<ActivityService>();
            _guardianService = services.GetRequiredService<GuardianService>();
            _commandHandler = handler;
            if (Emote.TryParse($"<:misc_glimmer:{glimmerId}>", out _glimmerEmote) == false)
                _glimmerEmote = null;

            if (_successFailEmotes == null)
            {
                _successFailEmotes = new List<Emoji>();
                _successFailEmotes.Add(new Emoji("✅"));
                _successFailEmotes.Add(new Emoji("❌"));
            }
        }

        public async Task<bool> CheckExistingRaidIsValid(Raid existingRaid, bool forceCommand)
        {
            if (existingRaid == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a Raid with that Id").Build());
                return false;
            }

            if (existingRaid.DateTimeClosed != DateTime.MinValue)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("This raid is already closed").Build());
                return false;
            }

            if (forceCommand)
            {
                IGuildUser guildUser = Context.Guild.GetUser(Context.User.Id);
                if (guildUser != null)
                {
                    if (guildUser.GuildPermissions.ModerateMembers)
                    {
                        return true;
                    }
                }
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Only a Moderator or Admin can edit/close a raid with this command.").Build());
                return false;
            }
            else
            {
                if (existingRaid.CreatedByUserId != Context.User.Id)
                {
                    await RespondAsync(embed: EmbedHelper.CreateFailedReply("Only the Guardian who created the raid can edit/close a raid").Build());
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> CheckExistingFireteamIsValid(Fireteam existingFireteam)
        {
            if (existingFireteam == null)
            {
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Could not find a Fireteam with that Id").Build());
                return false;
            }

            if (existingFireteam.CreatedByUserId != Context.User.Id)
            {
                IGuildUser guildUser = Context.Guild.GetUser(Context.User.Id);
                if (guildUser != null)
                {
                    if (guildUser.GuildPermissions.ModerateMembers)
                    {
                        return true;
                    }
                }
                await RespondAsync(embed: EmbedHelper.CreateFailedReply("Only the Guardian who created the Fireteam or an Admin can edit/close a raid").Build());
                return false;
            }

            return true;
        }

        public List<ActivityUser> AddPlayersToNewActivity(string args, int maxCount = 5, bool removePlayer = false)
        {
            string sanitisedSplit = Regex.Replace(args, @"[(?<=\<)(.*?)(?=\>)]", string.Empty);
            List<ActivityUser> activityUsers = new List<ActivityUser>();
            if (sanitisedSplit.Contains("@") == false) return activityUsers;
            string[] users = sanitisedSplit.Split('@');
            foreach (string user in users)
            {
                string sanitisedUser = string.Empty;
                int space = user.IndexOf(' ');
                if (space == -1)
                    sanitisedUser = user.Trim();
                else
                {
                    sanitisedUser = user.Substring(0, space).Trim();
                }
                if (string.IsNullOrEmpty(sanitisedUser)) continue;
                if (IsDigitsOnly(sanitisedUser) == false) continue;

                if (ulong.TryParse(sanitisedUser, out ulong userId))
                {
                    if (userId == Context.User.Id) continue;
                    if (removePlayer)
                    {
                        activityUsers.Add(new ActivityUser(userId, string.Empty));
                    }
                    else
                    {
                        IGuildUser guildUser = Context.Guild.GetUser(userId);
                        if (guildUser != null && guildUser.IsBot == false)
                        {
                            if (activityUsers.FirstOrDefault(x => x.UserId == userId) == null)
                                activityUsers.Add(new ActivityUser(userId, guildUser.DisplayName));
                        }
                    }
                }
            }

            while (activityUsers.Count > maxCount)
            {
                activityUsers.RemoveAt(activityUsers.Count - 1);
            }
            return activityUsers;
        }

        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        public EmbedBuilder AddActivityUsersField(EmbedBuilder embed, string title, List<ActivityUser> activityUsers)
        {
            const string PlayersTitle = "Players";
            const string SubstitutesTitle = "Subs";
            if (activityUsers == null || activityUsers.Count == 0)
            {
                embed.AddField(title, "-");
                return embed;
            }

            StringBuilder sb = new StringBuilder();
            foreach (ActivityUser user in activityUsers)
            {
                if (_glimmerEmote != null && title != SubstitutesTitle && user.Reacted)
                    sb.AppendLine($"{_glimmerEmote} {user.DisplayName}");
                else
                    sb.AppendLine(user.DisplayName);
            }

            embed.AddField(title, sb.ToString().Trim());
            return embed;
        }
    }
}
