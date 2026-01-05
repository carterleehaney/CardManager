using System;

namespace CardManager.Models
{
    public class Card
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal? MarketPrice { get; set; }
        public decimal? LowestPrice { get; set; }
        public decimal? LatestSalePrice { get; set; }
        public DateTime? LatestSaleDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public int Amount { get; set; } = 1;

        // Display properties
        public string CategoryDisplay => string.IsNullOrEmpty(Category) ? "—" : Category;
        public string MarketPriceDisplay => MarketPrice.HasValue ? $"${MarketPrice:N2}" : "N/A";
        public string LowestPriceDisplay => LowestPrice.HasValue ? $"${LowestPrice:N2}" : "N/A";
        public string LatestSaleDisplay => LatestSalePrice.HasValue ? $"${LatestSalePrice:N2}" : "N/A";
        public string LatestSaleDateDisplay => LatestSaleDate.HasValue ? LatestSaleDate.Value.ToString("MM/dd/yy h:mm tt") : "N/A";
        public string LastUpdatedDisplay => LastUpdated.ToString("MM/dd h:mm tt");
        
        // For backwards compatibility
        public decimal? Price => MarketPrice ?? LowestPrice;
        public string PriceDisplay => Price.HasValue ? $"${Price:N2}" : "N/A";

        // Total value for this card (Price × Amount)
        public decimal? TotalValue => Price.HasValue ? Price.Value * Amount : null;
        public string TotalValueDisplay => TotalValue.HasValue ? $"${TotalValue:N2}" : "N/A";
    }
}
