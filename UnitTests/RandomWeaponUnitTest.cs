using Boudica.Commands;
using Boudica.MongoDB.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace UnitTests
{
    [TestClass]
    public class RandomWeaponUnitTest
    {
        [TestMethod]
        public void KineticTests()
        {
            Random random = new Random();
            Weapon kineticWeapon = new Weapon();
            kineticWeapon.WeaponType = kineticWeapon.GetKineticWeapon(false, random);
            Assert.IsTrue((int)kineticWeapon.WeaponType >= 0);

            Weapon specialWeapon = new Weapon();
            specialWeapon.WeaponType = specialWeapon.GetSpecialWeapon(false, kineticWeapon.WeaponType, random);
            Assert.IsTrue((int)specialWeapon.WeaponType >= 0);

            Weapon heavyWeapon = new Weapon();
            heavyWeapon.WeaponType = heavyWeapon.GetHeavyWeapon(false, random);
            Assert.IsTrue((int)heavyWeapon.WeaponType >= 0);
        }

        [TestMethod]
        public void RandomCommand()
        {
            RandomLoadoutCommands loadoutCommands = new RandomLoadoutCommands(null);
        }
    }
}