using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CS2Marketplace.Models.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace CS2Marketplace.Services
{
    public class SteamApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _csfloatApiKey;
        private readonly IMemoryCache _cache;

        public SteamApiService(HttpClient httpClient, IConfiguration configuration, IMemoryCache memoryCache)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _csfloatApiKey = configuration["CsFloat:ApiKey"];
            if (string.IsNullOrEmpty(_csfloatApiKey))
            {
                throw new ArgumentException("CsFloat ApiKey is not set in configuration.");
            }
        }

        // Fetches the Steam user's profile (display name and avatar) using GetPlayerSummaries.
        public async Task<SteamUserProfile?> GetUserProfileAsync(string steamId, string steamApiKey)
        {
            string url = $"http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={steamApiKey}&steamids={steamId}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("response", out var responseElement) ||
                !responseElement.TryGetProperty("players", out var playersElement) ||
                playersElement.ValueKind != JsonValueKind.Array ||
                playersElement.GetArrayLength() == 0)
            {
                return null;
            }

            var player = playersElement.EnumerateArray().First();
            return new SteamUserProfile
            {
                SteamId = player.GetProperty("steamid").GetString() ?? string.Empty,
                PersonaName = player.GetProperty("personaname").GetString() ?? string.Empty,
                AvatarUrl = player.GetProperty("avatar").GetString() ?? string.Empty
            };
        }

        // Fetches the player's inventory.
        // If forceRefresh is false, the inventory is retrieved from cache (keyed by "inventory_{steamId}").
        // For each item with an inspect link, we concurrently request float data via the csfloat API.
        public async Task<List<InventoryItem>?> GetPlayerInventoryAsync(string steamId, string appId, string contextId, bool forceRefresh = false)
        {
            string cacheKey = $"inventory_{steamId}";
            if (!forceRefresh && _cache.TryGetValue(cacheKey, out List<InventoryItem> cachedInventory))
            {
                return cachedInventory;
            }

            string url = $"https://steamcommunity.com/inventory/{steamId}/{appId}/{contextId}?l=english&count=5000";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("assets", out var assetsElement) ||
                !doc.RootElement.TryGetProperty("descriptions", out var descriptionsElement))
            {
                return null;
            }

            var assets = assetsElement.EnumerateArray().ToList();
            var descriptions = descriptionsElement.EnumerateArray().ToList();
            var inventoryItems = new List<InventoryItem>();

            // Create a list to hold inspect tasks.
            var inspectTasks = new List<Task<(string inspectLink, FloatData? floatData, InventoryItem item)>>();

            foreach (var asset in assets)
            {
                string assetId = asset.GetProperty("assetid").GetString() ?? string.Empty;
                string classId = asset.GetProperty("classid").GetString() ?? string.Empty;
                string instanceId = asset.GetProperty("instanceid").GetString() ?? string.Empty;

                // Find the matching description.
                var description = descriptions.FirstOrDefault(d =>
                    d.GetProperty("classid").GetString() == classId &&
                    d.GetProperty("instanceid").GetString() == instanceId);

                if (description.ValueKind != JsonValueKind.Undefined)
                {
                    string name = description.TryGetProperty("name", out var nameProp)
                        ? nameProp.GetString() ?? "Unknown"
                        : "Unknown";
                    string iconUrl = description.TryGetProperty("icon_url", out var iconProp)
                        ? iconProp.GetString() ?? string.Empty
                        : string.Empty;

                    // Retrieve the inspect link.
                    string? inspectLink = null;
                    if (description.TryGetProperty("inspect_link", out var inspectLinkProp))
                    {
                        inspectLink = inspectLinkProp.GetString();
                        inspectLink = inspectLink.Replace("%owner_steamid%", steamId);
                        inspectLink = inspectLink.Replace("%assetid%", assetId);
                    }
                    else if (description.TryGetProperty("actions", out var actionsProp) &&
                             actionsProp.ValueKind == JsonValueKind.Array)
                    {
                        var firstAction = actionsProp.EnumerateArray().FirstOrDefault();
                        if (firstAction.ValueKind == JsonValueKind.Object &&
                            firstAction.TryGetProperty("link", out var linkProp))
                        {
                            inspectLink = linkProp.GetString();
                            inspectLink = inspectLink.Replace("%owner_steamid%", steamId);
                            inspectLink = inspectLink.Replace("%assetid%", assetId);
                        }
                    }

                    var item = new InventoryItem
                    {
                        AssetId = assetId,
                        ClassId = classId,
                        InstanceId = instanceId,
                        Name = name,
                        ImageUrl = !string.IsNullOrEmpty(iconUrl)
                            ? "https://steamcommunity-a.akamaihd.net/economy/image/" + iconUrl
                            : string.Empty,
                        FloatValue = null,
                        PatternIndex = null
                    };
                    inventoryItems.Add(item);

                    // If an inspect link exists, queue up an asynchronous request.
                    if (!string.IsNullOrEmpty(inspectLink))
                    {
                        inspectTasks.Add(Task.Run(async () =>
                        {
                            var floatData = await GetFloatDataForInspectUrlAsync(inspectLink);
                            return (inspectLink, floatData, item);
                        }));
                    }
                }
            }

            // Await all float data requests concurrently.
            var results = await Task.WhenAll(inspectTasks);

            // Map the results back to inventory items.
            foreach (var (inspectLink, floatData, item) in results)
            {
                if (floatData != null)
                {
                    item.FloatValue = floatData.Float;
                    item.PatternIndex = floatData.PatternIndex;
                }
                else
                {
                    Console.WriteLine($"Failed to retrieve float data for inspect link: {inspectLink}");
                }
            }

            // Cache the complete inventory for 5 minutes.
            _cache.Set(cacheKey, inventoryItems, TimeSpan.FromMinutes(5));
            return inventoryItems;
        }

        /// <summary>
        /// Calls the csfloat API for a single inspect URL using GET.
        /// Caches the result using the inspect URL as the key.
        /// </summary>
        private async Task<FloatData?> GetFloatDataForInspectUrlAsync(string inspectUrl)
        {
            if (_cache.TryGetValue(inspectUrl, out FloatData cached))
            {
                return cached;
            }

            string requestUrl = "https://api.csfloat.com/?url=" + WebUtility.UrlEncode(inspectUrl);
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Origin", "https://csfloat.com");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"CSFloat API call failed with status code {response.StatusCode} for URL {inspectUrl}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            try
            {
                var csResponse = JsonSerializer.Deserialize<CsfloatResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (csResponse?.ItemInfo != null)
                {
                    var result = new FloatData
                    {
                        Float = csResponse.ItemInfo.FloatValue,
                        PatternIndex = csResponse.ItemInfo.PaintIndex
                    };
                    _cache.Set(inspectUrl, result, TimeSpan.FromMinutes(30));
                    return result;
                }
                else
                {
                    Console.WriteLine("CSFloat API returned no iteminfo.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deserializing CSFloat API response: " + ex.Message);
                return null;
            }
        }

        // DTO for the csfloat response.
        private class CsfloatResponse
        {
            public CsfloatItemInfo ItemInfo { get; set; }
        }

        private class CsfloatItemInfo
        {
            public int Origin { get; set; }
            public int Quality { get; set; }
            public int Rarity { get; set; }
            public int PaintSeed { get; set; }
            public int Defindex { get; set; }
            public int PaintIndex { get; set; }
            public float FloatValue { get; set; }
            public float Min { get; set; }
            public float Max { get; set; }
            // Other properties (weapon_type, item_name, etc.) can be added if needed.
            public string S { get; set; }
            public string A { get; set; }
            public string D { get; set; }
            public string M { get; set; }
        }

        // Public DTO for float data.
        public class FloatData
        {
            public float? Float { get; set; }
            public int? PatternIndex { get; set; }
        }
    }

    // DTO for Steam user profile.
    public class SteamUserProfile
    {
        public string SteamId { get; set; } = string.Empty;
        public string PersonaName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
    }
}
