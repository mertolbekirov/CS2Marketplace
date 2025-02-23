using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

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

        // Constructs the Steam OpenID login URL using the provided return URL.
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

        // Validates the Steam OpenID callback and extracts the SteamId.
        public async Task<string> ValidateSteamCallback(IQueryCollection query)
        {
            if (!query.ContainsKey("openid.claimed_id"))
                return null;

            // Post back all query parameters with mode=check_authentication
            var postData = query.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
            postData["openid.mode"] = "check_authentication";
            var content = new FormUrlEncodedContent(postData);

            var response = await _httpClient.PostAsync(SteamOpenIdEndpoint, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (responseString.Contains("is_valid:true"))
            {
                // The claimed_id is in the format: http://steamcommunity.com/openid/id/XXXXXXXXXXXXXXX
                var claimedId = query["openid.claimed_id"].ToString();
                var steamId = claimedId.Substring(claimedId.LastIndexOf('/') + 1);
                return steamId;
            }
            return null;
        }
    }
}
