using System.Globalization;

namespace Portfolio.Web.Services;

/// <summary>Hero game-plan chart node. Positionally colored by the caller
/// (BR-5); every node links to #principles (decision 4). <see cref="How"/>
/// may be blank.</summary>
public sealed record GamePlanNode(string Term, string Reading, string How);

/// <summary>One `maxim | reading` pair for the Principles section (BR-7).
/// <see cref="Reading"/> may be blank.</summary>
public sealed record Principle(string Maxim, string Reading);

/// <summary>The closed set of belt ranks the CSS knows (ADR 0002); anything
/// else is rejected at save (<see cref="BjjRules.ValidateEras"/>) and dropped
/// at resolve (<see cref="BjjRules.ParseEras"/>). Declared in ladder order,
/// white to black.</summary>
public enum Belt
{
    White,
    Blue,
    Purple,
    Brown,
    Black,
}

/// <summary>One row of the road: a belt earned on <see cref="Date"/> at
/// <see cref="Gym"/>/<see cref="Location"/>, with what the owner was doing
/// then (<see cref="Role"/>) and the stripe count earned on that belt
/// (BR-8). <see cref="Gym"/>, <see cref="Location"/> and <see cref="Role"/>
/// may be blank.</summary>
public sealed record Era(DateOnly Date, Belt Belt, int Stripes, string Gym, string Location, string Role);

/// <summary>One rung of the ladder: a distinct belt from <see cref="Era"/>
/// history, carrying the stripes of the last era on that belt (BR-8). See
/// <see cref="BjjRules.Rungs"/>.</summary>
public sealed record Rung(Belt Belt, int Stripes);

/// <summary>One `label | value` tile for the Now section (BR-10).
/// <see cref="Value"/> may be blank.</summary>
public sealed record NowItem(string Label, string Value);

/// <summary>
/// Pure parsing and validation for the BJJ landing flavor's structured copy
/// (game plan, principles, belt degrees, eras, now). Lenient at resolve
/// (BR-4): a bad env or stored value never takes the landing page down —
/// malformed lines are dropped silently rather than thrown. Strict at save:
/// the Validate* methods return the first friendly error for
/// <see cref="SiteContentRules.Validate"/> to surface.
/// </summary>
public static class BjjRules
{
    /// <summary>Widest a rank bar's stripe count may be (BR-6); also the
    /// widest an era's stripe count may be (BR-8) — one closed 0-to-6 range
    /// for every belt-degree field the flavor has.</summary>
    public const int MaxDegrees = 6;

    /// <summary>The game-plan chart is exactly this many nodes or hidden (BR-5).</summary>
    public const int GamePlanNodeCount = 4;

    /// <summary>Widest the Principles section may be (BR-7).</summary>
    public const int MaxPrinciples = 6;

    /// <summary>Widest the road (the era table/ladder) may be (BR-8).</summary>
    public const int MaxEras = 12;

    /// <summary>Widest the Now section may be (BR-10).</summary>
    public const int MaxNowItems = 8;

    /// <summary>Longest a single text[] line may be, in characters (BR-11).</summary>
    public const int MaxLineLength = 500;

    /// <summary>The exact format an Era's date field must parse with
    /// (<see cref="DateOnly.TryParseExact(string, string, IFormatProvider?, DateTimeStyles, out DateOnly)"/>),
    /// and the format its rendered <c>&lt;time datetime&gt;</c> uses (BR-16:
    /// ISO, never localized).</summary>
    public const string DateFormat = "yyyy-MM-dd";

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

    // -- Eras / the road / Now (BR-8, BR-9, BR-10) ------------------------

    /// <summary>Trimmed, case-insensitive belt name to <see cref="Belt"/>, or
    /// null for anything outside the closed set (ADR 0002) — rejected at
    /// save (<see cref="ValidateEras"/>) and dropped at resolve
    /// (<see cref="ParseEras"/>).</summary>
    public static Belt? ParseBelt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "white" => Belt.White,
            "blue" => Belt.Blue,
            "purple" => Belt.Purple,
            "brown" => Belt.Brown,
            "black" => Belt.Black,
            _ => null,
        };
    }

    /// <summary>Out-parameter form of <see cref="ParseBelt"/> for callers that
    /// read more naturally as a bool test (Unit 11's current-belt resolution
    /// and validation) — reuses the same mapping rather than declaring the
    /// closed set a second time.</summary>
    public static bool TryParseBelt(string? value, out Belt belt)
    {
        var parsed = ParseBelt(value);
        belt = parsed ?? default;
        return parsed is not null;
    }

    /// <summary>The lowercase belt name (ADR 0002's closed set) — the CSS
    /// class name (<c>--rank-white</c> to <c>--rank-black</c>) and
    /// `data-belt` attribute value used by the road table and rank bar, and
    /// also the display name used in disagreement messages
    /// (<see cref="ValidateCurrentBeltAgainstEras"/>,
    /// <see cref="ValidateDegreesAgainstEras"/>). Also the value stored in
    /// <see cref="Portfolio.Web.Data.SiteContent.CurrentBelt"/> and shown in
    /// the admin's current-belt select (BR-20, BR-25).</summary>
    public static string BeltName(Belt belt) => belt switch
    {
        Belt.White => "white",
        Belt.Blue => "blue",
        Belt.Purple => "purple",
        Belt.Brown => "brown",
        Belt.Black => "black",
        _ => throw new ArgumentOutOfRangeException(nameof(belt), belt, "Unknown belt."),
    };

    /// <summary>First friendly error, or null when the current-belt text is
    /// blank (the env value, then black, applies — BR-20) or one of the five
    /// belt names (BR-22).</summary>
    public static string? ValidateCurrentBelt(string? text)
        => string.IsNullOrWhiteSpace(text) || TryParseBelt(text, out _)
            ? null
            : "Current belt must be white, blue, purple, brown or black";

    /// <summary>
    /// `date | belt | stripes | gym | location | role` per line, in the
    /// order entered (BR-8: chronological order is the owner's job; nothing
    /// re-sorts). `date` must parse as <see cref="DateFormat"/>; `belt` must
    /// be in the closed set; `stripes` is 0 to <see cref="MaxDegrees"/>
    /// (blank means 0); `gym`, `location` and `role` may be blank. A line
    /// missing date/belt/stripes, or with an unparsable date, belt or
    /// stripes, is dropped (BR-4). `role` absorbs any extra "|" the way
    /// `how`/`reading` do (see <see cref="TailField"/>). Overlong lines are
    /// truncated, not dropped (see <see cref="BoundLineLength"/>). Capped at
    /// <see cref="MaxEras"/> lines — resolve's leniency backstop, mirroring
    /// <see cref="ParsePrinciples"/>'s cap at <see cref="MaxPrinciples"/>.
    /// </summary>
    public static IReadOnlyList<Era> ParseEras(IReadOnlyList<string> lines)
    {
        var eras = new List<Era>();
        foreach (var line in lines)
        {
            var fields = SplitFields(BoundLineLength(line));
            if (fields.Length < 3)
            {
                continue; // date, belt and stripes are all required (BR-8).
            }

            if (!TryParseDate(fields[0], out var date))
            {
                continue;
            }

            var belt = ParseBelt(fields[1]);
            if (belt is null)
            {
                continue;
            }

            if (!TryParseStripes(fields[2], out var stripes))
            {
                continue;
            }

            var gym = fields.Length > 3 ? fields[3] : string.Empty;
            var location = fields.Length > 4 ? fields[4] : string.Empty;
            eras.Add(new Era(date, belt.Value, stripes, gym, location, TailField(fields, 5)));
            if (eras.Count == MaxEras)
            {
                break;
            }
        }

        return eras;
    }

    /// <summary>
    /// One rung per distinct belt in <paramref name="eras"/>, in
    /// first-appearance order, each carrying the stripes of the LAST era on
    /// that belt (BR-8) — a colored belt re-striped later (say, blue
    /// appearing twice) still produces one rung, not two, and shows the more
    /// recent stripe count.
    /// </summary>
    public static IReadOnlyList<Rung> Rungs(IReadOnlyList<Era> eras)
    {
        var order = new List<Belt>();
        var lastStripes = new Dictionary<Belt, int>();
        foreach (var era in eras)
        {
            if (!lastStripes.ContainsKey(era.Belt))
            {
                order.Add(era.Belt);
            }

            lastStripes[era.Belt] = era.Stripes; // Last write wins: the LAST era of that belt.
        }

        return order.Select(belt => new Rung(belt, lastStripes[belt])).ToList();
    }

    /// <summary>
    /// `label | value` per line; `label` required, `value` may be blank.
    /// Malformed lines are dropped and overlong lines are truncated (see
    /// <see cref="BoundLineLength"/>); capped at <see cref="MaxNowItems"/>
    /// lines, mirroring <see cref="ParsePrinciples"/>'s cap.
    /// </summary>
    public static IReadOnlyList<NowItem> ParseNow(IReadOnlyList<string> lines)
    {
        var items = new List<NowItem>();
        foreach (var line in lines)
        {
            var fields = SplitFields(BoundLineLength(line));
            if (fields.Length < 1 || fields[0].Length == 0)
            {
                continue;
            }

            items.Add(new NowItem(fields[0], TailField(fields, 1)));
            if (items.Count == MaxNowItems)
            {
                break;
            }
        }

        return items;
    }

    private static bool TryParseDate(string field, out DateOnly date)
        => DateOnly.TryParseExact(field, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>Blank counts as 0 (BR-8); otherwise an integer 0 to <see cref="MaxDegrees"/>.</summary>
    private static bool TryParseStripes(string field, out int stripes)
    {
        if (field.Length == 0)
        {
            stripes = 0;
            return true;
        }

        return int.TryParse(field, NumberStyles.Integer, CultureInfo.InvariantCulture, out stripes)
            && stripes is >= 0 and <= MaxDegrees;
    }

    /// <summary>First friendly error, or null when the lines are empty (no
    /// road, BR-2) or form 1 to 12 valid eras.</summary>
    public static string? ValidateEras(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            return null;
        }

        if (lines.Count > MaxEras)
        {
            return $"Eras: at most {MaxEras} lines allowed (yours has {lines.Count}).";
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Length > MaxLineLength)
            {
                return $"Eras line {i + 1} is limited to {MaxLineLength} characters.";
            }

            var fields = SplitFields(lines[i]);
            if (fields.Length < 3)
            {
                return $"Eras line {i + 1}: date, belt and stripes are required " +
                    "(format: date | belt | stripes | gym | location | role).";
            }

            if (!TryParseDate(fields[0], out _))
            {
                return $"Eras line {i + 1}: date must be YYYY-MM-DD.";
            }

            if (ParseBelt(fields[1]) is null)
            {
                return $"Eras line {i + 1}: belt must be white, blue, purple, brown or black.";
            }

            if (!TryParseStripes(fields[2], out _))
            {
                return $"Eras line {i + 1}: stripes must be 0 to {MaxDegrees}.";
            }
        }

        return null;
    }

    /// <summary>First friendly error, or null when the lines are empty (no
    /// Now section, BR-2) or form 1 to 8 valid items.</summary>
    public static string? ValidateNow(IReadOnlyList<string> lines)
    {
        if (lines.Count == 0)
        {
            return null;
        }

        if (lines.Count > MaxNowItems)
        {
            return $"Now: up to {MaxNowItems} lines allowed (yours has {lines.Count}).";
        }

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Length > MaxLineLength)
            {
                return $"Now line {i + 1} is limited to {MaxLineLength} characters.";
            }

            var fields = SplitFields(lines[i]);
            if (fields.Length < 1 || fields[0].Length == 0)
            {
                return $"Now line {i + 1}: a label is required (format: label | value).";
            }
        }

        return null;
    }

    /// <summary>The highest-ranked belt among <paramref name="eras"/> in
    /// ladder order (the <see cref="Belt"/> enum's declared order, white to
    /// black), or null when there are no eras — used by
    /// <see cref="ValidateCurrentBeltAgainstEras"/> (BR-24).</summary>
    public static Belt? HighestBelt(IReadOnlyList<Era> eras)
        => eras.Count == 0 ? null : eras.Max(e => e.Belt);

    /// <summary>Formats one value for a disagreement message: <paramref
    /// name="value"/> alone when it came from the admin's own draft
    /// (<paramref name="source"/> null), or <c>"{value}, from {source}"</c>
    /// when it was instead resolved from an environment variable. Shared by
    /// <see cref="ValidateCurrentBeltAgainstEras"/> and
    /// <see cref="ValidateDegreesAgainstEras"/> for all four value/source
    /// pairs they format.</summary>
    private static string WithSource(string value, string? source)
        => source is null ? value : $"{value}, from {source}";

    /// <summary>The "; override X here or change the environment." sentence
    /// shared by <see cref="ValidateCurrentBeltAgainstEras"/> and
    /// <see cref="ValidateDegreesAgainstEras"/>: names "the eras" when
    /// <paramref name="erasSource"/> is the side that came from the
    /// environment, otherwise <paramref name="subject"/> naming the other
    /// side ("the current belt", "the belt degrees").</summary>
    private static string DisagreementSuffix(string? erasSource, string subject)
        => $"; override {(erasSource is not null ? "the eras" : subject)} here or change the environment.";

    /// <summary>
    /// BR-24: the effective current belt must never rank below the highest
    /// belt the road records — a practitioner's stated rank cannot
    /// contradict their own history, even though the road itself never
    /// derives from the current belt (BR-20). Null when there are no eras,
    /// or <paramref name="currentBelt"/> ranks at or above
    /// <see cref="HighestBelt"/>; a blank current belt resolves to black
    /// (BR-20), the top of the ladder, so it can never trip this rule.
    /// <paramref name="beltSource"/> and <paramref name="erasSource"/> name
    /// the environment variable a side came from ("SITE_CURRENT_BELT",
    /// "SITE_ERAS") exactly like <see cref="ValidateDegreesAgainstEras"/>;
    /// leave both null for the draft-only case. Lenient at resolve: a
    /// belt below the road never hides the bar, only blocks the save.
    /// </summary>
    public static string? ValidateCurrentBeltAgainstEras(
        Belt currentBelt, IReadOnlyList<Era> eras, string? beltSource = null, string? erasSource = null)
    {
        var highestBelt = HighestBelt(eras);
        if (highestBelt is null || currentBelt >= highestBelt.Value)
        {
            return null;
        }

        if (beltSource is null && erasSource is null)
        {
            return $"Current belt ({BeltName(currentBelt)}) is below the road's highest belt " +
                $"({BeltName(highestBelt.Value)}).";
        }

        var beltText = WithSource(BeltName(currentBelt), beltSource);
        var highestText = WithSource(BeltName(highestBelt.Value), erasSource);

        return $"Current belt ({beltText}) is below the road's highest belt ({highestText})" +
            DisagreementSuffix(erasSource, "the current belt");
    }

    /// <summary>
    /// BR-23 (BR-9 generalized from "the black belt era" to "the last era on
    /// the current belt"): one fact (the current belt's stripe count) is
    /// recorded in two places — <c>BeltDegrees</c> and the last era on
    /// <paramref name="currentBelt"/> — kept equal at the only place they can
    /// drift: the save gate. Null (unset degrees, or no era on the current
    /// belt yet) never conflicts; when both exist, the LAST era on the
    /// current belt in <paramref name="eras"/> must agree.
    /// <paramref name="degreesSource"/> and <paramref name="erasSource"/>
    /// optionally name the environment variable a value came from (for
    /// example "SITE_BELT_DEGREES", "SITE_ERAS") when the caller resolved it
    /// from the environment rather than the admin's own draft; the failure
    /// message then says so and points the admin at what to fix. Leave both
    /// null for the draft-only case — the message is unchanged.
    /// </summary>
    public static string? ValidateDegreesAgainstEras(
        int? degrees, Belt currentBelt, IReadOnlyList<Era> eras, string? degreesSource = null, string? erasSource = null)
    {
        if (degrees is null)
        {
            return null;
        }

        var lastEraOnBelt = eras.LastOrDefault(e => e.Belt == currentBelt);
        if (lastEraOnBelt is null || lastEraOnBelt.Stripes == degrees.Value)
        {
            return null;
        }

        var beltName = BeltName(currentBelt);
        if (degreesSource is null && erasSource is null)
        {
            return $"Belt degrees ({degrees.Value}) and the {beltName} belt era's stripes ({lastEraOnBelt.Stripes}) disagree.";
        }

        var degreesText = WithSource(degrees.Value.ToString(CultureInfo.InvariantCulture), degreesSource);
        var stripesText = WithSource(lastEraOnBelt.Stripes.ToString(CultureInfo.InvariantCulture), erasSource);

        return $"Belt degrees ({degreesText}) and the {beltName} belt era's stripes ({stripesText}) disagree" +
            DisagreementSuffix(erasSource, "the belt degrees");
    }
}
