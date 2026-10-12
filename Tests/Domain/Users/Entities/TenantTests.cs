using FluentAssertions;
using MateHR.Domain.Tenants.Entities;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Tenants.ValueObjects;
using Xunit;

namespace MateHR.Domain.Tests.Users.Entities
{
    public class TenantTests
    {
        private static Tenant CreateTenant(
            string name = "Acme Corp",
            string slug = "Acme Corp",
            string cuit = "20304050601",
            string ownerEmail = "owner@acme.com",
            Industry industry = Industry.Technology,
            Address? address = null,
            TenantStatus status = TenantStatus.Active,
            RecruitmentMode recruitmentMode = RecruitmentMode.Internal,
            SubscriptionType subscriptionType = SubscriptionType.Free)
            => new(name, slug, cuit, ownerEmail, industry, address, status, recruitmentMode, subscriptionType);

        [Fact]
        public void Constructor_ValidData_SetsDefaults()
        {
            var tenant = new Tenant(
                "  Acme Corp  ",
                "Mi Empresa",
                "20304050601",
                "owner@acme.com",
                Industry.Technology,
                null,
                TenantStatus.Active,
                RecruitmentMode.Internal,
                SubscriptionType.Free);

            tenant.Id.Should().NotBe(Guid.Empty);
            tenant.Name.Should().Be("Acme Corp");
            tenant.Slug.Should().Be("mi-empresa");
            tenant.Status.Should().Be(TenantStatus.Active);
            tenant.RecruitmentMode.Should().Be(RecruitmentMode.Internal);
            tenant.SubscriptionType.Should().Be(SubscriptionType.Free);
            tenant.SubscriptionStatus.Should().Be(SubscriptionStatus.None);
            tenant.SubscriptionExpiresAt.Should().BeNull();
            tenant.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Constructor_WhitespaceName_ThrowsArgumentException()
        {
            Action act = () => CreateTenant(name: "   ");

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("name");
        }

        [Fact]
        public void Constructor_NameExceedsMaxLength_ThrowsArgumentException()
        {
            Action act = () => CreateTenant(name: new string('a', 201));

            act.Should()
                .Throw<ArgumentException>()
                .WithMessage("*200*");
        }

        [Fact]
        public void Constructor_WhitespaceSlug_ThrowsArgumentException()
        {
            Action act = () => CreateTenant(slug: "");

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("slug");
        }

        [Fact]
        public void Constructor_SlugWithoutValidAscii_ThrowsArgumentException()
        {
            Action act = () => CreateTenant(slug: "!!!");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_CuitWithLetters_ThrowsArgumentException()
        {
            Action act = () => CreateTenant(cuit: "20A30405061");

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("cuit");
        }

        [Theory]
        [InlineData("2030405060")]
        [InlineData("203040506012")]
        public void Constructor_CuitWrongLength_ThrowsArgumentException(string cuit)
        {
            Action act = () => CreateTenant(cuit: cuit);

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("cuit");
        }

        [Fact]
        public void Constructor_InvalidEmail_ThrowsArgumentException()
        {
            Action act = () => CreateTenant(ownerEmail: "no-es-un-email");

            act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("email");
        }

        [Theory]
        [InlineData(999)]
        public void Constructor_UndefinedIndustry_ThrowsArgumentOutOfRangeException(int value)
        {
            Action act = () => CreateTenant(industry: (Industry)value);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(999)]
        public void Constructor_UndefinedStatus_ThrowsArgumentOutOfRangeException(int value)
        {
            Action act = () => CreateTenant(status: (TenantStatus)value);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(999)]
        public void Constructor_UndefinedRecruitmentMode_ThrowsArgumentOutOfRangeException(int value)
        {
            Action act = () => CreateTenant(recruitmentMode: (RecruitmentMode)value);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(999)]
        public void Constructor_UndefinedSubscriptionType_ThrowsArgumentOutOfRangeException(int value)
        {
            Action act = () => CreateTenant(subscriptionType: (SubscriptionType)value);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ChangeStatus_ValidStatus_ChangesStatus()
        {
            var tenant = CreateTenant(status: TenantStatus.Active);

            tenant.ChangeStatus(TenantStatus.Suspended);

            tenant.Status.Should().Be(TenantStatus.Suspended);
            tenant.Name.Should().Be("Acme Corp");
            tenant.SubscriptionType.Should().Be(SubscriptionType.Free);
        }

        [Fact]
        public void ChangeStatus_UndefinedStatus_ThrowsArgumentOutOfRangeException()
        {
            var tenant = CreateTenant(status: TenantStatus.Active);

            Action act = () => tenant.ChangeStatus((TenantStatus)999);

            act.Should().Throw<ArgumentOutOfRangeException>();
            tenant.Status.Should().Be(TenantStatus.Active);
        }

        [Fact]
        public void ChangeRecruitmentMode_ValidMode_ChangesMode()
        {
            var tenant = CreateTenant(recruitmentMode: RecruitmentMode.Internal);

            tenant.ChangeRecruitmentMode(RecruitmentMode.External);

            tenant.RecruitmentMode.Should().Be(RecruitmentMode.External);
        }

        [Fact]
        public void ChangeRecruitmentMode_UndefinedMode_ThrowsArgumentOutOfRangeException()
        {
            var tenant = CreateTenant();

            Action act = () => tenant.ChangeRecruitmentMode((RecruitmentMode)999);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ChangeSubscriptionType_ValidType_ResetsSubscriptionStatusAndExpiry()
        {
            var tenant = CreateTenant(subscriptionType: SubscriptionType.Free);

            tenant.ChangeSubscriptionType(SubscriptionType.Monthly);

            tenant.SubscriptionType.Should().Be(SubscriptionType.Monthly);
            tenant.SubscriptionStatus.Should().Be(SubscriptionStatus.None);
            tenant.SubscriptionExpiresAt.Should().BeNull();
        }

        [Fact]
        public void ChangeSubscriptionType_UndefinedType_ThrowsArgumentOutOfRangeException()
        {
            var tenant = CreateTenant(subscriptionType: SubscriptionType.Free);

            Action act = () => tenant.ChangeSubscriptionType((SubscriptionType)999);

            act.Should().Throw<ArgumentOutOfRangeException>();
            tenant.SubscriptionType.Should().Be(SubscriptionType.Free);
        }

        [Fact]
        public void Update_ValidData_UpdatesAllFields()
        {
            var tenant = CreateTenant();
            var address = new Address
            {
                Street = "Av. Siempre Viva 742",
                City = "Springfield",
                State = "Buenos Aires",
                Country = "Argentina",
                PostalCode = "1900"
            };

            tenant.Update("  Nueva Empresa  ", "Nueva Empresa", "11222333444", "nuevo@empresa.com", Industry.Finance, address);

            tenant.Name.Should().Be("Nueva Empresa");
            tenant.Slug.Should().Be("nueva-empresa");
            tenant.CUIT.Should().Be("11222333444");
            tenant.OwnerEmail.Should().Be("nuevo@empresa.com");
            tenant.Industry.Should().Be(Industry.Finance);
            tenant.Address.Should().BeSameAs(address);
            tenant.Status.Should().Be(TenantStatus.Active);
            tenant.SubscriptionType.Should().Be(SubscriptionType.Free);
        }

        [Fact]
        public void Update_WhitespaceName_ThrowsArgumentException()
        {
            var tenant = CreateTenant();
            var originalName = tenant.Name;

            Action act = () => tenant.Update("   ", "Acme Corp", "20304050601", "owner@acme.com", Industry.Technology, null);

            act.Should().Throw<ArgumentException>();
            tenant.Name.Should().Be(originalName);
        }

        [Fact]
        public void Update_InvalidEmail_ThrowsArgumentException()
        {
            var tenant = CreateTenant();

            Action act = () => tenant.Update("Acme Corp", "Acme Corp", "20304050601", "abc", Industry.Technology, null);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Update_UndefinedIndustry_ThrowsArgumentOutOfRangeException()
        {
            var tenant = CreateTenant();

            Action act = () => tenant.Update("Acme Corp", "Acme Corp", "20304050601", "owner@acme.com", (Industry)999, null);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
