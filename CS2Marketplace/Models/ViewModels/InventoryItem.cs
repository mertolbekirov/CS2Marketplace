namespace CS2Marketplace.Models.ViewModels
{
    public class InventoryItem
    {
        // assetId is the unique identifier for each inventory item.
        public string AssetId { get; set; }
        public string ClassId { get; set; }
        public string InstanceId { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public float? FloatValue { get; set; }
        public int? PatternIndex { get; set; }
    }
}
