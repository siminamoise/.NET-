using AutoMapper;
using ProductApp.Models;
using System;
using System.Linq;

namespace ProductApp.Mapping.Resolvers
{
    public class BrandInitialsResolver : IValueResolver<Product, object, string>
    {
        public string Resolve(Product source, object destination, string destMember, ResolutionContext context)
        {
            if (string.IsNullOrWhiteSpace(source.Brand))
                return "?";

            var words = source.Brand.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 1)
                return words[0][0].ToString().ToUpper();

            return $"{words.First()[0]}{words.Last()[0]}".ToUpper();
        }
    }
}
