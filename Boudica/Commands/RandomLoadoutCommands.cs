using Boudica.Enums;
using Boudica.MongoDB.Models;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class RandomLoadoutCommands: ModuleBase
    {
        private Emote _kineticEmote;
        private Emote _arcEmote;
        private Emote _solarEmote;
        private Emote _voidEmote;
        private Emote _statisEmote;
        private Emote _exoticEmote;

#if DEBUG
        private const ulong KineticId = 1030219078436126802;
        private const ulong ArcId = 1030219045615702107;
        private const ulong SolarId= 1030219104289828974;
        private const ulong VoidId = 1030219152599826524;
        private const ulong StasisId = 1030219128054763660;
        private const ulong ExoticId = 1030222368108445788;
#else 
        private const ulong KineticId = 950740332738404402;
        private const ulong ArcId = 950740402204463124;
        private const ulong SolarId= 950740378254999632;
        private const ulong VoidId = 950740430524383232;
        private const ulong StasisId = 950740457690914836;
        private const ulong ExoticId = 701629432657608777;
#endif

        public RandomLoadoutCommands(IServiceProvider services)
        {
            Emote.TryParse($"<:elkinetic:{KineticId}>", out _kineticEmote);
            Emote.TryParse($"<:elarc:{ArcId}>", out _arcEmote);
            Emote.TryParse($"<:elsolar:{SolarId}>", out _solarEmote);
            Emote.TryParse($"<:elvoid:{VoidId}>", out _voidEmote);
            Emote.TryParse($"<:elstasis:{StasisId}>", out _statisEmote);
            Emote.TryParse($"<:engram5:{ExoticId}>", out _exoticEmote);
        }

        [Command("random loadout pve")]
        public async Task RandomLoadoutPvE()
        {
            RandomLoadout loadout = new RandomLoadout();
            loadout.GenerateRandomLoadout();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{GetElementTypeEmote(loadout.Subclass)} Subclass");
            sb.Append("");
            sb.Append($"{_kineticEmote} {loadout.KineticWeapon.WeaponType.ToName()}");
            if (loadout.KineticWeapon.IsExotic)
                sb.Append($" {_exoticEmote}");
            sb.AppendLine("");
            sb.Append($"{GetElementTypeEmote(loadout.SpecialWeapon.WeaponElementType)} {loadout.SpecialWeapon.WeaponType.ToName()}");
            if (loadout.SpecialWeapon.IsExotic)
                sb.Append($" {_exoticEmote}");
            sb.AppendLine("");
            sb.Append($"{GetElementTypeEmote(loadout.HeavyWeapon.WeaponElementType)} {loadout.HeavyWeapon.WeaponType.ToName()}");
            if (loadout.HeavyWeapon.IsExotic)
                sb.Append($" {_exoticEmote}");

            var embed = new EmbedBuilder();
            embed.Title = "Random Loadout";
            embed.Description = "If you do not have an exotic or it is not valid just use a legendary version. The same goes for elements";
            //embed.AddField("Your Loadout", sb.ToString());

            embed.AddField("Subclass", GetElementTypeEmote(loadout.Subclass));
            if(loadout.KineticWeapon.IsExotic)
                embed.AddField("Kinetic", $"{loadout.KineticWeapon.WeaponType.ToName()} {_exoticEmote}", true);
            else
                embed.AddField("Kinetic", $"{loadout.KineticWeapon.WeaponType.ToName()}", true);

            if (loadout.SpecialWeapon.IsExotic)
                embed.AddField("Special", $"{GetElementTypeEmote(loadout.SpecialWeapon.WeaponElementType)} {loadout.SpecialWeapon.WeaponType.ToName()} {_exoticEmote}", true);
            else
                embed.AddField("Special", $"{GetElementTypeEmote(loadout.SpecialWeapon.WeaponElementType)} {loadout.SpecialWeapon.WeaponType.ToName()}", true);

            if (loadout.HeavyWeapon.IsExotic)
                embed.AddField("Heavy", $"{GetElementTypeEmote(loadout.SpecialWeapon.WeaponElementType)} {loadout.HeavyWeapon.WeaponType.ToName()} {_exoticEmote}", true);
            else
                embed.AddField("Heavy", $"{GetElementTypeEmote(loadout.SpecialWeapon.WeaponElementType)} {loadout.HeavyWeapon.WeaponType.ToName()}", true);

            await RespondAsync(embed: embed.Build());
        }


        private Emote GetElementTypeEmote(ElementType elementType)
        {
            switch (elementType)
            {
                case ElementType.Arc:
                    return _arcEmote;
                case ElementType.Solar:
                    return _solarEmote;
                case ElementType.Void:
                    return _voidEmote;
                case ElementType.Statis:
                    return _statisEmote;
                default:
                    return null;
            }
        }





    }
}
