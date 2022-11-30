using Boudica.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class GuardianCharacter
    {
        public int Id { get; set; }
        public GuardianClass GuardianClass { get; set; }
    }
}
