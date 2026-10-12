using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public enum OpportunityStage
{
    Qualification,
    Proposal,
    Negotiation,
    Won,
    Lost
}

public enum OpportunityStatus
{
    Open,
    Won,
    Lost,
    OnHold
}

/// <summary>
/// Opportunity entity. Amount > 0 for active, probability 0-100, close date not in past.
/// Weighted value = Amount * Probability / 100.
/// </summary>
public class Opportunity
{
    [Key]
    public int OpportunityId { get; set; }

    [Required, MaxLength(200)]
    public string OpportunityName { get; set; } = string.Empty;

    public int? CustomerId { get; set; }

    public int? LeadId { get; set; }

    [Required, Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

    [Range(0, 100)]
    public int Probability { get; set; } = 50;

    public DateTime ExpectedCloseDate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(100)]
    public string? Source { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    // FK - Owner
    public string? AssignedTo { get; set; }

    [Required, MaxLength(450)]
    public string CreatedBy { get; set; } = string.Empty;

    // Calculated property (not stored)
    [NotMapped]
    public decimal WeightedValue => Amount * Probability / 100m;

    // Navigation
    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    [ForeignKey(nameof(LeadId))]
    public virtual Lead? Lead { get; set; }

    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedToUser { get; set; }

    [ForeignKey(nameof(CreatedBy))]
    public virtual ApplicationUser? CreatedByUser { get; set; }

    public virtual ICollection<FollowUp> FollowUps { get; set; } = [];
    public virtual ICollection<Activity> Activities { get; set; } = [];
}
