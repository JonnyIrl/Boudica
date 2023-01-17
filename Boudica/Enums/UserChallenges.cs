using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Enums
{
    public enum UserChallenges
    {
        [ChoiceDisplay("Rock Paper Scissors")]
        RockPaperScissors = 0,
        [ChoiceDisplay("Random Number between 1-100")]
        RandomNumber = 1,
        [ChoiceDisplay("Higher or Lower vs Boudica! Get 3 in a row to double your wager")]
        HigherOrLower = 2,
    }
}
