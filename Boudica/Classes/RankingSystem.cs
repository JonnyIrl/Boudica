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
        private readonly Dictionary<MajorRank, int[]> _rankRanges;

        public RankingSystem()
        {
            _rankRanges = new Dictionary<MajorRank, int[]>
        {
            { MajorRank.Bronze, new[] { 0, 100 } },
            { MajorRank.Silver, new[] { 101, 300 } },
            { MajorRank.Gold, new[] { 301, 600 } },
            { MajorRank.Platinum, new[] { 601, 1200 } },
            { MajorRank.Ascendant, new[] { 1201, 2400 } }
        };
        }

        public Rank GetRank(int score)
        {
            if (score < 0 || score > 2400)
            {
                throw new ArgumentOutOfRangeException(nameof(score), "Score must be between 0 and 2400.");
            }

            var majorRank = GetMajorRank(score);
            var minorRank = GetMinorRank(score, majorRank);

            return new Rank(majorRank, minorRank);
        }

        private MajorRank GetMajorRank(int score)
        {
            if (score <= 100)
            {
                return MajorRank.Bronze;
            }
            else if (score <= 300)
            {
                return MajorRank.Silver;
            }
            else if (score <= 600)
            {
                return MajorRank.Gold;
            }
            else if (score <= 1200)
            {
                return MajorRank.Platinum;
            }
            else
            {
                return MajorRank.Ascendant;
            }
        }

        private MinorRank GetMinorRank(int score, MajorRank majorRank)
        {
            var minScore = GetMinScoreForRank(majorRank);
            var maxScore = GetMaxScoreForRank(majorRank);

            if (score < minScore + (maxScore - minScore) / 3)
            {
                return MinorRank.III;
            }
            else if (score < minScore + (maxScore - minScore) * 2 / 3)
            {
                return MinorRank.II;
            }
            else
            {
                return MinorRank.I;
            }
        }

        private int GetMinScoreForRank(MajorRank majorRank)
        {
            switch (majorRank)
            {
                case MajorRank.Bronze:
                    return 0;
                case MajorRank.Silver:
                    return 101;
                case MajorRank.Gold:
                    return 301;
                case MajorRank.Platinum:
                    return 601;
                case MajorRank.Ascendant:
                    return 1201;
                default:
                    throw new ArgumentOutOfRangeException(nameof(majorRank), "Unknown major rank.");
            }
        }

        private int GetMaxScoreForRank(MajorRank majorRank)
        {
            switch (majorRank)
            {
                case MajorRank.Bronze:
                    return 100;
                case MajorRank.Silver:
                    return 300;
                case MajorRank.Gold:
                    return 600;
                case MajorRank.Platinum:
                    return 1200;
                case MajorRank.Ascendant:
                    return 2400;
                default:
                    throw new ArgumentOutOfRangeException(nameof(majorRank), "Unknown major rank.");
            }
        }

    }

}
