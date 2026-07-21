namespace Portfolio.Web.Services;

public static class PageSizes
{
    public const int Posts = 10;
    public const int Comments = 20;
    public const int Admin = 25;
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => CalculateTotalPages(TotalCount, PageSize);

    public bool HasMore => Page < TotalPages;

    /// <summary>Keeps a requested page inside [1, last]; an empty set is page 1 of 1.</summary>
    public static int ClampPage(int page, int totalCount, int pageSize)
        => Math.Clamp(page, 1, CalculateTotalPages(totalCount, pageSize));

    private static int CalculateTotalPages(int totalCount, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        return totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
    }
}
