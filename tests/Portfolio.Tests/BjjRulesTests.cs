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
            ["Guard", "Secure the position", "Auth, secrets."],
            BjjRules.SplitFields(" Guard | Secure the position | Auth, secrets. "));

    // -- ParseGamePlan ----------------------------------------------------

    [Fact]
    public void ParseGamePlan_FourValidLines_ReturnsFourNodesInOrder()
    {
        string[] lines =
        [
            "Guard | Secure the position | Auth, secrets, boundaries.",
            "Pass | Improve the position | Developer tooling.",
            "Mount | Keep control | Tests, reviews.",
            "Submit | Finish | Ship it.",
        ];

        var nodes = BjjRules.ParseGamePlan(lines);

        Assert.Equal(4, nodes.Count);
        Assert.Equal(new GamePlanNode("Guard", "Secure the position", "Auth, secrets, boundaries."), nodes[0]);
        Assert.Equal(new GamePlanNode("Submit", "Finish", "Ship it."), nodes[3]);
    }

    [Fact]
    public void ParseGamePlan_HowIsOptional()
    {
        string[] lines =
        [
            "Guard | Secure the position",
            "Pass | Improve the position",
            "Mount | Keep control",
            "Submit | Finish",
        ];

        var nodes = BjjRules.ParseGamePlan(lines);

        Assert.Equal(4, nodes.Count);
        Assert.All(nodes, n => Assert.Equal(string.Empty, n.How));
    }

    [Fact]
    public void ParseGamePlan_ThreeLines_ResolvesToEmpty()
        => Assert.Empty(BjjRules.ParseGamePlan(
        [
            "Guard | Secure the position",
            "Pass | Improve the position",
            "Mount | Keep control",
        ]));

    [Fact]
    public void ParseGamePlan_NoLines_ReturnsEmpty()
        => Assert.Empty(BjjRules.ParseGamePlan([]));

    [Fact]
    public void ParseGamePlan_MalformedLineDropped_LeavesOffCountChart_ResolvesEmpty()
    {
        // Four raw lines, but one ("Pass", no '|' at all) is malformed and
        // gets dropped, leaving three valid nodes — not four, so the whole
        // chart resolves to empty (BR-5) rather than an off-count chart.
        string[] lines =
        [
            "Guard | Secure the position",
            "Pass",
            "Mount | Keep control",
            "Submit | Finish",
        ];

        Assert.Empty(BjjRules.ParseGamePlan(lines));
    }

    [Fact]
    public void ParseGamePlan_BlankTermOrReading_LinesDropped()
    {
        string[] lines =
        [
            " | Secure the position", // blank term
            "Pass | ", // blank reading
            "Mount | Keep control",
            "Submit | Finish",
        ];

        // Both malformed lines drop, leaving two valid nodes: not four, so empty.
        Assert.Empty(BjjRules.ParseGamePlan(lines));
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
            "Guard | Secure the position",
            "Pass | Improve the position",
            "Mount | Keep control",
            "Submit | Finish",
        ]));

    [Fact]
    public void ValidateGamePlan_WrongCount_NamesExpectedCount()
    {
        var error = BjjRules.ValidateGamePlan(["Guard | Secure the position"]);

        Assert.NotNull(error);
        Assert.Contains("exactly 4", error);
    }

    [Fact]
    public void ValidateGamePlan_MalformedLine_NamesTheLineNumber()
    {
        var error = BjjRules.ValidateGamePlan(
        [
            "Guard | Secure the position",
            "Pass", // line 2: no '|', malformed
            "Mount | Keep control",
            "Submit | Finish",
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
            "Pass | Improve the position",
            "Mount | Keep control",
            "Submit | Finish",
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
}
