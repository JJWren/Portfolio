using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class CommentRulesTests
{
    [Fact]
    public void Validate_TrimsAndAcceptsNormalBody()
    {
        var result = CommentRules.Validate("  Nice post!  ", out var error);

        Assert.Equal("Nice post!", result);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_RejectsEmptyBodies(string? body)
    {
        var result = CommentRules.Validate(body, out var error);

        Assert.Null(result);
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_RejectsBodiesOverMaxLength()
    {
        var result = CommentRules.Validate(new string('x', CommentRules.MaxLength + 1), out var error);

        Assert.Null(result);
        Assert.Contains(CommentRules.MaxLength.ToString(), error);
    }

    [Fact]
    public void Validate_AcceptsBodyAtExactlyMaxLength()
    {
        var result = CommentRules.Validate(new string('x', CommentRules.MaxLength), out var error);

        Assert.NotNull(result);
        Assert.Null(error);
    }
}
