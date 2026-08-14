using DnsClient;

namespace Portfolio.Web.Services;

public enum MailDomainResult
{
    /// <summary>The domain has an MX record (or an A/AAAA fallback per RFC 5321).</summary>
    Deliverable,

    /// <summary>DNS answered authoritatively: no mail server exists for the domain.</summary>
    NoMailServer,

    /// <summary>DNS failed or timed out — treated as deliverable (fail-open).</summary>
    Unknown,
}

/// <summary>DNS lookup seam so tests never touch the network.</summary>
public interface IMxResolver
{
    /// <summary>True when the domain can receive mail; null when DNS didn't answer.</summary>
    Task<bool?> HasMailServerAsync(string domain, CancellationToken cancellationToken);
}

public class DnsClientMxResolver : IMxResolver
{
    private readonly LookupClient _client = new(new LookupClientOptions
    {
        Timeout = TimeSpan.FromSeconds(2),
        UseCache = true,
    });

    public async Task<bool?> HasMailServerAsync(string domain, CancellationToken cancellationToken)
    {
        var mx = await _client.QueryAsync(domain, QueryType.MX, cancellationToken: cancellationToken);
        // NXDOMAIN is a definitive "no such domain"; any other DNS error is
        // inconclusive and must fail open.
        if (mx.HasError && mx.Header.ResponseCode != DnsHeaderResponseCode.NotExistentDomain)
        {
            return null;
        }

        if (mx.Answers.MxRecords().Any())
        {
            return true;
        }

        // RFC 5321: no MX means fall back to an address record on the domain.
        var a = await _client.QueryAsync(domain, QueryType.A, cancellationToken: cancellationToken);
        if (a.Answers.ARecords().Any())
        {
            return true;
        }

        var aaaa = await _client.QueryAsync(domain, QueryType.AAAA, cancellationToken: cancellationToken);
        return aaaa.Answers.AaaaRecords().Any() ? true : false;
    }
}

/// <summary>
/// Checks whether a sender address's domain can actually receive mail.
/// Fail-open by design: DNS trouble must never cost a real message.
/// </summary>
public class MailDomainChecker(IMxResolver resolver, ILogger<MailDomainChecker> logger)
{
    private static readonly TimeSpan OverallTimeout = TimeSpan.FromSeconds(2.5);

    public async Task<MailDomainResult> CheckAsync(string email)
    {
        if (ContactSpamRules.DomainOf(email) is not { Length: > 0 } domain)
        {
            return MailDomainResult.Unknown;
        }

        try
        {
            using var cts = new CancellationTokenSource(OverallTimeout);
            return await resolver.HasMailServerAsync(domain, cts.Token) switch
            {
                true => MailDomainResult.Deliverable,
                false => MailDomainResult.NoMailServer,
                null => MailDomainResult.Unknown,
            };
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "MX check failed for domain {Domain}; failing open.", domain);
            return MailDomainResult.Unknown;
        }
    }
}
