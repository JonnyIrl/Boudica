using Boudica.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class BotRound
    {
        public int Round { get; set; }
        public int Number { get; set; }
        public int Guess { get; set; }

        public BotRound(RoundNumber roundNumber)
        {
            Round = (int)roundNumber;
        }

        public BotRound(RoundNumber roundNumber, int number)
        {
            Round = (int)roundNumber;
            Number = number;
        }
        public BotRound(RoundNumber roundNumber, int number, int guess)
        {
            Round = (int)roundNumber;
            Number = number;
            Guess = guess;
        }
    }
}
