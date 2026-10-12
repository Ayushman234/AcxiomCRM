using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public enum CustomerStatus
{
    Active,
    Inactive,
    Prospect,
    Churned
}

/// <summary>
/// Customer master record. Email and Phone must be unique.
/// </summary>
public class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [Required, MaxLength(20)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, MaxLength(256), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    [Required, MaxLength(450)]
    public string CreatedBy { get; set; } = string.Empty;  // FK to ApplicationUser.Id

    public string? AssignedTo { get; set; }  // FK to ApplicationUser.Id

    // Navigation
    [ForeignKey(nameof(CreatedBy))]
    public virtual ApplicationUser? CreatedByUser { get; set; }

    [ForeignKey(nameof(AssignedTo))]
    public virtual ApplicationUser? AssignedToUser { get; set; }

    public virtual ICollection<Lead> Leads { get; set; } = [];
    public virtual ICollection<Opportunity> Opportunities { get; set; } = [];
    public virtual ICollection<FollowUp> FollowUps { get; set; } = [];
    public virtual ICollection<Activity> Activities { get; set; } = [];
}
