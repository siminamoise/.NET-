using AutoMapper;
using ProductApp.Models;
using System.Globalization;

namespace ProductApp.Mapping.Resolvers
{
    public class PriceFormatterResolver : IValueResolver<Product, object, string>
    {
        public string Resolve(Product source, object destination, string destMember, ResolutionContext context)
        {
            return source.Price.ToString("C2", CultureInfo.CurrentCulture);
        }
    }
}
