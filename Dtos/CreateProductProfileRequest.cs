using System;
using ProductApp.Models;

namespace ProductApp.Dtos
{
    public class CreateProductProfileRequest
    {
        public required string Name { get; set; }
        public required string Brand { get; set; }
        public required string SKU { get; set; }
        public ProductCategory Category { get; set; }
        public decimal Price { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string? ImageUrl { get; set; }
        public int StockQuantity { get; set; } = 1;
    }
}
