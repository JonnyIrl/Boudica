using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class RecruitChecklist
    {
        public DateTime DateTimeJoined { get; set; }
        public DateTime DateTimeLastActivityJoined { get; set; }
        public bool JoinedPost { get; set; }
        public bool CreatedPost { get; set; }
        public int WarningCount { get; set; }
        public List<string> Notes { get; set; }

        public RecruitChecklist()
        {
            DateTimeJoined = DateTime.UtcNow;
            Notes = new List<string>();
        }
    }
}
