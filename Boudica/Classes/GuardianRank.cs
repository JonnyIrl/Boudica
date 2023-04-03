using Boudica.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Classes
{
    public class GuardianRank
    {
        public RankType Rank { get; set; }
        public int ScoreCap { get; set; }

        public GuardianRank(RankType rank, int score)
        {
            Rank = rank;
            ScoreCap = score;
        }
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
