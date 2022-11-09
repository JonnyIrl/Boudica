using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Services
{
    public class APIService
    {
        private readonly HttpClient _httpClient;
        public APIService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.publicapis.org/");
        }

        public async Task Test()
        {
            HttpResponseMessage response = await _httpClient.GetAsync("entries");
            response.EnsureSuccessStatusCode();
        }
    }
}
