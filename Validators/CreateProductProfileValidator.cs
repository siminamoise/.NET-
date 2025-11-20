using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductApp.Dtos;
using ProductApp.Models;

namespace ProductApp.Validators
{
    public class CreateProductProfileValidator : AbstractValidator<CreateProductProfileRequest>
    {
        private static readonly string[] InappropriateWords =
        {
            "badword", "xxx", "curse" // poți schimba după chef
        };

        private static readonly string[] HomeRestrictedWords =
        {
            "weapon", "explosive", "dangerous"
        };

        private static readonly string[] TechnologyKeywords =
        {
            "tech", "smart", "device", "phone", "laptop", "tablet", "digital", "console"
        };

        private readonly ApplicationContext _context;
        private readonly ILogger<CreateProductProfileValidator> _logger;

        public CreateProductProfileValidator(
            ApplicationContext context,
            ILogger<CreateProductProfileValidator> logger)
        {
            _context = context;
            _logger = logger;

            // Name
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .MinimumLength(1)
                .MaximumLength(200)
                .Must(BeValidName).WithMessage("Product name contains inappropriate content.")
                .MustAsync(BeUniqueName).WithMessage("A product with the same name already exists for this brand.");

            // Brand
            RuleFor(x => x.Brand)
                .NotEmpty().WithMessage("Brand is required.")
                .MinimumLength(2)
                .MaximumLength(100)
                .Must(BeValidBrandName).WithMessage("Brand name contains invalid characters.");

            // SKU
            RuleFor(x => x.SKU)
                .NotEmpty().WithMessage("SKU is required.")
                .Must(BeValidSKU).WithMessage("SKU must be 5-20 characters, alphanumeric with hyphens.")
                .MustAsync(BeUniqueSKU).WithMessage("SKU already exists in the system.");

            // Category
            RuleFor(x => x.Category)
                .IsInEnum().WithMessage("Invalid product category.");

            // Price
            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0.")
                .LessThan(10000).WithMessage("Price must be less than 10,000.");

            // ReleaseDate
            RuleFor(x => x.ReleaseDate)
                .Must(d => d >= new DateTime(1900, 1, 1))
                    .WithMessage("Release date cannot be before 1900.")
                .Must(d => d <= DateTime.UtcNow)
                    .WithMessage("Release date cannot be in the future.");

            // StockQuantity
            RuleFor(x => x.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.")
                .LessThanOrEqualTo(100000).WithMessage("Stock quantity cannot exceed 100,000.");

            // ImageUrl
            When(x => !string.IsNullOrWhiteSpace(x.ImageUrl), () =>
            {
                RuleFor(x => x.ImageUrl!)
                    .Must(BeValidImageUrl).WithMessage("ImageUrl must be a valid HTTP/HTTPS image URL ending with an image extension.");
            });

            // Business rules (async)
            RuleFor(x => x)
                .MustAsync(PassBusinessRules)
                .WithMessage("Product business rules are not satisfied.");

            // Conditional validation

            // Electronics
            When(x => x.Category == ProductCategory.Electronics, () =>
            {
                RuleFor(x => x.Price)
                    .GreaterThanOrEqualTo(50m)
                    .WithMessage("Electronics products must have a minimum price of $50.");

                RuleFor(x => x.Name)
                    .Must(ContainTechnologyKeywords)
                    .WithMessage("Electronics products must contain technology-related keywords in the name.");

                RuleFor(x => x.ReleaseDate)
                    .Must(d => d >= DateTime.UtcNow.AddYears(-5))
                    .WithMessage("Electronics products must be released within the last 5 years.");
            });

            // Home
            When(x => x.Category == ProductCategory.Home, () =>
            {
                RuleFor(x => x.Price)
                    .LessThanOrEqualTo(200m)
                    .WithMessage("Home products must not exceed $200.");

                RuleFor(x => x.Name)
                    .Must(BeAppropriateForHome)
                    .WithMessage("Home product name contains inappropriate terms.");
            });

            // Clothing
            When(x => x.Category == ProductCategory.Clothing, () =>
            {
                RuleFor(x => x.Brand)
                    .MinimumLength(3)
                    .WithMessage("Clothing brand name must be at least 3 characters.");
            });

            // Cross-field

            RuleFor(x => x)
                .Must(p => !(p.Price > 100m && p.StockQuantity > 20))
                .WithMessage("Expensive products (over $100) must have limited stock (≤ 20 units).");

            RuleFor(x => x)
                .Must(p => p.Category != ProductCategory.Electronics ||
                           p.ReleaseDate >= DateTime.UtcNow.AddYears(-5))
                .WithMessage("Electronics must be recent (released within 5 years).");
        }

        // === Helper methods ===

        private bool BeValidName(string name)
        {
            var lower = name.ToLowerInvariant();
            return !InappropriateWords.Any(w => lower.Contains(w));
        }

        private async Task<bool> BeUniqueName(CreateProductProfileRequest request, string name, CancellationToken ct)
        {
            var exists = await _context.Products
                .AnyAsync(p => p.Name == name && p.Brand == request.Brand, ct);

            if (exists)
            {
                _logger.LogInformation("BeUniqueName failed for Name={Name}, Brand={Brand}", name, request.Brand);
                return false;
            }

            return true;
        }

        private bool BeValidBrandName(string brand)
        {
            // letters, numbers, spaces, hyphens, apostrophes, dots
            var pattern = @"^[\p{L}0-9\s\-\.'’]+$";
            return Regex.IsMatch(brand, pattern);
        }

        private bool BeValidSKU(string sku)
        {
            sku = sku.Replace(" ", string.Empty);
            var pattern = "^[A-Za-z0-9-]{5,20}$";
            return Regex.IsMatch(sku, pattern);
        }

        private async Task<bool> BeUniqueSKU(string sku, CancellationToken ct)
        {
            _logger.LogInformation("Checking SKU uniqueness for {SKU}", sku);

            var exists = await _context.Products
                .AnyAsync(p => p.SKU == sku, ct);

            if (exists)
            {
                _logger.LogWarning("SKU {SKU} already exists.", sku);
                return false;
            }

            return true;
        }

        private bool BeValidImageUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                return false;

            var lower = uri.AbsolutePath.ToLowerInvariant();
            return lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") ||
                   lower.EndsWith(".png") || lower.EndsWith(".gif") ||
                   lower.EndsWith(".webp");
        }

        private async Task<bool> PassBusinessRules(CreateProductProfileRequest request, CancellationToken ct)
        {
            // Rule 1: Daily product addition limit (max 500 per day)
            var today = DateTime.UtcNow.Date;
            var todaysCount = await _context.Products
                .CountAsync(p => p.CreatedAt.Date == today, ct);

            if (todaysCount >= 500)
            {
                _logger.LogWarning("Daily product limit reached: {Count}", todaysCount);
                return false;
            }

            // Rule 2: Electronics minimum price check
            if (request.Category == ProductCategory.Electronics && request.Price < 50m)
            {
                _logger.LogWarning("Electronics product {Name} has price below minimum.", request.Name);
                return false;
            }

            // Rule 3: Home product content restrictions
            if (request.Category == ProductCategory.Home)
            {
                var lower = request.Name.ToLowerInvariant();
                if (HomeRestrictedWords.Any(w => lower.Contains(w)))
                {
                    _logger.LogWarning("Home product {Name} violates content restrictions.", request.Name);
                    return false;
                }
            }

            // Rule 4: High-value product stock limit (>$500 = max 10 stock)
            if (request.Price > 500m && request.StockQuantity > 10)
            {
                _logger.LogWarning("High-value product {Name} has too much stock ({Stock}).",
                    request.Name, request.StockQuantity);
                return false;
            }

            _logger.LogInformation("Business rules passed for product {Name}", request.Name);
            return true;
        }

        private bool ContainTechnologyKeywords(string name)
        {
            var lower = name.ToLowerInvariant();
            return TechnologyKeywords.Any(k => lower.Contains(k));
        }

        private bool BeAppropriateForHome(string name)
        {
            var lower = name.ToLowerInvariant();
            return !HomeRestrictedWords.Any(w => lower.Contains(w));
        }
    }
}
