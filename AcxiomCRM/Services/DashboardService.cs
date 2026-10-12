using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface IDashboardService
{
    Task<DashboardVM> GetDashboardDataAsync(string? userId, string role);
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _db;

    public DashboardService(ApplicationDbContext db) => _db = db;

    public async Task<DashboardVM> GetDashboardDataAsync(string? userId, string role)
    {
        var vm = new DashboardVM();

        // ── Scoped queries ────────────────────────────────────────────────
        var custQ = _db.Customers.AsQueryable();
        var leadQ = _db.Leads.AsQueryable();
        var oppQ = _db.Opportunities.AsQueryable();
        var fuQ = _db.FollowUps.AsQueryable();

        if (role == AppRoles.SalesExecutive && userId != null)
        {
            custQ = custQ.Where(c => c.AssignedTo == userId || c.CreatedBy == userId);
            leadQ = leadQ.Where(l => l.AssignedTo == userId || l.CreatedBy == userId);
            oppQ = oppQ.Where(o => o.AssignedTo == userId || o.CreatedBy == userId);
            fuQ = fuQ.Where(f => f.AssignedTo == userId || f.CreatedBy == userId);
        }

        // ── KPI Cards ─────────────────────────────────────────────────────
        vm.TotalCustomers = await custQ.CountAsync();
        vm.TotalLeads = await leadQ.CountAsync();
        vm.OpenLeads = await leadQ.CountAsync(l =>
            l.Status != LeadStatus.Converted && l.Status != LeadStatus.Lost);
        vm.TotalOpportunities = await oppQ.CountAsync();
        vm.OpenOpportunities = await oppQ.CountAsync(o => o.Status == OpportunityStatus.Open);
        vm.WonOpportunities = await oppQ.CountAsync(o => o.Status == OpportunityStatus.Won);
        vm.LostOpportunities = await oppQ.CountAsync(o => o.Status == OpportunityStatus.Lost);
        vm.TotalPipelineValue = (await oppQ
            .Where(o => o.Status == OpportunityStatus.Open)
            .Select(o => o.Amount)
            .ToListAsync())
            .Sum();
        vm.PendingFollowUps = await fuQ.CountAsync(f => f.Status == FollowUpStatus.Planned);
        vm.OverdueFollowUps = await fuQ.CountAsync(f =>
            f.Status == FollowUpStatus.Planned && f.FollowUpDate < DateTime.UtcNow);

        // ── Lead status chart ─────────────────────────────────────────────
        var leadStatusItems = await leadQ.Select(l => l.Status).ToListAsync();
        vm.LeadStatusCounts = leadStatusItems
            .GroupBy(s => s)
            .Select(g => new ChartDataPoint { Label = g.Key.ToString(), Value = g.Count() })
            .ToList();

        // ── Opportunity pipeline chart ─────────────────────────────────────
        var oppStages = await oppQ.Select(o => new { o.Stage, o.Amount }).ToListAsync();
        vm.OpportunityStageAmounts = oppStages
            .GroupBy(o => o.Stage)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.ToString(),
                Value = (int)g.Sum(o => o.Amount)
            })
            .ToList();

        // ── Monthly sales (last 6 months) ──────────────────────────────────
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var wonOpps = await _db.Opportunities
            .Where(o => o.Status == OpportunityStatus.Won && o.ModifiedDate >= sixMonthsAgo)
            .Select(o => new { o.ModifiedDate!.Value.Year, o.ModifiedDate.Value.Month, o.Amount })
            .ToListAsync();

        var monthlySales = wonOpps
            .GroupBy(o => new { o.Year, o.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Total = g.Sum(o => o.Amount)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        vm.MonthlySalesLabels = monthlySales
            .Select(x => new DateTime(x.Year, x.Month, 1).ToString("MMM yyyy"))
            .ToList();
        vm.MonthlySalesData = monthlySales.Select(x => x.Total).ToList();

        // ── Recent activities ──────────────────────────────────────────────
        vm.RecentActivities = await _db.AuditLogs
            .OrderByDescending(a => a.CreatedDate)
            .Take(10)
            .ToListAsync();

        return vm;
    }
}
