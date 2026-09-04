namespace Portfolio.Web.Services;

/// <summary>Hero game-plan chart node. Positionally colored by the caller
/// (BR-5); every node links to #principles (decision 4). <see cref="How"/>
/// may be blank.</summary>
public sealed record GamePlanNode(string Term, string Reading, string How);

/// <summary>One `maxim | reading` pair for the Principles section (BR-7).
/// <see cref="Reading"/> may be blank.</summary>
public sealed record Principle(string Maxim, string Reading);

/// <summary>
/// Pure parsing and validation for the BJJ landing flavor's structured copy
/// (game plan, principles, belt degrees). Lenient at resolve (BR-4): a bad
/// env or stored value never takes the landing page down — malformed lines
/// are dropped silently rather than thrown. Strict at save: the Validate*
/// methods return the first friendly error for
/// <see cref="SiteContentRules.Validate"/> to surface. Eras/Now (the road,
/// BR-8 to BR-10) are Phase 3 and deliberately not here yet.
/// </summary>
public static class BjjRules
{
    /// <summary>Widest a rank bar's stripe count may be (BR-6).</summary>
    public const int MaxDegrees = 6;

    /// <summary>The game-plan chart is exactly this many nodes or hidden (BR-5).</summary>
    public const int GamePlanNodeCount = 4;

    /// <summary>Widest the Principles section may be (BR-7).</summary>
    public const int MaxPrinciples = 6;

    /// <summary>Longest a single text[] line may be, in characters (BR-11).</summary>
    public const int MaxLineLength = 500;

    /// <summary>One entry per real newline, trimmed, blanks dropped. Shared
    /// by every multi-line field (game plan, principles, ...) the way
    /// SITE_ABOUT's paragraph split is shared today.</summary>
    public static IReadOnlyList<string> SplitLines(string? multiLine)
    {
        if (string.IsNullOrWhiteSpace(multiLine))
        {
            return [];
        }

        return multiLine.Split(
            ['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>Splits one line on the field separator '|', trimming each field.</summary>
    public static string[] SplitFields(string line)
        => line.Split('|').Select(static f => f.Trim()).ToArray();

    /// <summary>
    /// Bounds a raw line to <see cref="MaxLineLength"/> before it is split
    /// into fields — the same length ValidateGamePlan/ValidatePrinciples
    /// reject at save, but truncated here instead of dropped (BR-4: lenient
    /// at resolve). Truncating rather than dropping the whole line is the
    /// deliberate choice: dropping a game-plan line would leave an off-count
    /// chart (ParseGamePlan requires exactly four) and silently hide the
    /// whole node instead of just clipping its tail.
    /// </summary>
    private static string BoundLineLength(string line)
        => line.Length > MaxLineLength ? line[..MaxLineLength] : line;

    /// <summary>
    /// `term | reading | how` per line; `how` may be blank. BR-5: the chart
    /// is exactly four nodes or none, so any other resulting count — a
    /// three-line env value, a dropped fifth line — resolves to empty rather
    /// than an off-count chart. Overlong lines are truncated, not dropped
    /// (see <see cref="BoundLineLength"/>), so they still count toward the
    /// four.
    /// </summary>
    public static IReadOnlyList<GamePlanNode> ParseGamePlan(IReadOnlyList<string> lines)
    {
        var nodes = new List<GamePlanNode>();
        foreach (var line in lines)
        {
            var fields = SplitFields(BoundLineLength(line));
            if (fields.Length < 2 || fields[0].Length == 0 || fields[1].Length == 0)
            {
                continue; // Malformed line: dropped, not thrown (BR-4).
            }

            nodes.Add(new GamePlanNode(fields[0], fields[1], TailField(fields, 2)));
        }

        return nodes.Count == GamePlanNodeCount ? nodes : [];
    }

    /// <summary>
    /// The last field of a line absorbs any extra separators: a free-text
    /// `how` or `reading` that itself contains " | " is kept whole (re-joined
    /// with the same separator) instead of being silently cut at the next
    /// pipe. Empty when the line has no field at <paramref name="start"/>.
    /// </summary>
    private static string TailField(string[] fields, int start)
        => fields.Length > start ? string.Join(" | ", fields[start..]) : string.Empty;

    /// <summary>
    /// `maxim | reading` per line; `reading` may be blank. Malformed lines
    /// are dropped and overlong lines are truncated (see
    /// <see cref="BoundLineLength"/>); the surviving count is capped at
    /// <see cref="MaxPrinciples"/> so a stored or env value that predates a
    /// lower limit — or simply has too many lines — can never render more
    /// than the section's widest layout supports. Validate still enforces
    /// the 1-to-6 range with a friendly message at save (BR-7); this cap is
    /// resolve's leniency backstop, not a replacement for that check.
    /// </summary>
    public static IReadOnlyList<Principle> ParsePrinciples(IReadOnlyList<string> lines)
    {
        var principles = new List<Principle>();
        foreach (var line in lines)
        {
            var fields = SplitFields(BoundLineLength(line));
            if (fields.Length < 1 || fields[0].Length == 0)
            {
                continue;
            }

            principles.Add(new Principle(fields[0], TailField(fields, 1)));
            if (principles.Count == MaxPrinciples)
            {
                break;
            }
        }

        return principles;
    }

    /// <summary>0 to 6; out-of-range values clamp instead of dropping to zero
    /// (BR-6) so a slightly-wrong env value still shows a believable belt.</summary>
    public static int ClampDegrees(int? degrees)
        => degrees is null ? 0 : Math.Clamp(degrees.Value, 0, MaxDegrees);

    /// <summary>First friendly error, or null when the lines are empty (no
    /// chart, BR-2) or form a valid four-node game plan.</summary>
    public static string? ValidateGamePlan(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            return null;
        }

        if (lines.Count != GamePlanNodeCount)
        {
            return $"Game plan needs exactly {GamePlanNodeCount} lines (yours has {lines.Count}).";
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Length > MaxLineLength)
            {
                return $"Game plan line {i + 1} is limited to {MaxLineLength} characters.";
            }

            var fields = SplitFields(lines[i]);
            if (fields.Length < 2 || fields[0].Length == 0 || fields[1].Length == 0)
            {
                return $"Game plan line {i + 1}: term and reading are required (format: term | reading | how).";
            }
        }

        return null;
    }

    /// <summary>First friendly error, or null when the lines are empty (no
    /// section, BR-2) or form 1 to 6 valid principles.</summary>
    public static string? ValidatePrinciples(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            return null;
        }

        if (lines.Count > MaxPrinciples)
        {
            return $"Principles: up to {MaxPrinciples} lines allowed (yours has {lines.Count}).";
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Length > MaxLineLength)
            {
                return $"Principles line {i + 1} is limited to {MaxLineLength} characters.";
            }

            var fields = SplitFields(lines[i]);
            if (fields.Length < 1 || fields[0].Length == 0)
            {
                return $"Principles line {i + 1}: a maxim is required (format: maxim | reading).";
            }
        }

        return null;
    }

    /// <summary>First friendly error, or null when unset or within 0 to 6.</summary>
    public static string? ValidateDegrees(int? degrees)
    {
        if (degrees is null)
        {
            return null;
        }

        return degrees is < 0 or > MaxDegrees
            ? $"Belt degrees must be between 0 and {MaxDegrees} (yours is {degrees})."
            : null;
    }
}
