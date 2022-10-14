using Boudica.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class RandomLoadout
    {
        public ElementType Subclass { get; set; }
        public Weapon KineticWeapon { get; set; }
        public Weapon SpecialWeapon { get; set; }
        public Weapon HeavyWeapon { get; set; }

        public void GenerateRandomLoadout()
        {
            Random random = new Random(); 

            Subclass = (ElementType) random.Next(0, Enum.GetValues<ElementType>().Length);

            int exoticSlot = random.Next(1, 4);
            KineticWeapon = new Weapon();
            KineticWeapon.WeaponType = KineticWeapon.GetKineticWeapon(exoticSlot == 1);
            KineticWeapon.WeaponArchtype = WeaponArchtype.Kinetic;
            KineticWeapon.IsExotic = exoticSlot == 1;

            SpecialWeapon = new Weapon();
            SpecialWeapon.WeaponType = SpecialWeapon.GetSpecialWeapon(exoticSlot == 2, KineticWeapon.WeaponType);
            SpecialWeapon.WeaponArchtype = WeaponArchtype.Special;
            SpecialWeapon.WeaponElementType = (ElementType)random.Next(0, Enum.GetValues<ElementType>().Length - 1);
            SpecialWeapon.IsExotic = exoticSlot == 2;

            HeavyWeapon = new Weapon();
            HeavyWeapon.WeaponType = HeavyWeapon.GetHeavyWeapon(exoticSlot == 3);
            HeavyWeapon.WeaponArchtype = WeaponArchtype.Heavy;
            HeavyWeapon.WeaponElementType = (ElementType)random.Next(0, Enum.GetValues<ElementType>().Length - 1);
            HeavyWeapon.IsExotic = exoticSlot == 3;
        }
    }
}
