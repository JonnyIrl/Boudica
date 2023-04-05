using Boudica.Enums;
using Boudica.Enums.Bungie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Extensions
{
    public static class Extensions
    {
        public static string ToName(this ActivityModeType type)
        {
            switch (type)
            {
                case ActivityModeType.None:
                    return "None";
                case ActivityModeType.Story:
                    return "Story";
                case ActivityModeType.Strike:
                    return "Strike";
                case ActivityModeType.Raid:
                    return "Raid";
                case ActivityModeType.AllPvP:
                    return "PvP";
                case ActivityModeType.Patrol:
                    return "Patrol";
                case ActivityModeType.AllPvE:
                    return "PvE";
                case ActivityModeType.Control:
                    return "Control";
                case ActivityModeType.Clash:
                    return "Clash";
                case ActivityModeType.CrimsonDoubles:
                    return "Crimson Doubles";
                case ActivityModeType.Nightfall:
                    return "Nightfall";
                case ActivityModeType.HeroicNightfall:
                    return "Heroic Nightfall";
                case ActivityModeType.AllStrikes:
                    return "All Strikes";
                case ActivityModeType.IronBanner:
                    return "Iron Banner";
                case ActivityModeType.Reserved9:
                case ActivityModeType.Reserved11:
                case ActivityModeType.Reserved13:
                case ActivityModeType.Reserved20:
                case ActivityModeType.Reserved21:
                case ActivityModeType.Reserved22:
                case ActivityModeType.Reserved24:
                case ActivityModeType.Reserved26:
                case ActivityModeType.Reserved27:
                case ActivityModeType.Reserved28:
                case ActivityModeType.Reserved29:
                case ActivityModeType.Reserved30:
                    return type.ToString();
                case ActivityModeType.AllMayhem:
                    return "All Mayhem";
                case ActivityModeType.Supremacy:
                    return "Supremacy";
                case ActivityModeType.PrivateMatchesAll:
                    return "Private Match";
                case ActivityModeType.Survival:
                    return "Survival";
                case ActivityModeType.Countdown:
                    return "Countdown";
                case ActivityModeType.TrialsOfTheNine:
                    return "Trials of the Nine";
                case ActivityModeType.Social:
                    return "Social";
                case ActivityModeType.TrialsCountdown:
                    return "Trials Countdown";
                case ActivityModeType.TrialsSurvival:
                    return "Trials Survival";
                case ActivityModeType.IronBannerControl:
                    return "Iron Banner Control";
                case ActivityModeType.IronBannerClash:
                    return "Iron Banner Clash";
                case ActivityModeType.IronBannerSupremacy:
                    return "Iron Banner Supremacy";
                case ActivityModeType.ScoredNightfall:
                    return "Scored Nightfall";
                case ActivityModeType.ScoredHeroicNightfall:
                    return "Scored Heroic Nightfall";
                case ActivityModeType.Rumble:
                    return "Rumble";
                case ActivityModeType.AllDoubles:
                    return "Doubles";
                case ActivityModeType.Doubles:
                    return "Doubles";
                case ActivityModeType.PrivateMatchesClash:
                    return "Private Match - Clash";
                case ActivityModeType.PrivateMatchesControl:
                    return "Private Match - Control";
                case ActivityModeType.PrivateMatchesSupremacy:
                    return "Private Match - Supremacy";
                case ActivityModeType.PrivateMatchesCountdown:
                    return "Private Match - Countdown";
                case ActivityModeType.PrivateMatchesSurvival:
                    return "Private Match - Survival";
                case ActivityModeType.PrivateMatchesMayhem:
                    return "Private Match - Mayhem";
                case ActivityModeType.PrivateMatchesRumble:
                    return "Private Match - Rumble";
                case ActivityModeType.HeroicAdventure:
                    return "Heroic Adventure";
                case ActivityModeType.Showdown:
                    return "Showdown";
                case ActivityModeType.Lockdown:
                    return "Lockdown";
                case ActivityModeType.Scorched:
                    return "Scorched";
                case ActivityModeType.ScorchedTeam:
                    return "Team Scorched";
                case ActivityModeType.Gambit:
                    return "Gambit";
                case ActivityModeType.AllPvECompetitive:
                    return "PvE Competitive";
                case ActivityModeType.Breakthrough:
                    return "Breakthrough";
                case ActivityModeType.BlackArmoryRun:
                    return "Black Armoury";
                case ActivityModeType.Salvage:
                    return "Salvage";
                case ActivityModeType.IronBannerSalvage:
                    return "Iron Banner Salvage";
                case ActivityModeType.PvPCompetitive:
                    return "PvP Competitive";
                case ActivityModeType.PvPQuickplay:
                    return "PvP Quickplay";
                case ActivityModeType.ClashQuickplay:
                    return "Clash Quickplay";
                case ActivityModeType.ClashCompetitive:
                    return "Clash Competitive";
                case ActivityModeType.ControlQuickplay:
                    return "Control Quickplay";
                case ActivityModeType.ControlCompetitive:
                    return "Control Competitive";
                case ActivityModeType.GambitPrime:
                    return "Gambit Prime";
                case ActivityModeType.Reckoning:
                    return "Reckoning";
                case ActivityModeType.Menagerie:
                    return "Menagerie";
                case ActivityModeType.VexOffensive:
                    return "Vex Offensive";
                case ActivityModeType.NightmareHunt:
                    return "Nightmare Hunt";
                case ActivityModeType.Elimination:
                    return "Elimination";
                case ActivityModeType.Momentum:
                    return "Momentum";
                case ActivityModeType.Dungeon:
                    return "Dungeon";
                case ActivityModeType.Sundial:
                    return "Sundial";
                case ActivityModeType.TrialsOfOsiris:
                    return "Trials of Osiris";
                case ActivityModeType.Dares:
                    return "Dares";
                case ActivityModeType.Offensive:
                    return "Offensive";
                case ActivityModeType.LostSector:
                    return "Lost Sector";
                case ActivityModeType.Rift:
                    return "Rift";
                case ActivityModeType.ZoneControl:
                    return "Zone Control";
                case ActivityModeType.IronBannerRift:
                    return "Iron Banner Rift";
                default:
                    return "None";
            }

            return "None";
        }

        public static string ToName(this RoundNumber roundNumber)
        {
            switch (roundNumber)
            {
                case RoundNumber.FirstRound:
                    return "Round One";
                case RoundNumber.SecondRound:
                    return "Round Two";
                case RoundNumber.FinalRound:
                    return "Final Round";
                case RoundNumber.GameOverRound:
                    return "Game Over";
                default:
                    return "-";
            }
        }

        public static string ToName(this RankType type)
        {
            switch (type)
            {
                case RankType.BronzeIII:
                    return "Bronze III";
                case RankType.BronzeII:
                    return "Bronze II";
                case RankType.BronzeI:
                    return "Bronze I";
                case RankType.SilverIII:
                    return "Silver III";
                case RankType.SilverII:
                    return "Silver II";
                case RankType.SilverI:
                    return "Silver I";
                case RankType.GoldIII:
                    return "Gold III";
                case RankType.GoldII:
                    return "Gold II";
                case RankType.GoldI:
                    return "Gold I";
                case RankType.DiamondIII:
                    return "Diamond III";
                case RankType.DiamondII:
                    return "Diamond II";
                case RankType.DiamondI:
                    return "Diamond I";
                case RankType.PlatinumIII:
                    return "Platinum III";
                case RankType.PlatinumII:
                    return "Platinum II";
                case RankType.PlatinumI:
                    return "Platinum I";
            }

            return "Not found";
        }

        public static string ToName(this RaidName raid)
        {
            switch (raid)
            {
                case RaidName.DeepStoneCrypt:
                    return "Deep Stone Crypt";
                case RaidName.GardenOfSalvation:
                    return "Garden of Salvation";
                case RaidName.KingsFall:
                    return "King's Fall";
                case RaidName.LastWish:
                    return "Last Wish";
                case RaidName.RootOfNightmares:
                    return "Root of Nightmares";
                case RaidName.VaultOfGlass:
                    return "Vault of Glass";
                case RaidName.VowOfTheDisciple:
                    return "Vow of the Disciple";
                default:
                    return string.Empty;
            }
        }
    }
}
