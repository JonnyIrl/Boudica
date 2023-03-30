using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    using Boudica.Classes;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RankingSystemTests
    {
        [TestMethod]
        public void NewRankingSystem_DefaultRankIsUnranked()
        {
            var rs = new RankingSystem();

            Assert.AreEqual(RankType.Unranked, rs.GetRank().Type);
            Assert.AreEqual(0, rs.GetRank().MajorRank);
            Assert.AreEqual(0, rs.GetRank().MinorRank);
        }

        [TestMethod]
        public void NewRankingSystem_WithScore()
        {
            var rs = new RankingSystem(200);

            Assert.AreEqual(RankType.Silver, rs.GetRank().Type);
            Assert.AreEqual(2, rs.GetRank().MajorRank);
            Assert.AreEqual(0, rs.GetRank().MinorRank);
        }

        [TestMethod]
        public void Score_IncreasesScore()
        {
            var rs = new RankingSystem(200);

            rs.Score(50);

            Assert.AreEqual(250, rs.GetScore());
        }

        [TestMethod]
        public void Score_IncreasesRank()
        {
            var rs = new RankingSystem(200);

            rs.Score(200);

            Assert.AreEqual(RankType.Gold, rs.GetRank().Type);
            Assert.AreEqual(3, rs.GetRank().MajorRank);
            Assert.AreEqual(0, rs.GetRank().MinorRank);
        }

        [TestMethod]
        public void Score_DoesNotIncreaseRank()
        {
            var rs = new RankingSystem(400);

            rs.Score(50);

            Assert.AreEqual(RankType.Gold, rs.GetRank().Type);
            Assert.AreEqual(3, rs.GetRank().MajorRank);
            Assert.AreEqual(0, rs.GetRank().MinorRank);
        }

        [TestMethod]
        public void Score_MovesToNextMajorRank()
        {
            var rs = new RankingSystem(700);

            rs.Score(100);

            Assert.AreEqual(RankType.Platinum, rs.GetRank().Type);
            Assert.AreEqual(1, rs.GetRank().MajorRank);
            Assert.AreEqual(0, rs.GetRank().MinorRank);
        }

        [TestMethod]
        public void Score_MovesToNextMinorRank()
        {
            var rs = new RankingSystem(450);

            rs.Score(50);

            Assert.AreEqual(RankType.Gold, rs.GetRank().Type);
            Assert.AreEqual(3, rs.GetRank().MajorRank);
            Assert.AreEqual(1, rs.GetRank().MinorRank);
        }
    }



}
