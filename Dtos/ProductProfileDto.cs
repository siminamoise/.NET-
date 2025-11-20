using System;

namespace ProductApp.Dtos
{
    public class ProductProfileDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Brand { get; set; }
        public required string SKU { get; set; }
        public string CategoryDisplayName { get; set; } = null!;
        public decimal Price { get; set; }
        public string FormattedPrice { get; set; } = null!;
        public DateTime ReleaseDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public int StockQuantity { get; set; }
        public string ProductAge { get; set; } = null!;
        public string BrandInitials { get; set; } = null!;
        public string AvailabilityStatus { get; set; } = null!;
    }
}
