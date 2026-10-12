using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.DTOs;

// ── Customer DTOs ─────────────────────────────────────────────────────────
public class CustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateCustomerDto
{
    [Required(ErrorMessage = "Customer name is required.")]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [MaxLength(20)]
    [RegularExpression(@"^[0-9]{10,15}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    public string? AssignedTo { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

public class UpdateCustomerDto : CreateCustomerDto
{
    public CustomerStatus Status { get; set; }
}

// ── Lead DTOs ─────────────────────────────────────────────────────────────
public class LeadDto
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal ExpectedValue { get; set; }
    public string? AssignedToName { get; set; }
    public bool IsConverted { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateLeadDto
{
    [Required(ErrorMessage = "Lead name is required.")]
    [MaxLength(200)]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [MaxLength(20)]
    [RegularExpression(@"^[0-9]{10,15}$", ErrorMessage = "Enter a valid phone number.")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    public LeadSource Source { get; set; } = LeadSource.Other;

    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Range(0, 10000000)]
    public decimal ExpectedValue { get; set; }

    public string? AssignedTo { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

// ── Opportunity DTOs ──────────────────────────────────────────────────────
public class OpportunityDto
{
    public int OpportunityId { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public decimal Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Probability { get; set; }
    public decimal WeightedValue { get; set; }
    public DateTime ExpectedCloseDate { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateOpportunityDto
{
    [Required(ErrorMessage = "Opportunity name is required.")]
    [MaxLength(200)]
    public string OpportunityName { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 50;

    [Required(ErrorMessage = "Expected close date is required.")]
    public DateTime ExpectedCloseDate { get; set; }

    public string? AssignedTo { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

// ── Common response ────────────────────────────────────────────────────────
public class ApiErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = [];
    public int StatusCode { get; set; }
}

public class PagedApiResponse<T>
{
    public List<T> Data { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
