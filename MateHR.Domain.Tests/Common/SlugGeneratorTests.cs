using MateHR.Domain.Common;
using Xunit;

namespace MateHR.Domain.Tests.Common
{
    public class SlugGeneratorTests
    {
        [Theory]
        [InlineData("Empresa Tech SA", "empresa-tech-sa")]
        [InlineData("Empresa Otra", "empresa-otra")]
        [InlineData("Café", "caf")]
        [InlineData("123", "123")]
        [InlineData("a--b", "a-b")]
        public void Generate_ValidInput_ReturnsExpectedSlug(string input, string expected)
        {
            // Act
            var result = SlugGenerator.Generate(input);
            // Assert
            Assert.Equal(expected, result);
        }
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GenerateNullORWhiteSpace_ThrowsArgumentException(string? input)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => SlugGenerator.Generate(input!));
        }
        [Fact]
        public void Generate_InputWithoutValidAsciiCharacters_ThrowsArgumentException()
        {
            // Arrange
            var input = "!!!";
            // Act & Assert
            Assert.Throws<ArgumentException>(() => SlugGenerator.Generate(input));
        }
    }
}
