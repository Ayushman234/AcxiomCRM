using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IFollowUpService
{
    Task<(List<FollowUp> Items, int Total)> GetPagedAsync(FollowUpFilterVM filter, string? userId, string role);
    Task<FollowUp?> GetByIdAsync(int id);
    Task<(bool Success, string? Error)> CreateAsync(FollowUp followUp);
    Task<(bool Success, string? Error)> UpdateAsync(FollowUp followUp);
    Task<(bool Success, string? Error)> CompleteAsync(int id, string notes);
    Task<(bool Success, string? Error)> CancelAsync(int id);
    Task<List<FollowUp>> GetOverdueAsync(string? userId, string role);
    Task<List<FollowUp>> GetUpcomingAsync(string? userId, string role, int days = 7);
}

public class FollowUpService : IFollowUpService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public FollowUpService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<(List<FollowUp> Items, int Total)> GetPagedAsync(
        FollowUpFilterVM filter, string? userId, string role)
    {
        var query = _db.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.AssignedToUser)
            .AsQueryable();

        if (role == AppRoles.SalesExecutive && userId != null)
            query = query.Where(f => f.AssignedTo == userId || f.CreatedBy == userId);

        if (filter.Status.HasValue)
            query = query.Where(f => f.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
            query = query.Where(f => f.AssignedTo == filter.AssignedTo);
        if (filter.FromDate.HasValue)
            query = query.Where(f => f.FollowUpDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(f => f.FollowUpDate <= filter.ToDate.Value);

        var total = await query.CountAsync();
        var page = filter.Page < 1 ? 1 : filter.Page;
        var size = filter.PageSize < 1 ? 10 : filter.PageSize;
        var items = await query.OrderBy(f => f.FollowUpDate)
            .Skip((page - 1) * size).Take(size).ToListAsync();

        return (items, total);
    }

    public async Task<FollowUp?> GetByIdAsync(int id) =>
        await _db.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedToUser)
            .FirstOrDefaultAsync(f => f.FollowUpId == id);

    public async Task<(bool Success, string? Error)> CreateAsync(FollowUp followUp)
    {
        if (followUp.Status == FollowUpStatus.Planned &&
            followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
            return (false, "Follow-up date cannot be earlier than today.");

        followUp.CreatedDate = DateTime.UtcNow;
        _db.FollowUps.Add(followUp);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", "FollowUp", followUp.FollowUpId.ToString(),
            newValue: followUp.FollowUpDate.ToString("yyyy-MM-dd"));
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(FollowUp followUp)
    {
        var existing = await _db.FollowUps.FindAsync(followUp.FollowUpId);
        if (existing == null) return (false, "Follow-up not found.");

        if ((followUp.Status == FollowUpStatus.Planned || existing.Status == FollowUpStatus.Planned) &&
            followUp.FollowUpDate.Date < DateTime.UtcNow.Date)
            return (false, "Follow-up date cannot be earlier than today.");

        existing.Subject = followUp.Subject;
        existing.FollowUpDate = followUp.FollowUpDate;
        existing.FollowUpType = followUp.FollowUpType;
        existing.Remarks = followUp.Remarks;
        existing.Status = followUp.Status;
        existing.AssignedTo = followUp.AssignedTo;
        existing.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", "FollowUp", followUp.FollowUpId.ToString());
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CompleteAsync(int id, string notes)
    {
        var followUp = await _db.FollowUps.FindAsync(id);
        if (followUp == null) return (false, "Follow-up not found.");

        followUp.Status = FollowUpStatus.Completed;
        followUp.CompletedDate = DateTime.UtcNow;
        followUp.CompletionNotes = notes;
        followUp.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Complete", "FollowUp", id.ToString());
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CancelAsync(int id)
    {
        var followUp = await _db.FollowUps.FindAsync(id);
        if (followUp == null) return (false, "Follow-up not found.");

        followUp.Status = FollowUpStatus.Cancelled;
        followUp.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Cancel", "FollowUp", id.ToString());
        return (true, null);
    }

    public async Task<List<FollowUp>> GetOverdueAsync(string? userId, string role)
    {
        var query = _db.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.AssignedToUser)
            .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate < DateTime.UtcNow);

        if (role == AppRoles.SalesExecutive && userId != null)
            query = query.Where(f => f.AssignedTo == userId);

        return await query.OrderBy(f => f.FollowUpDate).ToListAsync();
    }

    public async Task<List<FollowUp>> GetUpcomingAsync(string? userId, string role, int days = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(days);
        var query = _db.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.AssignedToUser)
            .Where(f => f.Status == FollowUpStatus.Planned &&
                        f.FollowUpDate >= DateTime.UtcNow &&
                        f.FollowUpDate <= cutoff);

        if (role == AppRoles.SalesExecutive && userId != null)
            query = query.Where(f => f.AssignedTo == userId);

        return await query.OrderBy(f => f.FollowUpDate).ToListAsync();
    }
}
