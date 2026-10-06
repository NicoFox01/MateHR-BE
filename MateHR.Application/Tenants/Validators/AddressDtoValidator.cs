using FluentValidation;
using MateHR.Application.Tenants.DTOs;
using MateHR.Domain.Tenants.ValueObjects;

namespace MateHR.Application.Tenants.Validators
{
    public class AddressDtoValidator : AbstractValidator<AddressDto>
    {
        public AddressDtoValidator()
        {
            RuleFor(x => x.Street).MaximumLength(Address.StreetMaxLength);
            RuleFor(x => x.City).MaximumLength(Address.CityMaxLength);
            RuleFor(x => x.State).MaximumLength(Address.StateMaxLength);
            RuleFor(x=> x.Country).MaximumLength(Address.CountryMaxLength);
            RuleFor(x=>x.PostalCode).MaximumLength(Address.PostalCodeMaxLength);
        }
    }
}
