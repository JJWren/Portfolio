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

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton(SiteConfig.FromConfiguration(builder.Configuration));
builder.Services.AddSingleton<AdminEmails>();
builder.Services.AddSingleton<MarkdownService>();
builder.Services.AddSingleton<BlogService>();
builder.Services.AddSingleton<CommentService>();
builder.Services.AddSingleton<ProjectService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ContactRateLimiter>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<ContactService>();
builder.Services.AddSingleton<ImageUploadService>();
builder.Services.AddSingleton<AvatarService>();
builder.Services.AddSingleton<ProfileService>();
builder.Services.AddSingleton<MessageService>();
builder.Services.AddSingleton<ReportService>();
builder.Services.AddSingleton<ModerationService>();
builder.Services.AddSingleton<SiteContentService>();
builder.Services.AddSingleton<ThemeService>();

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

// OAuth callbacks need the original scheme/host when running behind a reverse proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
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

var app = builder.Build();

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

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// User-uploaded images live outside wwwroot (a volume in production).
var uploadsRoot = app.Services.GetRequiredService<ImageUploadService>().RootPath;
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads",
    // Upload filenames are single-use GUIDs — the content behind a URL can
    // never change, so clients may cache it forever.
    OnPrepareResponse = static ctx =>
        ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable",
});

app.MapAuthEndpoints();
app.MapSeoEndpoints();
app.MapHealthChecks("/healthz");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
