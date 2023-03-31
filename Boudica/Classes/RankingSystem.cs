using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Classes
{
    //    Example Useage
    //    RankingSystem rankingSystem = new RankingSystem();
    //    rankingSystem.Score(150); // Score is 150 (Bronze III)
    //    rankingSystem.Score(200); // Score is 350 (Silver III)
    //    rankingSystem.Score(450); // Score is 800 (Gold I)
    //    string currentRank = rankingSystem.GetRank();
    //    Console.WriteLine($"Current rank: {currentRank}"); // Outputs "Gold I"

    public enum RankType
    {
        BronzeIII = 1,
        BronzeII = 2,
        BronzeI = 3,
        SilverIII = 4,
        SilverII = 5,
        SilverI = 6,
        GoldIII = 7,
        GoldII = 8,
        GoldI = 9,
        DiamondIII = 10,
        DiamondII = 11,
        DiamondI = 12,
        PlatinumIII = 13,
        PlatinumII = 14,
        PlatinumI = 15
    }

    public class Rank
    {
        public RankType RankType { get; set; }
        public int MajorRank { get; set; }
        public int MinorRank { get; set; }
        public int MinimumScore { get; set; }

        public Rank(RankType rankType, int majorRank, int minorRank, int minimumScore)
        {
            RankType = rankType;
            MajorRank = majorRank;
            MinorRank = minorRank;
            MinimumScore = minimumScore;
        }
    }

    public class RankingSystem
    {
        private Dictionary<RankType, Rank> _ranks;

        public RankingSystem()
        {
            _ranks = new Dictionary<RankType, Rank>();

            // Initialize the ranks
            int score = 0;
            for (int majorRank = 1; majorRank <= 5; majorRank++)
            {
                for (int minorRank = 3; minorRank >= 1; minorRank--)
                {
                    score += CalculateMinorRankStep(majorRank, minorRank);

                    var rankType = (RankType)((majorRank - 1) * 3 + minorRank);
                    var rank = new Rank(rankType, majorRank, minorRank, score);

                    _ranks[rankType] = rank;
                }
            }
        }

        public Rank GetRank(int score)
        {
            var rankType = _ranks.Keys.LastOrDefault(k => _ranks[k].MinimumScore <= score);
            return _ranks[rankType];
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
