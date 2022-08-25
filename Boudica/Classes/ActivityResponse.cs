using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Classes
{
    public class ActivityResponse
    {
        public bool Success { get; set; }
        public bool PreviousReaction { get; set; }

        public ActivityResponse(bool success, bool previousReaction)
        {
            Success = success;
            PreviousReaction = previousReaction;
        }
    }
}
