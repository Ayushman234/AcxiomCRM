using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IReportService
{
    Task<List<Customer>> GetCustomerReportAsync(ReportFilterVM filter, string? userId, string role);
    Task<List<Lead>> GetLeadReportAsync(ReportFilterVM filter, string? userId, string role);
    Task<List<Opportunity>> GetOpportunityReportAsync(ReportFilterVM filter, string? userId, string role);
    Task<List<FollowUp>> GetFollowUpReportAsync(ReportFilterVM filter, string? userId, string role);
    Task<List<AuditLog>> GetAuditReportAsync(AuditFilterVM filter);
    Task<List<PipelineReportItem>> GetPipelineReportAsync(string? userId, string role);
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _db;

    public ReportService(ApplicationDbContext db) => _db = db;

    public async Task<List<Customer>> GetCustomerReportAsync(ReportFilterVM filter, string? userId, string role)
    {
        var q = _db.Customers.Include(c => c.AssignedToUser).AsQueryable();
        if (role == AppRoles.SalesExecutive && userId != null)
            q = q.Where(c => c.AssignedTo == userId);
        if (filter.FromDate.HasValue) q = q.Where(c => c.CreatedDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) q = q.Where(c => c.CreatedDate <= filter.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            q = q.Where(c => c.Status.ToString() == filter.Status);
        return await q.OrderByDescending(c => c.CreatedDate).ToListAsync();
    }

    public async Task<List<Lead>> GetLeadReportAsync(ReportFilterVM filter, string? userId, string role)
    {
        var q = _db.Leads.Include(l => l.AssignedToUser).AsQueryable();
        if (role == AppRoles.SalesExecutive && userId != null)
            q = q.Where(l => l.AssignedTo == userId);
        if (filter.FromDate.HasValue) q = q.Where(l => l.CreatedDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) q = q.Where(l => l.CreatedDate <= filter.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            q = q.Where(l => l.Status.ToString() == filter.Status);
        return await q.OrderByDescending(l => l.CreatedDate).ToListAsync();
    }

    public async Task<List<Opportunity>> GetOpportunityReportAsync(ReportFilterVM filter, string? userId, string role)
    {
        var q = _db.Opportunities.Include(o => o.Customer).Include(o => o.AssignedToUser).AsQueryable();
        if (role == AppRoles.SalesExecutive && userId != null)
            q = q.Where(o => o.AssignedTo == userId);
        if (filter.FromDate.HasValue) q = q.Where(o => o.CreatedDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) q = q.Where(o => o.CreatedDate <= filter.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            q = q.Where(o => o.Status.ToString() == filter.Status);
        return await q.OrderByDescending(o => o.CreatedDate).ToListAsync();
    }

    public async Task<List<FollowUp>> GetFollowUpReportAsync(ReportFilterVM filter, string? userId, string role)
    {
        var q = _db.FollowUps.Include(f => f.Customer).Include(f => f.AssignedToUser).AsQueryable();
        if (role == AppRoles.SalesExecutive && userId != null)
            q = q.Where(f => f.AssignedTo == userId);
        if (filter.FromDate.HasValue) q = q.Where(f => f.FollowUpDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) q = q.Where(f => f.FollowUpDate <= filter.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status))
            q = q.Where(f => f.Status.ToString() == filter.Status);
        return await q.OrderByDescending(f => f.FollowUpDate).ToListAsync();
    }

    public async Task<List<AuditLog>> GetAuditReportAsync(AuditFilterVM filter)
    {
        var q = _db.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.UserId)) q = q.Where(a => a.UserId == filter.UserId);
        if (!string.IsNullOrWhiteSpace(filter.EntityName)) q = q.Where(a => a.EntityName == filter.EntityName);
        if (!string.IsNullOrWhiteSpace(filter.Action)) q = q.Where(a => a.Action == filter.Action);
        if (filter.FromDate.HasValue) q = q.Where(a => a.CreatedDate >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) q = q.Where(a => a.CreatedDate <= filter.ToDate.Value);
        return await q.OrderByDescending(a => a.CreatedDate).Take(500).ToListAsync();
    }

    public async Task<List<PipelineReportItem>> GetPipelineReportAsync(string? userId, string role)
    {
        var q = _db.Opportunities
            .Include(o => o.AssignedToUser)
            .Where(o => o.Status == OpportunityStatus.Open)
            .AsQueryable();

        if (role == AppRoles.SalesExecutive && userId != null)
            q = q.Where(o => o.AssignedTo == userId);

        var opps = await q
            .Select(o => new
            {
                Stage = o.Stage.ToString(),
                Owner = (o.AssignedToUser != null ? o.AssignedToUser.FirstName + " " + o.AssignedToUser.LastName : "Unassigned"),
                o.Amount,
                o.Probability
            })
            .ToListAsync();

        return opps
            .GroupBy(o => new { o.Stage, o.Owner })
            .Select(g => new PipelineReportItem
            {
                Stage = g.Key.Stage,
                Owner = g.Key.Owner,
                Count = g.Count(),
                TotalAmount = g.Sum(o => o.Amount),
                WeightedAmount = g.Sum(o => o.Amount * o.Probability / 100m)
            })
            .ToList();
    }
}
