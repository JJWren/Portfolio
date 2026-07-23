using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class PostRulesTests
{
    [Fact]
    public void CheckLengths_AcceptsFieldsAtExactLimits()
    {
        var error = PostRules.CheckLengths(
            new string('t', PostRules.TitleMaxLength),
            new string('s', PostRules.SlugMaxLength),
            new string('m', PostRules.SummaryMaxLength),
            new string('h', PostRules.HeaderImagePathMaxLength));

        Assert.Null(error);
    }

    [Fact]
    public void CheckLengths_AcceptsMissingHeaderImage()
    {
        Assert.Null(PostRules.CheckLengths("Title", "title", string.Empty, null));
    }

    [Fact]
    public void CheckLengths_RejectsTitleOverLimit()
    {
        var error = PostRules.CheckLengths(
            new string('t', PostRules.TitleMaxLength + 1), "slug", string.Empty, null);

        Assert.NotNull(error);
        Assert.Contains(PostRules.TitleMaxLength.ToString(), error);
    }

    [Fact]
    public void CheckLengths_RejectsSlugOverLimit()
    {
        var error = PostRules.CheckLengths(
            "Title", new string('s', PostRules.SlugMaxLength + 1), string.Empty, null);

        Assert.NotNull(error);
        Assert.Contains(PostRules.SlugMaxLength.ToString(), error);
    }

    [Fact]
    public void CheckLengths_RejectsSummaryOverLimit()
    {
        var error = PostRules.CheckLengths(
            "Title", "slug", new string('m', PostRules.SummaryMaxLength + 1), null);

        Assert.NotNull(error);
        Assert.Contains(PostRules.SummaryMaxLength.ToString(), error);
    }

    [Fact]
    public void CheckLengths_RejectsHeaderImagePathOverLimit()
    {
        var error = PostRules.CheckLengths(
            "Title", "slug", string.Empty, new string('h', PostRules.HeaderImagePathMaxLength + 1));

        Assert.NotNull(error);
        Assert.Contains(PostRules.HeaderImagePathMaxLength.ToString(), error);
    }
}
