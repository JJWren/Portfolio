using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class SortStateTests
{
    [Fact]
    public void Toggle_NewColumn_UsesThatColumnsDefault()
    {
        var (column, direction) = SortState.Toggle(
            CommentSortColumn.CreatedAt, SortDirection.Descending,
            clicked: CommentSortColumn.Author,
            clickedDefault: SortDefaults.For(CommentSortColumn.Author));

        Assert.Equal(CommentSortColumn.Author, column);
        Assert.Equal(SortDirection.Ascending, direction);
    }

    [Fact]
    public void Toggle_ActiveColumn_FlipsAscendingToDescending()
    {
        var (column, direction) = SortState.Toggle(
            MessageSortColumn.Subject, SortDirection.Ascending,
            clicked: MessageSortColumn.Subject,
            clickedDefault: SortDirection.Ascending);

        Assert.Equal(MessageSortColumn.Subject, column);
        Assert.Equal(SortDirection.Descending, direction);
    }

    [Fact]
    public void Toggle_ActiveColumn_FlipsDescendingToAscending_IgnoringDefault()
    {
        // The clicked column's default plays no part when re-clicking the active column.
        var (column, direction) = SortState.Toggle(
            ReportSortColumn.CreatedAt, SortDirection.Descending,
            clicked: ReportSortColumn.CreatedAt,
            clickedDefault: SortDirection.Descending);

        Assert.Equal(ReportSortColumn.CreatedAt, column);
        Assert.Equal(SortDirection.Ascending, direction);
    }

    [Fact]
    public void Flip_RoundTrips()
    {
        Assert.Equal(SortDirection.Descending, SortState.Flip(SortDirection.Ascending));
        Assert.Equal(SortDirection.Ascending, SortState.Flip(SortDirection.Descending));
    }
}
