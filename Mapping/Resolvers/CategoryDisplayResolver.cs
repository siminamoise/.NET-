using AutoMapper;
using ProductApp.Models;

namespace ProductApp.Mapping.Resolvers
{
    public class CategoryDisplayResolver : IValueResolver<Product, object, string>
    {
        public string Resolve(Product source, object destination, string destMember, ResolutionContext context)
        {
            return source.Category switch
            {
                ProductCategory.Electronics => "Electronics & Technology",
                ProductCategory.Clothing => "Clothing & Fashion",
                ProductCategory.Books => "Books & Media",
                ProductCategory.Home => "Home & Garden",
                _ => "Uncategorized"
            };
        }
    }
}
