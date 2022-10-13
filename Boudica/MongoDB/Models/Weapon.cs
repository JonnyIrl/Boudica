using Boudica.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class Weapon
    {
        public WeaponType WeaponType { get; set; }
        public WeaponArchtype WeaponArchtype { get; set; }
        public WeaponElementType WeaponElementType { get; set; }
        public bool IsExotic { get; set; }


        public WeaponType GetKineticWeapon(bool isExotic)
        {
            List<WeaponType> kineticWeapons = new List<WeaponType>()
            {
                WeaponType.AutoRifle,
                WeaponType.HandCannon,
                WeaponType.PulseRifle,
                WeaponType.ScoutRifle,
                WeaponType.FusionRifle,
                WeaponType.SniperRifle,
                WeaponType.Shotgun,
                WeaponType.Sidearm,
                WeaponType.SpecialGrenadeLauncher,
                WeaponType.LinearFusionRifle,
                WeaponType.SMG,
                WeaponType.Bow
            };

            Random random = new Random();
            return kineticWeapons[random.Next(0, kineticWeapons.Count)];
        }

        public WeaponType GetSpecialWeapon(bool isExotic)
        {
            List<WeaponType> specialWeapons = new List<WeaponType>()
            {
                WeaponType.AutoRifle,
                WeaponType.HandCannon,
                WeaponType.PulseRifle,
                WeaponType.ScoutRifle,
                WeaponType.FusionRifle,
                WeaponType.Sidearm,
                WeaponType.SniperRifle,
                WeaponType.Shotgun,
                WeaponType.SpecialGrenadeLauncher,
                WeaponType.LinearFusionRifle,
                WeaponType.SMG,
                WeaponType.Bow,
                WeaponType.Glaive
            };

            if(isExotic)
            {
                specialWeapons.Remove(WeaponType.SniperRifle);
            }

            Random random = new Random();
            return specialWeapons[random.Next(0, specialWeapons.Count)];
        }

        public WeaponType GetHeavyWeapon(bool isExotic)
        {
            List<WeaponType> heavyWeapons = new List<WeaponType>()
            {
                WeaponType.SniperRifle,
                WeaponType.Shotgun,
                WeaponType.MachineGun,
                WeaponType.RocketLauncher,
                WeaponType.Sword,
                WeaponType.GrenadeLauncher,
                WeaponType.LinearFusionRifle
            };

            if(isExotic == false)
            {
                heavyWeapons.Remove(WeaponType.SniperRifle);
                heavyWeapons.Remove(WeaponType.Shotgun);
            }

            Random random = new Random();
            return heavyWeapons[random.Next(0, heavyWeapons.Count)];
        }
    }
}
