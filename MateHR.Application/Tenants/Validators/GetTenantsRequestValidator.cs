using FluentValidation;
using MateHR.Application.Tenants.DTOs;

namespace MateHR.Application.Tenants.Validators
{
    public class GetTenantsRequestValidator : AbstractValidator<GetTenantsRequest>
    {
        public GetTenantsRequestValidator()
        {
            RuleFor(t => t.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(t => t.PageSize).InclusiveBetween(1, 100);
        }
    }
}
