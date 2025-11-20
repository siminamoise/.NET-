using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ProductApp.Models;

namespace ProductApp.Validators.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ProductCategoryAttribute : ValidationAttribute
    {
        private readonly ProductCategory[] _allowed;

        public ProductCategoryAttribute(params ProductCategory[] allowed)
        {
            _allowed = allowed;
        }

        public override bool IsValid(object? value)
        {
            if (value is null)
                return false;

            if (value is ProductCategory category)
            {
                return _allowed.Contains(category);
            }

            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            var list = string.Join(", ", _allowed.Select(c => c.ToString()));
            return ErrorMessage ?? $"{name} must be one of the following categories: {list}.";
        }
    }
}
