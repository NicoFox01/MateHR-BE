using MateHR.Domain.Common;
using Xunit;

namespace MateHR.Domain.Tests.Common;

public class PagedResultTests
{
    [Theory]
    [InlineData(10, 1, 20, 1, false, false)]
    [InlineData(50, 1, 20, 3, false, true)]
    [InlineData(0, 1, 20, 0, false, false)]
    [InlineData(60, 2, 20, 3, true, true)]
    public void PagedResult_CalculatesPaginationPropertiesCorrectly(
        int totalCount,
        int pageNumber,
        int pageSize,
        int expectedTotalPages,
        bool expectedHasPreviousPage,
        bool expectedHasNextPage)
    {
        // Arrange
        var items = new List<string>();

        // Act
        var result = new PagedResult<string>(items, pageNumber, pageSize, totalCount);

        // Assert
        Assert.Equal(expectedTotalPages, result.TotalPages);
        Assert.Equal(expectedHasPreviousPage, result.HasPreviousPage);
        Assert.Equal(expectedHasNextPage, result.HasNextPage);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, -10)]
    public void PagedResult_WhenPageNumberOrPageSizeIsLessThanOne_ThrowsArgumentOutOfRangeException(
        int pageNumber,
        int pageSize)
    {
        // Arrange
        var items = new List<string>();
        const int totalCount = 10;

        // Act & Assert (mismo orden: items, pageNumber, pageSize, totalCount)
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PagedResult<string>(items, pageNumber, pageSize, totalCount));
    }

    [Fact]
    public void PagedResult_PreservesItemsListPassedInConstructor()
    {
        // Arrange
        var expectedItems = new List<string> { "item1", "item2", "item3" };

        // Act (mismo orden: items, pageNumber, pageSize, totalCount)
        var result = new PagedResult<string>(expectedItems, pageNumber: 1, pageSize: 10, totalCount: 3);

        // Assert
        Assert.Same(expectedItems, result.Items);
    }
}