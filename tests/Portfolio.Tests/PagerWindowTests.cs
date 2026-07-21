using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class PagerWindowTests
{
    [Fact]
    public void Numbers_ListsAllPagesWhenSevenOrFewer()
    {
        Assert.Equal(new int?[] { 1, 2, 3 }, PagerWindow.Numbers(2, 3));
    }

    [Fact]
    public void Numbers_WindowsAroundCurrentWithEllipses()
    {
        // 20 pages, current 10 → 1 … 9 10 11 … 20
        Assert.Equal(new int?[] { 1, null, 9, 10, 11, null, 20 }, PagerWindow.Numbers(10, 20));
    }

    [Fact]
    public void Numbers_NoLeadingEllipsisNearTheStart()
    {
        // current 2 → 1 2 3 … 20
        Assert.Equal(new int?[] { 1, 2, 3, null, 20 }, PagerWindow.Numbers(2, 20));
    }

    [Fact]
    public void Numbers_NoTrailingEllipsisNearTheEnd()
    {
        Assert.Equal(new int?[] { 1, null, 18, 19, 20 }, PagerWindow.Numbers(19, 20));
    }
}
