namespace Portfolio.Web.Services;

public static class PagerWindow
{
    /// <summary>
    /// Page numbers to render: the full run for ≤7 pages, otherwise first/last
    /// plus a window around the current page with null marking an ellipsis.
    /// </summary>
    public static IEnumerable<int?> Numbers(int currentPage, int totalPages)
    {
        if (totalPages <= 7)
        {
            for (var i = 1; i <= totalPages; i++)
            {
                yield return i;
            }
            yield break;
        }

        var window = new SortedSet<int> { 1, totalPages };
        for (var i = currentPage - 1; i <= currentPage + 1; i++)
        {
            if (i >= 1 && i <= totalPages)
            {
                window.Add(i);
            }
        }

        int? previous = null;
        foreach (var number in window)
        {
            if (previous is not null && number - previous > 1)
            {
                yield return null;
            }
            yield return number;
            previous = number;
        }
    }
}
