using AcxiomCRM.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Data;

/// <summary>
/// Main EF Core DbContext with Identity + CRM entities.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Lead> Leads { get; set; }
    public DbSet<Opportunity> Opportunities { get; set; }
    public DbSet<FollowUp> FollowUps { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Customer ──────────────────────────────────────────────────────
        builder.Entity<Customer>(e =>
        {
            e.HasIndex(c => c.Email).IsUnique();
            e.HasIndex(c => c.Phone).IsUnique();
            e.HasIndex(c => c.CustomerCode).IsUnique();

            // Avoid cascade cycles
            e.HasOne(c => c.CreatedByUser)
             .WithMany(u => u.Customers)
             .HasForeignKey(c => c.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(c => c.AssignedToUser)
             .WithMany()
             .HasForeignKey(c => c.AssignedTo)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Lead ──────────────────────────────────────────────────────────
        builder.Entity<Lead>(e =>
        {
            e.HasIndex(l => l.LeadCode).IsUnique();

            e.HasOne(l => l.AssignedToUser)
             .WithMany(u => u.Leads)
             .HasForeignKey(l => l.AssignedTo)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(l => l.CreatedByUser)
             .WithMany()
             .HasForeignKey(l => l.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(l => l.Customer)
             .WithMany(c => c.Leads)
             .HasForeignKey(l => l.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Opportunity ───────────────────────────────────────────────────
        builder.Entity<Opportunity>(e =>
        {
            e.HasOne(o => o.AssignedToUser)
             .WithMany(u => u.Opportunities)
             .HasForeignKey(o => o.AssignedTo)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(o => o.CreatedByUser)
             .WithMany()
             .HasForeignKey(o => o.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(o => o.Customer)
             .WithMany(c => c.Opportunities)
             .HasForeignKey(o => o.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(o => o.Lead)
             .WithMany(l => l.Opportunities)
             .HasForeignKey(o => o.LeadId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── FollowUp ──────────────────────────────────────────────────────
        builder.Entity<FollowUp>(e =>
        {
            e.HasOne(f => f.AssignedToUser)
             .WithMany(u => u.FollowUps)
             .HasForeignKey(f => f.AssignedTo)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(f => f.CreatedByUser)
             .WithMany()
             .HasForeignKey(f => f.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(f => f.Customer)
             .WithMany(c => c.FollowUps)
             .HasForeignKey(f => f.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(f => f.Lead)
             .WithMany(l => l.FollowUps)
             .HasForeignKey(f => f.LeadId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(f => f.Opportunity)
             .WithMany(o => o.FollowUps)
             .HasForeignKey(f => f.OpportunityId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Activity ──────────────────────────────────────────────────────
        builder.Entity<Activity>(e =>
        {
            e.HasOne(a => a.AssignedToUser)
             .WithMany(u => u.Activities)
             .HasForeignKey(a => a.AssignedTo)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(a => a.CreatedByUser)
             .WithMany()
             .HasForeignKey(a => a.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(a => a.Customer)
             .WithMany(c => c.Activities)
             .HasForeignKey(a => a.CustomerId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(a => a.Lead)
             .WithMany(l => l.Activities)
             .HasForeignKey(a => a.LeadId)
             .OnDelete(DeleteBehavior.SetNull);

            e.HasOne(a => a.Opportunity)
             .WithMany(o => o.Activities)
             .HasForeignKey(a => a.OpportunityId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── AuditLog ──────────────────────────────────────────────────────
        builder.Entity<AuditLog>(e =>
        {
            e.HasIndex(al => al.CreatedDate);
            e.HasIndex(al => al.UserId);
            e.HasIndex(al => al.EntityName);
        });
    }
}
