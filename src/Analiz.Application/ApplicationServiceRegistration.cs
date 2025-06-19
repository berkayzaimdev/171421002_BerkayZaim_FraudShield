using System.Reflection;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.ML;
using Analiz.Application.Interfaces.Services;
using Analiz.Application.Interfaces.Training;
using Analiz.Application.Services;
using Analiz.Application.Services.Training;
using Analiz.ML.Evaluator;
using Analiz.ML.Models.LightGBM;
using Analiz.ML.Models.PCA;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML;
using StackExchange.Redis;

namespace Analiz.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IModelService, ModelService>();
        services.AddScoped<IFraudDetectionService, FraudDetectionService>();
        //services.AddScoped<IRiskService, RiskService>();
        services.AddScoped<IFeatureExtractionService, FeatureEngineeringService>();

        services.AddScoped<IFraudRuleEngine, FraudRuleEngine>();
        services.AddScoped<IFraudRuleService, FraudRuleService>();
        services.AddScoped<IFraudRuleEventService, FraudRuleEventService>();
        services.AddScoped<IBlacklistService, BlacklistService>();
        services.AddScoped<IAlertService, AlertService>();
        services.AddSingleton<PythonMLIntegrationService>();
        //services.AddScoped<IFraudDetectionService, FraudDetectionService>();

        services.Configure<PCAConfiguration>(configuration.GetSection("PCAConfiguration"));
        services.Configure<LightGBMConfiguration>(configuration.GetSection("LightGBMConfiguration"));


        services.AddScoped<IModelEvaluator, ModelEvaluator>();
        services.AddSingleton<MLContext>();

       // services.AddScoped<IFraudModelTrainingService, FraudModelTrainingService>();

        var redisConnectionString = configuration.GetConnectionString("Redis")
                                    ?? "localhost:6379,abortConnect=false";

        // Redis cache ekle
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "FraudShield:";
        });

        // Redis multiplexer ekle
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(redisConnectionString));


        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}