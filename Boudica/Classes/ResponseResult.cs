using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Classes
{
    public class ResponseResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public ResponseResult(bool result, string message)
        {
            Success = result;
            Message = message;
        }
    }
}
