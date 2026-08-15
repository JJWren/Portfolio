using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Services;

namespace Portfolio.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();

    public DbSet<Comment> Comments => Set<Comment>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

    public DbSet<Report> Reports => Set<Report>();

    public DbSet<UserMessage> UserMessages => Set<UserMessage>();

    public DbSet<SiteContent> SiteContents => Set<SiteContent>();

    public DbSet<ThemeSettings> ThemeSettings => Set<ThemeSettings>();

    public DbSet<PageView> PageViews => Set<PageView>();

    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();

    public DbSet<AnalyticsState> AnalyticsStates => Set<AnalyticsState>();

    public DbSet<DailySiteStat> DailySiteStats => Set<DailySiteStat>();

    public DbSet<DailyRouteStat> DailyRouteStats => Set<DailyRouteStat>();

    public DbSet<DailyReferrerStat> DailyReferrerStats => Set<DailyReferrerStat>();

    public DbSet<DailyEventStat> DailyEventStats => Set<DailyEventStat>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(u => u.CustomDisplayName).HasMaxLength(40);
            user.Property(u => u.AvatarUrl).HasMaxLength(400);
            user.Property(u => u.BanReason).HasMaxLength(300);
        });

        builder.Entity<BlogPost>(post =>
        {
            post.HasIndex(p => p.Slug).IsUnique();
            post.HasIndex(p => new { p.IsPublished, p.PublishedAt });
            post.Property(p => p.Slug).HasMaxLength(PostRules.SlugMaxLength);
            post.Property(p => p.Title).HasMaxLength(PostRules.TitleMaxLength);
            post.Property(p => p.Summary).HasMaxLength(PostRules.SummaryMaxLength);
            post.Property(p => p.HeaderImagePath).HasMaxLength(PostRules.HeaderImagePathMaxLength);
            post.Property(p => p.HeaderImageAlt).HasMaxLength(PostRules.HeaderImageAltMaxLength);
        });

        builder.Entity<Comment>(comment =>
        {
            comment.Property(c => c.Body).HasMaxLength(2000);
            comment.HasOne(c => c.BlogPost)
                .WithMany()
                .HasForeignKey(c => c.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);
            comment.HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            comment.HasIndex(c => c.BlogPostId);
        });

        builder.Entity<Project>(project =>
        {
            project.Property(p => p.Title).HasMaxLength(ProjectRules.TitleMaxLength);
            project.Property(p => p.Summary).HasMaxLength(ProjectRules.SummaryMaxLength);
            project.Property(p => p.HeaderImagePath).HasMaxLength(ProjectRules.HeaderImagePathMaxLength);
            project.Property(p => p.HeaderImageAlt).HasMaxLength(ProjectRules.HeaderImageAltMaxLength);
            project.Property(p => p.HomepageUrl).HasMaxLength(ProjectRules.HomepageUrlMaxLength);
            project.Property(p => p.RepoUrl).HasMaxLength(ProjectRules.RepoUrlMaxLength);
        });

        builder.Entity<ContactMessage>(message =>
        {
            message.Property(m => m.Name).HasMaxLength(120);
            message.Property(m => m.Email).HasMaxLength(254);
            message.Property(m => m.Subject).HasMaxLength(200);
            message.Property(m => m.Body).HasMaxLength(4000);
            message.Property(m => m.FlagReason).HasMaxLength(ContactSpamRules.FlagReasonMaxLength);
            message.HasIndex(m => new { m.IsFlagged, m.IsRead });
        });

        builder.Entity<Report>(report =>
        {
            report.Property(r => r.Reason).HasMaxLength(60);
            report.Property(r => r.Details).HasMaxLength(1000);
            report.Property(r => r.CommentExcerpt).HasMaxLength(300);
            report.HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Cascade);
            report.HasOne(r => r.TargetUser)
                .WithMany()
                .HasForeignKey(r => r.TargetUserId)
                .OnDelete(DeleteBehavior.Cascade);
            report.HasOne(r => r.Comment)
                .WithMany()
                .HasForeignKey(r => r.CommentId)
                .OnDelete(DeleteBehavior.SetNull);
            report.HasIndex(r => r.Status);
            // Plain ReporterId index serves all-status queries (GetMineAsync);
            // the filtered composites below can't, since they only cover open rows.
            report.HasIndex(r => r.ReporterId);
            // Partial unique indexes back up the app-level duplicate check so
            // concurrent submissions can't create duplicate open reports.
            report.HasIndex(r => new { r.ReporterId, r.CommentId })
                .IsUnique()
                .HasDatabaseName("IX_Reports_OpenCommentReport")
                .HasFilter("\"Status\" = 0 AND \"TargetType\" = 0");
            report.HasIndex(r => new { r.ReporterId, r.TargetUserId })
                .IsUnique()
                .HasDatabaseName("IX_Reports_OpenUserReport")
                .HasFilter("\"Status\" = 0 AND \"TargetType\" = 1");
        });

        builder.Entity<UserMessage>(message =>
        {
            message.Property(m => m.Body).HasMaxLength(2000);
            message.Property(m => m.QuotedComment).HasMaxLength(300);
            message.HasOne(m => m.Recipient)
                .WithMany()
                .HasForeignKey(m => m.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);
            message.HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.SetNull);
            message.HasOne(m => m.Report)
                .WithMany()
                .HasForeignKey(m => m.ReportId)
                .OnDelete(DeleteBehavior.SetNull);
            message.HasIndex(m => new { m.RecipientId, m.IsRead });
        });

        builder.Entity<SiteContent>(content =>
        {
            // Single fixed-key row (SiteContent.SingletonId) upserted by SiteContentService.
            content.Property(c => c.Id).ValueGeneratedNever();
            content.Property(c => c.HeroHeading).HasMaxLength(SiteContentRules.HeroHeadingMaxLength);
            content.Property(c => c.Tagline).HasMaxLength(SiteContentRules.TaglineMaxLength);
            content.Property(c => c.About).HasMaxLength(SiteContentRules.AboutMaxLength);
            content.Property(c => c.OwnerPhotoAlt).HasMaxLength(SiteContentRules.OwnerPhotoAltMaxLength);
        });

        builder.Entity<PageView>(view =>
        {
            view.Property(v => v.Path).HasMaxLength(AnalyticsRules.PathMaxLength);
            view.Property(v => v.ReferrerHost).HasMaxLength(AnalyticsRules.ReferrerMaxLength);
            view.Property(v => v.VisitorKey).HasMaxLength(AnalyticsRules.VisitorKeyLength);
            // Rollup and retention both scan by time.
            view.HasIndex(v => v.OccurredAt);
        });

        builder.Entity<AnalyticsEvent>(evt =>
        {
            evt.Property(e => e.Name).HasMaxLength(AnalyticsRules.EventNameMaxLength);
            evt.Property(e => e.Target).HasMaxLength(AnalyticsRules.EventTargetMaxLength);
            evt.Property(e => e.VisitorKey).HasMaxLength(AnalyticsRules.VisitorKeyLength);
            evt.HasIndex(e => e.OccurredAt);
        });

        builder.Entity<AnalyticsState>(state =>
        {
            // Single fixed-key row (AnalyticsState.SingletonId) created lazily
            // by AnalyticsService.
            state.Property(s => s.Id).ValueGeneratedNever();
            state.Property(s => s.Secret).HasMaxLength(64);
        });

        builder.Entity<DailySiteStat>(stat =>
        {
            // Day is the natural key and the rollup watermark.
            stat.HasKey(s => s.Day);
        });

        builder.Entity<DailyRouteStat>(stat =>
        {
            stat.Property(s => s.Path).HasMaxLength(AnalyticsRules.PathMaxLength);
            stat.HasIndex(s => new { s.Day, s.Path }).IsUnique();
        });

        builder.Entity<DailyReferrerStat>(stat =>
        {
            stat.Property(s => s.ReferrerHost).HasMaxLength(AnalyticsRules.ReferrerMaxLength);
            stat.HasIndex(s => new { s.Day, s.ReferrerHost }).IsUnique();
        });

        builder.Entity<DailyEventStat>(stat =>
        {
            stat.Property(s => s.Name).HasMaxLength(AnalyticsRules.EventNameMaxLength);
            stat.Property(s => s.Target).HasMaxLength(AnalyticsRules.EventTargetMaxLength);
            stat.HasIndex(s => new { s.Day, s.Name });
        });

        builder.Entity<ThemeSettings>(theme =>
        {
            // Single fixed-key row (ThemeSettings.SingletonId) upserted by ThemeService.
            theme.Property(t => t.Id).ValueGeneratedNever();
            // Explicit jsonb: Npgsql's default mapping for string dictionaries is hstore.
            theme.Property(t => t.Overrides).HasColumnType("jsonb");
        });
    }
}
