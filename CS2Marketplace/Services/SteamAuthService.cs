using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using CS2Marketplace.Models;

namespace CS2Marketplace.Services
{
    public class SteamAuthService
    {
        private const string SteamOpenIdEndpoint = "https://steamcommunity.com/openid/login";
        private readonly HttpClient _httpClient;

        public SteamAuthService()
        {
            _httpClient = new HttpClient();
        }

        // Builds the Steam OpenID login URL using the provided return URL.
        public string GetSteamLoginUrl(string returnUrl)
        {
            var queryParams = new[]
            {
                "openid.ns=http://specs.openid.net/auth/2.0",
                "openid.mode=checkid_setup",
                "openid.return_to=" + Uri.EscapeDataString(returnUrl),
                "openid.realm=" + Uri.EscapeDataString(new Uri(returnUrl).GetLeftPart(UriPartial.Authority)),
                "openid.identity=http://specs.openid.net/auth/2.0/identifier_select",
                "openid.claimed_id=http://specs.openid.net/auth/2.0/identifier_select"
            };

            return SteamOpenIdEndpoint + "?" + string.Join("&", queryParams);
        }

        // Validates the callback and extracts the SteamId.
        public async Task<string> ValidateSteamCallback(IQueryCollection query)
        {
            if (!query.ContainsKey("openid.claimed_id"))
                return null;

            // Prepare parameters to post back to Steam for verification.
            var postData = query.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
            postData["openid.mode"] = "check_authentication";
            var content = new FormUrlEncodedContent(postData);

            // Post back to Steam.
            var response = await _httpClient.PostAsync(SteamOpenIdEndpoint, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (responseString.Contains("is_valid:true"))
            {
                // Extract SteamId from openid.claimed_id (format: http://steamcommunity.com/openid/id/XXXXXXXXXXXXXXX)
                var claimedId = query["openid.claimed_id"].ToString();
                var steamId = claimedId.Substring(claimedId.LastIndexOf('/') + 1);
                return steamId;
            }

            return null;
        }

        // Fetches the Steam user's profile data using GetPlayerSummaries.
        public async Task<SteamUserProfile> GetUserProfileAsync(string steamId, string steamApiKey)
        {
            // Construct the URL for the GetPlayerSummaries API.
            string url = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={steamApiKey}&steamids={steamId}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();

            // Parse JSON response (expected structure: { "response": { "players": [ { "steamid": "...", "personaname": "...", "avatar": "..." } ] } })
            using var doc = JsonDocument.Parse(json);
            var players = doc.RootElement.GetProperty("response").GetProperty("players");
            if (players.GetArrayLength() > 0)
            {
                var player = players[0];
                return new SteamUserProfile
                {
                    SteamId = player.GetProperty("steamid").GetString(),
                    PersonaName = player.GetProperty("personaname").GetString(),
                    AvatarUrl = player.GetProperty("avatar").GetString()
                };
            }
            return null;
        }
    }

    // Simple DTO for the Steam user profile.
    public class SteamUserProfile
    {
        public string SteamId { get; set; }
        public string PersonaName { get; set; }
        public string AvatarUrl { get; set; }
    }
}
