using BungieSharper.Entities.Destiny.Definitions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Helpers
{
    public static class ManifestHelper
    {
        public static Dictionary<long, DestinyActivityDefinition> DestinyActivityDefinitions = new Dictionary<long, DestinyActivityDefinition>();
        public static Dictionary<long, string> Activities = new();
        public static void Load()
        {
            
            if (File.Exists("ManifestFiles/json/DestinyActivityDefinition.json"))
            {
                string json = File.ReadAllText("ManifestFiles/json/DestinyActivityDefinition.json");
                DestinyActivityDefinitions = JsonConvert.DeserializeObject<Dictionary<long, DestinyActivityDefinition>>(json);
                if (string.IsNullOrEmpty(json) == false)
                {
                    //dynamic items = JsonConvert.DeserializeObject<dynamic>(json);
                    //var results = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    

                }
            }
        }
    }
}
