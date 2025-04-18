﻿namespace CS2Marketplace.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string ExternalItemId { get; set; }  // This could be a Steam asset id for the item type
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Rarity { get; set; }  // or condition/quality
        public float? FloatValue { get; set; }
        public int? PatternIndex { get; set; }
        public string InspectLink { get; set; }  // Steam inspect link for the item
    }
}
