using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class SortDefaultsTests
{
    [Theory]
    [InlineData(CommentSortColumn.Author, SortDirection.Ascending)]
    [InlineData(CommentSortColumn.Post, SortDirection.Ascending)]
    [InlineData(CommentSortColumn.CreatedAt, SortDirection.Descending)]
    [InlineData(CommentSortColumn.State, SortDirection.Ascending)]
    public void For_CommentColumns_DatesDescendOthersAscend(CommentSortColumn column, SortDirection expected)
        => Assert.Equal(expected, SortDefaults.For(column));

    [Theory]
    [InlineData(MessageSortColumn.From, SortDirection.Ascending)]
    [InlineData(MessageSortColumn.Subject, SortDirection.Ascending)]
    [InlineData(MessageSortColumn.ReceivedAt, SortDirection.Descending)]
    [InlineData(MessageSortColumn.State, SortDirection.Ascending)]
    public void For_MessageColumns_DatesDescendOthersAscend(MessageSortColumn column, SortDirection expected)
        => Assert.Equal(expected, SortDefaults.For(column));

    [Theory]
    [InlineData(ReportSortColumn.Type, SortDirection.Ascending)]
    [InlineData(ReportSortColumn.Reason, SortDirection.Ascending)]
    [InlineData(ReportSortColumn.Target, SortDirection.Ascending)]
    [InlineData(ReportSortColumn.CreatedAt, SortDirection.Descending)]
    [InlineData(ReportSortColumn.Status, SortDirection.Ascending)]
    public void For_ReportColumns_DatesDescendOthersAscend(ReportSortColumn column, SortDirection expected)
        => Assert.Equal(expected, SortDefaults.For(column));
}
