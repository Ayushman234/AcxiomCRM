using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface ILeadService
{
    Task<(List<Lead> Items, int Total)> GetPagedAsync(LeadFilterVM filter, string? userId, string role);
    Task<Lead?> GetByIdAsync(int id);
    Task<(bool Success, string? Error)> CreateAsync(Lead lead);
    Task<(bool Success, string? Error)> UpdateAsync(Lead lead, LeadStatus newStatus);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
    Task<(bool Success, string? Error)> ConvertLeadAsync(int leadId, string userId);
    bool IsValidTransition(LeadStatus current, LeadStatus next);
    Task<string> GenerateCodeAsync();
}

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    // Valid lead status transitions
    private static readonly Dictionary<LeadStatus, LeadStatus[]> _validTransitions = new()
    {
        [LeadStatus.New] = [LeadStatus.Contacted, LeadStatus.Lost],
        [LeadStatus.Contacted] = [LeadStatus.Qualified, LeadStatus.Unqualified, LeadStatus.Lost],
        [LeadStatus.Qualified] = [LeadStatus.Converted, LeadStatus.Lost],
        [LeadStatus.Unqualified] = [LeadStatus.Contacted, LeadStatus.Lost],
        [LeadStatus.Converted] = [],
        [LeadStatus.Lost] = [LeadStatus.New]   // Allow re-engagement
    };

    public LeadService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<(List<Lead> Items, int Total)> GetPagedAsync(
        LeadFilterVM filter, string? userId, string role)
    {
        var query = _db.Leads
            .Include(l => l.AssignedToUser)
            .Include(l => l.Customer)
            .AsQueryable();

        if (role == AppRoles.SalesExecutive && userId != null)
            query = query.Where(l => l.AssignedTo == userId || l.CreatedBy == userId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.ToLower();
            query = query.Where(l =>
                l.LeadName.ToLower().Contains(s) ||
                l.Email.ToLower().Contains(s) ||
                (l.CompanyName != null && l.CompanyName.ToLower().Contains(s)));
        }
        if (filter.Status.HasValue)
            query = query.Where(l => l.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
            query = query.Where(l => l.AssignedTo == filter.AssignedTo);

        var total = await query.CountAsync();
        var page = filter.Page < 1 ? 1 : filter.Page;
        var size = filter.PageSize < 1 ? 10 : filter.PageSize;
        var items = await query
            .OrderByDescending(l => l.CreatedDate)
            .Skip((page - 1) * size).Take(size).ToListAsync();

        return (items, total);
    }

    public async Task<Lead?> GetByIdAsync(int id) =>
        await _db.Leads
            .Include(l => l.AssignedToUser)
            .Include(l => l.CreatedByUser)
            .Include(l => l.Customer)
            .Include(l => l.FollowUps)
            .Include(l => l.Activities)
            .Include(l => l.Opportunities)
            .FirstOrDefaultAsync(l => l.LeadId == id);

    public async Task<(bool Success, string? Error)> CreateAsync(Lead lead)
    {
        lead.LeadCode = await GenerateCodeAsync();
        lead.CreatedDate = DateTime.UtcNow;
        lead.Status = LeadStatus.New;
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", "Lead", lead.LeadId.ToString(), newValue: lead.LeadName);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Lead lead, LeadStatus newStatus)
    {
        var existing = await _db.Leads.FindAsync(lead.LeadId);
        if (existing == null) return (false, "Lead not found.");

        if (existing.Status != newStatus && !IsValidTransition(existing.Status, newStatus))
            return (false, $"Cannot transition from {existing.Status} to {newStatus}.");

        var oldStatus = existing.Status.ToString();
        existing.LeadName = lead.LeadName;
        existing.Email = lead.Email;
        existing.Phone = lead.Phone;
        existing.CompanyName = lead.CompanyName;
        existing.Source = lead.Source;
        existing.Status = newStatus;
        existing.Priority = lead.Priority;
        existing.ExpectedValue = lead.ExpectedValue;
        existing.Notes = lead.Notes;
        existing.AssignedTo = lead.AssignedTo;
        existing.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", "Lead", lead.LeadId.ToString(),
            oldValue: oldStatus, newValue: newStatus.ToString());
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var lead = await _db.Leads.FindAsync(id);
        if (lead == null) return (false, "Lead not found.");
        if (lead.Status == LeadStatus.Converted)
            return (false, "Converted leads cannot be deleted.");

        lead.Status = LeadStatus.Lost;
        lead.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Deactivate", "Lead", id.ToString());
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ConvertLeadAsync(int leadId, string userId)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var lead = await _db.Leads.FindAsync(leadId);
            if (lead == null) return (false, "Lead not found.");
            if (lead.Status != LeadStatus.Qualified)
                return (false, "Only Qualified leads can be converted.");
            if (lead.IsConverted)
                return (false, "Lead is already converted.");

            // Create customer
            var count = await _db.Customers.CountAsync() + 1;
            var customer = new Customer
            {
                CustomerCode = $"CUST-{count:D5}",
                CustomerName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Status = CustomerStatus.Active,
                CreatedBy = userId,
                AssignedTo = lead.AssignedTo,
                CreatedDate = DateTime.UtcNow
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            // Update lead
            lead.Status = LeadStatus.Converted;
            lead.IsConverted = true;
            lead.ConvertedDate = DateTime.UtcNow;
            lead.CustomerId = customer.CustomerId;
            lead.ModifiedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
            await _audit.LogAsync("Convert", "Lead", leadId.ToString(),
                oldValue: "Qualified", newValue: $"Converted→Customer:{customer.CustomerId}");
            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Conversion failed: {ex.Message}");
        }
    }

    public bool IsValidTransition(LeadStatus current, LeadStatus next) =>
        _validTransitions.TryGetValue(current, out var allowed) && allowed.Contains(next);

    public async Task<string> GenerateCodeAsync()
    {
        var count = await _db.Leads.CountAsync() + 1;
        return $"LEAD-{count:D5}";
    }
}
