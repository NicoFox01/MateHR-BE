using System;
using System.Collections.Generic;
using System.Text;

namespace MateHR.Domain.Tenants.ValueObjects
{
    public class Address
    {
        public const int StreetMaxLength = 200;
        public const int CityMaxLength = 100;
        public const int StateMaxLength = 100;
        public const int CountryMaxLength = 100;
        public const int PostalCodeMaxLength = 20;
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
    }
}
