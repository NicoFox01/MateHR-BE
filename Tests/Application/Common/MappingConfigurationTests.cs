using AutoMapper;
using FluentAssertions;
using MateHR.Application.Tenants.DTOs;
using MateHR.Application.Tenants.Mappings;
using MateHR.Application.Users.Mappings;
using MateHR.Domain.Common;
using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Tenants.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace MateHR.Application.Tests.Common
{
    public class MappingConfigurationTests
    {
        private static Tenant CreateTenant() => new(
            "Acme Corp",
            "Acme Corp",
            "20304050601",
            "owner@acme.com",
            Industry.Technology,
            null,
            TenantStatus.Active,
            RecruitmentMode.Internal,
            SubscriptionType.Free);

        [Fact]
        public void TenantMapping_AssertConfigurationIsValid_Passes()
        {
            var configuration = new MapperConfiguration(cfg => cfg.AddProfile<TenantMapping>(), NullLoggerFactory.Instance);

            Action act = () => configuration.AssertConfigurationIsValid();

            act.Should().NotThrow<AutoMapperConfigurationException>();
        }

        [Fact]
        public void UserMapping_AssertConfigurationIsValid_Passes()
        {
            var configuration = new MapperConfiguration(cfg => cfg.AddProfile<UserMapping>(), NullLoggerFactory.Instance);

            Action act = () => configuration.AssertConfigurationIsValid();

            act.Should().NotThrow<AutoMapperConfigurationException>();
        }
        [Fact]
        public void RecruitmentMappings_AssertConfigurationIsValid_Passes()
        {
            var configuration = new MapperConfiguration(cfg => cfg.AddMaps(typeof(MateHR.Application.DependencyInjection).Assembly),
                NullLoggerFactory.Instance);
            Action act = () => configuration.AssertConfigurationIsValid();
            act.Should().NotThrow<AutoMapperConfigurationException>();
        }

        [Fact]
        public void Map_TenantToTenantResponse_MapsProperties()
        {
            var mapper = TestMapper.Create();
            var tenant = CreateTenant();

            var response = mapper.Map<TenantResponse>(tenant);

            response.Id.Should().Be(tenant.Id);
            response.Name.Should().Be(tenant.Name);
            response.Slug.Should().Be(tenant.Slug);
            response.CUIT.Should().Be(tenant.CUIT);
            response.OwnerEmail.Should().Be(tenant.OwnerEmail);
            response.Industry.Should().Be(tenant.Industry);
            response.Status.Should().Be(tenant.Status);
            response.RecruitmentMode.Should().Be(tenant.RecruitmentMode);
            response.SubscriptionType.Should().Be(tenant.SubscriptionType);
            response.CreatedAt.Should().Be(tenant.CreatedAt);
        }

        [Fact]
        public void Map_AddressDtoToAddress_MapsProperties()
        {
            var mapper = TestMapper.Create();
            var dto = new AddressDto
            {
                Street = "Av. Siempre Viva 742",
                City = "Springfield",
                State = "Buenos Aires",
                Country = "Argentina",
                PostalCode = "1900"
            };

            var address = mapper.Map<Address>(dto);

            address.Street.Should().Be(dto.Street);
            address.City.Should().Be(dto.City);
            address.State.Should().Be(dto.State);
            address.Country.Should().Be(dto.Country);
            address.PostalCode.Should().Be(dto.PostalCode);
        }

        [Fact]
        public void Map_AddressToAddressDto_MapsProperties()
        {
            var mapper = TestMapper.Create();
            var address = new Address
            {
                Street = "Av. Siempre Viva 742",
                City = "Springfield",
                State = "Buenos Aires",
                Country = "Argentina",
                PostalCode = "1900"
            };

            var dto = mapper.Map<AddressDto>(address);

            dto.Street.Should().Be(address.Street);
            dto.City.Should().Be(address.City);
            dto.State.Should().Be(address.State);
            dto.Country.Should().Be(address.Country);
            dto.PostalCode.Should().Be(address.PostalCode);
        }

        [Fact]
        public void Map_PagedResultOfTenantToPagedResultOfTenantResponse_MapsItems()
        {
            var mapper = TestMapper.Create();
            var items = new List<Tenant> { CreateTenant(), CreateTenant() };
            var paged = new PagedResult<Tenant>(items, 1, 20, 2);

            var mapped = mapper.Map<PagedResult<TenantResponse>>(paged);

            mapped.Items.Should().HaveCount(2);
            mapped.Items.Should().AllBeOfType<TenantResponse>();
            mapped.PageNumber.Should().Be(1);
            mapped.PageSize.Should().Be(20);
            mapped.TotalCount.Should().Be(2);
        }
    }
}
