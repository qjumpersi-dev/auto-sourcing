using AutoSourcing.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.Data;

public class AutoSourcingDbContext : DbContext
{
    public AutoSourcingDbContext(DbContextOptions<AutoSourcingDbContext> options) : base(options)
    {
    }

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<OutreachMessage> OutreachMessages => Set<OutreachMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Lead>(entity =>
        {
            entity.HasIndex(l => l.Email).IsUnique().HasFilter("[Email] <> ''");
            entity.HasIndex(l => l.ExternalId);
            entity.Property(l => l.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(l => l.LastName).HasMaxLength(100).IsRequired();
            entity.Property(l => l.Email).HasMaxLength(320).IsRequired();
            entity.Property(l => l.Phone).HasMaxLength(50);
            entity.Property(l => l.Company).HasMaxLength(200);
            entity.Property(l => l.JobTitle).HasMaxLength(200);
            entity.Property(l => l.LinkedInUrl).HasMaxLength(500);
            entity.Property(l => l.Source).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<OutreachMessage>(entity =>
        {
            entity.Property(m => m.Subject).HasMaxLength(500);
            entity.Property(m => m.Body).IsRequired();
            entity.Property(m => m.ErrorMessage).HasMaxLength(2000);

            entity.HasOne(m => m.Lead)
                .WithMany(l => l.OutreachMessages)
                .HasForeignKey(m => m.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Campaign)
                .WithMany(c => c.OutreachMessages)
                .HasForeignKey(m => m.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
