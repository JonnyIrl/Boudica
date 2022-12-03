﻿using Boudica.Helpers;
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
        private const string ManifestJsonPath = "ManifestFiles/json";
        private readonly HttpClient _httpClient;
        private GuardianService _guardianService;
        public APIService(IServiceProvider services)
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://www.bungie.net/");
            _guardianService = services.GetRequiredService<GuardianService>();
        }

        public async Task<Tuple<bool, string>> GetManifestInformation()
        {
#if DEBUG
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.DebugBungieApiKey);
#else
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
#endif

            var response = await _httpClient.GetAsync($"https://www.bungie.net/Platform/Destiny2/Manifest/");
            var content = response.Content.ReadAsStringAsync().Result;
            dynamic item = JsonConvert.DeserializeObject(content);

            string destinyManifestVersion = item.Response.version;
            if (!File.Exists($"{ManifestJsonPath}/{destinyManifestVersion}.json"))
            {
                Console.WriteLine($"Found new manifest v.{destinyManifestVersion}.");
                File.WriteAllText($"{ManifestJsonPath}/{destinyManifestVersion}.json", JsonConvert.SerializeObject(item, Formatting.Indented));
            }
            else
            {
                Console.WriteLine($"Found existing manifest v.{destinyManifestVersion}. No need to download.");
            }

            // Activities
            string path = item.Response.jsonWorldComponentContentPaths.en["DestinyActivityDefinition"];
            string fileName = path.Split('/').LastOrDefault();
            if (!Directory.Exists($"{ManifestJsonPath}/DestinyActivityDefinition"))
                Directory.CreateDirectory($"{ManifestJsonPath}/DestinyActivityDefinition");
            //if (!File.Exists($"{ManifestJsonPath}/DestinyActivityDefinition/{fileName}"))
            //{
                Console.WriteLine($"[MANIFEST] Storing DestinyActivityDefinition locally...");
                string activityListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en["DestinyActivityDefinition"]}";
                response = await _httpClient.GetAsync(activityListUrl);
                content = response.Content.ReadAsStringAsync().Result;
            dynamic result2 = JsonConvert.DeserializeObject(content);
            foreach(var result3 in result2)
            {
                Dictionary<long, string> dictionaryResults = JsonConvert.DeserializeObject<Dictionary<long, string>>(result3);
            }
            
                var activityList = JsonConvert.DeserializeObject<Dictionary<string, string>>(content);
            int breakHere = 0;

            //}

            return new Tuple<bool, string>(true, content);
        }

        public async Task<Tuple<string, string>> GetValidDestinyMembership(string bungieTag)
        {
#if DEBUG
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.DebugBungieApiKey);
#else
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
#endif
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
       
        public async Task<Tuple<bool, string>> GetGuardianCharacterInformation(string membershipType, string membershipId)
        {
#if DEBUG
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.DebugBungieApiKey);
#else
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
#endif

            var response = await _httpClient.GetAsync($"https://www.bungie.net/platform/Destiny2/" + (membershipType) + "/Profile/" + membershipId + "?components=100,200");
            var content = response.Content.ReadAsStringAsync().Result;
            dynamic item = JsonConvert.DeserializeObject(content);

            if (IsBungieAPIDown(content))
            {
                return new Tuple<bool, string>(false, "Bungie API is temporary down, try again later.");
            }

            if (item.ErrorCode != 1)
            {
                return new Tuple<bool, string>(false, $"An error occured with that account. Is there a connected Destiny 2 account?");
            }

            return new Tuple<bool, string>(true, content);
        }

        public async Task<Tuple<bool, string>> GetCharacterActivity(string membershipType, string membershipId, string characterId)
        {
#if DEBUG
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.DebugBungieApiKey);
#else
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
#endif
            ///Destiny2/{membershipType}/Account/{destinyMembershipId}/Character/{characterId}/Stats/Activities/ 
            var response = await _httpClient.GetAsync($"https://www.bungie.net/platform/Destiny2/" + (membershipType) + "/Account/" + membershipId + "/Character/" + characterId + "/Stats/Activities/?count=10");
            var content = response.Content.ReadAsStringAsync().Result;
            dynamic item = JsonConvert.DeserializeObject(content);

            if (IsBungieAPIDown(content))
            {
                return new Tuple<bool, string>(false, "Bungie API is temporary down, try again later.");
            }

            if (item.ErrorCode != 1)
            {
                return new Tuple<bool, string>(false, $"An error occured with that account. Is there a connected Destiny 2 account?");
            }

            return new Tuple<bool, string>(true, content);
        }

        public bool IsBungieAPIDown(string jsonResponse)
        {
            dynamic item = JsonConvert.DeserializeObject(jsonResponse);
            string status = item?.ErrorStatus;
            return status != "Success";
        }

        public async Task<bool> IsPublicAccount(string bungieTag, int memId)
        {
            bool isPublic = false;

#if DEBUG
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.DebugBungieApiKey);
#else
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
#endif

            var response = await _httpClient.GetAsync($"https://www.bungie.net/platform/Destiny2/SearchDestinyPlayer/{memId}/" + Uri.EscapeDataString(bungieTag));
            var content = response.Content.ReadAsStringAsync().Result;
            dynamic item = JsonConvert.DeserializeObject(content);

            isPublic = item.Response[0].isPublic;

            return isPublic;
        }

        public async Task<Guardian> RefreshCode(Guardian guardian)
        {
            if (guardian == null) return null;
#if DEBUG
            var values = new Dictionary<string, string>
                {
                    { "client_id", $"{BoudicaConfig.DebugBungieClientId}" },
                    { "client_secret", $"{BoudicaConfig.DebugBungieClientSecret}" },
                    { "Content-Type", "application/x-www-form-urlencoded" },
                    { "grant_type", "refresh_token" },
                    { "refresh_token", $"{guardian.RefreshToken}" }
                };
#else
            var values = new Dictionary<string, string>
                {
                    { "client_id", $"{BoudicaConfig.BungieClientId}" },
                    { "client_secret", $"{BoudicaConfig.BungieClientSecret}" },
                    { "Content-Type", "application/x-www-form-urlencoded" },
                    { "grant_type", "refresh_token" },
                    { "refresh_token", $"{guardian.RefreshToken}" }
                };
#endif
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

        public async Task LinkUserToDatabase(ulong discordId, string userName, string MembershipID, string MembershipType, string BungieName, OAuthHelper.CodeResult CodeResult)
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
