using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

// ── Dashboard ─────────────────────────────────────────────────────────────
public class DashboardVM
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public int PendingFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }

    // Chart data
    public List<ChartDataPoint> LeadStatusCounts { get; set; } = [];
    public List<ChartDataPoint> OpportunityStageAmounts { get; set; } = [];
    public List<string> MonthlySalesLabels { get; set; } = [];
    public List<decimal> MonthlySalesData { get; set; } = [];

    // Recent
    public List<AuditLog> RecentActivities { get; set; } = [];

    // Date filter
    public string DateFilter { get; set; } = "month";
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public int Value { get; set; }
}

// ── Filter ViewModels ─────────────────────────────────────────────────────
public class CustomerFilterVM
{
    public string? Search { get; set; }
    public CustomerStatus? Status { get; set; }
    public string? SortBy { get; set; } = "name";
    public bool SortDesc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class LeadFilterVM
{
    public string? Search { get; set; }
    public LeadStatus? Status { get; set; }
    public string? AssignedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class OpportunityFilterVM
{
    public string? Search { get; set; }
    public OpportunityStage? Stage { get; set; }
    public OpportunityStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class FollowUpFilterVM
{
    public FollowUpStatus? Status { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ActivityFilterVM
{
    public string? Search { get; set; }
    public ActivityType? Type { get; set; }
    public ActivityStatus? Status { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

// ── Report filters ────────────────────────────────────────────────────────
public class ReportFilterVM
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? StartDate { get => FromDate; set => FromDate = value; }
    public DateTime? EndDate { get => ToDate; set => ToDate = value; }
    public string? Status { get; set; }
    public string? AssignedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AuditFilterVM
{
    public string? UserId { get; set; }
    public string? EntityName { get; set; }
    public string? Action { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? StartDate { get => FromDate; set => FromDate = value; }
    public DateTime? EndDate { get => ToDate; set => ToDate = value; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

// ── Pagination ─────────────────────────────────────────────────────────────
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
}

// ── Report output ──────────────────────────────────────────────────────────
public class PipelineReportItem
{
    public string Stage { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}
