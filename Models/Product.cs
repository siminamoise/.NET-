using System;

namespace ProductApp.Models
{
    public class Product
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Brand { get; set; }
        public required string SKU { get; set; }
        public ProductCategory Category { get; set; }
        public decimal Price { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public int StockQuantity { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
