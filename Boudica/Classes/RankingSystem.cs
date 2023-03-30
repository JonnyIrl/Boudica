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
        Unranked,
        Bronze,
        Silver,
        Gold,
        Platinum,
        Diamond
    }

    public class Rank
    {
        public RankType Type { get; set; }
        public int MajorRank { get; set; }
        public int MinorRank { get; set; }

        public Rank(RankType type, int majorRank, int minorRank)
        {
            Type = type;
            MajorRank = majorRank;
            MinorRank = minorRank;
        }

        public override string ToString()
        {
            return $"{Type} {MajorRank}.{MinorRank}";
        }
    }

    public class RankingSystem
    {
        private int _score;
        private Rank _rank;

        private static readonly Dictionary<RankType, int[]> _rankThresholds = new Dictionary<RankType, int[]> {
        { RankType.Bronze, new int[] { 0, 100 } },
        { RankType.Silver, new int[] { 101, 300 } },
        { RankType.Gold, new int[] { 301, 700 } },
        { RankType.Platinum, new int[] { 701, 1500 } },
        { RankType.Diamond, new int[] { 1501, int.MaxValue } }
    };

        public RankingSystem()
        {
            _score = 0;
            _rank = new Rank(RankType.Unranked, 0, 0);
        }

        public RankingSystem(int score)
        {
            _score = score;
            _rank = CalculateRank(score);
        }

        public void Score(int points)
        {
            _score += points;
            var newRank = CalculateRank(_score);

            if (newRank.Type > _rank.Type || (newRank.Type == _rank.Type && newRank.MajorRank > _rank.MajorRank))
            {
                _rank = newRank;
            }
        }

        public Rank GetRank()
        {
            return _rank;
        }

        public int GetScore()
        {
            return _score;
        }

        private static Rank CalculateRank(int score)
        {
            foreach (var rank in Enum.GetValues(typeof(RankType)).Cast<RankType>().Reverse())
            {
                var thresholds = _rankThresholds[rank];
                if (score >= thresholds[0] && score <= thresholds[1])
                {
                    var majorRank = (score - thresholds[0]) / ((thresholds[1] - thresholds[0]) / 3) + 1;
                    var minorRank = (score - thresholds[0]) % ((thresholds[1] - thresholds[0]) / 3) + 1;
                    return new Rank(rank, majorRank, minorRank);
                }
            }

            return new Rank(RankType.Unranked, 0, 0);
        }
    }



}
