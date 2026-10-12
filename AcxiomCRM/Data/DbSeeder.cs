using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AcxiomCRM.Data;

/// <summary>
/// Seeds roles, standard accounts (Admin, Manager, Sales Executive),
/// and initial CRM demo entities (Customers, Leads, Opportunities, Follow-Ups, Activities).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();
        var config = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

        // ── Ensure roles exist ────────────────────────────────────────────
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // ── Seed users ────────────────────────────────────────────────────
        var admin = await EnsureUserAsync(userManager, "admin@acxiomcrm.com", "Admin@1234", "System", "Administrator", AppRoles.Admin);
        var manager = await EnsureUserAsync(userManager, "manager@acxiomcrm.com", "Manager@1234", "Sarah", "Connor", AppRoles.Manager);
        var sales = await EnsureUserAsync(userManager, "sales@acxiomcrm.com", "Sales@1234", "John", "Doe", AppRoles.SalesExecutive);

        // ── Seed Customers if empty ───────────────────────────────────────
        if (!await db.Customers.AnyAsync())
        {
            var c1 = new Customer
            {
                CustomerCode = "CUST-0001",
                CustomerName = "Acme Global Industries",
                CompanyName = "Acme Corp",
                Email = "procurement@acme.com",
                Phone = "18005550101",
                Address = "100 Innovation Way",
                City = "San Jose",
                State = "CA",
                Status = CustomerStatus.Active,
                Notes = "Strategic Fortune 500 account; quarterly cloud license review scheduled.",
                CreatedBy = admin.Id,
                AssignedTo = sales.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-30)
            };

            var c2 = new Customer
            {
                CustomerCode = "CUST-0002",
                CustomerName = "Apex Logistics International",
                CompanyName = "Apex Logistics",
                Email = "ops@apexlogistics.io",
                Phone = "18005550102",
                Address = "450 Harbor Boulevard",
                City = "Seattle",
                State = "WA",
                Status = CustomerStatus.Active,
                Notes = "Supply chain software renewal in Q4.",
                CreatedBy = admin.Id,
                AssignedTo = sales.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-15)
            };

            db.Customers.AddRange(c1, c2);
            await db.SaveChangesAsync();

            // ── Seed Leads if empty ───────────────────────────────────────────
            var l1 = new Lead
            {
                LeadCode = "LEAD-0001",
                LeadName = "David Miller",
                Email = "dmiller@initech.com",
                Phone = "18005550103",
                CompanyName = "Initech Solutions",
                Source = LeadSource.Website,
                Status = LeadStatus.Qualified,
                Priority = LeadPriority.High,
                ExpectedValue = 45000m,
                Notes = "Inquired via pricing page for 250 enterprise seats.",
                CreatedBy = sales.Id,
                AssignedTo = sales.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-5)
            };

            var l2 = new Lead
            {
                LeadCode = "LEAD-0002",
                LeadName = "Elena Vance",
                Email = "elena.v@blackmesa.org",
                Phone = "18005550104",
                CompanyName = "Black Mesa Labs",
                Source = LeadSource.Referral,
                Status = LeadStatus.Contacted,
                Priority = LeadPriority.Medium,
                ExpectedValue = 28000m,
                Notes = "Referred by Acme Corp CTO.",
                CreatedBy = sales.Id,
                AssignedTo = sales.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-2)
            };

            db.Leads.AddRange(l1, l2);
            await db.SaveChangesAsync();

            // ── Seed Opportunities if empty ───────────────────────────────────
            var opp1 = new Opportunity
            {
                OpportunityName = "Enterprise Cloud Migration",
                CustomerId = c1.CustomerId,
                Amount = 125000m,
                Stage = OpportunityStage.Negotiation,
                Status = OpportunityStatus.Open,
                Probability = 75,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(20),
                Source = "Website",
                Notes = "Final contract review with legal counsel underway.",
                CreatedBy = admin.Id,
                AssignedTo = sales.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-10)
            };

            var opp2 = new Opportunity
            {
                OpportunityName = "Fleet Telematics SaaS Suite",
                CustomerId = c2.CustomerId,
                Amount = 60000m,
                Stage = OpportunityStage.Proposal,
                Status = OpportunityStatus.Open,
                Probability = 50,
                ExpectedCloseDate = DateTime.UtcNow.AddDays(45),
                Source = "Referral",
                Notes = "Proposal delivered; demo scheduled next Tuesday.",
                CreatedBy = admin.Id,
                AssignedTo = sales.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-7)
            };

            db.Opportunities.AddRange(opp1, opp2);
            await db.SaveChangesAsync();

            // ── Seed Follow-Ups ───────────────────────────────────────────────
            var f1 = new FollowUp
            {
                CustomerId = c1.CustomerId,
                OpportunityId = opp1.OpportunityId,
                FollowUpDate = DateTime.UtcNow.AddDays(2),
                FollowUpType = FollowUpType.Meeting,
                Subject = "Quarterly Business Review & SOW Signing",
                Remarks = "Executive sponsor review with VP of Infrastructure.",
                Status = FollowUpStatus.Planned,
                AssignedTo = sales.Id,
                CreatedBy = sales.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            };

            var f2 = new FollowUp
            {
                LeadId = l1.LeadId,
                FollowUpDate = DateTime.UtcNow.AddHours(4),
                FollowUpType = FollowUpType.Demo,
                Subject = "Platform Feature Walkthrough",
                Remarks = "Show dashboard analytics and role permission granularity.",
                Status = FollowUpStatus.Planned,
                AssignedTo = sales.Id,
                CreatedBy = sales.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-1)
            };

            db.FollowUps.AddRange(f1, f2);

            // ── Seed Activities ───────────────────────────────────────────────
            var a1 = new Activity
            {
                CustomerId = c1.CustomerId,
                ActivityType = ActivityType.Call,
                Subject = "Contract alignment teleconference",
                Description = "Agreed on payment milestones and warranty clauses.",
                ActivityDate = DateTime.UtcNow.AddDays(-3),
                CreatedBy = sales.Id,
                AssignedTo = sales.Id,
                CreatedDate = DateTime.UtcNow.AddDays(-3)
            };

            db.Activities.Add(a1);

            // ── Seed Initial Audit Log ────────────────────────────────────────
            var audit = new AuditLog
            {
                UserId = admin.Id,
                UserName = admin.Email,
                Action = "Seed",
                EntityName = "System",
                RecordId = "1",
                Details = "Initial enterprise data seeded successfully",
                CreatedDate = DateTime.UtcNow,
                Result = "Success"
            };

            db.AuditLogs.Add(audit);
            await db.SaveChangesAsync();
        }
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string firstName,
        string lastName,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                IsActive = true,
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };
            await userManager.CreateAsync(user, password);
            await userManager.AddToRoleAsync(user, role);
        }
        return user;
    }
}
