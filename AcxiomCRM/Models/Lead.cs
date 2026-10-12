using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public enum LeadStatus
{
    New,
    Contacted,
    Qualified,
    Unqualified,
    Converted,
    Lost
}

public enum LeadSource
{
    Website,
    Referral,
    ColdCall,
    Email,
    SocialMedia,
    Advertisement,
    TradeShow,
    Other
}

public enum LeadPriority
{
    Low,
    Medium,
    High
}

/// <summary>
/// Lead entity. Valid status transitions enforced in service layer.
/// </summary>
public class Lead
{
    [Key]
    public int LeadId { get; set; }

    [Required, MaxLength(20)]
    public string LeadCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string LeadName { get; set; } = string.Empty;

    [Required, MaxLength(256), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    public LeadSource Source { get; set; } = LeadSource.Other;

    public LeadStatus Status { get; set; } = LeadStatus.New;

    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ExpectedValue { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    // FK - Assigned sales executive
    public string? AssignedTo { get; set; }

    [Required, MaxLength(450)]
    public string CreatedBy { get; set; } = string.Empty;

    public int? CustomerId { get; set; }  // Set on conversion

    public bool IsConverted { get; set; } = false;

    public DateTime? ConvertedDate { get; set; }

    // Navigation
    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedToUser { get; set; }

    [ForeignKey(nameof(CreatedBy))]
    public virtual ApplicationUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    public virtual ICollection<FollowUp> FollowUps { get; set; } = [];
    public virtual ICollection<Activity> Activities { get; set; } = [];
    public virtual ICollection<Opportunity> Opportunities { get; set; } = [];
}
