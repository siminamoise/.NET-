using AutoMapper;
using ProductApp.Dtos;
using ProductApp.Models;
using ProductApp.Mapping.Resolvers;
using System;

namespace ProductApp.Mapping
{
    public class AdvancedProductMappingProfile : Profile
    {
        public AdvancedProductMappingProfile()
        {
            // Mapare: CreateProductProfileRequest → Product
            CreateMap<CreateProductProfileRequest, Product>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(_ => DateTime.UtcNow))
                .ForMember(dest => dest.IsAvailable, opt => opt.MapFrom(src => src.StockQuantity > 0))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            CreateMap<Product, ProductProfileDto>()
                // Custom resolvers
                .ForMember(dest => dest.CategoryDisplayName, opt => opt.MapFrom<CategoryDisplayResolver>())
                .ForMember(dest => dest.FormattedPrice, opt => opt.MapFrom<PriceFormatterResolver>())
                .ForMember(dest => dest.ProductAge, opt => opt.MapFrom<ProductAgeResolver>())
                .ForMember(dest => dest.BrandInitials, opt => opt.MapFrom<BrandInitialsResolver>())
                .ForMember(dest => dest.AvailabilityStatus, opt => opt.MapFrom<AvailabilityStatusResolver>())

                // ✅ Conditional ImageUrl mapping (Task 1.2)
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src =>
                    src.Category == ProductCategory.Home ? null : src.ImageUrl))

                // ✅ Conditional Price mapping (Task 1.2)
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src =>
                    src.Category == ProductCategory.Home ? src.Price * 0.9m : src.Price));
        }
    }
}
