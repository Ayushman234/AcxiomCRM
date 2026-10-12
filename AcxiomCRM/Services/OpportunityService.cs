using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IOpportunityService
{
    Task<(List<Opportunity> Items, int Total)> GetPagedAsync(OpportunityFilterVM filter, string? userId, string role);
    Task<Opportunity?> GetByIdAsync(int id);
    Task<(bool Success, string? Error)> CreateAsync(Opportunity opp);
    Task<(bool Success, string? Error)> UpdateAsync(Opportunity opp);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
    Task<decimal> GetTotalPipelineAsync(string? userId, string role);
}

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public OpportunityService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<(List<Opportunity> Items, int Total)> GetPagedAsync(
        OpportunityFilterVM filter, string? userId, string role)
    {
        var query = _db.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.AssignedToUser)
            .AsQueryable();

        if (role == AppRoles.SalesExecutive && userId != null)
            query = query.Where(o => o.AssignedTo == userId || o.CreatedBy == userId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.ToLower();
            query = query.Where(o =>
                o.OpportunityName.ToLower().Contains(s) ||
                (o.Customer != null && o.Customer.CustomerName.ToLower().Contains(s)));
        }
        if (filter.Stage.HasValue)
            query = query.Where(o => o.Stage == filter.Stage.Value);
        if (filter.Status.HasValue)
            query = query.Where(o => o.Status == filter.Status.Value);

        var total = await query.CountAsync();
        var page = filter.Page < 1 ? 1 : filter.Page;
        var size = filter.PageSize < 1 ? 10 : filter.PageSize;
        var items = await query.OrderByDescending(o => o.CreatedDate)
            .Skip((page - 1) * size).Take(size).ToListAsync();

        return (items, total);
    }

    public async Task<Opportunity?> GetByIdAsync(int id) =>
        await _db.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedToUser)
            .Include(o => o.CreatedByUser)
            .Include(o => o.FollowUps)
            .Include(o => o.Activities)
            .FirstOrDefaultAsync(o => o.OpportunityId == id);

    private static (bool Valid, string? Error) ValidateBusiness(Opportunity opp)
    {
        if (opp.Status == OpportunityStatus.Open || opp.Status == OpportunityStatus.OnHold)
        {
            if (opp.Amount <= 0)
                return (false, "Opportunity Amount must be greater than 0.");
            if (opp.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
                return (false, "Expected Close Date cannot be in the past.");
        }
        if (opp.Probability < 0 || opp.Probability > 100)
            return (false, "Probability must be between 0 and 100.");
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(Opportunity opp)
    {
        var (valid, error) = ValidateBusiness(opp);
        if (!valid) return (false, error);

        opp.CreatedDate = DateTime.UtcNow;
        _db.Opportunities.Add(opp);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", "Opportunity", opp.OpportunityId.ToString(),
            newValue: opp.OpportunityName);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Opportunity opp)
    {
        var (valid, error) = ValidateBusiness(opp);
        if (!valid) return (false, error);

        opp.ModifiedDate = DateTime.UtcNow;
        _db.Opportunities.Update(opp);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", "Opportunity", opp.OpportunityId.ToString());
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var opp = await _db.Opportunities.FindAsync(id);
        if (opp == null) return (false, "Opportunity not found.");

        opp.Status = OpportunityStatus.Lost;
        opp.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Deactivate", "Opportunity", id.ToString());
        return (true, null);
    }

    public async Task<decimal> GetTotalPipelineAsync(string? userId, string role)
    {
        var query = _db.Opportunities
            .Where(o => o.Status == OpportunityStatus.Open);

        if (role == AppRoles.SalesExecutive && userId != null)
            query = query.Where(o => o.AssignedTo == userId);

        return await query.SumAsync(o => o.Amount * o.Probability / 100m);
    }
}
