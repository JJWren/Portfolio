using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class PagedResultTests
{
    [Theory]
    [InlineData(0, 10, 1)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(95, 10, 10)]
    public void TotalPages_RoundsUpAndNeverGoesBelowOne(int total, int pageSize, int expected)
    {
        var result = new PagedResult<int>([], 1, pageSize, total);
        Assert.Equal(expected, result.TotalPages);
    }

    [Theory]
    [InlineData(0, 25, 1)]     // below range clamps to 1
    [InlineData(-5, 25, 1)]
    [InlineData(99, 25, 1)]    // beyond range clamps to last page
    [InlineData(2, 30, 2)]     // in range stays
    public void ClampPage_KeepsRequestsInRange(int requested, int totalCount, int expected)
    {
        Assert.Equal(expected, PagedResult<int>.ClampPage(requested, totalCount, 25));
    }

    [Fact]
    public void HasMore_TrueOnlyBeforeTheLastPage()
    {
        Assert.True(new PagedResult<int>([], 1, 10, 25).HasMore);
        Assert.False(new PagedResult<int>([], 3, 10, 25).HasMore);
    }
}
