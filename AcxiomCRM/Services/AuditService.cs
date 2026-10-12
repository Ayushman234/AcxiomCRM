using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

/// <summary>
/// Service for recording audit events. 
/// Never record passwords, hashes, or tokens.
/// </summary>
public interface IAuditService
{
    Task LogAsync(string action, string entityName, string? recordId = null,
        string? oldValue = null, string? newValue = null, string? details = null,
        string result = "Success");
}

public class AuditService : IAuditService
{
    private readonly Data.ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> _userManager;

    public AuditService(
        Data.ApplicationDbContext db,
        IHttpContextAccessor httpContextAccessor,
        Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    public async Task LogAsync(string action, string entityName, string? recordId = null,
        string? oldValue = null, string? newValue = null, string? details = null,
        string result = "Success")
    {
        var ctx = _httpContextAccessor.HttpContext;
        var userName = ctx?.User?.Identity?.Name;
        string? userId = null;

        if (userName != null)
        {
            var user = await _userManager.FindByNameAsync(userName);
            userId = user?.Id;
            userName = user?.FullName ?? userName;
        }

        var ipAddress = ctx?.Connection?.RemoteIpAddress?.ToString();

        var log = new AuditLog
        {
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityName = entityName,
            RecordId = recordId,
            OldValue = oldValue,
            NewValue = newValue,
            Details = details,
            Result = result,
            IpAddress = ipAddress,
            CreatedDate = DateTime.UtcNow
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
