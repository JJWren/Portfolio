using System.Linq.Expressions;

namespace Portfolio.Web.Services;

public enum SortDirection
{
    Ascending,
    Descending,
}

/// <summary>Sortable columns of the admin comments table.</summary>
public enum CommentSortColumn
{
    Author,
    Post,
    CreatedAt,
    State,
}

/// <summary>Sortable columns of the admin contact-message inbox.</summary>
public enum MessageSortColumn
{
    From,
    Subject,
    ReceivedAt,
    State,
}

/// <summary>Sortable columns of the admin reports inbox.</summary>
public enum ReportSortColumn
{
    Type,
    Reason,
    Target,
    CreatedAt,
    Status,
}

/// <summary>Pure toggle rules for sortable admin lists.</summary>
public static class SortState
{
    /// <summary>Clicking the active column reverses it; a new column starts at its default direction.</summary>
    public static (TColumn Column, SortDirection Direction) Toggle<TColumn>(
        TColumn activeColumn,
        SortDirection activeDirection,
        TColumn clicked,
        SortDirection clickedDefault)
        where TColumn : struct, Enum
        => EqualityComparer<TColumn>.Default.Equals(activeColumn, clicked)
            ? (clicked, Flip(activeDirection))
            : (clicked, clickedDefault);

    public static SortDirection Flip(SortDirection direction)
        => direction == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
}

/// <summary>First-click directions: date columns show newest first, everything else ascends.</summary>
public static class SortDefaults
{
    public static SortDirection For(CommentSortColumn column)
        => column == CommentSortColumn.CreatedAt ? SortDirection.Descending : SortDirection.Ascending;

    public static SortDirection For(MessageSortColumn column)
        => column == MessageSortColumn.ReceivedAt ? SortDirection.Descending : SortDirection.Ascending;

    public static SortDirection For(ReportSortColumn column)
        => column == ReportSortColumn.CreatedAt ? SortDirection.Descending : SortDirection.Ascending;
}

/// <summary>Applies a key selector in the requested direction.</summary>
public static class QuerySort
{
    public static IOrderedQueryable<T> By<T, TKey>(
        IQueryable<T> source, Expression<Func<T, TKey>> key, SortDirection direction)
        => direction == SortDirection.Ascending ? source.OrderBy(key) : source.OrderByDescending(key);
}
