using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Portfolio.Web.Components;
using Portfolio.Web.Data;
using Portfolio.Web.Endpoints;
using Portfolio.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// FR-D1: self-hosters without a reverse proxy get no server-identifying
// header either; the production proxy already substitutes its own.
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The framework would otherwise add its own baseline anti-clickjacking
// header ahead of SecurityHeadersMiddleware: by default, antiforgery-token
// generation (used across the app's interactive forms) sends
// X-Frame-Options: SAMEORIGIN unconditionally. Because that write happens
// deep in endpoint execution, it registers its Response.OnStarting callback
// after this app's own middleware and so runs first, leaving
// SecurityHeadersMiddleware's fill-if-absent logic unable to replace it
// with the stricter DENY (FR-D1). Suppressed here; every response still
// gets X-Frame-Options from this app's own middleware.
builder.Services.AddAntiforgery(options => options.SuppressXFrameOptionsHeader = true);

builder.Services.AddSingleton(SiteConfig.FromConfiguration(builder.Configuration));
builder.Services.AddSingleton(SecurityOptions.FromConfiguration(builder.Configuration));
builder.Services.AddSingleton<AdminEmails>();
builder.Services.AddSingleton<MarkdownService>();
builder.Services.AddSingleton<BlogService>();
builder.Services.AddSingleton<CommentService>();
builder.Services.AddSingleton<ProjectService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ContactRateLimiter>();
// FR-D12: the contact limiter's algorithm, generalized (SubmissionLimiter);
// CommentLimiter and ReportLimiter reuse it with their own per-window
// numbers for the circuit's two other submission paths.
builder.Services.AddSingleton<CommentLimiter>();
builder.Services.AddSingleton<ReportLimiter>();
builder.Services.AddSingleton<ContactFormTimestamp>();
// Explicit factory: container-driven construction would pick the
// IEnumerable<string> test constructor (DI resolves IEnumerable<T> as "all
// registered T" — an empty list) and silently produce an empty blocklist.
builder.Services.AddSingleton(_ => new DisposableEmailDomains());
builder.Services.AddSingleton<IMxResolver, DnsClientMxResolver>();
builder.Services.AddSingleton<MailDomainChecker>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<ContactService>();
builder.Services.AddSingleton<ImageUploadService>();
builder.Services.AddSingleton<AvatarService>();
builder.Services.AddSingleton<OwnerPhotoService>();
builder.Services.AddSingleton<ResumeService>();
builder.Services.AddSingleton<ProfileService>();
builder.Services.AddSingleton<MessageService>();
builder.Services.AddSingleton<ReportService>();
builder.Services.AddSingleton<ModerationService>();
builder.Services.AddSingleton<SiteContentService>();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddSingleton<AnalyticsService>();
builder.Services.AddSingleton<AnalyticsRollupService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AnalyticsRollupService>());

// Factory for interactive components; scoped context for Identity stores.
// EnableDynamicJson: ThemeSettings.Overrides maps Dictionary<string,string>
// to jsonb, which Npgsql only (de)serializes behind this explicit opt-in —
// without it every read throws and the theme silently falls back to the
// built-in palette (ThemeService's DB-blip guard swallows the error).
void UseNpgsqlWithDynamicJson(DbContextOptionsBuilder options)
    => options.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        npgsql => npgsql.ConfigureDataSource(dataSource => dataSource.EnableDynamicJson()));

builder.Services.AddDbContextFactory<AppDbContext>(UseNpgsqlWithDynamicJson);
builder.Services.AddDbContext<AppDbContext>(
    UseNpgsqlWithDynamicJson,
    optionsLifetime: ServiceLifetime.Singleton);

// Persist data-protection keys so auth cookies survive container restarts.
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrEmpty(keysPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

// OAuth callbacks need the original scheme/host when running behind a
// reverse proxy. FR-D14: TRUSTED_PROXIES narrows whose X-Forwarded-For is
// honored after the two lists are cleared below; blank keeps every peer
// trusted (today's behaviour), so no self-hoster breaks.
var trusted = TrustedProxies.Parse(builder.Configuration["TRUSTED_PROXIES"]);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    foreach (var proxy in trusted.Proxies)
    {
        options.KnownProxies.Add(proxy);
    }

    foreach (var network in trusted.Networks)
    {
        options.KnownIPNetworks.Add(network);
    }
});

// Sign-in is external OAuth only — no password accounts.
builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

var auth = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});
auth.AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    // Anonymous visitors land on the provider picker; authenticated non-admins
    // keep the default access-denied path, which resolves to a 404 so the
    // admin area stays invisible.
    options.LoginPath = "/signin";
    options.ReturnUrlParameter = "returnUrl";
});

// Only providers with credentials in the environment are registered and shown.
var enabledProviders = new List<OAuthProvider>();

if (OAuthProviders.ReadCredentials(builder.Configuration, "GITHUB") is { } github)
{
    auth.AddGitHub(options =>
    {
        options.ClientId = github.ClientId;
        options.ClientSecret = github.ClientSecret;
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.Scope.Add("user:email");
    });
    enabledProviders.Add(new OAuthProvider("GitHub", "GitHub"));
}

if (OAuthProviders.ReadCredentials(builder.Configuration, "GOOGLE") is { } google)
{
    auth.AddGoogle(options =>
    {
        options.ClientId = google.ClientId;
        options.ClientSecret = google.ClientSecret;
        options.SignInScheme = IdentityConstants.ExternalScheme;
    });
    enabledProviders.Add(new OAuthProvider("Google", "Google"));
}

if (OAuthProviders.ReadCredentials(builder.Configuration, "DISCORD") is { } discord)
{
    auth.AddDiscord(options =>
    {
        options.ClientId = discord.ClientId;
        options.ClientSecret = discord.ClientSecret;
        options.SignInScheme = IdentityConstants.ExternalScheme;
        options.Scope.Add("email");
    });
    enabledProviders.Add(new OAuthProvider("Discord", "Discord"));
}

builder.Services.AddSingleton(new OAuthProviders(enabledProviders));
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

// FR-D9: named per-client-address policies on the auth group, the feeds and
// the counted redirects; a path-scoped global limiter for the OAuth handler
// callback paths. See RateLimitPolicies.Configure for the policy values and
// the 429 rejection response.
builder.Services.AddRateLimiter(RateLimitPolicies.Configure);

var app = builder.Build();

if (trusted.Skipped.Count > 0)
{
    app.Services.GetRequiredService<ILogger<Program>>().LogWarning(
        "TRUSTED_PROXIES: ignoring entries that are not an IP address or CIDR network: {Skipped}",
        string.Join(", ", trusted.Skipped));
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (!await roleManager.RoleExistsAsync(AuthEndpoints.AdminRole))
    {
        await roleManager.CreateAsync(new IdentityRole(AuthEndpoints.AdminRole));
    }

    if (builder.Configuration.GetValue<bool>("SEED_DEMO_DATA"))
    {
        await DemoSeeder.SeedAsync(db);
    }
}

app.UseForwardedHeaders();

// Security headers on every response (FR-D1, FR-D2, FR-D5): placed right
// after forwarded headers and before the HEAD-as-GET rewrite below, so
// static assets, the /uploads files, the exception handler's re-execution,
// the re-executed 404, the health check and every Blazor page all pass
// through it — see SecurityHeadersMiddleware for why OnStarting and
// fill-if-absent.
app.UseMiddleware<SecurityHeadersMiddleware>();

// Blazor component endpoints match GET only, so bare HEAD requests 405.
// Serve HEAD as GET with the body discarded (RFC 9110: same status and
// headers, no content); the method is restored afterwards for logging.
app.Use(async (context, next) =>
{
    if (!HttpMethods.IsHead(context.Request.Method))
    {
        await next(context);
        return;
    }

    context.Request.Method = HttpMethods.Get;
    // Mark the rewrite so analytics doesn't count probes as page views.
    context.Items[AnalyticsMiddleware.RewrittenHeadKey] = true;
    var originalBody = context.Response.Body;
    context.Response.Body = Stream.Null;
    try
    {
        await next(context);
    }
    finally
    {
        context.Response.Body = originalBody;
        context.Request.Method = HttpMethods.Head;
    }
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// The container serves plain HTTP; TLS terminates at the reverse proxy.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Explicit so endpoint selection happens after the HEAD-as-GET rewrite
// above — the implicit UseRouting would run before every middleware in
// this file and match HEAD against GET-only endpoints (405).
app.UseRouting();

// After routing, so the matched endpoint's policy can resolve, and before
// authentication, so a rejected request never reaches authentication,
// authorization, antiforgery or the analytics middleware (FR-D10, NFR-15).
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// After auth so the Admin-role exclusion sees the signed-in user.
app.UseMiddleware<AnalyticsMiddleware>();

app.MapStaticAssets();

// User-uploaded images live outside wwwroot (a volume in production).
var uploadsRoot = app.Services.GetRequiredService<ImageUploadService>().RootPath;
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads",
    // Upload filenames are single-use GUIDs — the content behind a URL can
    // never change, so clients may cache it forever. FR-D4: a restrictive
    // policy of its own (a scripted SVG, validated by extension only, must
    // never run) set here, ahead of SecurityHeadersMiddleware's
    // fill-if-absent, so the page policy never overwrites it.
    OnPrepareResponse = static ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        ctx.Context.Response.Headers["Content-Security-Policy"] = SecurityHeadersRules.UploadsCsp;
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    },
});

app.MapAuthEndpoints();
app.MapSeoEndpoints();
app.MapAnalyticsEndpoints();
app.MapHealthChecks("/healthz");
// Same reasoning as the antiforgery header above: Blazor Web Apps (.NET 8+)
// otherwise add their own Content-Security-Policy: frame-ancestors 'self'
// to interactive component responses, which — by the same OnStarting
// ordering — would win over this app's fuller policy under fill-if-absent.
// null disables the framework default (its own documented escape hatch);
// this app's own middleware supplies frame-ancestors 'none' (FR-D2) on
// every response, first render included, so nothing is left unprotected.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(o => o.ContentSecurityFrameAncestorsPolicy = null);

app.Run();
