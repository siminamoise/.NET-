using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProductApp.Common.Logging;
using ProductApp.Dtos;
using ProductApp.Models;

namespace ProductApp.Handlers
{
    public class CreateProductHandler
    {
        private readonly ApplicationContext _context;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;
        private readonly IValidator<CreateProductProfileRequest> _validator;
        private readonly ILogger<CreateProductHandler> _logger;

        public CreateProductHandler(
            ApplicationContext context,
            IMapper mapper,
            IMemoryCache cache,
            IValidator<CreateProductProfileRequest> validator,
            ILogger<CreateProductHandler> logger)
        {
            _context = context;
            _mapper = mapper;
            _cache = cache;
            _validator = validator;
            _logger = logger;
        }

        public async Task<ProductProfileDto> HandleAsync(
            CreateProductProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            var operationId = Guid.NewGuid().ToString("N")[..8];

            using var scope = _logger.BeginScope("ProductOperation {OperationId} {SKU}", operationId, request.SKU);

            _logger.LogInformation(
                new EventId(LogEvents.ProductCreationStarted),
                "Starting product creation | Name={Name}, Brand={Brand}, SKU={SKU}, Category={Category}",
                request.Name, request.Brand, request.SKU, request.Category);

            var totalSw = Stopwatch.StartNew();
            var validationSw = new Stopwatch();
            var dbSw = new Stopwatch();

            try
            {
                // Validation
                validationSw.Start();
                ValidationResult validationResult = await _validator.ValidateAsync(request, cancellationToken);
                validationSw.Stop();

                if (!validationResult.IsValid)
                {
                    _logger.LogWarning(
                        new EventId(LogEvents.ProductValidationFailed),
                        "Validation failed for product | Name={Name}, SKU={SKU}. Errors: {Errors}",
                        request.Name,
                        request.SKU,
                        string.Join("; ", validationResult.Errors));

                    var ex = new ValidationException(validationResult.Errors);

                    var metricsFailed = new ProductCreationMetrics(
                        operationId,
                        request.Name,
                        request.SKU,
                        request.Category,
                        validationSw.Elapsed,
                        TimeSpan.Zero,
                        totalSw.Elapsed,
                        false,
                        ex.Message);

                    _logger.LogProductCreationMetrics(metricsFailed);

                    throw ex;
                }

                _logger.LogInformation(
                    new EventId(LogEvents.SKUValidationPerformed),
                    "SKU validation passed for {SKU}", request.SKU);

                _logger.LogInformation(
                    new EventId(LogEvents.StockValidationPerformed),
                    "Stock validation passed for {SKU} with Stock={Stock}",
                    request.SKU, request.StockQuantity);

                // Extra DB-level SKU uniqueness check
                var skuExists = await _context.Products
                    .AnyAsync(p => p.SKU == request.SKU, cancellationToken);

                if (skuExists)
                {
                    var message = $"Product with SKU '{request.SKU}' already exists.";
                    _logger.LogWarning(
                        new EventId(LogEvents.ProductValidationFailed),
                        message);

                    var ex = new ValidationException(message);

                    var metricsFailed = new ProductCreationMetrics(
                        operationId,
                        request.Name,
                        request.SKU,
                        request.Category,
                        validationSw.Elapsed,
                        TimeSpan.Zero,
                        totalSw.Elapsed,
                        false,
                        ex.Message);

                    _logger.LogProductCreationMetrics(metricsFailed);

                    throw ex;
                }

                // Map & save to DB
                dbSw.Start();
                _logger.LogInformation(
                    new EventId(LogEvents.DatabaseOperationStarted),
                    "Starting database operation for SKU={SKU}", request.SKU);

                var product = _mapper.Map<Product>(request);

                _context.Products.Add(product);
                await _context.SaveChangesAsync(cancellationToken);

                dbSw.Stop();

                _logger.LogInformation(
                    new EventId(LogEvents.DatabaseOperationCompleted),
                    "Database operation completed for ProductId={ProductId}",
                    product.Id);

                // Cache invalidation
                const string cacheKey = "all_products";
                _cache.Remove(cacheKey);

                _logger.LogInformation(
                    new EventId(LogEvents.CacheOperationPerformed),
                    "Cache invalidated for key {CacheKey}", cacheKey);

                var dto = _mapper.Map<ProductProfileDto>(product);

                totalSw.Stop();

                var metrics = new ProductCreationMetrics(
                    operationId,
                    dto.Name,
                    dto.SKU,
                    request.Category,
                    validationSw.Elapsed,
                    dbSw.Elapsed,
                    totalSw.Elapsed,
                    true,
                    null);

                _logger.LogProductCreationMetrics(metrics);

                _logger.LogInformation(
                    new EventId(LogEvents.ProductCreationCompleted),
                    "Product creation completed successfully for ProductId={ProductId}",
                    product.Id);

                return dto;
            }
            catch (ValidationException)
            {
                // deja logat
                throw;
            }
            catch (Exception ex)
            {
                totalSw.Stop();

                var metrics = new ProductCreationMetrics(
                    operationId,
                    request.Name,
                    request.SKU,
                    request.Category,
                    validationSw.Elapsed,
                    dbSw.Elapsed,
                    totalSw.Elapsed,
                    false,
                    ex.Message);

                _logger.LogError(
                    new EventId(LogEvents.ProductCreationCompleted),
                    ex,
                    "Unexpected error while creating product | Name={Name}, SKU={SKU}",
                    request.Name,
                    request.SKU);

                _logger.LogProductCreationMetrics(metrics);

                throw;
            }
        }
    }
}
