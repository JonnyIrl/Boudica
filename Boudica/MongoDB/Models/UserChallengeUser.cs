using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class UserChallengeUser
    {
        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public string Answer { get; set; }
    }
}
