using System.Security.Cryptography;
using System.Text;

namespace Portfolio.Web.Services;

/// <summary>
/// Derives the anonymous daily visitor identifier: a one-way hash of the
/// per-install secret, the UTC date, the client IP, and the User-Agent.
/// The raw inputs are never stored, and because the date is part of the
/// hash, keys cannot link the same visitor across days.
/// </summary>
public static class VisitorKey
{
    public static string Compute(byte[] secret, DateOnly utcDay, string ip, string userAgent)
    {
        var payload = new StringBuilder()
            .Append(utcDay.ToString("yyyy-MM-dd"))
            .Append('\n').Append(ip)
            .Append('\n').Append(userAgent)
            .ToString();

        var hash = HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(hash);
    }
}
