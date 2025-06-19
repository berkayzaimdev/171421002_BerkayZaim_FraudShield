using System.Reflection;
using Analiz.Domain;
using Analiz.Domain.Entities;
using Analiz.Domain.Extensions;
using Analiz.Domain.ValueObjects;
using FraudShield.TransactionAnalysis.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Analiz.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly IDomainEventService? _domainEventService;

    // Transactions
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionFlag> TransactionFlags { get; set; }

    // Models
    public DbSet<ModelMetadata> Models { get; set; }

    // Analysis
    public DbSet<AnalysisResult> AnalysisResults { get; set; }
    public DbSet<RiskFactor> RiskFactors { get; set; }
    public DbSet<RiskProfile> RiskProfiles { get; set; }
    public DbSet<RiskEvaluation> RiskEvaluations { get; set; } = null!;
    public DbSet<BlacklistItem> BlacklistItems { get; set; }

    // Alerts and Rules
    public DbSet<FraudAlert> FraudAlerts { get; set; }
    public DbSet<FraudRule> FraudRules { get; set; }
    public DbSet<FraudRuleEvent> FraudRuleEvents { get; set; }

    // Features
    public DbSet<FeatureConfiguration> FeatureConfigurations { get; set; }
    public DbSet<FeatureImportance> FeatureImportance { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(Entity).IsAssignableFrom(entityType.ClrType))
                modelBuilder.Entity(entityType.ClrType)
                    .Ignore(nameof(Entity.DomainEvents));

            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType)) entityType.AddSoftDeleteQueryFilter();
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }


    private void UpdateAuditFields()
    {
        foreach (var entry in ChangeTracker.Entries<Entity>())
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.CreatedBy = "system";
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedAt = DateTime.UtcNow;
                    entry.Entity.LastModifiedBy = "system";
                    break;
            }
    }

    private List<DomainEvent> GetDomainEvents()
    {
        var domainEvents = ChangeTracker.Entries<Entity>()
            .Select(x => x.Entity.DomainEvents)
            .SelectMany(x => x)
            .ToList();

        return domainEvents;
    }

    private async Task DispatchEvents(List<DomainEvent> events)
    {
        foreach (var @event in events) await _domainEventService.Publish(@event);
    }
}