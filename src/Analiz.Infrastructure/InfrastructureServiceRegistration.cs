using Analiz.Application.Interfaces.Infrastructure;
using Analiz.Infrastructure.Services;
using FraudShield.TransactionAnalysis.Domain.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Analiz.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IDomainEventService, DomainEventService>();
        /* services.Configure<BackgroundJobSettings>(configuration.GetSection("BackgroundJobs"));

         services.AddHostedService<ModelRetrainingJob>();
         services.AddHostedService<PerformanceMonitoringJob>();
         services.AddHostedService<DataCleanupJob>();
         */
        services.AddScoped<ITestDataService, TestDataService>();
        return services;
    }
}