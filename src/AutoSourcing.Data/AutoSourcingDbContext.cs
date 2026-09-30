using AutoSourcing.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoSourcing.Data;

public class AutoSourcingDbContext : DbContext
{
    public AutoSourcingDbContext(DbContextOptions<AutoSourcingDbContext> options) : base(options)
    {
    }

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadEmail> LeadEmails => Set<LeadEmail>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<OutreachMessage> OutreachMessages => Set<OutreachMessage>();
    public DbSet<Sequence> Sequences => Set<Sequence>();
    public DbSet<SequenceStep> SequenceSteps => Set<SequenceStep>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<OrganizationProfile> OrganizationProfiles => Set<OrganizationProfile>();
    public DbSet<ChannelConsent> ChannelConsents => Set<ChannelConsent>();
    public DbSet<LeadProfile> LeadProfiles => Set<LeadProfile>();
    public DbSet<PolicyGuardrails> PolicyGuardrails => Set<PolicyGuardrails>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

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
            entity.Property(l => l.Location).HasMaxLength(200);
            entity.Property(l => l.LinkedInUrl).HasMaxLength(500);
            entity.Property(l => l.Source).HasMaxLength(100).IsRequired();
            entity.Property(l => l.Country).HasMaxLength(100);

            entity.HasIndex(l => l.ConversationKey);

            entity.HasMany(l => l.ChannelConsents)
                .WithOne(c => c.Lead)
                .HasForeignKey(c => c.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(l => l.Conversations)
                .WithOne(c => c.Lead)
                .HasForeignKey(c => c.LeadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LeadEmail>(entity =>
        {
            entity.HasIndex(e => new { e.LeadId, e.Email }).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(320).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Priority).IsRequired();

            entity.HasOne(e => e.Lead)
                .WithMany(l => l.Emails)
                .HasForeignKey(e => e.LeadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Description).HasMaxLength(2000);

            entity.HasOne(c => c.Sequence)
                .WithMany(s => s.Campaigns)
                .HasForeignKey(c => c.SequenceId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OutreachMessage>(entity =>
        {
            entity.Property(m => m.Subject).HasMaxLength(500);
            entity.Property(m => m.Body).IsRequired();
            entity.Property(m => m.ErrorMessage).HasMaxLength(2000);

            entity.Property(m => m.StepOrder);
            entity.Property(m => m.OpenedAt);
            entity.Property(m => m.ClickedAt);
            entity.Property(m => m.RepliedAt);
            entity.Property(m => m.FromAddress).HasMaxLength(320);
            entity.Property(m => m.FromName).HasMaxLength(200);
            entity.Property(m => m.ReplyTo).HasMaxLength(320);

            entity.HasOne(m => m.Lead)
                .WithMany(l => l.OutreachMessages)
                .HasForeignKey(m => m.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Campaign)
                .WithMany(c => c.OutreachMessages)
                .HasForeignKey(m => m.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Sequence>(entity =>
        {
            entity.Property(s => s.Name).HasMaxLength(200).IsRequired();
            entity.Property(s => s.Description).HasMaxLength(2000);

            entity.HasMany(s => s.Steps)
                .WithOne(st => st.Sequence)
                .HasForeignKey(st => st.SequenceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SequenceStep>(entity =>
        {
            entity.Property(st => st.Name).HasMaxLength(200).IsRequired();
            entity.Property(st => st.SubjectTemplate).HasMaxLength(500);
            entity.Property(st => st.BodyTemplate).IsRequired();
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.Property(j => j.Title).HasMaxLength(200).IsRequired();
            entity.Property(j => j.Location).HasMaxLength(200);
            entity.Property(j => j.Industry).HasMaxLength(200);
            entity.Property(j => j.AdvertUrl).HasMaxLength(500);
            entity.Property(j => j.AdvertCopy).HasColumnType("nvarchar(max)");
            entity.Property(j => j.MustHaves).HasColumnType("nvarchar(max)");
            entity.Property(j => j.NiceToHaves).HasColumnType("nvarchar(max)");
            entity.Property(j => j.Education).HasColumnType("nvarchar(max)");
            entity.Property(j => j.Skills).HasColumnType("nvarchar(max)");
            entity.Property(j => j.AttractiveReasons).HasColumnType("nvarchar(max)");
            entity.Property(j => j.ScreeningDetails).HasColumnType("nvarchar(max)");
            entity.Property(j => j.HiringManager).HasMaxLength(200);
            entity.Property(j => j.Department).HasMaxLength(200);
            entity.Property(j => j.SalaryNotes).HasMaxLength(500);
            entity.Property(j => j.SalaryFrom).HasPrecision(18, 2);
            entity.Property(j => j.SalaryTo).HasPrecision(18, 2);
        });

        modelBuilder.Entity<OrganizationProfile>(entity =>
        {
            entity.Property(o => o.OrgName).HasMaxLength(200);
            entity.Property(o => o.EscalationEmail).HasMaxLength(320);
            entity.Property(o => o.About).HasColumnType("nvarchar(max)");
            entity.Property(o => o.EVP).HasColumnType("nvarchar(max)");
            entity.Property(o => o.Culture).HasColumnType("nvarchar(max)");
            entity.Property(o => o.HiringProcess).HasColumnType("nvarchar(max)");
            entity.Property(o => o.EEO).HasColumnType("nvarchar(max)");
            entity.Property(o => o.GuardRails).HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<ChannelConsent>(entity =>
        {
            entity.Property(c => c.OptInSource).HasMaxLength(200);
            entity.Property(c => c.Notes).HasMaxLength(500);
            entity.HasIndex(c => new { c.LeadId, c.Channel }).IsUnique();
        });

        modelBuilder.Entity<LeadProfile>(entity =>
        {
            entity.Property(p => p.Headline).HasMaxLength(500);
            entity.Property(p => p.Summary).HasColumnType("nvarchar(max)");
            entity.Property(p => p.SelfReportedSkills).HasColumnType("nvarchar(max)");
            entity.Property(p => p.AIInferredSkills).HasColumnType("nvarchar(max)");
            entity.Property(p => p.WorkExperience).HasColumnType("nvarchar(max)");
            entity.Property(p => p.Education).HasColumnType("nvarchar(max)");
            entity.Property(p => p.Certifications).HasColumnType("nvarchar(max)");
            entity.Property(p => p.Industries).HasColumnType("nvarchar(max)");
            entity.Property(p => p.Languages).HasColumnType("nvarchar(max)");
            entity.Property(p => p.Memberships).HasColumnType("nvarchar(max)");
            entity.Property(p => p.Publications).HasColumnType("nvarchar(max)");
            entity.Property(p => p.Awards).HasColumnType("nvarchar(max)");
            entity.Property(p => p.Patents).HasColumnType("nvarchar(max)");

            entity.HasOne(p => p.Lead)
                .WithOne(l => l.Profile)
                .HasForeignKey<LeadProfile>(p => p.LeadId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PolicyGuardrails>(entity =>
        {
            entity.Property(p => p.WhatAiMayAnswer).HasColumnType("nvarchar(max)");
            entity.Property(p => p.EscalationTriggers).HasColumnType("nvarchar(max)");
            entity.Property(p => p.RefusalTopics).HasColumnType("nvarchar(max)");
            entity.Property(p => p.RequiredDisclaimers).HasColumnType("nvarchar(max)");
            entity.Property(p => p.MarketRules).HasColumnType("nvarchar(max)");
            entity.Property(p => p.NeedsHumanStates).HasColumnType("nvarchar(max)");
            entity.Property(p => p.ConfidenceThreshold).HasPrecision(5, 2);
        });

        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.Property(c => c.Role).HasMaxLength(20).IsRequired();
            entity.Property(c => c.Content).HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(320).IsRequired();
            entity.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(u => u.SendFromAddress).HasMaxLength(320);
            entity.Property(u => u.SendFromName).HasMaxLength(200);
            entity.Property(u => u.ReplyToAddress).HasMaxLength(320);
            entity.Property(u => u.MicrosoftAccountEmail).HasMaxLength(320);
            entity.Property(u => u.MicrosoftRefreshToken).HasColumnType("nvarchar(max)");
            entity.Property(u => u.MicrosoftAccessToken).HasColumnType("nvarchar(max)");

            entity.HasMany(u => u.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(s => s.TokenHash).IsUnique();
            entity.Property(s => s.TokenHash).HasMaxLength(200).IsRequired();
        });
    }
}
