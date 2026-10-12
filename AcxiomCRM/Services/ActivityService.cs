using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IActivityService
{
    Task<(List<Activity> Items, int Total)> GetPagedAsync(ActivityFilterVM filter, string? userId, string role);
    Task<Activity?> GetByIdAsync(int id);
    Task<(bool Success, string? Error)> CreateAsync(Activity activity);
    Task<(bool Success, string? Error)> UpdateAsync(Activity activity);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
}

public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public ActivityService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<(List<Activity> Items, int Total)> GetPagedAsync(
        ActivityFilterVM filter, string? userId, string role)
    {
        var query = _db.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedToUser)
            .AsQueryable();

        if (role == AppRoles.SalesExecutive && userId != null)
            query = query.Where(a => a.AssignedTo == userId || a.CreatedBy == userId);

        if (filter.Type.HasValue)
            query = query.Where(a => a.ActivityType == filter.Type.Value);
        if (filter.Status.HasValue)
            query = query.Where(a => a.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
            query = query.Where(a => a.AssignedTo == filter.AssignedTo);
        if (filter.FromDate.HasValue)
            query = query.Where(a => a.ActivityDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(a => a.ActivityDate <= filter.ToDate.Value);

        var total = await query.CountAsync();
        var page = filter.Page < 1 ? 1 : filter.Page;
        var size = filter.PageSize < 1 ? 10 : filter.PageSize;
        var items = await query.OrderByDescending(a => a.ActivityDate)
            .Skip((page - 1) * size).Take(size).ToListAsync();

        return (items, total);
    }

    public async Task<Activity?> GetByIdAsync(int id) =>
        await _db.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedToUser)
            .FirstOrDefaultAsync(a => a.ActivityId == id);

    public async Task<(bool Success, string? Error)> CreateAsync(Activity activity)
    {
        activity.CreatedDate = DateTime.UtcNow;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", "Activity", activity.ActivityId.ToString(),
            newValue: activity.Subject);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Activity activity)
    {
        activity.ModifiedDate = DateTime.UtcNow;
        _db.Activities.Update(activity);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", "Activity", activity.ActivityId.ToString());
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity == null) return (false, "Activity not found.");
        activity.Status = ActivityStatus.Cancelled;
        activity.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Delete", "Activity", id.ToString());
        return (true, null);
    }
}
