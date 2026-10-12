using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

/// <summary>
/// REST API for Customers. Requires authentication.
/// Never exposes EF entities directly — uses CustomerDto.
/// </summary>
[ApiController]
[Route("api/customers")]
[Authorize]
[Produces("application/json")]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomersApiController(ICustomerService customerService,
        UserManager<ApplicationUser> userManager)
    {
        _customerService = customerService;
        _userManager = userManager;
    }

    private async Task<(string userId, string role)> GetContextAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        return (user!.Id, roles.FirstOrDefault() ?? AppRoles.SalesExecutive);
    }

    /// <summary>GET /api/customers — List customers with pagination and search</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new ApiErrorResponse { Message = "Invalid pagination parameters.", StatusCode = 400 });

        var (userId, role) = await GetContextAsync();
        var filter = new ViewModels.CustomerFilterVM { Search = search, Page = page, PageSize = pageSize };
        var (items, total) = await _customerService.GetPagedAsync(filter, userId, role);

        var data = items.Select(MapToDto).ToList();
        return Ok(new PagedApiResponse<CustomerDto>
        {
            Data = data,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    /// <summary>GET /api/customers/{id}</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
            return NotFound(new ApiErrorResponse { Message = "Customer not found.", StatusCode = 404 });

        var (userId, role) = await GetContextAsync();
        if (role == AppRoles.SalesExecutive &&
            customer.AssignedTo != userId && customer.CreatedBy != userId)
            return StatusCode(403, new ApiErrorResponse { Message = "Access denied.", StatusCode = 403 });

        return Ok(MapToDto(customer));
    }

    /// <summary>POST /api/customers</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiErrorResponse
            {
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList(),
                StatusCode = 400
            });

        var user = await _userManager.GetUserAsync(User);
        var customer = new Customer
        {
            CustomerName = dto.CustomerName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Address = dto.Address,
            City = dto.City,
            State = dto.State,
            Notes = dto.Notes,
            AssignedTo = dto.AssignedTo,
            CreatedBy = user!.Id
        };

        var (success, error) = await _customerService.CreateAsync(customer);
        if (!success)
        {
            if (error!.Contains("email") || error.Contains("phone"))
                return Conflict(new ApiErrorResponse { Message = error, StatusCode = 409 });
            return BadRequest(new ApiErrorResponse { Message = error, StatusCode = 400 });
        }

        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, MapToDto(customer));
    }

    /// <summary>PUT /api/customers/{id}</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiErrorResponse
            {
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList(),
                StatusCode = 400
            });

        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
            return NotFound(new ApiErrorResponse { Message = "Customer not found.", StatusCode = 404 });

        var (userId, role) = await GetContextAsync();
        if (role == AppRoles.SalesExecutive &&
            customer.AssignedTo != userId && customer.CreatedBy != userId)
            return StatusCode(403, new ApiErrorResponse { Message = "Access denied.", StatusCode = 403 });

        customer.CustomerName = dto.CustomerName;
        customer.Email = dto.Email;
        customer.Phone = dto.Phone;
        customer.CompanyName = dto.CompanyName;
        customer.Address = dto.Address;
        customer.City = dto.City;
        customer.State = dto.State;
        customer.Notes = dto.Notes;
        customer.AssignedTo = dto.AssignedTo;
        customer.Status = dto.Status;

        var (success, error) = await _customerService.UpdateAsync(customer);
        if (!success)
            return Conflict(new ApiErrorResponse { Message = error!, StatusCode = 409 });

        return Ok(MapToDto(customer));
    }

    /// <summary>DELETE /api/customers/{id} — Deactivates customer</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Manager}")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, error) = await _customerService.DeleteAsync(id);
        if (!success)
            return NotFound(new ApiErrorResponse { Message = error!, StatusCode = 404 });

        return NoContent();
    }

    private static CustomerDto MapToDto(Customer c) => new()
    {
        CustomerId = c.CustomerId,
        CustomerCode = c.CustomerCode,
        CustomerName = c.CustomerName,
        Email = c.Email,
        Phone = c.Phone,
        CompanyName = c.CompanyName,
        City = c.City,
        State = c.State,
        Status = c.Status.ToString(),
        AssignedTo = c.AssignedTo,
        AssignedToName = c.AssignedToUser?.FullName,
        CreatedDate = c.CreatedDate
    };
}
