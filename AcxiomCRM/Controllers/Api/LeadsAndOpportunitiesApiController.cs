using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/leads")]
[Authorize]
[Produces("application/json")]
public class LeadsApiController : ControllerBase
{
    private readonly ILeadService _leadService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeadsApiController(ILeadService leadService, UserManager<ApplicationUser> userManager)
    {
        _leadService = leadService;
        _userManager = userManager;
    }

    private async Task<(string userId, string role)> GetContextAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        return (user!.Id, roles.FirstOrDefault() ?? AppRoles.SalesExecutive);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (userId, role) = await GetContextAsync();
        var filter = new ViewModels.LeadFilterVM { Search = search, Page = page, PageSize = pageSize };
        var (items, total) = await _leadService.GetPagedAsync(filter, userId, role);

        return Ok(new PagedApiResponse<LeadDto>
        {
            Data = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var lead = await _leadService.GetByIdAsync(id);
        if (lead == null) return NotFound(new ApiErrorResponse { Message = "Lead not found.", StatusCode = 404 });
        return Ok(MapToDto(lead));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeadDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiErrorResponse
            {
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList(),
                StatusCode = 400
            });

        var user = await _userManager.GetUserAsync(User);
        var lead = new Lead
        {
            LeadName = dto.LeadName,
            Email = dto.Email,
            Phone = dto.Phone,
            CompanyName = dto.CompanyName,
            Source = dto.Source,
            Priority = dto.Priority,
            ExpectedValue = dto.ExpectedValue,
            Notes = dto.Notes,
            AssignedTo = dto.AssignedTo,
            CreatedBy = user!.Id
        };

        var (success, error) = await _leadService.CreateAsync(lead);
        if (!success)
            return BadRequest(new ApiErrorResponse { Message = error!, StatusCode = 400 });

        return CreatedAtAction(nameof(GetById), new { id = lead.LeadId }, MapToDto(lead));
    }

    private static LeadDto MapToDto(Lead l) => new()
    {
        LeadId = l.LeadId,
        LeadCode = l.LeadCode,
        LeadName = l.LeadName,
        Email = l.Email,
        Phone = l.Phone,
        CompanyName = l.CompanyName,
        Source = l.Source.ToString(),
        Status = l.Status.ToString(),
        Priority = l.Priority.ToString(),
        ExpectedValue = l.ExpectedValue,
        AssignedToName = l.AssignedToUser?.FullName,
        IsConverted = l.IsConverted,
        CreatedDate = l.CreatedDate
    };
}

[ApiController]
[Route("api/opportunities")]
[Authorize]
[Produces("application/json")]
public class OpportunitiesApiController : ControllerBase
{
    private readonly IOpportunityService _oppService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OpportunitiesApiController(IOpportunityService oppService,
        UserManager<ApplicationUser> userManager)
    {
        _oppService = oppService;
        _userManager = userManager;
    }

    private async Task<(string userId, string role)> GetContextAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var roles = await _userManager.GetRolesAsync(user!);
        return (user!.Id, roles.FirstOrDefault() ?? AppRoles.SalesExecutive);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var (userId, role) = await GetContextAsync();
        var filter = new ViewModels.OpportunityFilterVM { Search = search, Page = page, PageSize = pageSize };
        var (items, total) = await _oppService.GetPagedAsync(filter, userId, role);

        return Ok(new PagedApiResponse<OpportunityDto>
        {
            Data = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var opp = await _oppService.GetByIdAsync(id);
        if (opp == null) return NotFound(new ApiErrorResponse { Message = "Opportunity not found.", StatusCode = 404 });
        return Ok(MapToDto(opp));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOpportunityDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiErrorResponse
            {
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList(),
                StatusCode = 400
            });

        // Business validation
        if (dto.ExpectedCloseDate.Date < DateTime.UtcNow.Date)
            return BadRequest(new ApiErrorResponse
            {
                Message = "Expected Close Date cannot be in the past.",
                StatusCode = 400
            });

        var user = await _userManager.GetUserAsync(User);
        var opp = new Opportunity
        {
            OpportunityName = dto.OpportunityName,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Notes = dto.Notes,
            AssignedTo = dto.AssignedTo,
            CreatedBy = user!.Id,
            Status = OpportunityStatus.Open
        };

        var (success, error) = await _oppService.CreateAsync(opp);
        if (!success)
            return BadRequest(new ApiErrorResponse { Message = error!, StatusCode = 400 });

        return CreatedAtAction(nameof(GetById), new { id = opp.OpportunityId }, MapToDto(opp));
    }

    private static OpportunityDto MapToDto(Opportunity o) => new()
    {
        OpportunityId = o.OpportunityId,
        OpportunityName = o.OpportunityName,
        CustomerId = o.CustomerId,
        CustomerName = o.Customer?.CustomerName,
        Amount = o.Amount,
        Stage = o.Stage.ToString(),
        Status = o.Status.ToString(),
        Probability = o.Probability,
        WeightedValue = o.WeightedValue,
        ExpectedCloseDate = o.ExpectedCloseDate,
        AssignedToName = o.AssignedToUser?.FullName,
        CreatedDate = o.CreatedDate
    };
}
