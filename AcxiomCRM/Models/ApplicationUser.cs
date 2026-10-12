using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

/// <summary>
/// Extended Identity user with CRM-specific profile fields.
/// Never expose PasswordHash or SecurityStamp to UI/API.
/// </summary>
public class ApplicationUser : IdentityUser
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginDate { get; set; }

    // Navigation
    public virtual ICollection<Customer> Customers { get; set; } = [];
    public virtual ICollection<Lead> Leads { get; set; } = [];
    public virtual ICollection<Opportunity> Opportunities { get; set; } = [];
    public virtual ICollection<FollowUp> FollowUps { get; set; } = [];
    public virtual ICollection<Activity> Activities { get; set; } = [];
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = [];

    public string FullName => $"{FirstName} {LastName}".Trim();
}
