using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Structs
{
    public struct UserBotChallengeResult
    {
        public int GlimmerWon { get; set; } = 0;
        public int GlimmerLost { get; set; } = 0;
        public int GamesWon { get; set; } = 0;
        public int GamesLost { get; set; } = 0;
    }
}
