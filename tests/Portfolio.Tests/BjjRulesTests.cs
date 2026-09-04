using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class BjjRulesTests
{
    // -- SplitLines / SplitFields ---------------------------------------

    [Fact]
    public void SplitLines_TrimsAndDropsBlankLines()
        => Assert.Equal(["a | b", "c | d"], BjjRules.SplitLines("a | b\r\n\n  c | d  \n"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SplitLines_NoContent_ReturnsEmpty(string? value)
        => Assert.Empty(BjjRules.SplitLines(value));

    [Fact]
    public void SplitFields_SplitsOnPipeAndTrimsEachField()
        => Assert.Equal(
            ["Warm-up", "Loosen up", "Stretch first."],
            BjjRules.SplitFields(" Warm-up | Loosen up | Stretch first. "));

    // -- ParseGamePlan ----------------------------------------------------

    [Fact]
    public void ParseGamePlan_FourValidLines_ReturnsFourNodesInOrder()
    {
        string[] lines =
        [
            "Warm-up | Loosen up | Stretch first.",
            "Drill | Repeat the motion | Slow reps first.",
            "Roll | Test it live | Go at full speed.",
            "Rest | Recover | Sleep and eat well.",
        ];

        var nodes = BjjRules.ParseGamePlan(lines);

        Assert.Equal(4, nodes.Count);
        Assert.Equal(new GamePlanNode("Warm-up", "Loosen up", "Stretch first."), nodes[0]);
        Assert.Equal(new GamePlanNode("Rest", "Recover", "Sleep and eat well."), nodes[3]);
    }

    [Fact]
    public void ParseGamePlan_HowIsOptional()
    {
        string[] lines =
        [
            "Warm-up | Loosen up",
            "Drill | Repeat the motion",
            "Roll | Test it live",
            "Rest | Recover",
        ];

        var nodes = BjjRules.ParseGamePlan(lines);

        Assert.Equal(4, nodes.Count);
        Assert.All(nodes, n => Assert.Equal(string.Empty, n.How));
    }

    [Fact]
    public void ParseGamePlan_ThreeLines_ResolvesToEmpty()
        => Assert.Empty(BjjRules.ParseGamePlan(
        [
            "Warm-up | Loosen up",
            "Drill | Repeat the motion",
            "Roll | Test it live",
        ]));

    [Fact]
    public void ParseGamePlan_NoLines_ReturnsEmpty()
        => Assert.Empty(BjjRules.ParseGamePlan([]));

    [Fact]
    public void ParseGamePlan_MalformedLineDropped_LeavesOffCountChart_ResolvesEmpty()
    {
        // Four raw lines, but one ("Nope", no '|' at all) is malformed and
        // gets dropped, leaving three valid nodes — not four, so the whole
        // chart resolves to empty (BR-5) rather than an off-count chart.
        string[] lines =
        [
            "Warm-up | Loosen up",
            "Nope",
            "Roll | Test it live",
            "Rest | Recover",
        ];

        Assert.Empty(BjjRules.ParseGamePlan(lines));
    }

    [Fact]
    public void ParseGamePlan_BlankTermOrReading_LinesDropped()
    {
        string[] lines =
        [
            " | Loosen up", // blank term
            "Drill | ", // blank reading
            "Roll | Test it live",
            "Rest | Recover",
        ];

        // Both malformed lines drop, leaving two valid nodes: not four, so empty.
        Assert.Empty(BjjRules.ParseGamePlan(lines));
    }

    [Fact]
    public void ParseGamePlan_OverlongLine_TruncatesInsteadOfDroppingTheLine()
    {
        // The "how" field alone pushes this line past MaxLineLength; term
        // and reading are short, so they survive untouched and only the
        // tail of "how" is clipped.
        var overlongHow = new string('a', 600);
        string[] lines =
        [
            $"Warm-up | Loosen up | {overlongHow}",
            "Drill | Repeat the motion",
            "Roll | Test it live",
            "Rest | Recover",
        ];
        Assert.True(lines[0].Length > BjjRules.MaxLineLength);

        var nodes = BjjRules.ParseGamePlan(lines);

        // BR-4/BR-11: the overlong line is truncated, not dropped — dropping
        // it would leave three nodes, and BR-5 requires exactly four or
        // none, so the whole chart would vanish instead of just losing the
        // tail of one "how" line.
        Assert.Equal(4, nodes.Count);
        Assert.Equal("Warm-up", nodes[0].Term);
        Assert.Equal("Loosen up", nodes[0].Reading);
        Assert.True(nodes[0].How.Length < overlongHow.Length);
    }

    // -- ParsePrinciples ----------------------------------------------------

    [Fact]
    public void ParsePrinciples_ValidLines_ReturnsPrinciplesInOrder()
    {
        string[] lines = ["Ship small. | Small is safe.", "Write it down. | "];

        var principles = BjjRules.ParsePrinciples(lines);

        Assert.Equal(2, principles.Count);
        Assert.Equal(new Principle("Ship small.", "Small is safe."), principles[0]);
        Assert.Equal(new Principle("Write it down.", string.Empty), principles[1]);
    }

    [Fact]
    public void ParsePrinciples_ReadingIsOptional()
    {
        var principles = BjjRules.ParsePrinciples(["Ship small."]);

        Assert.Single(principles);
        Assert.Equal(string.Empty, principles[0].Reading);
    }

    [Fact]
    public void ParsePrinciples_BlankMaxim_LineDropped()
    {
        var principles = BjjRules.ParsePrinciples([" | Small is safe.", "Write it down. | "]);

        Assert.Single(principles);
        Assert.Equal("Write it down.", principles[0].Maxim);
    }

    [Fact]
    public void ParsePrinciples_NoLines_ReturnsEmpty()
        => Assert.Empty(BjjRules.ParsePrinciples([]));

    [Fact]
    public void ParsePrinciples_OverlongLine_TruncatesInsteadOfDroppingTheLine()
    {
        var overlongReading = new string('a', 600);
        string line = $"Show up. | {overlongReading}";
        Assert.True(line.Length > BjjRules.MaxLineLength);

        var principles = BjjRules.ParsePrinciples([line]);

        Assert.Single(principles);
        Assert.Equal("Show up.", principles[0].Maxim);
        Assert.True(principles[0].Reading.Length < overlongReading.Length);
    }

    [Fact]
    public void ParsePrinciples_MoreThanMaxPrinciples_CapsAtTheLimit()
    {
        var lines = Enumerable.Range(1, BjjRules.MaxPrinciples + 3).Select(i => $"Line {i} | reading").ToArray();

        var principles = BjjRules.ParsePrinciples(lines);

        // Resolve's leniency backstop (BR-4): Validate is what normally
        // rejects too many lines at save with a friendly message; this cap
        // just ensures Resolve itself can never hand the page more than the
        // section's layout supports.
        Assert.Equal(BjjRules.MaxPrinciples, principles.Count);
        Assert.Equal("Line 1", principles[0].Maxim);
        Assert.Equal($"Line {BjjRules.MaxPrinciples}", principles[^1].Maxim);
    }

    // -- ClampDegrees ----------------------------------------------------

    [Theory]
    [InlineData(null, 0)]
    [InlineData(0, 0)]
    [InlineData(3, 3)]
    [InlineData(6, 6)]
    [InlineData(7, 6)]
    [InlineData(-1, 0)]
    [InlineData(100, 6)]
    public void ClampDegrees_ClampsToZeroToSix(int? input, int expected)
        => Assert.Equal(expected, BjjRules.ClampDegrees(input));

    // -- ValidateGamePlan ----------------------------------------------------

    [Fact]
    public void ValidateGamePlan_Empty_ReturnsNull()
        => Assert.Null(BjjRules.ValidateGamePlan([]));

    [Fact]
    public void ValidateGamePlan_FourValidLines_ReturnsNull()
        => Assert.Null(BjjRules.ValidateGamePlan(
        [
            "Warm-up | Loosen up",
            "Drill | Repeat the motion",
            "Roll | Test it live",
            "Rest | Recover",
        ]));

    [Fact]
    public void ValidateGamePlan_WrongCount_NamesExpectedCount()
    {
        var error = BjjRules.ValidateGamePlan(["Warm-up | Loosen up"]);

        Assert.NotNull(error);
        Assert.Contains("exactly 4", error);
    }

    [Fact]
    public void ValidateGamePlan_MalformedLine_NamesTheLineNumber()
    {
        var error = BjjRules.ValidateGamePlan(
        [
            "Warm-up | Loosen up",
            "Nope", // line 2: no '|', malformed
            "Roll | Test it live",
            "Rest | Recover",
        ]);

        Assert.NotNull(error);
        Assert.Contains("Game plan line 2", error);
    }

    [Fact]
    public void ValidateGamePlan_OverlongLine_NamesTheLineNumber()
    {
        string[] lines =
        [
            new string('x', BjjRules.MaxLineLength + 1) + " | reading",
            "Drill | Repeat the motion",
            "Roll | Test it live",
            "Rest | Recover",
        ];

        var error = BjjRules.ValidateGamePlan(lines);

        Assert.NotNull(error);
        Assert.Contains("Game plan line 1", error);
    }

    // -- ValidatePrinciples ----------------------------------------------------

    [Fact]
    public void ValidatePrinciples_Empty_ReturnsNull()
        => Assert.Null(BjjRules.ValidatePrinciples([]));

    [Fact]
    public void ValidatePrinciples_UpToSixLines_ReturnsNull()
        => Assert.Null(BjjRules.ValidatePrinciples(["a | b", "c | d", "e | f", "g | h", "i | j", "k | l"]));

    [Fact]
    public void ValidatePrinciples_TooManyLines_NamesTheLimit()
    {
        var lines = Enumerable.Range(1, 7).Select(i => $"Line {i} | reading").ToArray();

        var error = BjjRules.ValidatePrinciples(lines);

        Assert.NotNull(error);
        Assert.Contains("up to 6", error);
    }

    [Fact]
    public void ValidatePrinciples_BlankMaxim_NamesTheLineNumber()
    {
        var error = BjjRules.ValidatePrinciples(["Ship small. | ok", " | blank maxim"]);

        Assert.NotNull(error);
        Assert.Contains("Principles line 2", error);
    }

    [Fact]
    public void ValidatePrinciples_OverlongLine_NamesTheLineNumber()
    {
        var error = BjjRules.ValidatePrinciples(["ok | ok", new string('x', BjjRules.MaxLineLength + 1)]);

        Assert.NotNull(error);
        Assert.Contains("Principles line 2", error);
    }

    // -- ValidateDegrees ----------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(6)]
    public void ValidateDegrees_WithinRangeOrUnset_ReturnsNull(int? degrees)
        => Assert.Null(BjjRules.ValidateDegrees(degrees));

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void ValidateDegrees_OutOfRange_NamesTheRange(int degrees)
    {
        var error = BjjRules.ValidateDegrees(degrees);

        Assert.NotNull(error);
        Assert.Contains("0 and 6", error);
    }

    [Fact]
    public void ParseGamePlan_ExtraSeparatorInHow_KeepsTheWholeTail()
    {
        // The last field absorbs extra pipes instead of losing the text after them.
        string[] lines =
        [
            "Warm-up | Loosen up | Stretch first | then breathe",
            "Drill | Repeat the motion | Slowly",
            "Roll | Test it live | Lightly",
            "Rest | Recover | Sleep",
        ];

        var nodes = BjjRules.ParseGamePlan(lines);

        Assert.Equal(4, nodes.Count);
        Assert.Equal("Stretch first | then breathe", nodes[0].How);
    }

    [Fact]
    public void ParsePrinciples_ExtraSeparatorInReading_KeepsTheWholeTail()
    {
        var principles = BjjRules.ParsePrinciples(["Show up. | Consistency | not talent"]);

        var principle = Assert.Single(principles);
        Assert.Equal("Show up.", principle.Maxim);
        Assert.Equal("Consistency | not talent", principle.Reading);
    }
}
