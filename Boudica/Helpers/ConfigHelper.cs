using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Helpers
{
    public class ConfigHelper
    {
        public static bool LoadConfig()
        {
            BoudicaConfig boudicaConfig;
            if (File.Exists(BoudicaConfig.FilePath))
            {
                string json = File.ReadAllText(BoudicaConfig.FilePath);
                boudicaConfig = JsonConvert.DeserializeObject<BoudicaConfig>(json);
                return true;
            }
            else
            {
                boudicaConfig = new BoudicaConfig();
                Console.Error.WriteLine("No config file found");
                return false;
            }

        }
    }
}
