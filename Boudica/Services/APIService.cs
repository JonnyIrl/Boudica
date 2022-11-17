using Boudica.Helpers;
using Boudica.MongoDB.Models;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
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
        private GuardianService _guardianService;
        public APIService(IServiceProvider services)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(" https://www.bungie.net/");
            _guardianService = services.GetRequiredService<GuardianService>();
        }

        public async Task Test()
        {
            //GET https://www.bungie.net/en/oauth/authorize?client_id=12345&response_type=code&state=6i0mkLx79Hp91nzWVeHrzHG4

            //HttpResponseMessage response = await _httpClient.GetAsync($"en/oauth/authorize?client_id={clientId}&response_type=code&state=6i0mkLx79Hp91nzWVeHrzHG4");
            //string responseResult = await response.Content.ReadAsStringAsync();
            //response.EnsureSuccessStatusCode();
        }

        public async Task<Tuple<string, string>> GetValidDestinyMembership(string bungieTag)
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
            string membershipType = string.Empty;
            var response = await _httpClient.GetAsync($"https://www.bungie.net/platform/Destiny2/SearchDestinyPlayer/-1/" + Uri.EscapeDataString(bungieTag));
            var content = response.Content.ReadAsStringAsync().Result;
            dynamic item = JsonConvert.DeserializeObject(content);

            if (IsBungieAPIDown(content))
            {
                return new Tuple<string, string>(string.Empty, membershipType);
            }

            if (item != null)
                for (var i = 0; i < item.Response.Count; i++)
                {
                    string memId = item.Response[i].membershipId;

                    var memResponse = await _httpClient.GetAsync($"https://www.bungie.net/Platform/Destiny2/-1/Profile/{memId}/LinkedProfiles/?getAllMemberships=true");
                    var memContent = await memResponse.Content.ReadAsStringAsync();
                    dynamic memItem = JsonConvert.DeserializeObject(memContent);

                    var lastPlayed = new DateTime();
                    var goodProfile = -1;

                    if (memItem == null || memItem.ErrorCode != 1) continue;

                    for (var j = 0; j < memItem.Response.profiles.Count; j++)
                    {
                        if (memItem.Response.profiles[j].isCrossSavePrimary == true)
                        {
                            membershipType = memItem.Response.profiles[j].membershipType;
                            return new Tuple<string, string>(memId, membershipType);
                        }

                        if (DateTime.Parse(memItem.Response.profiles[j].dateLastPlayed.ToString()) <= lastPlayed) continue;

                        lastPlayed = DateTime.Parse(memItem.Response.profiles[j].dateLastPlayed.ToString());
                        goodProfile = j;
                    }

                    if (goodProfile == -1) continue;
                    return new Tuple<string,string>(memItem.Response.profiles[goodProfile].membershipId, memItem.Response.profiles[goodProfile].membershipType);
                }

            return new Tuple<string, string>(string.Empty, string.Empty);
        }
       

        public bool IsBungieAPIDown(string JSONContent)
        {
            dynamic item = JsonConvert.DeserializeObject(JSONContent);
            string status = item.ErrorStatus;
            return !status.Equals("Success");
        }

        public async Task<bool> IsPublicAccount(string bungieTag, int memId)
        {
            bool isPublic = false;

            _httpClient.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);

            var response = await _httpClient.GetAsync($"https://www.bungie.net/platform/Destiny2/SearchDestinyPlayer/{memId}/" + Uri.EscapeDataString(bungieTag));
            var content = response.Content.ReadAsStringAsync().Result;
            dynamic item = JsonConvert.DeserializeObject(content);

            isPublic = item.Response[0].isPublic;

            return isPublic;
        }

        public async Task<Guardian> RefreshCode(Guardian guardian)
        {
            var values = new Dictionary<string, string>
                {
                    { "client_id", $"{BoudicaConfig.BungieClientId}" },
                    { "client_secret", $"{BoudicaConfig.BungieClientSecret}" },
                    { "Content-Type", "application/x-www-form-urlencoded" },
                    { "grant_type", "refresh_token" },
                    { "refresh_token", $"{guardian.RefreshToken}" }
                };
            var postContent = new FormUrlEncodedContent(values);

            var response = await _httpClient.PostAsync("https://www.bungie.net/Platform/App/OAuth/Token/", postContent);
            var content = response.Content.ReadAsStringAsync().Result;
            dynamic item = JsonConvert.DeserializeObject(content);

            if (item.refresh_token == null || item.access_token == null)
            {
                Console.WriteLine($"Received null tokens from refresh; keep all tokens the same as before.");
                return guardian;
            }

            guardian.RefreshToken = item.refresh_token;
            guardian.AccessToken = item.access_token;
            guardian.AccessExpiration = DateTime.Now.Add(TimeSpan.FromSeconds(double.Parse($"{item.expires_in}")));
            guardian.RefreshExpiration = DateTime.Now.Add(TimeSpan.FromSeconds(double.Parse($"{item.refresh_expires_in}")));

            await _guardianService.UpdateGuardianTokens(guardian.Id, guardian.AccessToken, guardian.RefreshToken, guardian.AccessExpiration, guardian.RefreshExpiration);
            return guardian;
        }

        public async Task<Guardian> GetLinkedUser(ulong discordId)
        {
            Guardian guardian = await _guardianService.GetGuardian(discordId);
            if (guardian == null)
                return null;
            if (guardian.AccessToken == null)
                return null;
            if (guardian.AccessToken.Equals("[ACCESS TOKEN]"))
                return null;
            if (guardian.Id == discordId)
            {
                if (guardian.RefreshExpiration < DateTime.Now)
                {
                    return null;
                }
                else if (guardian.AccessExpiration < DateTime.Now)
                {
                    guardian = await RefreshCode(guardian);
                }
                return guardian;
            }
            return null;
        }

        public async Task AddUserToConfig(ulong discordId, string userName, string MembershipID, string MembershipType, string BungieName, OAuthHelper.CodeResult CodeResult)
        {
            Guardian existingGuardian = await _guardianService.GetGuardian(discordId);
            if (existingGuardian == null)
            {
                existingGuardian = new Guardian()
                {
                    Id = discordId,
                    Glimmer = 0,
                    Username = userName,
                    BungieMembershipId = MembershipID,
                    BungieMembershipType = MembershipType,
                    UniqueBungieName = BungieName,
                    AccessToken = CodeResult.Access,
                    RefreshToken = CodeResult.Refresh,
                    AccessExpiration = DateTime.Now.Add(CodeResult.AccessExpiration),
                    RefreshExpiration = DateTime.Now.Add(CodeResult.RefreshExpiration)
                };

                await _guardianService.InsertGuardian(existingGuardian);
            }
            else
            {
                existingGuardian.BungieMembershipId = MembershipID;
                existingGuardian.BungieMembershipType = MembershipType;
                existingGuardian.UniqueBungieName = BungieName;
                existingGuardian.AccessToken = CodeResult.Access;
                existingGuardian.RefreshToken = CodeResult.Refresh;
                existingGuardian.AccessExpiration = DateTime.Now.Add(CodeResult.AccessExpiration);
                existingGuardian.RefreshExpiration = DateTime.Now.Add(CodeResult.RefreshExpiration);
                await _guardianService.UpdateGuardian(existingGuardian);
            }
        }

        public async Task<bool> IsExistingLinkedUser(ulong discordId)
        {
            Guardian guardian = await _guardianService.GetGuardian(discordId);
            if (guardian == null)
                return false;

            if (guardian.AccessToken == null || guardian.AccessToken.Equals("[ACCESS TOKEN]"))
                return false;

            return true;
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
