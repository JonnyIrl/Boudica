using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Helpers
{
    public sealed class BoudicaConfig
    {
        public static string FilePath { get; } = @"config.json";

        [JsonProperty("AppName")]
        public static string AppName { get; set; } = "Boudica";

        [JsonProperty("Version")]
        public static double Version { get; set; } = 1.0;

        [JsonProperty("BungieApiKey")]
        public static string BungieApiKey { get; set; } 

        [JsonProperty("BungieClientId")]
        public static string BungieClientId { get; set; }

        [JsonProperty("BungieClientSecret")]
        public static string BungieClientSecret { get; set; }
    }
}
