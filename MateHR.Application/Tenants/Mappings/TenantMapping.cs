using AutoMapper;
using MateHR.Application.Tenants.DTOs;
using MateHR.Domain.Common;
using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Tenants.ValueObjects;


namespace MateHR.Application.Tenants.Mappings
{
    public class TenantMapping : Profile
    {
        public TenantMapping() 
        {
            CreateMap<Tenant, TenantResponse>();
            CreateMap<Address, AddressDto>();
            CreateMap<AddressDto, Address>();
            CreateMap<PagedResult<Tenant>, PagedResult<TenantResponse>>();
        }
    }
}
