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
            foreach (var rankRange in _rankRanges)
            {
                if (score >= rankRange.Value[0] && score <= rankRange.Value[1])
                {
                    var major = rankRange.Key;
                    var minor = (MinorRank)((score - rankRange.Value[0]) / (rankRange.Value[1] - rankRange.Value[0] + 1) * 3);
                    return new Rank(major, minor);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(score));
        }
    }

}
