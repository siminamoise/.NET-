using AutoMapper;
using ProductApp.Models;

namespace ProductApp.Mapping.Resolvers
{
    public class AvailabilityStatusResolver : IValueResolver<Product, object, string>
    {
        public string Resolve(Product source, object destination, string destMember, ResolutionContext context)
        {
            if (!source.IsAvailable)
                return "Out of Stock";

            if (source.StockQuantity == 0)
                return "Unavailable";
            if (source.StockQuantity == 1)
                return "Last Item";
            if (source.StockQuantity <= 5)
                return "Limited Stock";
            return "In Stock";
        }
    }
}
