using Microsoft.EntityFrameworkCore;
using Portfolio.Web.Data;

namespace Portfolio.Web.Services;

public class ProjectService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<List<Project>> GetVisibleAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Projects.AsNoTracking()
            .Where(p => p.IsVisible)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Title)
            .ToListAsync();
    }

    public async Task<List<Project>> GetAllForAdminAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Projects.AsNoTracking()
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Title)
            .ToListAsync();
    }

    public async Task<Project?> GetByIdAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Project> SaveAsync(Project project)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        project.UpdatedAt = now;

        if (project.Id == 0)
        {
            project.CreatedAt = now;
            if (project.SortOrder == 0)
            {
                var max = await db.Projects.MaxAsync(p => (int?)p.SortOrder) ?? 0;
                project.SortOrder = max + 1;
            }
            db.Projects.Add(project);
        }
        else
        {
            db.Projects.Update(project);
        }

        await db.SaveChangesAsync();
        return project;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Projects.Where(p => p.Id == id).ExecuteDeleteAsync();
    }

    public async Task ToggleVisibilityAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Projects.Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsVisible, p => !p.IsVisible)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
    }

    /// <summary>Swaps carousel position with the neighbor above (-1) or below (+1).</summary>
    public async Task MoveAsync(int id, int direction)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null)
        {
            return;
        }

        var neighbor = direction < 0
            ? await db.Projects.Where(p => p.SortOrder < project.SortOrder)
                .OrderByDescending(p => p.SortOrder).FirstOrDefaultAsync()
            : await db.Projects.Where(p => p.SortOrder > project.SortOrder)
                .OrderBy(p => p.SortOrder).FirstOrDefaultAsync();
        if (neighbor is null)
        {
            return;
        }

        (project.SortOrder, neighbor.SortOrder) = (neighbor.SortOrder, project.SortOrder);
        await db.SaveChangesAsync();
    }
}
