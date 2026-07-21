using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
            post.Property(p => p.Slug).HasMaxLength(160);
            post.Property(p => p.Title).HasMaxLength(200);
            post.Property(p => p.Summary).HasMaxLength(500);
            post.Property(p => p.HeaderImagePath).HasMaxLength(400);
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
            project.Property(p => p.Title).HasMaxLength(120);
            project.Property(p => p.Summary).HasMaxLength(500);
            project.Property(p => p.HeaderImagePath).HasMaxLength(400);
            project.Property(p => p.HomepageUrl).HasMaxLength(400);
            project.Property(p => p.RepoUrl).HasMaxLength(400);
        });

        builder.Entity<ContactMessage>(message =>
        {
            message.Property(m => m.Name).HasMaxLength(120);
            message.Property(m => m.Email).HasMaxLength(254);
            message.Property(m => m.Subject).HasMaxLength(200);
            message.Property(m => m.Body).HasMaxLength(4000);
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
    }
}
