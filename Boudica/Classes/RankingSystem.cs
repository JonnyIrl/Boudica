using Boudica.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Classes
{ 
    public class RankingSystem
    {
        private static Dictionary<RankType, int> _ranks;

        public RankingSystem()
        {
            if (_ranks == null)
            {
                _ranks = new Dictionary<RankType, int>();
                for (int majorRank = 1; majorRank <= 5; majorRank++)
                {
                    for (int minorRank = 3; minorRank >= 1; minorRank--)
                    {
                        int scoreCapForRank = CalculateMinorRankStep(majorRank, minorRank);
                        RankType rankType = (RankType)((majorRank - 1) * 3 + minorRank);
                        _ranks[rankType] = scoreCapForRank;
                    }
                }
            }
        }

        public GuardianRank GetRank(int score)
        {
            return new GuardianRank(RankType.BronzeIII, 0);
        }

        private int CalculateMinorRankStep(int majorRank, int minorRank)
        {
            switch (majorRank)
            {
                case 1:
                    return minorRank == 3 ? 100 : minorRank == 2 ? 200 : 300;
                case 2:
                    return minorRank == 3 ? 200 : minorRank == 2 ? 400 : 600;
                case 3:
                    return minorRank == 3 ? 400 : minorRank == 2 ? 800 : 1200;
                case 4:
                    return minorRank == 3 ? 800 : minorRank == 2 ? 1600 : 2400;
                case 5:
                    return minorRank == 3 ? 1600 : minorRank == 2 ? 3200 : 4800;
                default:
                    return 0;
            }
        }
    }
}
