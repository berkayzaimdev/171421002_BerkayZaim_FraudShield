using Analiz.Application.Interfaces.Repositories;
using Analiz.Application.Interfaces.Services;
using Analiz.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Analiz.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
        
        var builder = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("DefaultConnection"))
        {
            MinPoolSize = 5,
            MaxPoolSize = 50,
            ConnectionIdleLifetime = 300, // 5 dakika
            Pooling = true
        };

        var connectionString = builder.ToString();

        // DbContext yapılandırması
        services.AddDbContextPool<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    3,
                    TimeSpan.FromSeconds(5),
                    new[] { "53300" }); // too many clients hatası için
            });

            // DB Context ayarları
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
        }, 128); // DbContext pool size artırıldı

        // Factory kaydı
        services.AddPooledDbContextFactory<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions => { npgsqlOptions.EnableRetryOnFailure(3); });
        });

        services.AddScoped<IAnalysisResultRepository, AnalysisResultRepository>();
        services.AddScoped<IFeatureRepository, FeatureRepository>();
        services.AddScoped<IModelRepository, ModelRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IFeatureConfigurationRepository, FeatureConfigurationRepository>();
        services.AddScoped<IFraudRuleRepository, FraudRuleRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IFraudAlertRepository, FraudAlertRepository>();
        services.AddScoped<IBlacklistRepository, BlacklistRepository>();
        services.AddScoped<IFraudRuleEventRepository, FraudRuleEventRepository>();
        services.AddScoped<IRiskEvaluationRepository, RiskEvaluationRepository>();
        services.AddScoped<IRiskFactorRepository, RiskFactorRepository>();
        return services;
    }
}