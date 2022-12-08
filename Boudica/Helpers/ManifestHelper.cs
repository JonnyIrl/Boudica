using Boudica.Services;
using BungieSharper.Entities.Destiny.Definitions;
using Microsoft.Extensions.DependencyInjection;
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
        public static Dictionary<long, string> Activities = new();
        public static Dictionary<long, string> Nightfalls = new();

        public static void LoadManifestInfo(APIService apiService)
        {
            Console.WriteLine("Getting Manifest Files");
            var result = apiService.DownloadNewManifestFiles().Result;
            Console.WriteLine("Done getting Manifest Files " + result);
        }
    }
}
