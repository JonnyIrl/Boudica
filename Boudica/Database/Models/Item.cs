using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Database.Models
{
    [Table(nameof(Item))]
    public class Item
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int DebuffPercentage { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsSecondary { get; set; }
        public bool IsSuper { get; set; }

        public string GetType()
        {
            if (IsPrimary) return "Kinectic";
            if (IsSecondary) return "Secondary";
            if (IsSuper) return "Super";
            return "N/A";
        }
    }
}
