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
        public ElementType WeaponElementType { get; set; }
        public bool IsExotic { get; set; }


        public WeaponType GetKineticWeapon(bool isExotic, Random random)
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

            return kineticWeapons[random.Next(0, kineticWeapons.Count)];
        }
        public WeaponType GetSpecialWeapon(bool isExotic, WeaponType kineticType, Random random)
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

            WeaponType randomWeaponType = specialWeapons[random.Next(0, specialWeapons.Count)];
            while(randomWeaponType == kineticType)
            {
                randomWeaponType = specialWeapons[random.Next(0, specialWeapons.Count)];
            }

            return randomWeaponType;
        }
        public WeaponType GetHeavyWeapon(bool isExotic, Random random)
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

            return heavyWeapons[random.Next(0, heavyWeapons.Count)];
        }
    }

    public static partial class Extensions
    {
        public static string ToName(this WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.AutoRifle:
                    return "Auto Rifle";
                case WeaponType.HandCannon:
                    return "Hand Cannon";
                case WeaponType.PulseRifle:
                    return "Pulse Rifle";
                case WeaponType.ScoutRifle:
                    return "Scout Rifle";
                case WeaponType.FusionRifle:
                    return "Fusion Rifle";
                case WeaponType.SniperRifle:
                    return "Sniper Rifle";
                case WeaponType.Shotgun:
                    return "Shotgun";
                case WeaponType.MachineGun:
                    return "Machine Gun";
                case WeaponType.RocketLauncher:
                    return "Rocket Launcher";
                case WeaponType.Sidearm:
                    return "Sidearm";
                case WeaponType.Sword:
                    return "Sword";
                case WeaponType.SpecialGrenadeLauncher:
                    return "Special Grendade Launcher";
                case WeaponType.GrenadeLauncher:
                    return "Grenade Launcher";
                case WeaponType.TraceRifle:
                    return "Trace Rifle";
                case WeaponType.LinearFusionRifle:
                    return "Linear Fusion Rifle";
                case WeaponType.SMG:
                    return "SMG";
                case WeaponType.Bow:
                    return "Bow";
                case WeaponType.Glaive:
                    return "Glaive";
            }

            return string.Empty;
        }
    }
}
