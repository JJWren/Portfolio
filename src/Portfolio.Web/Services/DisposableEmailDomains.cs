using System.Collections.Frozen;
using System.Reflection;

namespace Portfolio.Web.Services;

/// <summary>
/// Vendored blocklist of disposable-email domains (Resources/
/// disposable-email-domains.txt, embedded at build time). Matching covers the
/// exact domain and every parent suffix, so subdomain burners can't slip by.
/// </summary>
public class DisposableEmailDomains
{
    private readonly FrozenSet<string> _domains;

    public DisposableEmailDomains()
        : this(ReadEmbeddedList())
    {
    }

    public DisposableEmailDomains(IEnumerable<string> domains)
        => _domains = domains
            .Select(d => d.Trim())
            .Where(d => d.Length > 0 && !d.StartsWith('#'))
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public int Count => _domains.Count;

    public bool Contains(string domain)
    {
        domain = domain.Trim().TrimEnd('.');
        while (domain.Length > 0)
        {
            if (_domains.Contains(domain))
            {
                return true;
            }

            var dot = domain.IndexOf('.');
            if (dot < 0)
            {
                return false;
            }

            domain = domain[(dot + 1)..];
        }

        return false;
    }

    private static IEnumerable<string> ReadEmbeddedList()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string resourceName = "Portfolio.Web.Resources.disposable-email-domains.txt";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line)
        {
            lines.Add(line);
        }

        return lines;
    }
}
