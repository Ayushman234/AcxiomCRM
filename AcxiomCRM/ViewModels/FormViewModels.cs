using System.ComponentModel.DataAnnotations;
using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

// ── Account ViewModels ────────────────────────────────────────────────────
public class LoginVM
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}

public class RegisterVM
{
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$",
        ErrorMessage = "Password must contain uppercase, lowercase, and a digit.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// ── Customer ViewModels ───────────────────────────────────────────────────
public class CustomerVM
{
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Customer name is required.")]
    [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [MaxLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
    [RegularExpression(@"^[0-9]{10,15}$", ErrorMessage = "Enter a valid phone number (10-15 digits).")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Company Name")]
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

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    // For dropdown
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> SalesExecutives { get; set; } = [];
}

// ── Lead ViewModels ───────────────────────────────────────────────────────
public class LeadVM
{
    public int LeadId { get; set; }

    [Required(ErrorMessage = "Lead name is required.")]
    [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    [MaxLength(20)]
    [RegularExpression(@"^[0-9]{10,15}$", ErrorMessage = "Enter a valid phone number (10-15 digits).")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    public LeadSource Source { get; set; } = LeadSource.Other;

    [Required(ErrorMessage = "Status is required.")]
    public LeadStatus Status { get; set; } = LeadStatus.New;

    public LeadPriority Priority { get; set; } = LeadPriority.Medium;

    [Range(0, 10000000, ErrorMessage = "Expected value must be between 0 and 10,000,000.")]
    [Display(Name = "Expected Value")]
    public decimal ExpectedValue { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> SalesExecutives { get; set; } = [];
}

// ── Opportunity ViewModels ────────────────────────────────────────────────
public class OpportunityVM
{
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity name is required.")]
    [MaxLength(200)]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [Required(ErrorMessage = "Amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal Amount { get; set; }

    public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

    public OpportunityStatus Status { get; set; } = OpportunityStatus.Open;

    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 50;

    [Required(ErrorMessage = "Expected close date is required.")]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddMonths(1);

    [MaxLength(100)]
    public string? Source { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Customers { get; set; } = [];
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Leads { get; set; } = [];
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> SalesExecutives { get; set; } = [];
}

// ── FollowUp ViewModels ────────────────────────────────────────────────────
public class FollowUpVM
{
    public int FollowUpId { get; set; }

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [Display(Name = "Opportunity")]
    public int? OpportunityId { get; set; }

    [Required(ErrorMessage = "Follow-up date is required.")]
    [Display(Name = "Follow-Up Date")]
    public DateTime FollowUpDate { get; set; } = DateTime.Today;

    [Display(Name = "Type")]
    public FollowUpType FollowUpType { get; set; } = FollowUpType.Call;

    [Required(ErrorMessage = "Subject is required.")]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Remarks { get; set; }

    public FollowUpStatus Status { get; set; } = FollowUpStatus.Planned;

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Customers { get; set; } = [];
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Leads { get; set; } = [];
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Opportunities { get; set; } = [];
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> SalesExecutives { get; set; } = [];
}

// ── Activity ViewModels ────────────────────────────────────────────────────
public class ActivityVM
{
    public int ActivityId { get; set; }

    [Display(Name = "Activity Type")]
    public ActivityType ActivityType { get; set; } = ActivityType.Call;

    [Required(ErrorMessage = "Subject is required.")]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Activity date is required.")]
    [Display(Name = "Activity Date")]
    public DateTime ActivityDate { get; set; } = DateTime.Today;

    [Display(Name = "Duration (Minutes)")]
    public int? DurationMinutes { get; set; }

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    public ActivityStatus Status { get; set; } = ActivityStatus.Planned;

    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Customers { get; set; } = [];
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Leads { get; set; } = [];
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> SalesExecutives { get; set; } = [];
}

// ── User Management ViewModels ─────────────────────────────────────────────
public class UserListVM
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsLockedOut { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLoginDate { get; set; }
}

public class CreateUserVM
{
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

public class EditUserVM
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}

public class ChangePasswordVM
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(8)]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
