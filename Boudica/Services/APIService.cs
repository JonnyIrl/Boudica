using Boudica.Helpers;
using Boudica.MongoDB.Models;
using BungieSharper.Entities.Destiny.Definitions;
using BungieSharper.Entities.Destiny.Definitions.ActivityModifiers;
using BungieSharper.Entities.Destiny.Definitions.Presentation;
using BungieSharper.Entities.Destiny.Definitions.Records;
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
        public static Dictionary<long, string> Activities = new();
        public static Dictionary<long, string> Nightfalls = new();


        private const string ManifestJsonPath = "ManifestFiles/json";
        private GuardianService _guardianService;
        public string DestinyManifestVersion { get; internal set; } = "v0.0";
        public APIService(IServiceProvider services)
        {
            _guardianService = services.GetRequiredService<GuardianService>();
        }

        public async Task<Tuple<string, string>> GetValidDestinyMembership(string bungieTag)
        {
            using (HttpClient client = new HttpClient())
            {
#if DEBUG
                client.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
#else
                client.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
#endif
                string membershipType = string.Empty;
                var response = await client.GetAsync($"https://www.bungie.net/platform/Destiny2/SearchDestinyPlayer/-1/" + Uri.EscapeDataString(bungieTag));
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

                        var memResponse = await client.GetAsync($"https://www.bungie.net/Platform/Destiny2/-1/Profile/{memId}/LinkedProfiles/?getAllMemberships=true");
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
                        return new Tuple<string, string>(memItem.Response.profiles[goodProfile].membershipId, memItem.Response.profiles[goodProfile].membershipType);
                    }

                return new Tuple<string, string>(string.Empty, string.Empty);
            }
        }

        public async Task<Tuple<bool, string>> GetGuardianCharacterInformation(string membershipType, string membershipId)
        {
            using (HttpClient client = new HttpClient())
            {
#if DEBUG
                client.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
#else
                client.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
#endif

                var response = await client.GetAsync($"https://www.bungie.net/platform/Destiny2/" + (membershipType) + "/Profile/" + membershipId + "?components=100,200");
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
        }

        public async Task<Tuple<bool, string>> GetCharacterActivity(string membershipType, string membershipId, string characterId, string accessToken)
        {
            if(string.IsNullOrEmpty(characterId))
            {
                return new Tuple<bool, string>(false, "Could not find activity history for that Character.");
            }
            using (HttpClient client = new HttpClient())
            {
#if DEBUG
                client.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
#else
                client.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
#endif

                var response = await client.GetAsync($"https://www.bungie.net/platform/Destiny2/" + (membershipType) + "/Account/" + membershipId + "/Character/" + characterId + "/Stats/Activities/?count=10");
                var content = await response.Content.ReadAsStringAsync();
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
            using (HttpClient client = new HttpClient())
            {
#if DEBUG
                client.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
#else
                client.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
#endif

                var response = await client.GetAsync($"https://www.bungie.net/platform/Destiny2/SearchDestinyPlayer/{memId}/" + Uri.EscapeDataString(bungieTag));
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                isPublic = item.Response[0].isPublic;

                return isPublic;
            }
        }

        public async Task<Guardian> RefreshCode(Guardian guardian)
        {
            if (guardian == null) return null;
#if DEBUG
            var values = new Dictionary<string, string>
                {
                    { "client_id", $"{BoudicaConfig.BungieClientId}" },
                    { "client_secret", $"{BoudicaConfig.BungieClientSecret}" },
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
            using (HttpClient client = new HttpClient())
            {
                var response = await client.PostAsync("https://www.bungie.net/Platform/App/OAuth/Token/", postContent);
                var content = await response.Content.ReadAsStringAsync();
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
            }

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

        public async Task<bool> IsNewManifestVersion()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
                var response = await client.GetAsync($"https://www.bungie.net/Platform/Destiny2/Manifest/");
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                if (IsBungieAPIDown(content))
                {
                    return false;
                }
                if (item == null) return false;
                return DestinyManifestVersion != $"{item.Response.version}";
            }
        }

        public async Task<bool> DownloadNewManifestFiles()
        {
            if(Directory.Exists(ManifestJsonPath) == false)
            {
                Console.WriteLine("Manifest Directory does not exist");
                Directory.CreateDirectory(ManifestJsonPath);
            }
            else
            {
                Console.WriteLine("Manifest Directory already exists");
            }

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);
                var response = await client.GetAsync($"https://www.bungie.net/Platform/Destiny2/Manifest/");
                var content = await response.Content.ReadAsStringAsync();
                dynamic item = JsonConvert.DeserializeObject(content);

                if (IsBungieAPIDown(content))
                {
                    return false;
                }
                DestinyManifestVersion = item.Response.version;
                if (!File.Exists($"{ManifestJsonPath}{DestinyManifestVersion}.json"))
                {
                    Console.WriteLine($"Found v.{DestinyManifestVersion}. Downloading and storing locally...");
                    await File.WriteAllTextAsync($"{ManifestJsonPath}{DestinyManifestVersion}.json", JsonConvert.SerializeObject(item, Formatting.Indented));
                }
                else
                {
                    Console.WriteLine($"Found v.{DestinyManifestVersion}. No download needed.");
                }

                #region Inventory Items
                string path = item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyInventoryItemDefinition)}"];
                string fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyInventoryItemDefinition> invItemList = new();
                if (!Directory.Exists($"{ManifestJsonPath}{nameof(DestinyInventoryItemDefinition)}"))
                    Directory.CreateDirectory($"{ManifestJsonPath}{nameof(DestinyInventoryItemDefinition)}");
                if (!File.Exists($"{ManifestJsonPath}{nameof(DestinyInventoryItemDefinition)}/{fileName}"))
                {
                    Console.WriteLine($"Storing locally...{nameof(DestinyInventoryItemDefinition)}");
                    string invItemUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyInventoryItemDefinition)}"]}";
                    response = await client.GetAsync(invItemUrl);
                    content = await response.Content.ReadAsStringAsync();
                    invItemList = JsonConvert.DeserializeObject<Dictionary<string, DestinyInventoryItemDefinition>>(content);
                    await File.WriteAllTextAsync($"{ManifestJsonPath}{nameof(DestinyInventoryItemDefinition)}/{fileName}", JsonConvert.SerializeObject(invItemList, Formatting.Indented));
                }
                else
                {
                    Console.WriteLine($"{nameof(DestinyInventoryItemDefinition)} already exists");
                }
                #endregion

                #region Vendors
                // Vendors
                path = item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyVendorDefinition)}"];
                fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyVendorDefinition> vendorList = new();
                if (!Directory.Exists($"{ManifestJsonPath}{nameof(DestinyVendorDefinition)}"))
                    Directory.CreateDirectory($"{ManifestJsonPath}{nameof(DestinyVendorDefinition)}");
                if (!File.Exists($"{ManifestJsonPath}{nameof(DestinyVendorDefinition)}/{fileName}"))
                {
                    Console.WriteLine($"Storing locally... {nameof(DestinyVendorDefinition)}");
                    string vendorListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyVendorDefinition)}"]}";
                    response = await client.GetAsync(vendorListUrl);
                    content = await response.Content.ReadAsStringAsync();
                    vendorList = JsonConvert.DeserializeObject<Dictionary<string, DestinyVendorDefinition>>(content);
                    await File.WriteAllTextAsync($"{ManifestJsonPath}{nameof(DestinyVendorDefinition)}/{fileName}", JsonConvert.SerializeObject(vendorList, Formatting.Indented));
                }
                else
                {
                    Console.WriteLine($"{nameof(DestinyVendorDefinition)} already exists");
                }
                #endregion

                #region Activities
                path = item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyActivityDefinition)}"];
                fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyActivityDefinition> activityList = new();
                if (!Directory.Exists($"{ManifestJsonPath}{nameof(DestinyActivityDefinition)}"))
                    Directory.CreateDirectory($"{ManifestJsonPath}{nameof(DestinyActivityDefinition)}");
                if (!File.Exists($"{ManifestJsonPath}{nameof(DestinyActivityDefinition)}/{fileName}"))
                {
                    Console.WriteLine($"Storing locally...{nameof(DestinyActivityDefinition)}");
                    string activityListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyActivityDefinition)}"]}";
                    response = await client.GetAsync(activityListUrl);
                    content = await response.Content.ReadAsStringAsync();
                    activityList = JsonConvert.DeserializeObject<Dictionary<string, DestinyActivityDefinition>>(content);
                    await File.WriteAllTextAsync($"{ManifestJsonPath}{nameof(DestinyActivityDefinition)}/{fileName}", JsonConvert.SerializeObject(activityList, Formatting.Indented));
                }
                else
                {
                    Console.WriteLine($"{nameof(DestinyActivityDefinition)} already exists");
                    activityList = JsonConvert.DeserializeObject<Dictionary<string, DestinyActivityDefinition>>(await File.ReadAllTextAsync($"{ManifestJsonPath}{nameof(DestinyActivityDefinition)}/{fileName}"));
                }


                #endregion

                #region Places
                path = item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyPlaceDefinition)}"];
                fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyPlaceDefinition> placeList = new();
                if (!Directory.Exists($"{ManifestJsonPath}{nameof(DestinyPlaceDefinition)}"))
                    Directory.CreateDirectory($"{ManifestJsonPath}{nameof(DestinyPlaceDefinition)}");
                if (!File.Exists($"{ManifestJsonPath}{nameof(DestinyPlaceDefinition)}/{fileName}"))
                {
                    Console.WriteLine($"Storing locally...{nameof(DestinyPlaceDefinition)}");
                    string placeListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyPlaceDefinition)}"]}";
                    response = await client.GetAsync(placeListUrl);
                    content = await response.Content.ReadAsStringAsync();
                    placeList = JsonConvert.DeserializeObject<Dictionary<string, DestinyPlaceDefinition>>(content);
                    await File.WriteAllTextAsync($"{ManifestJsonPath}{nameof(DestinyPlaceDefinition)}/{fileName}", JsonConvert.SerializeObject(placeList, Formatting.Indented));
                }
                else
                {
                    Console.WriteLine($"{nameof(DestinyPlaceDefinition)} already exists");
                    placeList = JsonConvert.DeserializeObject<Dictionary<string, DestinyPlaceDefinition>>(await File.ReadAllTextAsync($"{ManifestJsonPath}{nameof(DestinyPlaceDefinition)}/{fileName}"));
                }
                #endregion

                #region Modifiers
                path = item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyActivityModifierDefinition)}"];
                fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyActivityModifierDefinition> modifierList = new();
                if (!Directory.Exists($"{ManifestJsonPath}{nameof(DestinyActivityModifierDefinition)}"))
                    Directory.CreateDirectory($"{ManifestJsonPath}{nameof(DestinyActivityModifierDefinition)}");
                if (!File.Exists($"{ManifestJsonPath}{nameof(DestinyActivityModifierDefinition)}/{fileName}"))
                {
                    Console.WriteLine($"Storing locally...{nameof(DestinyActivityModifierDefinition)}");
                    string modifierListUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyActivityModifierDefinition)}"]}";
                    response = await client.GetAsync(modifierListUrl);
                    content = await response.Content.ReadAsStringAsync();
                    modifierList = JsonConvert.DeserializeObject<Dictionary<string, DestinyActivityModifierDefinition>>(content);
                    await File.WriteAllTextAsync($"{ManifestJsonPath}{nameof(DestinyActivityModifierDefinition)}/{fileName}", JsonConvert.SerializeObject(modifierList, Formatting.Indented));
                }
                else
                {
                    Console.WriteLine($"{nameof(DestinyActivityModifierDefinition)} already exists");
                }
                #endregion

                #region Records/Triumphs
                path = item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyRecordDefinition)}"];
                fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyRecordDefinition> recordList = new();
                if (!Directory.Exists($"{ManifestJsonPath}{nameof(DestinyRecordDefinition)}"))
                    Directory.CreateDirectory($"{ManifestJsonPath}{nameof(DestinyRecordDefinition)}");
                if (!File.Exists($"{ManifestJsonPath}{nameof(DestinyRecordDefinition)}/{fileName}"))
                {
                    Console.WriteLine($"Storing locally...{nameof(DestinyRecordDefinition)}");
                    string recordUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyRecordDefinition)}"]}";
                    response = await client.GetAsync(recordUrl);
                    content = await response.Content.ReadAsStringAsync();
                    recordList = JsonConvert.DeserializeObject<Dictionary<string, DestinyRecordDefinition>>(content);
                    await File.WriteAllTextAsync($"{ManifestJsonPath}{nameof(DestinyRecordDefinition)}/{fileName}", JsonConvert.SerializeObject(recordList, Formatting.Indented));
                }
                else
                {
                    Console.WriteLine($"{nameof(DestinyRecordDefinition)} already exists");
                }
                #endregion

                #region Presentation Nodes
                path = item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyPresentationNodeDefinition)}"];
                fileName = path.Split('/').LastOrDefault();
                Dictionary<string, DestinyPresentationNodeDefinition> presentNodeList = new();
                if (!Directory.Exists($"{ManifestJsonPath}{nameof(DestinyPresentationNodeDefinition)}"))
                    Directory.CreateDirectory($"{ManifestJsonPath}{nameof(DestinyPresentationNodeDefinition)}");
                if (!File.Exists($"{ManifestJsonPath}{nameof(DestinyPresentationNodeDefinition)}/{fileName}"))
                {
                    Console.WriteLine($"Storing locally...{nameof(DestinyPresentationNodeDefinition)}");
                    string presentNodeUrl = $"https://www.bungie.net{item.Response.jsonWorldComponentContentPaths.en[$"{nameof(DestinyPresentationNodeDefinition)}"]}";
                    response = await client.GetAsync(presentNodeUrl);
                    content = await response.Content.ReadAsStringAsync();
                    presentNodeList = JsonConvert.DeserializeObject<Dictionary<string, DestinyPresentationNodeDefinition>>(content);
                    await File.WriteAllTextAsync($"{ManifestJsonPath}{nameof(DestinyPresentationNodeDefinition)}/{fileName}", JsonConvert.SerializeObject(presentNodeList, Formatting.Indented));
                }
                else
                {
                    Console.WriteLine($"{nameof(DestinyPresentationNodeDefinition)} already exists");
                }
                #endregion


                foreach (var activity in activityList)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(activity.Value.DisplayProperties.Name))
                        {
                            if (placeList.ContainsKey($"{activity.Value.PlaceHash}"))
                            {
                                Activities.Add(activity.Value.Hash, placeList[$"{activity.Value.PlaceHash}"].DisplayProperties.Name);
                                continue;
                            }
                        }

                        Activities.Add(activity.Value.Hash, activity.Value.DisplayProperties.Name);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error assigning activity list item");
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex);
                    }
                }
            }

            return true;
        }
    }
}
