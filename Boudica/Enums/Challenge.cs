using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Enums
{
    public enum Challenge
    {
        [ChoiceDisplay("Rock Paper Scissors")]
        RockPaperScissors = 0,
        [ChoiceDisplay("Random Number between 1-100")]
        RandomNumber = 1,
    }
}
