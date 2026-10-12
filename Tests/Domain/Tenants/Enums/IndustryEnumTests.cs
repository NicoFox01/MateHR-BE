using FluentAssertions;
using MateHR.Domain.Tenants.Enums;
using MateHR.Domain.Users.Enums;
using Xunit;

namespace MateHR.Domain.Tests.Tenants.Enums
{
    public class IndustryEnumTests
    {
        [Fact]
        public void Industry_Technology_IsZero()
        {
            ((int)Industry.Technology).Should().Be(0);
        }

        [Fact]
        public void TenantStatus_Active_IsOne()
        {
            ((int)TenantStatus.Active).Should().Be(1);
        }

        [Fact]
        public void UserRole_SuperAdmin_IsOne()
        {
            ((int)UserRole.SuperAdmin).Should().Be(1);
        }

        [Fact]
        public void RecruitmentMode_Internal_IsOne()
        {
            ((int)RecruitmentMode.Internal).Should().Be(1);
        }

        [Fact]
        public void SubscriptionType_Free_IsOne()
        {
            ((int)SubscriptionType.Free).Should().Be(1);
        }

        [Fact]
        public void SubscriptionStatus_None_IsZero()
        {
            ((int)SubscriptionStatus.None).Should().Be(0);
        }
    }
}
