using Boudica.Classes;
using Discord;
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

        public OAuthHelper()
        {
            _listener = new HttpListener();
            _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
            _listener.Prefixes.Add("http://*:8080/");
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
                //LogHelper.ConsoleLog("[OAUTH] Connection Received.");

                var query = context.Request.QueryString;
                string resultReason = "None";

                OAuthResult result = new()
                {
                    Reason = "None"
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
                   Console.WriteLine("ErrorReason.MissingParameters");
                }

                _listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
                //LogHelper.ConsoleLog("[OAUTH] Sending Request.");

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                string responseString = "You are going to be redirected.";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                response.ContentLength64 = buffer.Length;
                if (result.Reason != "None")
                {
                    //LogHelper.ConsoleLog($"[OAUTH] Redirecting to Link Fail with reason {result.Reason}.");
                    response.Redirect($"https://www.levante.dev/link-fail/?error={Convert.ToInt32(result.Reason)}");
                }
                else
                {
                    //LogHelper.ConsoleLog("[OAUTH] Redirecting to Link Success.");
                    response.Redirect($"https://www.levante.dev/link-success/?discDisp={Uri.EscapeDataString("JonnyIrl")}");
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
                    //LogHelper.ConsoleLog("[OAUTH] Unable to send response write data.");
                }
                Console.WriteLine("[OAUTH] Flow completed. Listening...");
            }
            catch (Exception x)
            {
                _listener.BeginGetContext(new AsyncCallback(GetToken), _listener);
            }
        }

        private async Task<OAuthResult> ProcessCode(string Code, ulong DiscordID)
        {
            var result = new OAuthResult() { Reason = "None" };

            string clientId = "42024";
            string clientSecret = "5asjaX53bkGaPz3v9qbEj4ds.txu8rTKiNzq7ojqSdM";

            using (var client = new HttpClient())
            {
                var values = new Dictionary<string, string>
                {
                    { "client_id", $"{clientId}" },
                    { "client_secret", $"{clientSecret}" },
                    { "Authorization",  $"Basic {Code}" },
                    { "Content-Type", "application/x-www-form-urlencoded" },
                    { "grant_type", "authorization_code" },
                    { "code", Code },
                };
                var postContent = new FormUrlEncodedContent(values);

                var response = client.PostAsync("https://www.bungie.net/Platform/App/OAuth/Token/", postContent).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                dynamic item = JsonConvert.DeserializeObject(content);

                result.AccessToken = item.access_token;
                result.RefreshToken = item.refresh_token;
                try
                {
                    result.AccessTokenExpiry = TimeSpan.FromSeconds(double.Parse($"{item.expires_in}"));
                    result.RefreshTokenExpiry = TimeSpan.FromSeconds(double.Parse($"{item.refresh_expires_in}"));
                }
                catch
                {
                    result.Reason = "Unknown";
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
                    result.Reason = "OldCode";
                    return result;
                }

                if (memType <= -2)
                {
                    result.Reason = "NoProfileDataFound";
                    return result;
                }

                IUser user = LevanteCordInstance.Client.GetUser(DiscordID);
                if (user == null)
                    user = LevanteCordInstance.Client.Rest.GetUserAsync(DiscordID).Result;

                if (user == null)
                {
                    result.Reason = "DiscordUserNotFound";
                    return result;
                }

                var auth = new EmbedAuthorBuilder()
                {
                    Name = $"Account Linking",
                };

                var embed = new EmbedBuilder()
                {
                    Color = Color.Green,
                    Author = auth,
                };
                embed.Description =
                    $"Linking Successful.\n" +
                    $"Your Discord account ({user.Mention}) is now linked to **{bungieTag}**.";

                embed.AddField(x =>
                {
                    x.Name = $"> Default Platform";
                    x.Value = $"{(Guardian.Platform)memType}";
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
                //if (DataConfig.IsExistingLinkedUser(user.Id))
                //    DataConfig.DeleteUserFromConfig(user.Id);

                //DataConfig.AddUserToConfig(user.Id, memId, $"{memType}", bungieTag, result);
                //Link up User

                result.DiscordDisplayName = $"{user.Username}#{user.Discriminator}";
                return result;
            }
        }
    }
}
