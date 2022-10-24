using Boudica.Enums;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Enums
{
    public enum TrialsMap
    {
        [ChoiceDisplay("Altar of Flame")]
        AltarofFlame,
        [ChoiceDisplay("Bannerfall")]
        Bannerfall,
        [ChoiceDisplay("Burnout")]
        Burnout,
        [ChoiceDisplay("Cathedral of Dusk")]
        CathedralofDusk,
        [ChoiceDisplay("Disjunction")]
        Disjunction,
        [ChoiceDisplay("Distant Shore")]
        DistantShore,
        [ChoiceDisplay("Endless Vale")]
        EndlessVale,
        [ChoiceDisplay("Eternity")]
        Eternity,
        [ChoiceDisplay("Exodus Blue")]
        ExodusBlue,
        [ChoiceDisplay("Fragment")]
        Fragment,
        [ChoiceDisplay("Javelin-4")]
        Javelin4,
        [ChoiceDisplay("Midtown")]
        Midtown,
        [ChoiceDisplay("Pacifica")]
        Pacifica,
        [ChoiceDisplay("Radiant Cliffs")]
        RadiantCliffs,
        [ChoiceDisplay("Rusted Lands")]
        RustedLands,
        [ChoiceDisplay("The Dead Cliffs")]
        TheDeadCliffs,
        [ChoiceDisplay("The Fortress")]
        TheFortress,
        [ChoiceDisplay("Twilight Gap")]
        TwilightGap,
        [ChoiceDisplay("Vostok")]
        Vostok,
        [ChoiceDisplay("Widow’s Court")]
        WidowsCourt,
        [ChoiceDisplay("Wormhaven")]
        Wormhaven
    }
}

public static partial class Extensions
{
    public static string ToName(this TrialsMap trialsMap)
    {
        switch (trialsMap)
        {
            case TrialsMap.AltarofFlame:
                return("Altar of Flame");
            case TrialsMap.Bannerfall:
                return("Bannerfall");
            case TrialsMap.Burnout:
                return("Burnout");
            case TrialsMap.CathedralofDusk:
                return("Cathedral of Dusk");
            case TrialsMap.Disjunction:
                return("Disjunction");
            case TrialsMap.DistantShore:
                return("Distant Shore");
            case TrialsMap.EndlessVale:
                return("Endless Vale");
            case TrialsMap.Eternity:
                return("Eternity");
            case TrialsMap.ExodusBlue:
                return("Exodus Blue");
            case TrialsMap.Fragment:
                return("Fragment");
            case TrialsMap.Javelin4:
                return("Javelin-4");
            case TrialsMap.Midtown:
                return("Midtown");
            case TrialsMap.Pacifica:
                return("Pacifica");
            case TrialsMap.RadiantCliffs:
                return("Radiant Cliffs");
            case TrialsMap.RustedLands:
                return("Rusted Lands");
            case TrialsMap.TheDeadCliffs:
                return("The Dead Cliffs");
            case TrialsMap.TheFortress:
                return("The Fortress");
            case TrialsMap.TwilightGap:
                return("Twilight Gap");
            case TrialsMap.Vostok:
                return("Vostok");
            case TrialsMap.WidowsCourt:
                return("Widow’s Court");
            case TrialsMap.Wormhaven:
                return("Wormhaven");
            default:
                return("No map");
        }
    }
}
