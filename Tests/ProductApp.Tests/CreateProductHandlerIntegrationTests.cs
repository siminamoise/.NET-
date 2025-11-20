using System;
using System.Globalization;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using ProductApp.Common.Logging;
using ProductApp.Dtos;
using ProductApp.Handlers;
using ProductApp.Mapping;
using ProductApp.Models;
using ProductApp.Validators;
using Xunit;

namespace ProductApp.Tests
{
    public class CreateProductHandlerIntegrationTests : IDisposable
    {
        private readonly ApplicationContext _context;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly Mock<ILogger<CreateProductHandler>> _handlerLoggerMock;
        private readonly Mock<ILogger<CreateProductProfileValidator>> _validatorLoggerMock;
        private readonly IValidator<CreateProductProfileRequest> _validator;
        private readonly CreateProductHandler _handler;

        public CreateProductHandlerIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationContext(options);

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<AdvancedProductMappingProfile>();
            });

            _mapper = mapperConfig.CreateMapper();

            _cache = new MemoryCache(new MemoryCacheOptions());

            _handlerLoggerMock = new Mock<ILogger<CreateProductHandler>>();
            _validatorLoggerMock = new Mock<ILogger<CreateProductProfileValidator>>();

            _validator = new CreateProductProfileValidator(_context, _validatorLoggerMock.Object);
            _handler = new CreateProductHandler(
                _context,
                _mapper,
                _cache,
                _validator,
                _handlerLoggerMock.Object);
        }

        public void Dispose()
        {
            _context.Dispose();
            _cache.Dispose();
        }

        [Fact]
        public async Task Handle_ValidElectronicsProductRequest_CreatesProductWithCorrectMappings()
        {
            // Arrange
            var request = new CreateProductProfileRequest
            {
                Name = "Smart Tech Phone",
                Brand = "Tech Brand",
                SKU = "ELEC-12345",
                Category = ProductCategory.Electronics,
                Price = 999.99m,
                ReleaseDate = DateTime.UtcNow.AddMonths(-3),
                ImageUrl = "https://example.com/image.jpg",
                StockQuantity = 10
            };

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("Electronics & Technology", result.CategoryDisplayName);
            Assert.Equal("TB", result.BrandInitials);

            Assert.True(result.ProductAge.Contains("months old") ||
                        result.ProductAge == "New Release");

            var symbol = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;
            //Assert.StartsWith(symbol, result.FormattedPrice);
            Assert.True(
                result.FormattedPrice.Contains(symbol),
                $"FormattedPrice '{result.FormattedPrice}' does not contain the currency symbol '{symbol}'."
            );

            Assert.Equal("In Stock", result.AvailabilityStatus);

            _handlerLoggerMock.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.Is<EventId>(e => e.Id == LogEvents.ProductCreationStarted),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_DuplicateSKU_ThrowsValidationExceptionWithLogging()
        {
            // Arrange
            var existing = new Product
            {
                Id = Guid.NewGuid(),
                Name = "Existing Product",
                Brand = "Brand X",
                SKU = "SKU-12345",
                Category = ProductCategory.Books,
                Price = 20m,
                ReleaseDate = DateTime.UtcNow.AddYears(-1),
                CreatedAt = DateTime.UtcNow,
                IsAvailable = true,
                StockQuantity = 5
            };
            _context.Products.Add(existing);
            await _context.SaveChangesAsync();

            var request = new CreateProductProfileRequest
            {
                Name = "New Product",
                Brand = "Brand Y",
                SKU = "SKU-12345",
                Category = ProductCategory.Books,
                Price = 30m,
                ReleaseDate = DateTime.UtcNow.AddMonths(-2),
                ImageUrl = "https://example.com/book.jpg",
                StockQuantity = 3
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<FluentValidation.ValidationException>(
                () => _handler.HandleAsync(request));

            Assert.Contains("already exists", ex.Message, StringComparison.OrdinalIgnoreCase);

            _handlerLoggerMock.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.Is<EventId>(e => e.Id == LogEvents.ProductValidationFailed),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task Handle_HomeProductRequest_AppliesDiscountAndConditionalMapping()
        {
            // Arrange
            var request = new CreateProductProfileRequest
            {
                Name = "Nice Garden Lamp",
                Brand = "Home Brand",
                SKU = "HOME-99999",
                Category = ProductCategory.Home,
                Price = 100m,
                ReleaseDate = DateTime.UtcNow.AddMonths(-1),
                ImageUrl = "https://example.com/lamp.jpg",
                StockQuantity = 4
            };

            // Act
            var result = await _handler.HandleAsync(request);

            // Assert
            Assert.Equal("Home & Garden", result.CategoryDisplayName);

            // 10% discount applied in mapping profile
            Assert.Equal(90m, result.Price);

            // ImageUrl filtered for Home category
            Assert.Null(result.ImageUrl);
        }
    }
}
