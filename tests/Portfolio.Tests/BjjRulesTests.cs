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

    // -- ParseBelt / CssName ----------------------------------------------

    [Theory]
    [InlineData("white", Belt.White)]
    [InlineData("blue", Belt.Blue)]
    [InlineData("purple", Belt.Purple)]
    [InlineData("brown", Belt.Brown)]
    [InlineData("black", Belt.Black)]
    [InlineData("White", Belt.White)]
    [InlineData("BLUE", Belt.Blue)]
    [InlineData(" black ", Belt.Black)]
    public void ParseBelt_KnownNames_ParsesTrimmedAndCaseInsensitively(string value, Belt expected)
        => Assert.Equal(expected, BjjRules.ParseBelt(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("coral")]
    [InlineData("red")]
    [InlineData("whites")]
    public void ParseBelt_UnknownOrBlank_ReturnsNull(string? value)
        => Assert.Null(BjjRules.ParseBelt(value));

    [Theory]
    [InlineData(Belt.White, "white")]
    [InlineData(Belt.Blue, "blue")]
    [InlineData(Belt.Purple, "purple")]
    [InlineData(Belt.Brown, "brown")]
    [InlineData(Belt.Black, "black")]
    public void CssName_ReturnsTheLowercaseClassName(Belt belt, string expected)
        => Assert.Equal(expected, BjjRules.CssName(belt));

    // -- ParseEras ----------------------------------------------------------

    [Fact]
    public void ParseEras_GoodLine_ParsesEveryField()
    {
        var eras = BjjRules.ParseEras(["2020-09-23 | brown | 4 | Test Gym | Test City | Coaching."]);

        var era = Assert.Single(eras);
        Assert.Equal(new DateOnly(2020, 9, 23), era.Date);
        Assert.Equal(Belt.Brown, era.Belt);
        Assert.Equal(4, era.Stripes);
        Assert.Equal("Test Gym", era.Gym);
        Assert.Equal("Test City", era.Location);
        Assert.Equal("Coaching.", era.Role);
    }

    [Fact]
    public void ParseEras_BlankStripesField_CountsAsZero()
    {
        var eras = BjjRules.ParseEras(["2020-09-23 | brown |  | Test Gym | Test City | Coaching."]);

        Assert.Equal(0, Assert.Single(eras).Stripes);
    }

    [Fact]
    public void ParseEras_BadDate_LineDropped()
        => Assert.Empty(BjjRules.ParseEras(["2020-13-40 | brown | 4 | Gym | City | Role."]));

    [Fact]
    public void ParseEras_NonIsoDateFormat_LineDropped()
        // BR-8 requires exactly YYYY-MM-DD; a different, otherwise-valid date
        // format must still be rejected (dropped, not reinterpreted).
        => Assert.Empty(BjjRules.ParseEras(["09/23/2020 | brown | 4 | Gym | City | Role."]));

    [Fact]
    public void ParseEras_BadBelt_LineDropped()
        => Assert.Empty(BjjRules.ParseEras(["2020-09-23 | coral | 4 | Gym | City | Role."]));

    [Fact]
    public void ParseEras_StripesAboveMax_LineDropped()
        => Assert.Empty(BjjRules.ParseEras(["2020-09-23 | brown | 7 | Gym | City | Role."]));

    [Fact]
    public void ParseEras_NegativeStripes_LineDropped()
        => Assert.Empty(BjjRules.ParseEras(["2020-09-23 | brown | -1 | Gym | City | Role."]));

    [Fact]
    public void ParseEras_NonNumericStripes_LineDropped()
        => Assert.Empty(BjjRules.ParseEras(["2020-09-23 | brown | four | Gym | City | Role."]));

    [Fact]
    public void ParseEras_FewerThanThreeFields_LineDropped()
        => Assert.Empty(BjjRules.ParseEras(["2020-09-23 | brown"]));

    [Fact]
    public void ParseEras_BlankGymLocationRole_Allowed()
    {
        var eras = BjjRules.ParseEras(["2020-09-23 | brown | 4"]);

        var era = Assert.Single(eras);
        Assert.Equal(string.Empty, era.Gym);
        Assert.Equal(string.Empty, era.Location);
        Assert.Equal(string.Empty, era.Role);
    }

    [Fact]
    public void ParseEras_OrderIsPreservedAsEntered()
    {
        string[] lines =
        [
            "2020-09-23 | brown | 4 | Gym | City | Third.",
            "2005-12-01 | white | 2 | Gym | City | First.",
            "2018-01-30 | blue | 3 | Gym | City | Second.",
        ];

        var eras = BjjRules.ParseEras(lines);

        // Nothing re-sorts by date (BR-8: chronological order is the
        // owner's job) — entry order survives even though these dates
        // are not ascending.
        Assert.Equal("Third.", eras[0].Role);
        Assert.Equal("First.", eras[1].Role);
        Assert.Equal("Second.", eras[2].Role);
    }

    [Fact]
    public void ParseEras_MoreThanMaxEras_CapsAtTheLimit()
    {
        var lines = Enumerable.Range(1, BjjRules.MaxEras + 3)
            .Select(i => $"2020-01-{i:D2} | white | 0 | Gym | City | Role {i}")
            .ToArray();

        var eras = BjjRules.ParseEras(lines);

        Assert.Equal(BjjRules.MaxEras, eras.Count);
        Assert.Equal("Role 1", eras[0].Role);
        Assert.Equal($"Role {BjjRules.MaxEras}", eras[^1].Role);
    }

    [Fact]
    public void ParseEras_ExtraSeparatorInRole_KeepsTheWholeTail()
    {
        var eras = BjjRules.ParseEras(["2020-09-23 | brown | 4 | Gym | City | Coaching | and teaching"]);

        Assert.Equal("Coaching | and teaching", Assert.Single(eras).Role);
    }

    [Fact]
    public void ParseEras_OverlongLine_TruncatesInsteadOfDroppingTheLine()
    {
        var overlongRole = new string('a', 600);
        var line = $"2020-09-23 | brown | 4 | Gym | City | {overlongRole}";
        Assert.True(line.Length > BjjRules.MaxLineLength);

        var eras = BjjRules.ParseEras([line]);

        var era = Assert.Single(eras);
        Assert.Equal(new DateOnly(2020, 9, 23), era.Date);
        Assert.True(era.Role.Length < overlongRole.Length);
    }

    // -- Rungs ----------------------------------------------------------

    [Fact]
    public void Rungs_FiveDistinctBelts_OneRungEachInFirstAppearanceOrder()
    {
        Era[] eras =
        [
            new(new DateOnly(2005, 12, 1), Belt.White, 2, "Gym", "City", "Role"),
            new(new DateOnly(2018, 1, 30), Belt.Blue, 3, "Gym", "City", "Role"),
            new(new DateOnly(2019, 8, 23), Belt.Purple, 1, "Gym", "City", "Role"),
            new(new DateOnly(2020, 9, 23), Belt.Brown, 4, "Gym", "City", "Role"),
            new(new DateOnly(2025, 12, 9), Belt.Black, 0, "Gym", "City", "Role"),
        ];

        var rungs = BjjRules.Rungs(eras);

        Assert.Equal([Belt.White, Belt.Blue, Belt.Purple, Belt.Brown, Belt.Black], rungs.Select(r => r.Belt));
        Assert.Equal(2, rungs[0].Stripes);
        Assert.Equal(0, rungs[4].Stripes);
    }

    [Fact]
    public void Rungs_RepeatedBelt_YieldsOneRungCarryingTheLastErasStripes()
    {
        Era[] eras =
        [
            new(new DateOnly(2014, 3, 20), Belt.Purple, 1, "Gym", "City", "First purple era."),
            new(new DateOnly(2015, 1, 1), Belt.Purple, 3, "Gym", "City", "Later purple era."),
        ];

        var rungs = BjjRules.Rungs(eras);

        var rung = Assert.Single(rungs);
        Assert.Equal(Belt.Purple, rung.Belt);
        Assert.Equal(3, rung.Stripes); // The LAST era's stripes, not the first.
    }

    [Fact]
    public void Rungs_RepeatedBeltNotAdjacent_KeepsItsFirstAppearancePosition()
    {
        Era[] eras =
        [
            new(new DateOnly(2014, 3, 20), Belt.Purple, 1, "Gym", "City", "Purple."),
            new(new DateOnly(2016, 9, 9), Belt.Brown, 4, "Gym", "City", "Brown."),
            new(new DateOnly(2017, 1, 1), Belt.Purple, 4, "Gym", "City", "Purple again."),
        ];

        var rungs = BjjRules.Rungs(eras);

        Assert.Equal(2, rungs.Count);
        Assert.Equal(Belt.Purple, rungs[0].Belt); // First-appearance order wins the position...
        Assert.Equal(4, rungs[0].Stripes);         // ...but the stripe count is still the LAST era's.
        Assert.Equal(Belt.Brown, rungs[1].Belt);
    }

    [Fact]
    public void Rungs_NoEras_ReturnsEmpty()
        => Assert.Empty(BjjRules.Rungs([]));

    // -- ParseNow ----------------------------------------------------------

    [Fact]
    public void ParseNow_LabelRequiredValueOptional()
    {
        var items = BjjRules.ParseNow(["Teaches | Adult no-gi.", "Building | "]);

        Assert.Equal(2, items.Count);
        Assert.Equal(new NowItem("Teaches", "Adult no-gi."), items[0]);
        Assert.Equal(new NowItem("Building", string.Empty), items[1]);
    }

    [Fact]
    public void ParseNow_BlankLabel_LineDropped()
    {
        var items = BjjRules.ParseNow([" | value only", "Teaches | Adult no-gi."]);

        Assert.Single(items);
        Assert.Equal("Teaches", items[0].Label);
    }

    [Fact]
    public void ParseNow_NoLines_ReturnsEmpty()
        => Assert.Empty(BjjRules.ParseNow([]));

    [Fact]
    public void ParseNow_MoreThanMaxNowItems_CapsAtTheLimit()
    {
        var lines = Enumerable.Range(1, BjjRules.MaxNowItems + 2).Select(i => $"Label {i} | Value {i}").ToArray();

        var items = BjjRules.ParseNow(lines);

        Assert.Equal(BjjRules.MaxNowItems, items.Count);
        Assert.Equal("Label 1", items[0].Label);
        Assert.Equal($"Label {BjjRules.MaxNowItems}", items[^1].Label);
    }

    [Fact]
    public void ParseNow_ExtraSeparatorInValue_KeepsTheWholeTail()
    {
        var items = BjjRules.ParseNow(["Building | A bot | with reminders"]);

        Assert.Equal("A bot | with reminders", Assert.Single(items).Value);
    }

    [Fact]
    public void ParseNow_OverlongLine_TruncatesInsteadOfDroppingTheLine()
    {
        var overlongValue = new string('a', 600);
        var line = $"Building | {overlongValue}";

        var items = BjjRules.ParseNow([line]);

        var item = Assert.Single(items);
        Assert.Equal("Building", item.Label);
        Assert.True(item.Value.Length < overlongValue.Length);
    }

    // -- ValidateEras ----------------------------------------------------------

    [Fact]
    public void ValidateEras_Empty_ReturnsNull()
        => Assert.Null(BjjRules.ValidateEras([]));

    [Fact]
    public void ValidateEras_ValidLines_ReturnsNull()
        => Assert.Null(BjjRules.ValidateEras(
        [
            "2005-12-01 | white | 2 | Gym | City | Role.",
            "2018-01-30 | blue | 3 | Gym | City | Role.",
        ]));

    [Fact]
    public void ValidateEras_BlankGymLocationRole_Allowed()
        => Assert.Null(BjjRules.ValidateEras(["2020-09-23 | brown | 4"]));

    [Fact]
    public void ValidateEras_BadDate_NamesTheLineNumber()
    {
        var error = BjjRules.ValidateEras(
        [
            "2005-12-01 | white | 2 | Gym | City | Role.",
            "not-a-date | blue | 3 | Gym | City | Role.",
        ]);

        Assert.NotNull(error);
        Assert.Contains("Eras line 2", error);
        Assert.Contains("date must be YYYY-MM-DD", error);
    }

    [Fact]
    public void ValidateEras_BadBelt_NamesTheLineNumber()
    {
        var error = BjjRules.ValidateEras(
        [
            "2005-12-01 | white | 2 | Gym | City | Role.",
            "2018-01-30 | coral | 3 | Gym | City | Role.",
            "2019-08-23 | purple | 1 | Gym | City | Role.",
        ]);

        Assert.NotNull(error);
        Assert.Contains("Eras line 2", error);
        Assert.Contains("belt must be white, blue, purple, brown or black", error);
    }

    [Fact]
    public void ValidateEras_StripesOutOfRange_NamesTheLineNumber()
    {
        var error = BjjRules.ValidateEras(
        [
            "2005-12-01 | white | 2 | Gym | City | Role.",
            "2018-01-30 | blue | 3 | Gym | City | Role.",
            "2019-08-23 | purple | 1 | Gym | City | Role.",
            "2020-09-23 | brown | 7 | Gym | City | Role.",
        ]);

        Assert.NotNull(error);
        Assert.Contains("Eras line 4", error);
        Assert.Contains("stripes must be 0 to 6", error);
    }

    [Fact]
    public void ValidateEras_TooManyLines_NamesTheLimit()
    {
        var lines = Enumerable.Range(1, BjjRules.MaxEras + 1)
            .Select(i => $"2020-01-01 | white | 0 | Gym | City | Role {i}")
            .ToArray();

        var error = BjjRules.ValidateEras(lines);

        Assert.NotNull(error);
        Assert.Contains("at most 12", error);
    }

    [Fact]
    public void ValidateEras_MissingRequiredFields_NamesTheLineNumber()
    {
        var error = BjjRules.ValidateEras(["2020-09-23 | brown"]);

        Assert.NotNull(error);
        Assert.Contains("Eras line 1", error);
        Assert.Contains("date, belt and stripes are required", error);
    }

    [Fact]
    public void ValidateEras_OverlongLine_NamesTheLineNumber()
    {
        string[] lines =
        [
            "2005-12-01 | white | 2 | Gym | City | Role.",
            $"2018-01-30 | blue | 3 | Gym | City | {new string('x', BjjRules.MaxLineLength)}",
        ];

        var error = BjjRules.ValidateEras(lines);

        Assert.NotNull(error);
        Assert.Contains("Eras line 2", error);
        Assert.Contains($"limited to {BjjRules.MaxLineLength} characters", error);
    }

    // -- ValidateNow ----------------------------------------------------------

    [Fact]
    public void ValidateNow_Empty_ReturnsNull()
        => Assert.Null(BjjRules.ValidateNow([]));

    [Fact]
    public void ValidateNow_ValidLines_ReturnsNull()
        => Assert.Null(BjjRules.ValidateNow(["Teaches | Adult no-gi.", "Building | "]));

    [Fact]
    public void ValidateNow_TooManyLines_NamesTheLimit()
    {
        var lines = Enumerable.Range(1, BjjRules.MaxNowItems + 1).Select(i => $"Label {i} | Value {i}").ToArray();

        var error = BjjRules.ValidateNow(lines);

        Assert.NotNull(error);
        Assert.Contains("up to 8", error);
    }

    [Fact]
    public void ValidateNow_BlankLabel_NamesTheLineNumber()
    {
        var error = BjjRules.ValidateNow(["Teaches | Adult no-gi.", " | blank label"]);

        Assert.NotNull(error);
        Assert.Contains("Now line 2", error);
        Assert.Contains("a label is required", error);
    }

    [Fact]
    public void ValidateNow_OverlongLine_NamesTheLineNumber()
    {
        var error = BjjRules.ValidateNow(["ok | ok", new string('x', BjjRules.MaxLineLength + 1)]);

        Assert.NotNull(error);
        Assert.Contains("Now line 2", error);
    }

    // -- ValidateDegreesAgainstEras (BR-9) --------------------------------

    [Fact]
    public void ValidateDegreesAgainstEras_DegreesUnset_ReturnsNull()
    {
        Era[] eras = [new(new DateOnly(2025, 12, 9), Belt.Black, 3, "Gym", "City", "Role")];

        Assert.Null(BjjRules.ValidateDegreesAgainstEras(null, eras));
    }

    [Fact]
    public void ValidateDegreesAgainstEras_NoBlackEra_ReturnsNull()
    {
        Era[] eras = [new(new DateOnly(2020, 9, 23), Belt.Brown, 4, "Gym", "City", "Role")];

        Assert.Null(BjjRules.ValidateDegreesAgainstEras(3, eras));
    }

    [Fact]
    public void ValidateDegreesAgainstEras_NoErasAtAll_ReturnsNull()
        => Assert.Null(BjjRules.ValidateDegreesAgainstEras(3, []));

    [Fact]
    public void ValidateDegreesAgainstEras_MatchingStripes_ReturnsNull()
    {
        Era[] eras = [new(new DateOnly(2025, 12, 9), Belt.Black, 3, "Gym", "City", "Role")];

        Assert.Null(BjjRules.ValidateDegreesAgainstEras(3, eras));
    }

    [Fact]
    public void ValidateDegreesAgainstEras_DisagreeingStripes_ReturnsFriendlyError()
    {
        Era[] eras = [new(new DateOnly(2025, 12, 9), Belt.Black, 1, "Gym", "City", "Role")];

        var error = BjjRules.ValidateDegreesAgainstEras(0, eras);

        Assert.NotNull(error);
        Assert.Contains("Belt degrees (0)", error);
        Assert.Contains("black belt era's stripes (1)", error);
        Assert.Contains("disagree", error);
    }

    [Fact]
    public void ValidateDegreesAgainstEras_MultipleBlackEras_UsesTheLastOne()
    {
        Era[] eras =
        [
            new(new DateOnly(2025, 12, 9), Belt.Black, 0, "Gym", "City", "First black era."),
            new(new DateOnly(2026, 6, 1), Belt.Black, 1, "Gym", "City", "Second black era."),
        ];

        // Degrees agrees with the LAST black era (1), not the first (0).
        Assert.Null(BjjRules.ValidateDegreesAgainstEras(1, eras));
        Assert.NotNull(BjjRules.ValidateDegreesAgainstEras(0, eras));
    }
}
