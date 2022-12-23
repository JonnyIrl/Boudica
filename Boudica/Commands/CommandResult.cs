using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Commands
{
    public class CommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public CommandResult(bool result, string message)
        {
            Success = result;
            Message = message;
        }
    }
}
