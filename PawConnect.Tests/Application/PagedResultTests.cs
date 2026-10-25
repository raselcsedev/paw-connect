using PawConnect.Api.Application.DTOs.Common;

namespace PawConnect.Tests;

public class PagedResultTests
{
    private static PagedResult<string> Page(int page, int pageSize, int total) =>
        new(new List<string>(), page, pageSize, total);

    [Theory]
    [InlineData(25, 12, 3)]
    [InlineData(24, 12, 2)]
    [InlineData(1, 12, 1)]
    [InlineData(0, 12, 0)]
    public void TotalPages_RoundsUp(int total, int pageSize, int expected)
    {
        Assert.Equal(expected, Page(1, pageSize, total).TotalPages);
    }

    [Fact]
    public void FirstPage_HasNextButNoPrevious()
    {
        var page = Page(1, 12, 25);

        Assert.True(page.HasNext);
        Assert.False(page.HasPrevious);
    }

    [Fact]
    public void LastPage_HasPreviousButNoNext()
    {
        var page = Page(3, 12, 25);

        Assert.False(page.HasNext);
        Assert.True(page.HasPrevious);
    }
}