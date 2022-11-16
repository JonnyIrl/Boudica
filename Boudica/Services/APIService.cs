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
            _httpClient.BaseAddress = new Uri(" https://www.bungie.net/");
        }

        public async Task Test()
        {
            //GET https://www.bungie.net/en/oauth/authorize?client_id=12345&response_type=code&state=6i0mkLx79Hp91nzWVeHrzHG4
            string clientId = "42011";
            string clientSecret = "5asjaX53bkGaPz3v9qbEj4ds.txu8rTKiNzq7ojqSdM";

            HttpResponseMessage response = await _httpClient.GetAsync($"en/oauth/authorize?client_id={clientId}&response_type=code&state=6i0mkLx79Hp91nzWVeHrzHG4");
            string responseResult = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();
        }

        public async Task PostToken()
        {
            //platform/app/oauth/token/
            //HttpResponseMessage response = await _httpClient.PostAsync($"platform/app/oauth/token/");
            //string responseResult = response.ToString();
            //response.EnsureSuccessStatusCode();
        }
    }
}
