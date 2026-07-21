namespace Portfolio.Web.Services;

public record OAuthProvider(string Scheme, string DisplayName);

/// <summary>
/// The sign-in providers enabled for this deployment. A provider is enabled
/// when both OAUTH__{NAME}__CLIENTID and OAUTH__{NAME}__CLIENTSECRET are set.
/// </summary>
public class OAuthProviders(IReadOnlyList<OAuthProvider> enabled)
{
    public IReadOnlyList<OAuthProvider> Enabled { get; } = enabled;

    public bool Any => Enabled.Count > 0;

    public static (string ClientId, string ClientSecret)? ReadCredentials(IConfiguration config, string name)
    {
        var id = config[$"OAUTH:{name}:CLIENTID"];
        var secret = config[$"OAUTH:{name}:CLIENTSECRET"];
        return string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(secret)
            ? null
            : (id, secret);
    }
}
