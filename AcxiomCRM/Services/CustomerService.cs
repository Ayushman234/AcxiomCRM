using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services;

public interface ICustomerService
{
    Task<(List<Customer> Items, int Total)> GetPagedAsync(CustomerFilterVM filter, string? userId, string role);
    Task<Customer?> GetByIdAsync(int id);
    Task<(bool Success, string? Error)> CreateAsync(Customer customer);
    Task<(bool Success, string? Error)> UpdateAsync(Customer customer);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
    Task<string> GenerateCodeAsync();
    Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null);
    Task<bool> IsPhoneUniqueAsync(string phone, int? excludeId = null);
}

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public CustomerService(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<(List<Customer> Items, int Total)> GetPagedAsync(
        CustomerFilterVM filter, string? userId, string role)
    {
        var query = _db.Customers
            .Include(c => c.AssignedToUser)
            .Include(c => c.CreatedByUser)
            .AsQueryable();

        // Role-based scope
        if (role == AppRoles.SalesExecutive && userId != null)
            query = query.Where(c => c.AssignedTo == userId || c.CreatedBy == userId);

        // Filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.ToLower();
            query = query.Where(c =>
                c.CustomerName.ToLower().Contains(s) ||
                c.Email.ToLower().Contains(s) ||
                c.Phone.Contains(s) ||
                (c.CompanyName != null && c.CompanyName.ToLower().Contains(s)));
        }
        if (filter.Status.HasValue)
            query = query.Where(c => c.Status == filter.Status.Value);

        var total = await query.CountAsync();

        // Sorting
        query = filter.SortBy switch
        {
            "email" => filter.SortDesc ? query.OrderByDescending(c => c.Email) : query.OrderBy(c => c.Email),
            "company" => filter.SortDesc ? query.OrderByDescending(c => c.CompanyName) : query.OrderBy(c => c.CompanyName),
            "status" => filter.SortDesc ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            "date" => filter.SortDesc ? query.OrderByDescending(c => c.CreatedDate) : query.OrderBy(c => c.CreatedDate),
            _ => filter.SortDesc ? query.OrderByDescending(c => c.CustomerName) : query.OrderBy(c => c.CustomerName)
        };

        var page = filter.Page < 1 ? 1 : filter.Page;
        var size = filter.PageSize < 1 ? 10 : filter.PageSize;
        var items = await query.Skip((page - 1) * size).Take(size).ToListAsync();

        return (items, total);
    }

    public async Task<Customer?> GetByIdAsync(int id) =>
        await _db.Customers
            .Include(c => c.AssignedToUser)
            .Include(c => c.CreatedByUser)
            .Include(c => c.Leads)
            .Include(c => c.Opportunities)
            .Include(c => c.FollowUps)
            .Include(c => c.Activities)
            .FirstOrDefaultAsync(c => c.CustomerId == id);

    public async Task<(bool Success, string? Error)> CreateAsync(Customer customer)
    {
        if (!await IsEmailUniqueAsync(customer.Email))
            return (false, "A customer with this email already exists.");
        if (!await IsPhoneUniqueAsync(customer.Phone))
            return (false, "A customer with this phone number already exists.");

        customer.CustomerCode = await GenerateCodeAsync();
        customer.CreatedDate = DateTime.UtcNow;
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Create", "Customer", customer.CustomerId.ToString(),
            newValue: customer.CustomerName);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(Customer customer)
    {
        if (!await IsEmailUniqueAsync(customer.Email, customer.CustomerId))
            return (false, "A customer with this email already exists.");
        if (!await IsPhoneUniqueAsync(customer.Phone, customer.CustomerId))
            return (false, "A customer with this phone number already exists.");

        customer.ModifiedDate = DateTime.UtcNow;
        _db.Customers.Update(customer);
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Update", "Customer", customer.CustomerId.ToString());
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var customer = await _db.Customers.FindAsync(id);
        if (customer == null) return (false, "Customer not found.");

        customer.Status = CustomerStatus.Inactive;
        customer.ModifiedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync("Deactivate", "Customer", id.ToString(), oldValue: "Active");
        return (true, null);
    }

    public async Task<string> GenerateCodeAsync()
    {
        var count = await _db.Customers.CountAsync() + 1;
        return $"CUST-{count:D5}";
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null)
    {
        var q = _db.Customers.Where(c => c.Email == email);
        if (excludeId.HasValue) q = q.Where(c => c.CustomerId != excludeId.Value);
        return !await q.AnyAsync();
    }

    public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeId = null)
    {
        var q = _db.Customers.Where(c => c.Phone == phone);
        if (excludeId.HasValue) q = q.Where(c => c.CustomerId != excludeId.Value);
        return !await q.AnyAsync();
    }
}
