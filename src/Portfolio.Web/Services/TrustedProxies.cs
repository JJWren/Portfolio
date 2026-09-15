using System.Net;

namespace Portfolio.Web.Services;

/// <summary>
/// <c>TRUSTED_PROXIES</c> parsed into the two forwarded-headers trust lists
/// (FR-D14): single addresses for <c>ForwardedHeadersOptions.KnownProxies</c>,
/// CIDR networks for <c>KnownIPNetworks</c>, and anything that parsed as
/// neither, for the one startup warning.
/// </summary>
public sealed record TrustedProxyList(
    IReadOnlyList<IPAddress> Proxies,
    IReadOnlyList<IPNetwork> Networks,
    IReadOnlyList<string> Skipped);

/// <summary>Pure parser for <c>TRUSTED_PROXIES</c>.</summary>
public static class TrustedProxies
{
    /// <summary>
    /// Splits <paramref name="value"/> on commas, trims each token, and
    /// drops blanks. Each remaining token is tried as a CIDR network first
    /// (so "192.0.2.0/24" is never mistaken for a bare address), then as a
    /// single address, else recorded as skipped. Blank or null input
    /// returns three empty lists — today's trust-every-peer behaviour.
    /// </summary>
    public static TrustedProxyList Parse(string? value)
    {
        var proxies = new List<IPAddress>();
        var networks = new List<IPNetwork>();
        var skipped = new List<string>();

        if (!string.IsNullOrWhiteSpace(value))
        {
            foreach (var raw in value.Split(','))
            {
                var token = raw.Trim();
                if (token.Length == 0)
                {
                    continue;
                }

                if (IPNetwork.TryParse(token, out var network))
                {
                    networks.Add(network);
                }
                else if (IPAddress.TryParse(token, out var address))
                {
                    proxies.Add(address);
                }
                else
                {
                    skipped.Add(token);
                }
            }
        }

        return new TrustedProxyList(proxies, networks, skipped);
    }
}
