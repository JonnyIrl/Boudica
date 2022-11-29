using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Classes
{
    public class OAuthResult
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string Reason { get; set; }
        public TimeSpan AccessTokenExpiry { get; set; }
        public TimeSpan RefreshTokenExpiry { get; set; }
    }
}
