using Boudica.Classes;
using Boudica.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{

    [TestClass]
    public class RankingSystemTests
    {
        private RankingSystem _rankingSystem;

        [TestInitialize]
        public void SetUp()
        {
            _rankingSystem = new RankingSystem();
        }

        [DataTestMethod]
        [DataRow(-1)]
        [DataRow(2410)]
        public void GetRank_OutOfRange_ThrowsArgumentOutOfRangeException(int score)
        {
            // Arrange

            // Act & Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => _rankingSystem.GetRank(score));
        }

        [DataTestMethod]
        [DataRow(0, MajorRank.Bronze, MinorRank.I)]
        [DataRow(50, MajorRank.Bronze, MinorRank.I)]
        [DataRow(99, MajorRank.Bronze, MinorRank.I)]
        [DataRow(100, MajorRank.Bronze, MinorRank.I)]
        [DataRow(101, MajorRank.Silver, MinorRank.I)]
        [DataRow(150, MajorRank.Silver, MinorRank.I)]
        [DataRow(299, MajorRank.Silver, MinorRank.I)]
        [DataRow(300, MajorRank.Silver, MinorRank.I)]
        [DataRow(301, MajorRank.Gold, MinorRank.I)]
        [DataRow(450, MajorRank.Gold, MinorRank.I)]
        [DataRow(599, MajorRank.Gold, MinorRank.I)]
        [DataRow(600, MajorRank.Gold, MinorRank.I)]
        [DataRow(601, MajorRank.Platinum, MinorRank.I)]
        [DataRow(900, MajorRank.Platinum, MinorRank.I)]
        [DataRow(1200, MajorRank.Platinum, MinorRank.I)]
        [DataRow(1201, MajorRank.Ascendant, MinorRank.I)]
        [DataRow(1800, MajorRank.Ascendant, MinorRank.I)]
        [DataRow(2400, MajorRank.Ascendant, MinorRank.I)]
        public void GetRank_ValidScore_ReturnsCorrectRank(int score, MajorRank expectedMajorRank, MinorRank expectedMinorRank)
        {
            // Arrange

            // Act
            var actualRank = _rankingSystem.GetRank(score);

            // Assert
            Assert.AreEqual(expectedMajorRank, actualRank.Major);
            Assert.AreEqual(expectedMinorRank, actualRank.Minor);
        }
    }
}
