using System.Text.Json;
using System.Text.Json.Serialization;
using Analiz.Application;
using Analiz.Infrastructure;
using Analiz.Persistence;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

// Konsol loglama sağlayıcısını ekle ve formatı özelleştir
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = false; // Scopes'ları gösterme
    options.SingleLine = true; // Tek satırlık loglar
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss "; // Zaman damgasını özelleştir
});

// CORS Policy ekleme - Daha geniş izinler
builder.Services.AddCors(options =>
{
    options.AddPolicy("FraudDetectionPolicy", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000", 
            "http://localhost:3001", 
            "http://127.0.0.1:3000",
            "http://127.0.0.1:3001"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
        .SetIsOriginAllowed(origin => true) // Development için tüm origin'lere izin
        .WithExposedHeaders("*");
    });
    
    // Development için genel CORS policy
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Fraud Analysis API",
        Version = "v1",
        Description = "API for fraud detection and analysis"
    });
    c.UseInlineDefinitionsForEnums();
});

// Add other services
builder.Services.AddMemoryCache();

// Register application services
builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration)
    .AddPersistenceServices(builder.Configuration);


var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment()) 
{
    app.UseDeveloperExceptionPage();
    // Development ortamında açık CORS policy kullan
    app.UseCors("AllowAll");
}
else
{
    // Production ortamında daha kısıtlı CORS policy kullan
    app.UseCors("FraudDetectionPolicy");
}

// Configure Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Fraud Analysis API V1");
    c.RoutePrefix = "swagger";
    c.EnableTryItOutByDefault();
    c.DefaultModelsExpandDepth(-1); // Hide schemas section
});

// Basic middleware pipeline
app.UseRouting();
app.UseHttpsRedirection();
app.UseAuthorization();

// Global error handling - after routing, before endpoints
/*
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionHandlerFeature?.Error;

        var error = new
        {
            Status = context.Response.StatusCode,
            Message = exception?.Message,
            Detail = app.Environment.IsDevelopment() ? exception?.ToString() : null
        };

        await context.Response.WriteAsJsonAsync(error);
    });
});
*/
// Map endpoints
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    // Health check endpoint
    endpoints.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }));
});

/*
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!ctx.Set<FraudRule>().Any())
    {
        var rules = FraudRulesSeeder.GetSeedRules();
        ctx.AddRange(rules);
        ctx.SaveChanges();
        Console.WriteLine($"Seed edilen kural sayısı: {rules.Count}");
    }
}
*/
await app.RunAsync();