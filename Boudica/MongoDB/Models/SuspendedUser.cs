using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.MongoDB.Models
{
    public class SuspendedUser
    {
        public ulong Id { get; set; }
        public DateTime DateTimeSuspendedUntil { get; set; }
        public ulong SuspendedByUserId { get; set; }

        public SuspendedUser(ulong id, DateTime dateTimeSuspendedUntil, ulong suspendedById)
        {
            Id = id;
            DateTimeSuspendedUntil = dateTimeSuspendedUntil;
            SuspendedByUserId = suspendedById;
        }
    }
}
