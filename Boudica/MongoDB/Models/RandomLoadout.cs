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

        private Random Generator { get; set; }

        public RandomLoadout()
        {
            Generator = new Random();
        }

        public void GenerateRandomLoadout()
        {
            int subClass = Generator.Next(0, Enum.GetValues<ElementType>().Length);
            Subclass = (ElementType)subClass;

            int exoticSlot = 0;// random.Next(1, 4);
            KineticWeapon = new Weapon();
            KineticWeapon.WeaponType = KineticWeapon.GetKineticWeapon(exoticSlot == 1, Generator);
            KineticWeapon.WeaponArchtype = WeaponArchtype.Kinetic;
            //KineticWeapon.IsExotic = exoticSlot == 1;

            SpecialWeapon = new Weapon();
            SpecialWeapon.WeaponType = SpecialWeapon.GetSpecialWeapon(exoticSlot == 2, KineticWeapon.WeaponType, Generator);
            SpecialWeapon.WeaponArchtype = WeaponArchtype.Special;
            int specialWeapon = Generator.Next(0, Enum.GetValues<ElementType>().Length);
            SpecialWeapon.WeaponElementType = (ElementType)specialWeapon;
            //SpecialWeapon.IsExotic = exoticSlot == 2;

            HeavyWeapon = new Weapon();
            HeavyWeapon.WeaponType = HeavyWeapon.GetHeavyWeapon(exoticSlot == 3, Generator);
            HeavyWeapon.WeaponArchtype = WeaponArchtype.Heavy;
            int heavyWeapon = Generator.Next(0, Enum.GetValues<ElementType>().Length);
            HeavyWeapon.WeaponElementType = (ElementType)heavyWeapon;
            //HeavyWeapon.IsExotic = exoticSlot == 3;
        }
    }
}
