using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Enums
{
    public enum RaidName
    {
        [ChoiceDisplay("Deep Stone Crypt")]
        DeepStoneCrypt,
        [ChoiceDisplay("Garden of Salvation")]
        GardenOfSalvation,
        [ChoiceDisplay("King's Fall")]
        KingsFall,
        [ChoiceDisplay("Last Wish")]
        LastWish,
        [ChoiceDisplay("Root of Nightmares")]
        RootOfNightmares,
        [ChoiceDisplay("Vault of Glass")]
        VaultOfGlass,
        [ChoiceDisplay("Vow of the Disciple")]
        VowOfTheDisciple
    }
}
