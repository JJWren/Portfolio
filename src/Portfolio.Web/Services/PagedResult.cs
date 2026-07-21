namespace Portfolio.Web.Services;

public static class PageSizes
{
    public const int Posts = 10;
    public const int Comments = 20;
    public const int Admin = 25;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasMore => Page < TotalPages;

    /// <summary>Keeps a requested page inside [1, last]; an empty set is page 1 of 1.</summary>
    public static int ClampPage(int page, int totalCount, int pageSize)
    {
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return Math.Clamp(page, 1, totalPages);
    }
}
