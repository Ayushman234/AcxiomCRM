using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public enum FollowUpStatus
{
    Planned,
    Completed,
    Missed,
    Cancelled
}

public enum FollowUpType
{
    Call,
    Email,
    Meeting,
    Demo,
    Other
}

/// <summary>
/// Follow-up record. FollowUpDate cannot be earlier than today for new/planned follow-ups.
/// </summary>
public class FollowUp
{
    [Key]
    public int FollowUpId { get; set; }

    public int? CustomerId { get; set; }

    public int? LeadId { get; set; }

    public int? OpportunityId { get; set; }

    [Required]
    public DateTime FollowUpDate { get; set; }

    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

    [Required, MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Remarks { get; set; }

    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    public string? AssignedTo { get; set; }

    [Required, MaxLength(450)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    [MaxLength(2000)]
    public string? CompletionNotes { get; set; }

    // Navigation
    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    [ForeignKey(nameof(LeadId))]
    public virtual Lead? Lead { get; set; }

    [ForeignKey(nameof(OpportunityId))]
    public virtual Opportunity? Opportunity { get; set; }

    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedToUser { get; set; }

    [ForeignKey(nameof(CreatedBy))]
    public virtual ApplicationUser? CreatedByUser { get; set; }

    [NotMapped]
    public bool IsOverdue => Status == FollowUpStatus.Planned && FollowUpDate < DateTime.UtcNow;
}
