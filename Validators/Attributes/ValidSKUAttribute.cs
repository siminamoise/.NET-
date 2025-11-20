using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace ProductApp.Validators.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ValidSKUAttribute : ValidationAttribute, IClientModelValidator
    {
        private const string Pattern = "^[A-Za-z0-9-]{5,20}$";

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
                return ValidationResult.Success;

            var input = value.ToString()!.Replace(" ", string.Empty);

            if (string.IsNullOrWhiteSpace(input))
                return new ValidationResult(ErrorMessage ?? "SKU is required.");

            if (!Regex.IsMatch(input, Pattern))
                return new ValidationResult(ErrorMessage ?? "SKU must be 5-20 characters, alphanumeric with hyphens.");

            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            context.Attributes["data-val"] = "true";
            context.Attributes["data-val-validsku"] = ErrorMessage ?? "Invalid SKU format.";
            context.Attributes["data-val-validsku-pattern"] = Pattern;
        }
    }
}
