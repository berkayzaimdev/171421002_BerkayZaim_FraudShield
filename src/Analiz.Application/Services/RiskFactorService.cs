using System.Text;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Application.Interfaces.Services;
using Analiz.Domain;
using Analiz.Domain.Entities;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Analiz.Application.Services;

public class RiskFactorService : IRiskFactorService
{
    private readonly IRiskFactorRepository _riskFactorRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILogger<RiskFactorService> _logger;

    // Risk eşik değerleri
    private const int HIGH_RISK_FACTOR_THRESHOLD = 3;
    private const double HIGH_RISK_SCORE_THRESHOLD = 0.7;

    public RiskFactorService(
        IRiskFactorRepository riskFactorRepository,
        ITransactionRepository transactionRepository,
        IUserProfileRepository userProfileRepository,
        ILogger<RiskFactorService> logger)
    {
        _riskFactorRepository = riskFactorRepository ?? throw new ArgumentNullException(nameof(riskFactorRepository));
        _transactionRepository =
            transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _userProfileRepository =
            userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<RiskFactor>> GetTransactionRiskFactorsAsync(Guid transactionId)
    {
        try
        {
            _logger.LogInformation("İşlem risk faktörleri getiriliyor. TransactionId: {TransactionId}", transactionId);
            return await _riskFactorRepository.GetAllForTransactionAsync(transactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlem risk faktörleri getirilirken hata oluştu. TransactionId: {TransactionId}",
                transactionId);
            throw new Exception("İşlem risk faktörleri getirilirken hata oluştu", ex);
        }
    }

    public async Task<Dictionary<string, int>> GetUserRiskFactorDistributionAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Kullanıcı risk faktör dağılımı getiriliyor. UserId: {UserId}", userId);
            return await _riskFactorRepository.GetMostCommonFactorsAsync(userId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı risk faktör dağılımı getirilirken hata oluştu. UserId: {UserId}", userId);
            throw new Exception("Kullanıcı risk faktör dağılımı getirilirken hata oluştu", ex);
        }
    }

    public async Task<RiskProfile> CalculateUserRiskProfileAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Kullanıcı risk profili hesaplanıyor. UserId: {UserId}", userId);

            // Kullanıcının işlemlerini getir
            var userTransactions = await _transactionRepository.GetUserTransactionHistoryAsync(userId.ToString(), 100);

            if (userTransactions == null || !userTransactions.Any())
                // İşlem geçmişi yoksa varsayılan düşük riskli profil oluştur
                return new RiskProfile
                {
                    UserId = userId,
                    AverageRiskScore = 0.1,
                    TransactionCount = 0,
                    HighRiskTransactionCount = 0,
                    CommonRiskFactors = new Dictionary<string, int>(),
                    LastUpdated = DateTime.UtcNow,
                    AverageTransactionAmount = 0
                };

            // Kullanıcının risk faktörlerini getir
            var riskFactors = await _riskFactorRepository.GetByUserIdAsync(userId.ToString());

            // Risk faktörlerini say ve grupla
            var factorCounts = riskFactors
                .GroupBy(rf => rf.Code)
                .ToDictionary(g => g.Key, g => g.Count());

            // Yüksek riskli işlem sayısını hesapla
            var highRiskCount = userTransactions.Count(t =>
                t.RiskScore != null && t.RiskScore.Level >= RiskLevel.High);

            // Ortalama risk puanını hesapla
            var totalRiskScore = userTransactions
                .Where(t => t.RiskScore != null)
                .Sum(t => GetNormalizedRiskScore(t.RiskScore.Level));

            var avgRiskScore = userTransactions.Any(t => t.RiskScore != null)
                ? totalRiskScore / userTransactions.Count(t => t.RiskScore != null)
                : 0.1;

            // Ortalama işlem tutarını hesapla
            var avgAmount = userTransactions.Average(t => (double)t.Amount);

            // Risk profilini oluştur ve döndür
            var profile = new RiskProfile
            {
                UserId = userId,
                AverageRiskScore = avgRiskScore,
                TransactionCount = userTransactions.Count,
                HighRiskTransactionCount = highRiskCount,
                CommonRiskFactors = factorCounts,
                LastUpdated = DateTime.UtcNow,
                AverageTransactionAmount = avgAmount
            };

            // Profili kaydet
            await _userProfileRepository.SaveUserRiskProfileAsync(profile);

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı risk profili hesaplanırken hata oluştu. UserId: {UserId}", userId);
            throw new Exception("Kullanıcı risk profili hesaplanırken hata oluştu", ex);
        }
    }

    public async Task<bool> AddRiskFactorAsync(RiskFactor riskFactor)
    {
        try
        {
            _logger.LogInformation("Risk faktörü ekleniyor. Description: {Description}", riskFactor.Description);
            return await _riskFactorRepository.AddAsync(riskFactor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk faktörü eklenirken hata oluştu");
            throw new Exception("Risk faktörü eklenirken hata oluştu", ex);
        }
    }

    public async Task<bool> AddRiskFactorsAsync(List<RiskFactor> riskFactors)
    {
        try
        {
            _logger.LogInformation("Toplu risk faktörleri ekleniyor. Sayı: {Count}", riskFactors.Count);
            return await _riskFactorRepository.AddRangeAsync(riskFactors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Toplu risk faktörleri eklenirken hata oluştu");
            throw new Exception("Toplu risk faktörleri eklenirken hata oluştu", ex);
        }
    }

    public async Task<List<RiskFactor>> IdentifyUserBehaviorRiskFactorsAsync(Guid userId,
        TransactionData currentTransaction)
    {
        try
        {
            _logger.LogInformation("Kullanıcı davranış risk faktörleri belirleniyor. UserId: {UserId}", userId);

            var riskFactors = new List<RiskFactor>();

            // Kullanıcının mevcut risk profilini al
            var userProfile = await _userProfileRepository.GetUserRiskProfileAsync(userId.ToString());

            // Profil yoksa veya yeni oluşturulduysa, risk faktörü ekleme
            if (userProfile == null || userProfile.TransactionCount < 5)
                return riskFactors;

            // Genel risk puanını kontrol et
            if (userProfile.AverageRiskScore > HIGH_RISK_SCORE_THRESHOLD)
                riskFactors.Add(RiskFactor.Create(
                    RiskFactorType.UserBehavior,
                    $"Kullanıcı yüksek riskli geçmişe sahip (ort. puan: {userProfile.AverageRiskScore:F2})",
                    userProfile.AverageRiskScore));

            // Tekrarlayan risk faktörlerini kontrol et
            foreach (var commonFactor in userProfile.CommonRiskFactors)
                if (commonFactor.Value >= HIGH_RISK_FACTOR_THRESHOLD)
                {
                    riskFactors.Add(RiskFactor.Create(
                        RiskFactorType.RecurringPattern,
                        $"Tekrarlayan risk örüntüsü: {commonFactor.Key} (sıklık: {commonFactor.Value})",
                        Math.Min(0.5 + commonFactor.Value * 0.1, 0.9)));

                    // Sadece en yaygın faktörü ekle
                    break;
                }

            // İşlem tutarını kontrol et
            if (currentTransaction.Amount > (decimal)(userProfile.AverageTransactionAmount * 3))
                riskFactors.Add(RiskFactor.Create(
                    RiskFactorType.AnomalyDetection,
                    $"Ortalama işlem tutarından çok yüksek ({currentTransaction.Amount:C2} > {userProfile.AverageTransactionAmount:C2} x 3)",
                    Math.Min((double)currentTransaction.Amount / (userProfile.AverageTransactionAmount * 5), 0.9)));

            return riskFactors;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kullanıcı davranış risk faktörleri belirlenirken hata oluştu. UserId: {UserId}",
                userId);
            return new List<RiskFactor>(); // Hata durumunda boş liste döndür
        }
    }

    public async Task<List<RiskFactor>> GetHighRiskFactorsAsync(RiskLevel minSeverity = RiskLevel.High, int limit = 50)
    {
        try
        {
            _logger.LogInformation("Yüksek riskli faktörler getiriliyor. MinSeverity: {MinSeverity}", minSeverity);
            return await _riskFactorRepository.GetHighSeverityFactorsAsync(minSeverity, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yüksek riskli faktörler getirilirken hata oluştu");
            throw new Exception("Yüksek riskli faktörler getirilirken hata oluştu", ex);
        }
    }

    public async Task<Dictionary<RiskFactorType, int>> GetRiskFactorTrendAsync(Guid userId, TimeSpan period)
    {
        try
        {
            _logger.LogInformation("Risk faktör trendi hesaplanıyor. UserId: {UserId}, Period: {Period}", userId,
                period);

            var startDate = DateTime.UtcNow.Subtract(period);
            var riskFactors = await _riskFactorRepository.GetByUserIdAsync(userId.ToString(), startDate);

            // Risk faktör türlerine göre grupla ve say
            var trendByType = riskFactors
                .GroupBy(rf =>
                {
                    // Code'dan RiskFactorType'a dönüştür
                    Enum.TryParse<RiskFactorType>(rf.Code, out var factorType);
                    return factorType;
                })
                .ToDictionary(g => g.Key, g => g.Count());

            return trendByType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk faktör trendi hesaplanırken hata oluştu. UserId: {UserId}", userId);
            throw new Exception("Risk faktör trendi hesaplanırken hata oluştu", ex);
        }
    }

    public async Task<bool> IsRecurringRiskPatternAsync(Guid userId, RiskFactorType factorType, int threshold = 3)
    {
        try
        {
            var count = await _riskFactorRepository.GetFactorCountByTypeAsync(userId.ToString(), factorType);
            return count >= threshold;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Tekrarlayan risk örüntüsü kontrolünde hata oluştu. UserId: {UserId}, FactorType: {FactorType}",
                userId, factorType);
            return false; // Hata durumunda varsayılan olarak false döndür
        }
    }

    public async Task<string> GenerateRiskFactorReportAsync(Guid transactionId)
    {
        try
        {
            var transaction = await _transactionRepository.GetTransactionAsync(transactionId);
            if (transaction == null)
                throw new Exception($"Transaction not found: {transactionId}");

            var riskFactors = await _riskFactorRepository.GetAllForTransactionAsync(transactionId);

            var reportBuilder = new StringBuilder();
            reportBuilder.AppendLine($"Risk Faktör Raporu - İşlem: {transactionId}");
            reportBuilder.AppendLine($"Tarih: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
            reportBuilder.AppendLine($"İşlem Tarihi: {transaction.TransactionTime:yyyy-MM-dd HH:mm:ss}");
            reportBuilder.AppendLine($"Tutar: {transaction.Amount:C2}");
            reportBuilder.AppendLine($"Kullanıcı ID: {transaction.UserId}");
            reportBuilder.AppendLine();

            reportBuilder.AppendLine("Risk Faktörleri:");
            if (riskFactors.Any())
            {
                foreach (var factor in riskFactors.OrderByDescending(rf => rf.Severity))
                {
                    reportBuilder.AppendLine($"- {factor.Description}");
                    reportBuilder.AppendLine($"  Şiddet: {factor.Severity}, Güven: {factor.Confidence:P2}");
                    reportBuilder.AppendLine($"  Kod: {factor.Code}");
                    reportBuilder.AppendLine();
                }

                // Şiddet seviyelerine göre özet
                var severitySummary = riskFactors
                    .GroupBy(rf => rf.Severity)
                    .Select(g => new { Severity = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Severity);

                reportBuilder.AppendLine("Şiddet Özeti:");
                foreach (var item in severitySummary)
                    reportBuilder.AppendLine($"- {item.Severity}: {item.Count} faktör");
            }
            else
            {
                reportBuilder.AppendLine("Bu işlem için risk faktörü bulunmamaktadır.");
            }

            return reportBuilder.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Risk faktör raporu oluşturulurken hata oluştu. TransactionId: {TransactionId}",
                transactionId);
            throw new Exception("Risk faktör raporu oluşturulurken hata oluştu", ex);
        }
    }

    #region Helper Methods

    private double GetNormalizedRiskScore(RiskLevel level)
    {
        return level switch
        {
            RiskLevel.Critical => 1.0,
            RiskLevel.High => 0.8,
            RiskLevel.Medium => 0.5,
            RiskLevel.Low => 0.2,
            _ => 0.0
        };
    }

    #endregion
}