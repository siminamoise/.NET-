using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ProductApp.Validators.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PriceRangeAttribute : ValidationAttribute
    {
        private readonly decimal _min;
        private readonly decimal _max;

        public PriceRangeAttribute(double min, double max)
        {
            _min = Convert.ToDecimal(min, CultureInfo.InvariantCulture);
            _max = Convert.ToDecimal(max, CultureInfo.InvariantCulture);
        }

        public override bool IsValid(object? value)
        {
            if (value is null)
                return true;

            if (value is decimal price)
            {
                return price >= _min && price <= _max;
            }

            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            var minStr = _min.ToString("C2", CultureInfo.CurrentCulture);
            var maxStr = _max.ToString("C2", CultureInfo.CurrentCulture);
            return ErrorMessage ?? $"{name} must be between {minStr} and {maxStr}.";
        }
    }
}
