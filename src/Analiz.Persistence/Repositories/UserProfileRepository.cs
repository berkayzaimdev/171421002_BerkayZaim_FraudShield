using System.Text.Json;
using Analiz.Application.Interfaces.Services;
using Analiz.Domain;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Analiz.Persistence.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<UserProfileRepository> _logger;
    private readonly string _profileStoragePath;

    public UserProfileRepository(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ILogger<UserProfileRepository> logger,
        string profileStoragePath = "Data/UserProfiles")
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _profileStoragePath = profileStoragePath;

        // Ensure directory exists
        Directory.CreateDirectory(_profileStoragePath);
    }

    public async Task<RiskProfile> GetUserRiskProfileAsync(string userId)
    {
        try
        {
            // Try to get from file storage
            var filePath = GetProfileFilePath(userId.ToString());
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var profile = JsonSerializer.Deserialize<RiskProfile>(json);

                // Check if profile is recent enough
                if (profile != null && (DateTime.UtcNow - profile.LastUpdated).TotalHours < 24) return profile;
            }

            // If no profile or outdated, generate a new one
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Get user transactions
            var transactions = await context.Transactions
                .Where(t => t.UserId == userId && !t.IsDeleted)
                .Include(t => t.RiskScore)
                .OrderByDescending(t => t.TransactionTime)
                .Take(100)
                .ToListAsync();

            // Build a new profile
            var newProfile = BuildProfileFromTransactions(userId, transactions);

            // Save the new profile
            await SaveUserRiskProfileAsync(newProfile);

            return newProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user risk profile for {UserId}", userId);

            // Return a default profile in case of error
            return new RiskProfile
            {
                UserId = Guid.Parse(userId),
                AverageRiskScore = 0.5, // Default to medium risk
                TransactionCount = 0,
                HighRiskTransactionCount = 0,
                CommonRiskFactors = new Dictionary<string, int>(),
                LastUpdated = DateTime.UtcNow,
                AverageTransactionAmount = 0
            };
        }
    }

    public async Task SaveUserRiskProfileAsync(RiskProfile profile)
    {
        try
        {
            // Update LastUpdated timestamp
            profile.LastUpdated = DateTime.UtcNow;

            // Serialize to JSON
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Save to file
            var filePath = GetProfileFilePath(profile.UserId.ToString());
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("Successfully saved risk profile for user {UserId}", profile.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user risk profile for {UserId}", profile.UserId);
            throw new RepositoryException($"Failed to save user risk profile for {profile.UserId}", ex);
        }
    }

    private string GetProfileFilePath(string userId)
    {
        // Sanitize user ID for use in filename
        var safeUserId = Path.GetInvalidFileNameChars()
            .Aggregate(userId, (current, c) => current.Replace(c, '_'));

        return Path.Combine(_profileStoragePath, $"{safeUserId}.json");
    }

    private RiskProfile BuildProfileFromTransactions(string userId, List<Transaction> transactions)
    {
        if (transactions == null || !transactions.Any())
            // İşlem yoksa varsayılan düşük riskli profil döndür
            return new RiskProfile
            {
                UserId = Guid.Parse(userId),
                AverageRiskScore = 0.1,
                TransactionCount = 0,
                HighRiskTransactionCount = 0,
                CommonRiskFactors = new Dictionary<string, int>(),
                LastUpdated = DateTime.UtcNow,
                AverageTransactionAmount = 0
            };

        // Risk puanı toplamı ve yüksek riskli işlem sayısını hesapla
        double totalRiskScore = 0;
        var highRiskCount = 0;
        var factorCounts = new Dictionary<string, int>();

        foreach (var transaction in transactions)
            // Risk puanını hesapla ve topla
            if (transaction.RiskScore != null)
            {
                var normalizedScore = GetNormalizedRiskScore(transaction.RiskScore.Level);
                totalRiskScore += normalizedScore;

                // Yüksek riskli işlemleri say
                if (transaction.RiskScore.Level >= RiskLevel.High) highRiskCount++;

                // Risk faktörlerini say
                if (transaction.RiskScore.Factors != null)
                    foreach (var factor in transaction.RiskScore.Factors)
                    {
                        if (!factorCounts.ContainsKey(factor)) factorCounts[factor] = 0;

                        factorCounts[factor]++;
                    }
            }

        // Ortalama risk puanı ve işlem miktarını hesapla
        var averageRiskScore = transactions.Any(t => t.RiskScore != null)
            ? totalRiskScore / transactions.Count(t => t.RiskScore != null)
            : 0.1;

        var averageAmount = transactions.Average(t => (double)t.Amount);

        // Risk profilini oluştur
        var profile = new RiskProfile
        {
            UserId = Guid.Parse(userId),
            AverageRiskScore = averageRiskScore,
            TransactionCount = transactions.Count,
            HighRiskTransactionCount = highRiskCount,
            CommonRiskFactors = factorCounts,
            LastUpdated = DateTime.UtcNow,
            AverageTransactionAmount = averageAmount
        };

        return profile;
    }

    private double GetNormalizedRiskScore(RiskLevel level)
    {
        return level switch
        {
            RiskLevel.Critical => 1.0,
            RiskLevel.High => 0.8,
            RiskLevel.Medium => 0.5,
            RiskLevel.Low => 0.2,
            _ => 0.1
        };
    }
}