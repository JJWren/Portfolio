using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class QuerySortTests
{
    private sealed record Row(int Value, int Tiebreak);

    [Fact]
    public void By_Ascending_OrdersSmallestFirst()
    {
        var source = new[] { 2, 1, 3 }.AsQueryable();

        Assert.Equal([1, 2, 3], QuerySort.By(source, x => x, SortDirection.Ascending).ToList());
    }

    [Fact]
    public void By_Descending_OrdersLargestFirst()
    {
        var source = new[] { 2, 1, 3 }.AsQueryable();

        Assert.Equal([3, 2, 1], QuerySort.By(source, x => x, SortDirection.Descending).ToList());
    }

    [Fact]
    public void By_ComposesWithThenBy()
    {
        // The services chain .ThenByDescending(Id) as a deterministic tiebreak.
        var rows = new[] { new Row(1, 2), new Row(1, 1), new Row(0, 9) }.AsQueryable();

        var sorted = QuerySort.By(rows, r => r.Value, SortDirection.Ascending)
            .ThenByDescending(r => r.Tiebreak)
            .ToList();

        Assert.Equal([new Row(0, 9), new Row(1, 2), new Row(1, 1)], sorted);
    }
}
