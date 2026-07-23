using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class ProjectRulesTests
{
    [Fact]
    public void CheckLengths_AcceptsFieldsAtExactLimits()
    {
        var error = ProjectRules.CheckLengths(
            new string('t', ProjectRules.TitleMaxLength),
            new string('m', ProjectRules.SummaryMaxLength),
            new string('h', ProjectRules.HeaderImagePathMaxLength),
            new string('u', ProjectRules.HomepageUrlMaxLength),
            new string('r', ProjectRules.RepoUrlMaxLength));

        Assert.Null(error);
    }

    [Fact]
    public void CheckLengths_AcceptsMissingOptionalFields()
    {
        Assert.Null(ProjectRules.CheckLengths("Title", string.Empty, null, null, null));
    }

    [Fact]
    public void CheckLengths_RejectsTitleOverLimit()
    {
        var error = ProjectRules.CheckLengths(
            new string('t', ProjectRules.TitleMaxLength + 1), string.Empty, null, null, null);

        Assert.NotNull(error);
        Assert.Contains(ProjectRules.TitleMaxLength.ToString(), error);
    }

    [Fact]
    public void CheckLengths_RejectsSummaryOverLimit()
    {
        var error = ProjectRules.CheckLengths(
            "Title", new string('m', ProjectRules.SummaryMaxLength + 1), null, null, null);

        Assert.NotNull(error);
        Assert.Contains(ProjectRules.SummaryMaxLength.ToString(), error);
    }

    [Fact]
    public void CheckLengths_RejectsHeaderImagePathOverLimit()
    {
        var error = ProjectRules.CheckLengths(
            "Title", string.Empty, new string('h', ProjectRules.HeaderImagePathMaxLength + 1), null, null);

        Assert.NotNull(error);
        Assert.Contains(ProjectRules.HeaderImagePathMaxLength.ToString(), error);
    }

    [Fact]
    public void CheckLengths_RejectsHomepageUrlOverLimit()
    {
        var error = ProjectRules.CheckLengths(
            "Title", string.Empty, null, new string('u', ProjectRules.HomepageUrlMaxLength + 1), null);

        Assert.NotNull(error);
        Assert.Contains(ProjectRules.HomepageUrlMaxLength.ToString(), error);
    }

    [Fact]
    public void CheckLengths_RejectsRepoUrlOverLimit()
    {
        var error = ProjectRules.CheckLengths(
            "Title", string.Empty, null, null, new string('r', ProjectRules.RepoUrlMaxLength + 1));

        Assert.NotNull(error);
        Assert.Contains(ProjectRules.RepoUrlMaxLength.ToString(), error);
    }
}
