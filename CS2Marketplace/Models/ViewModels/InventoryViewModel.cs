namespace CS2Marketplace.Models.ViewModels
{
    public class InventoryViewModel
    {
        public List<InventoryItem> Items { get; set; } = new();
        public bool IsOwnInventory { get; set; }
        public bool HasApiKey { get; set; }
        public string TargetUsername { get; set; } = string.Empty;
        public List<string> ActiveListingIds { get; set; } = new();
    }

}
