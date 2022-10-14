using Boudica.Commands;
using Boudica.MongoDB.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class RandomWeaponUnitTest
    {
        [TestMethod]
        public void KineticTests()
        {
            Weapon kineticWeapon = new Weapon();
            kineticWeapon.WeaponType = kineticWeapon.GetKineticWeapon(false);
            Assert.IsTrue((int)kineticWeapon.WeaponType >= 0);

            Weapon specialWeapon = new Weapon();
            specialWeapon.WeaponType = specialWeapon.GetSpecialWeapon(false, kineticWeapon.WeaponType);
            Assert.IsTrue((int)specialWeapon.WeaponType >= 0);

            Weapon heavyWeapon = new Weapon();
            heavyWeapon.WeaponType = heavyWeapon.GetHeavyWeapon(false);
            Assert.IsTrue((int)heavyWeapon.WeaponType >= 0);
        }

        [TestMethod]
        public void RandomCommand()
        {
            RandomLoadoutCommands loadoutCommands = new RandomLoadoutCommands(null);
        }
    }
}