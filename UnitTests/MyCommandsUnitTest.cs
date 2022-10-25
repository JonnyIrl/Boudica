using Boudica.MongoDB.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class MyCommandsUnitTest
    {
        [TestMethod]
        public void FilterRaids()
        {
            const ulong MyUserId = 244209636897456129;
            const ulong DifferentUserId = 1019141857848078357;
            const ulong GuildId = 559759505223712779;
            const ulong GuildId2 = 559759505223712775;
            List<Raid> raids = new List<Raid>();
            raids.Add(CreateRaid(MyUserId, GuildId, DifferentUserId));
            raids.Add(CreateRaid(DifferentUserId, GuildId, MyUserId));
            raids.Add(CreateRaid(DifferentUserId, GuildId, DifferentUserId));
            raids.Add(CreateRaid(MyUserId, GuildId2, DifferentUserId));

            raids.RemoveAll(x => (x.CreatedByUserId != MyUserId && x.Players.Any(x => x.UserId == MyUserId) == false) || x.GuidId != GuildId);
            Assert.AreEqual(2, raids.Count);
        }

        private Raid CreateRaid(ulong createdByUserId, ulong guildId, ulong playerId)
        {
            return new Raid()
            {
                CreatedByUserId = createdByUserId,
                GuidId = guildId,
                DateTimeCreated = DateTime.Now,
                Id = 7,
                Players = new List<ActivityUser>()
                {
                    new ActivityUser(playerId, "DisplayName")
                }
            };
        }
    }
}
