using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Web.Services;

namespace Portfolio.Tests;

public class MailDomainCheckerTests
{
    private sealed class FakeResolver(Func<string, Task<bool?>> answer) : IMxResolver
    {
        public Task<bool?> HasMailServerAsync(string domain, CancellationToken cancellationToken)
            => answer(domain);
    }

    private static MailDomainChecker Create(Func<string, Task<bool?>> answer)
        => new(new FakeResolver(answer), NullLogger<MailDomainChecker>.Instance);

    [Fact]
    public async Task CheckAsync_MailServerFound_ReturnsDeliverable()
    {
        var checker = Create(_ => Task.FromResult<bool?>(true));
        Assert.Equal(MailDomainResult.Deliverable, await checker.CheckAsync("a@gmail.com"));
    }

    [Fact]
    public async Task CheckAsync_NoMailServer_ReturnsNoMailServer()
    {
        var checker = Create(_ => Task.FromResult<bool?>(false));
        Assert.Equal(MailDomainResult.NoMailServer, await checker.CheckAsync("a@fake.invalid"));
    }

    [Fact]
    public async Task CheckAsync_DnsDidNotAnswer_FailsOpen()
    {
        var checker = Create(_ => Task.FromResult<bool?>(null));
        Assert.Equal(MailDomainResult.Unknown, await checker.CheckAsync("a@b.com"));
    }

    [Fact]
    public async Task CheckAsync_ResolverThrows_FailsOpen()
    {
        var checker = Create(_ => throw new TimeoutException());
        Assert.Equal(MailDomainResult.Unknown, await checker.CheckAsync("a@b.com"));
    }

    [Fact]
    public async Task CheckAsync_MalformedEmail_FailsOpen()
    {
        var checker = Create(_ => Task.FromResult<bool?>(false));
        Assert.Equal(MailDomainResult.Unknown, await checker.CheckAsync("not-an-email"));
    }

    [Fact]
    public async Task CheckAsync_PassesDomainToResolver()
    {
        string? seen = null;
        var checker = Create(d => { seen = d; return Task.FromResult<bool?>(true); });
        await checker.CheckAsync("user@sub.example.org");
        Assert.Equal("sub.example.org", seen);
    }
}
