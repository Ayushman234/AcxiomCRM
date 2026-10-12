using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

/// <summary>
/// Append-only audit log. Never allow modification or deletion via UI.
/// Never store passwords, hashes, tokens, or secrets.
/// </summary>
public class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    [MaxLength(256)]
    public string? UserName { get; set; }

    [Required, MaxLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? RecordId { get; set; }

    [MaxLength(4000)]
    public string? OldValue { get; set; }

    [MaxLength(4000)]
    public string? NewValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? Details { get; set; }

    // Result: Success, Failed, etc.
    [MaxLength(50)]
    public string Result { get; set; } = "Success";
}
