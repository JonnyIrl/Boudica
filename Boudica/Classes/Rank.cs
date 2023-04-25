using Boudica.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Classes
{
    public class Rank
    {
        public MajorRank Major { get; private set; }
        public MinorRank Minor { get; private set; }

        public Rank(MajorRank major, MinorRank minor)
        {
            Major = major;
            Minor = minor;
        }
    }
}
