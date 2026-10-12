using FluentAssertions;
using MateHR.Domain.Tenants.ValueObjects;
using Xunit;

namespace MateHR.Domain.Tests.Tenants.ValueObjects
{
    public class AddressTests
    {
        [Fact]
        public void NewInstance_DefaultsAreEmptyStrings()
        {
            var address = new Address();

            address.Street.Should().Be(string.Empty);
            address.City.Should().Be(string.Empty);
            address.State.Should().Be(string.Empty);
            address.Country.Should().Be(string.Empty);
            address.PostalCode.Should().Be(string.Empty);
        }
    }
}
