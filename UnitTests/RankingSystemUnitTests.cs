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
        public void GetRank_ShouldReturnBronzeIII_WhenScoreIs0()
        {
            var rankingSystem = new RankingSystem();
            var rank = rankingSystem.GetRank(0);

            Assert.AreEqual(RankType.BronzeIII, rank.RankType);
        }

        [TestMethod]
        public void GetRank_ShouldReturnBronzeIII_WhenScoreIs50()
        {
            var rankingSystem = new RankingSystem();
            var rank = rankingSystem.GetRank(50);

            Assert.AreEqual(RankType.BronzeIII, rank.RankType);
        }

        [TestMethod]
        public void GetRank_ShouldReturnBronzeII_WhenScoreIs100()
        {
            var rankingSystem = new RankingSystem();
            var rank = rankingSystem.GetRank(100);

            Assert.AreEqual(RankType.BronzeII, rank.RankType);
        }

        [TestMethod]
        public void GetRank_ShouldReturnBronzeI_WhenScoreIs150()
        {
            var rankingSystem = new RankingSystem();
            var rank = rankingSystem.GetRank(150);

            Assert.AreEqual(RankType.BronzeI, rank.RankType);
        }

        [TestMethod]
        public void GetRank_ShouldReturnSilverIII_WhenScoreIs200()
        {
            var rankingSystem = new RankingSystem();
            var rank = rankingSystem.GetRank(200);

            Assert.AreEqual(RankType.SilverIII, rank.RankType);
        }

        [TestMethod]
        public void GetRank_ShouldReturnSilverII_WhenScoreIs300()
        {
            var rankingSystem = new RankingSystem();
            var rank = rankingSystem.GetRank(300);

            Assert.AreEqual(RankType.SilverII, rank.RankType);
        }

        [TestMethod]
        public void GetRank_ShouldReturnSilverI_WhenScoreIs400()
        {
            var rankingSystem = new RankingSystem();
            var rank = rankingSystem.GetRank(400);

            Assert.AreEqual(RankType.SilverI, rank.RankType);
        }

        [TestMethod]
        public void GetRank_ShouldReturnGoldIII_WhenScoreIs500()
        {
            var rankingSystem = new RankingSystem();
            var rank = rankingSystem.GetRank(500);

            Assert.AreEqual(RankType.GoldIII, rank.RankType);
        }

        [TestMethod]
        public void GetRank_ShouldReturnGoldII_WhenScoreIs600()
        {
            var rankingSystem = new RankingSystem();
            var rank = rankingSystem.GetRank(600);

            Assert.AreEqual(RankType.GoldII, rank.RankType);
        }

        [TestMethod]
        public void GetRank_ShouldReturnGoldI_WhenScoreIs700()
        {
            var rankingSystem = new RankingSystem();
            var rank = rankingSystem.GetRank(700);

            Assert.AreEqual(RankType.GoldI, rank.RankType);
        }

        [TestMethod]
        public void GetRank_ShouldReturnDiamondIII_WhenScoreIs800()
        {
            var rankingSystem = new RankingSystem();
            var rank = rankingSystem.GetRank(800);

            Assert.AreEqual(RankType.DiamondIII, rank.RankType);
        }

        [TestMethod]
        public void GetRank_ShouldReturnDiamondII_WhenScoreIs1200()
        {
            var rankingSystem = new RankingSystem();
            var rank = rankingSystem.GetRank(1200);

            Assert.AreEqual(RankType.DiamondII, rank.RankType);
        }
    }
}
