using Boudica.Classes;
using Boudica.Enums;
using Boudica.Services;
using Discord;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Boudica.Helpers
{
    public class OAuthHelper
    {
        private HttpListener _listener;
        private APIService _apiService;

        public OAuthHelper(IServiceProvider services)
        {
            _apiService = services.GetRequiredService<APIService>();
            _listener = new HttpListener();
            _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            _listener.Prefixes.Add("https://localhost:8081/");
            _listener.Start();
            _listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
            Console.WriteLine("[OAUTH] Listening...");
        }

        public async void GetToken(IAsyncResult ar)
        {
            try
            {
                if (!HttpListener.IsSupported)
                {
                    Console.WriteLine("[OAUTH] HttpListener is not supported.");
                    return;
                }

                HttpListenerContext context = _listener.EndGetContext(ar);
                //Console.WriteLine("[OAUTH] Connection Received.");

                var query = context.Request.QueryString;

                CodeResult result = new()
                {
                    DiscordDisplayName = $"Levante#3845",
                    Reason = ErrorReason.None
                };

                if (query != null && query.Count > 0)
                {
                    if (!string.IsNullOrEmpty(query["code"]))
                    {
                        var base64EncodedBytes = Convert.FromBase64String($"{query["state"]}");
                        ulong discordId = ulong.Parse(Encoding.UTF8.GetString(base64EncodedBytes));
                        result = await ProcessCode($"{query["code"]}", discordId).ConfigureAwait(false);
                    }
                    else if (!string.IsNullOrEmpty(query["error"]))
                    {
                        Console.WriteLine($"[OAUTH] Error occurred: {query["error_description"]}.");
                        _listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
                        return;
                    }
                }
                else
                {
                    result.Reason = ErrorReason.MissingParameters;
                }

                _listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
                //Console.WriteLine("[OAUTH] Sending Request.");

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                string responseString = "You are going to be redirected.";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                response.ContentLength64 = buffer.Length;
                if (result.Reason != ErrorReason.None)
                {
                    //Console.WriteLine($"[OAUTH] Redirecting to Link Fail with reason {result.Reason}.");
                    response.Redirect($"https://www.levante.dev/link-fail/?error={Convert.ToInt32(result.Reason)}");
                }
                else
                {
                    //Console.WriteLine("[OAUTH] Redirecting to Link Success.");
                    response.Redirect($"https://www.levante.dev/link-success/?discDisp={Uri.EscapeDataString(result.DiscordDisplayName)}");
                }

                // simulate work
                //await Task.Delay(500);

                try
                {
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    // You must close the output stream.
                    output.Close();
                }
                catch (Exception x)
                {
                    //Console.WriteLine("[OAUTH] Unable to send response write data.");
                }
                Console.WriteLine("[OAUTH] Flow completed. Listening...");
            }
            catch (Exception x)
            {
                _listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
            }
        }

        private async Task<CodeResult> ProcessCode(string Code, ulong DiscordID)
        {
            var result = new CodeResult()
            {
                DiscordDisplayName = $"Boudica#4601",
                Reason = ErrorReason.None
            };

            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                    { "client_id", $"{BoudicaConfig.BungieClientId}" },
                    { "client_secret", $"{BoudicaConfig.BungieClientSecret}" },
                    { "Authorization",  $"Basic {Code}" },
                    { "Content-Type", "application/x-www-form-urlencoded" },
                    { "grant_type", "authorization_code" },
                    { "code", Code },
                };
                var postContent = new FormUrlEncodedContent(values);

                var response = client.PostAsync("https://www.bungie.net/Platform/App/OAuth/Token/", postContent).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                result.Access = item.access_token;
                result.Refresh = item.refresh_token;
                try
                {
                    result.AccessExpiration = TimeSpan.FromSeconds(double.Parse($"{item.expires_in}"));
                    result.RefreshExpiration = TimeSpan.FromSeconds(double.Parse($"{item.refresh_expires_in}"));
                }
                catch
                {
                    result.Reason = ErrorReason.Unknown;
                    return result;
                }

                string bungieTag = "";
                string memId = "";
                int memType = -2;
                try
                {
                    memType = GetMembershipDataFromBungieId($"{item.membership_id}", out memId, out bungieTag);
                }
                catch
                {
                    result.Reason = ErrorReason.OldCode;
                    return result;
                }

                if (memType <= -2)
                {
                    result.Reason = ErrorReason.NoProfileDataFound;
                    return result;
                }

                IUser user = BoudicaInstance.Client.GetUser(DiscordID);
                if (user == null)
                    user = BoudicaInstance.Client.Rest.GetUserAsync(DiscordID).Result;

                if (user == null)
                {
                    result.Reason = ErrorReason.DiscordUserNotFound;
                    return result;
                }

                var auth = new EmbedAuthorBuilder()
                {
                    Name = $"Account Linking",
                };
                var foot = new EmbedFooterBuilder()
                {
                    Text = $"Powered by {BoudicaConfig.AppName} v{String.Format("{0:0.00#}", BoudicaConfig.Version)}",
                };
                var embed = new EmbedBuilder()
                {
                    Color = Color.Green,
                    Author = auth,
                    Footer = foot,
                };
                embed.Description =
                    $"Linking Successful.\n" +
                    $"Your Discord account ({user.Mention}) is now linked to **{bungieTag}**.";

                embed.AddField(x =>
                {
                    x.Name = $"> Default Platform";
                    x.Value = $"{(Platform)memType}";
                    x.IsInline = false;
                });
                try
                {
                    await user.SendMessageAsync(embed: embed.Build());
                }
                catch
                {
                    result.Reason = ErrorReason.NoDiscordMessageSent;
                    return result;
                }

                // Don't make users have to unlink to do this.
                //if (await _apiService.IsExistingLinkedUser(user.Id))
                //    DataConfig.DeleteUserFromConfig(user.Id);

                await _apiService.LinkUserToDatabase(user.Id, user.Username, memId, $"{memType}", bungieTag, result);
                result.DiscordDisplayName = $"{user.Username}#{user.Discriminator}";
                return result;
            }
        }

        private int GetMembershipDataFromBungieId(string BungieID, out string MembershipID, out string BungieTag)
        {
            ///Platform/Destiny2/254/Profile/17125100/LinkedProfiles/
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-Key", BoudicaConfig.BungieApiKey);

                string memId = "";
                string memType = "";

                var memResponse = client.GetAsync($"https://www.bungie.net/Platform/Destiny2/-1/Profile/{BungieID}/LinkedProfiles/?getAllMemberships=true").Result;
                var memContent = memResponse.Content.ReadAsStringAsync().Result;
                dynamic memItem = JsonConvert.DeserializeObject(memContent);

                var lastPlayed = new DateTime();
                var goodProfile = -1;

                if (memItem == null || memItem.ErrorCode != 1)
                {
                    BungieTag = null;
                    MembershipID = null;
                    return -2;
                }

                for (var j = 0; j < memItem.Response.profiles.Count; j++)
                {
                    if (memItem.Response.profiles[j].isCrossSavePrimary == true)
                    {
                        memType = memItem.Response.profiles[j].membershipType;
                        memId = memItem.Response.profiles[j].membershipId;
                        goodProfile = j;
                        break;
                    }

                    if (DateTime.Parse(memItem.Response.profiles[j].dateLastPlayed.ToString()) <= lastPlayed) continue;

                    lastPlayed = DateTime.Parse(memItem.Response.profiles[j].dateLastPlayed.ToString());
                    goodProfile = j;
                }

                if (goodProfile == -1)
                {
                    BungieTag = null;
                    MembershipID = null;
                    return -2;
                }

                memType = memItem.Response.profiles[goodProfile].membershipType;
                memId = memItem.Response.profiles[goodProfile].membershipId;

                Console.WriteLine($"[OAUTH] Received tokens for {memItem.Response.bnetMembership.supplementalDisplayName} on platform {memType}.");

                MembershipID = $"{memId}";
                string bungieTagCode = $"{memItem.Response.bnetMembership.bungieGlobalDisplayNameCode}".PadLeft(4, '0');
                BungieTag = $"{memItem.Response.bnetMembership.bungieGlobalDisplayName}#{bungieTagCode}";
                return int.Parse($"{memType}");
            }
        }

        public class CodeResult
        {
            public string DiscordDisplayName;
            public ErrorReason Reason;
            public string Access;
            public string Refresh;
            public TimeSpan AccessExpiration;
            public TimeSpan RefreshExpiration;
        }

        public enum ErrorReason
        {
            None,
            MissingParameters,
            OldCode,
            NoProfileDataFound,
            DiscordUserNotFound,
            NoDiscordMessageSent,
            Unknown,
        }
    }
}
