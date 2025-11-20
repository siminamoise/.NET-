using AutoMapper;
using ProductApp.Models;
using System;

namespace ProductApp.Mapping.Resolvers
{
    public class ProductAgeResolver : IValueResolver<Product, object, string>
    {
        public string Resolve(Product source, object destination, string destMember, ResolutionContext context)
        {
            var days = (DateTime.UtcNow - source.ReleaseDate).TotalDays;

            if (days < 30)
                return "New Release";
            if (days < 365)
                return $"{Math.Floor(days / 30)} months old";
            if (days < 1825)
                return $"{Math.Floor(days / 365)} years old";
            if (days == 1825)
                return "Classic";

            return "Old Product";
        }
    }
}
