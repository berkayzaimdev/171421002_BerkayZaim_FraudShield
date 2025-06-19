using System.Collections.Concurrent;
using System.Text.Json;
using Analiz.Application.DTOs.Response;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Application.Interfaces.Services;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.Rule.Context;
using Analiz.Domain.ValueObjects;
using FraudShield.TransactionAnalysis.Domain.Enums;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;
using Microsoft.Extensions.Logging;

namespace Analiz.Application.Services;

/// <summary>
/// Fraud kural motoru implementasyonu
/// </summary>
public class FraudRuleEngine : IFraudRuleEngine
{
    private readonly IFraudRuleRepository _ruleRepository;
    private readonly IBlacklistService _blacklistService;
    private readonly IFraudRuleEventRepository _ruleEventRepository;
    private readonly ILogger<FraudRuleEngine> _logger;

    // Cache mekanizması - gerçek uygulamada Redis kullanılabilir
    private List<FraudRule> _cachedRules = new();

    private Dictionary<RuleCategory, List<FraudRule>>
        _rulesByCategory = new();

    public FraudRuleEngine(
        IFraudRuleRepository ruleRepository, IBlacklistService blacklistService,
        IFraudRuleEventRepository ruleEventRepository,
        ILogger<FraudRuleEngine> logger)
    {
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _blacklistService = blacklistService;
        _ruleEventRepository = ruleEventRepository ?? throw new ArgumentNullException(nameof(ruleEventRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Başlangıçta kuralları yükle
        Task.Run(ReloadRulesAsync).GetAwaiter().GetResult();
    }

    // Add to FraudRuleEngine class
    public async Task<List<RuleResult>> EvaluateRulesAsync(TransactionData transactionData)
    {
        // Create evaluation context from the transaction data
        var evaluationContext = new RuleEvaluationContext
        {
            Transaction = MapTransactionDataToContext(transactionData),
            IpAddress = MapToIpAddressContext(transactionData),
            Device = MapToDeviceContext(transactionData),
            EvaluationTime = DateTime.Now
        };

        // Evaluate all relevant rules
        var ruleResults = await EvaluateAllRulesAsync(evaluationContext);

        // Convert RuleEvaluationResult to RuleResult
        var triggeredRules = ruleResults
            .Where(r => r.IsTriggered)
            .Select(r => new RuleResult
            {
                RuleId = r.RuleCode,
                IsTriggered = r.IsTriggered,
                RuleName = r.RuleName,
                Confidence = r.TriggerScore,
                RuleDescription = r.TriggerDetails,
                Action = r.Actions.FirstOrDefault(),
                Score = r.TriggerScore * 100
            })
            .ToList();

        // Burada tetiklenen kural aksiyonlarını doğrudan uygulama mekanizması eklenebilir
        return triggeredRules;
    }

    private TransactionContext MapTransactionDataToContext(TransactionData data)
    {
        if (data == null) return null;

        // VO’ya kısaltma
        var vo = data.AdditionalData ?? new TransactionAdditionalData();

        var context = new TransactionContext
        {
            TransactionId = data.TransactionId,
            AccountId = data.UserId,
            Amount = data.Amount,
            TransactionDate = data.Timestamp,
            TransactionType = data.Type,
            RecipientCountry = data.Location?.Country,

            // VO’daki hız ve ortalama değerleri doğrudan mapliyoruz
            UserTransactionCount24h = vo.TransactionVelocity24h ?? 0,
            UserAverageTransactionAmount = vo.AverageTransactionAmount ?? 0m,
            // Eğer toplam tutar VO’da yoksa, dilerseniz Count24h × Average ile hesaplayabilirsiniz:
            UserTotalAmount24h = (vo.TransactionVelocity24h ?? 0) * (vo.AverageTransactionAmount ?? 0m),
            DaysSinceFirstTransaction = vo.DaysSinceFirstTransaction ?? 0,

            // VO'da olmayanlar için varsayılan
            UniqueRecipientCount1h = 0,

            // VO’daki tüm ekstra alanları buraya koyacağız
            AdditionalData = new Dictionary<string, object>()
        };

        // --- Ekstra VO alanlarını flatten ederek AdditionalData'ya ekliyoruz ---
        // Kart bilgileri
        if (!string.IsNullOrEmpty(vo.CardType)) context.AdditionalData["CardType"] = vo.CardType;
        if (!string.IsNullOrEmpty(vo.CardBin)) context.AdditionalData["CardBin"] = vo.CardBin;
        if (!string.IsNullOrEmpty(vo.CardLast4)) context.AdditionalData["CardLast4"] = vo.CardLast4;
        if (vo.CardExpiryMonth.HasValue) context.AdditionalData["CardExpiryMonth"] = vo.CardExpiryMonth.Value;
        if (vo.CardExpiryYear.HasValue) context.AdditionalData["CardExpiryYear"] = vo.CardExpiryYear.Value;

        // Banka bilgileri
        if (!string.IsNullOrEmpty(vo.BankName)) context.AdditionalData["BankName"] = vo.BankName;
        if (!string.IsNullOrEmpty(vo.BankCountry)) context.AdditionalData["BankCountry"] = vo.BankCountry;

        // V-Faktörler (Dictionary<string, float>)
        foreach (var kvp in vo.VFactors) context.AdditionalData[kvp.Key] = kvp.Value;

        // Diğer flag’ler
        if (vo.IsNewPaymentMethod.HasValue) context.AdditionalData["IsNewPaymentMethod"] = vo.IsNewPaymentMethod.Value;
        if (vo.IsInternational.HasValue) context.AdditionalData["IsInternational"] = vo.IsInternational.Value;

        // CustomValues
        foreach (var kvp in vo.CustomValues) context.AdditionalData[kvp.Key] = kvp.Value;

        return context;
    }

    private IpAddressContext MapToIpAddressContext(TransactionData data)
    {
        if (data?.DeviceInfo == null || string.IsNullOrEmpty(data.DeviceInfo.IpAddress))
            return null;

        // VO’ları kısaltma
        var vo = data.AdditionalData ?? new TransactionAdditionalData();
        var info = data.DeviceInfo;
        var custom = vo.CustomValues;
        var addInfo = info.AdditionalInfo;

        // Helper yerinde parse fonksiyonları
        int ParseInt(string key, int @default = 0)
        {
            return custom.TryGetValue(key, out var s) && int.TryParse(s, out var v) ? v : @default;
        }

        bool ParseBool(string key, bool @default = false)
        {
            return custom.TryGetValue(key, out var s) && bool.TryParse(s, out var v) ? v : @default;
        }

        // Context nesnesini inşa et
        var context = new IpAddressContext
        {
            IpAddress = info.IpAddress,
            CountryCode = data.Location?.Country,
            City = data.Location?.City,
            IspAsn = addInfo.TryGetValue("IspAsn", out var asn) ? asn : null,

            ReputationScore = ParseInt("IpReputationScore", 50),
            IsBlacklisted = ParseBool("IsIpBlacklisted"),
            BlacklistNotes = custom.TryGetValue("IpBlacklistNotes", out var notes) ? notes : null,
            IsDatacenterOrProxy = ParseBool("IsDatacenterOrProxy"),
            NetworkType = custom.TryGetValue("NetworkType", out var nt) ? nt : null,

            UniqueAccountCount10m = ParseInt("IpUniqueAccountCount10m"),
            UniqueAccountCount1h = ParseInt("IpUniqueAccountCount1h"),
            UniqueAccountCount24h = ParseInt("IpUniqueAccountCount24h"),
            FailedLoginCount10m = ParseInt("IpFailedLoginCount10m"),

            AdditionalData = new Dictionary<string, object>()
        };

        // High-risk bölge indirimi
        if (data.Location?.IsHighRiskRegion == true)
            context.ReputationScore =
                Math.Max(1, context.ReputationScore - 30);

        // DeviceInfo.AdditionalInfo içindeki tüm anahtar-değerleri alın
        foreach (var kvp in addInfo) context.AdditionalData[kvp.Key] = kvp.Value;

        // CustomValues’dan "Ip" ile başlayan tüm anahtarları da ekleyin
        foreach (var kvp in custom)
            if (kvp.Key.StartsWith("Ip", StringComparison.OrdinalIgnoreCase))
                context.AdditionalData[kvp.Key] = kvp.Value;

        return context;
    }


    private DeviceContext MapToDeviceContext(TransactionData data)
    {
        if (data?.DeviceInfo == null || string.IsNullOrEmpty(data.DeviceInfo.DeviceId))
            return null;

        var context = new DeviceContext
        {
            DeviceId = data.DeviceInfo.DeviceId,
            DeviceType = data.DeviceInfo.DeviceType,
            IpAddress = data.DeviceInfo.IpAddress,

            // Extract browser info from UserAgent if available
            Browser = ExtractBrowserFromUserAgent(data.DeviceInfo.UserAgent),

            // Initialize with defaults or extracted values
            OperatingSystem = ExtractOSFromUserAgent(data.DeviceInfo.UserAgent),
            IsEmulator = ExtractBoolFromAdditionalData(data.DeviceInfo.AdditionalInfo, "IsEmulator"),
            IsJailbroken = ExtractBoolFromAdditionalData(data.DeviceInfo.AdditionalInfo, "IsJailbroken"),

            // Convert DeviceInfo.AdditionalInfo to Dictionary<string, object>
            AdditionalData = data.DeviceInfo.AdditionalInfo?.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value)
        };

        // Try to extract date information if available
        if (data.DeviceInfo.AdditionalInfo != null)
        {
            if (data.DeviceInfo.AdditionalInfo.TryGetValue("FirstSeenDate", out var firstSeenDateStr))
                if (DateTime.TryParse(firstSeenDateStr, out var firstSeenDate))
                    context.FirstSeenDate = firstSeenDate;

            if (data.DeviceInfo.AdditionalInfo.TryGetValue("LastSeenDate", out var lastSeenDateStr))
            {
                if (DateTime.TryParse(lastSeenDateStr, out var lastSeenDate))
                    context.LastSeenDate = lastSeenDate;
                else
                    context.LastSeenDate = DateTime.UtcNow; // Default to current time if not available
            }

            // Extract additional numerical data
            context.UniqueAccountCount24h =
                ExtractIntFromAdditionalData(data.DeviceInfo.AdditionalInfo, "UniqueAccountCount24h");
            context.UniqueIpCount24h = ExtractIntFromAdditionalData(data.DeviceInfo.AdditionalInfo, "UniqueIpCount24h");
        }

        // Set GPS location if available from transaction data
        if (data.Location != null)
            context.GpsLocation = new GpsLocation
            {
                Latitude = data.Location.Latitude,
                Longitude = data.Location.Longitude,
                Country = data.Location.Country,
                City = data.Location.City
            };

        return context;
    }

// Helper method to extract currency from additional data
    private string ExtractCurrencyFromAdditionalData(Dictionary<string, string> additionalData)
    {
        if (additionalData == null) return "USD"; // Default currency

        if (additionalData.TryGetValue("Currency", out var currency) && !string.IsNullOrEmpty(currency))
            return currency;

        return "USD"; // Default currency
    }

// Helper methods to extract typed values from dictionaries
    private int ExtractIntFromAdditionalData(Dictionary<string, string> additionalData, string key,
        int defaultValue = 0)
    {
        if (additionalData == null || !additionalData.TryGetValue(key, out var valueStr))
            return defaultValue;

        return int.TryParse(valueStr, out var value) ? value : defaultValue;
    }

    private decimal ExtractDecimalFromAdditionalData(Dictionary<string, string> additionalData, string key,
        decimal defaultValue = 0)
    {
        if (additionalData == null || !additionalData.TryGetValue(key, out var valueStr))
            return defaultValue;

        return decimal.TryParse(valueStr, out var value) ? value : defaultValue;
    }

    private bool ExtractBoolFromAdditionalData(Dictionary<string, string> additionalData, string key,
        bool defaultValue = false)
    {
        if (additionalData == null || !additionalData.TryGetValue(key, out var valueStr))
            return defaultValue;

        return bool.TryParse(valueStr, out var value) ? value : defaultValue;
    }

    private string ExtractStringFromAdditionalData(Dictionary<string, string> additionalData, string key,
        string defaultValue = "")
    {
        if (additionalData == null || !additionalData.TryGetValue(key, out var value))
            return defaultValue;

        return value;
    }

// Extract browser and OS info from UserAgent
    private string ExtractBrowserFromUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";

        // Very simple extraction logic - in a real implementation use a proper UserAgent parser
        if (userAgent.Contains("Chrome"))
            return "Chrome";
        if (userAgent.Contains("Firefox"))
            return "Firefox";
        if (userAgent.Contains("Safari"))
            return "Safari";
        if (userAgent.Contains("Edge"))
            return "Edge";
        if (userAgent.Contains("MSIE") || userAgent.Contains("Trident"))
            return "Internet Explorer";

        return "Unknown";
    }

    private string ExtractOSFromUserAgent(string userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return "Unknown";

        // Very simple extraction logic - in a real implementation use a proper UserAgent parser
        if (userAgent.Contains("Windows"))
            return "Windows";
        if (userAgent.Contains("Mac OS"))
            return "MacOS";
        if (userAgent.Contains("Linux"))
            return "Linux";
        if (userAgent.Contains("Android"))
            return "Android";
        if (userAgent.Contains("iOS") || userAgent.Contains("iPhone") || userAgent.Contains("iPad"))
            return "iOS";

        return "Unknown";
    }

// Extract relevant additional data with prefix filtering
    private Dictionary<string, object> ExtractRelevantAdditionalData(Dictionary<string, string> additionalData,
        string prefix)
    {
        if (additionalData == null) return new Dictionary<string, object>();

        return additionalData
            .Where(kvp => kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value);
    }


    /// <summary>
    /// Tüm kuralları değerlendir
    /// </summary>
    public async Task<List<RuleEvaluationResult>> EvaluateAllRulesAsync(RuleEvaluationContext context)
    {
        try
        {
            List<RuleEvaluationResult> results = new();

            // Null kontrolü ve loglama ekleyin
            if (_cachedRules == null || _cachedRules.Count == 0)
            {
                _logger.LogWarning("No rules found in cache. Reloading rules...");
                await ReloadRulesAsync();

                // Hala boşsa, boş liste dön
                if (_cachedRules == null || _cachedRules.Count == 0)
                {
                    _logger.LogError("Failed to load rules from database");
                    return results;
                }
            }

            // Rule validasyonu ekleyin
            var validRules = _cachedRules.Where(r =>
                r != null &&
                !string.IsNullOrEmpty(r.Name) &&
                !string.IsNullOrEmpty(r.RuleCode) &&
                (r.Status == RuleStatus.Active || (r.Status == RuleStatus.TestMode && context.IsTestMode))
            ).ToList();

            _logger.LogInformation("Found {ValidCount} valid rules out of {TotalCount} loaded rules",
                validRules.Count, _cachedRules.Count);

            foreach (var rule in validRules)
            {
                try
                {
                    var result = await EvaluateRuleAsync(rule, context);
                    results.Add(result);

                    // Olay oluşturma kodları...
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating rule {RuleCode} - {RuleName}",
                        rule.RuleCode, rule.Name);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating all rules");
            throw;
        }
    }

    /// <summary>
    /// Belirli bir kategorideki kuralları değerlendir
    /// </summary>
    public async Task<List<RuleEvaluationResult>> EvaluateRulesByCategoryAsync(
        RuleCategory category, RuleEvaluationContext context)
    {
        try
        {
            List<RuleEvaluationResult> results = new();

            // Kategori cache'de yoksa boş liste dön
            if (!_rulesByCategory.TryGetValue(category, out var categoryRules)) return results;

            // Kategorideki aktif kuralları değerlendir
            foreach (var rule in categoryRules.Where(r => r.Status == RuleStatus.Active ||
                                                          (r.Status == RuleStatus.TestMode && context.IsTestMode)))
                try
                {
                    var result = await EvaluateRuleAsync(rule, context);
                    results.Add(result);

                    // Eğer kural tetiklendiyse ve aksiyon gerektiriyorsa, olay oluştur
                    if (result.IsTriggered && ShouldCreateEvent(rule.Actions))
                    {
                        var eventId = await CreateRuleEventAsync(rule, context, result);
                        if (eventId.HasValue)
                        {
                            result.EventCreated = true;
                            result.EventId = eventId;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating rule {RuleCode} - {RuleName}",
                        rule.RuleCode, rule.Name);
                }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating rules for category {Category}", category);
            throw;
        }
    }

    /// <summary>
    /// Kuralları yeniden yükle
    /// </summary>
    public async Task ReloadRulesAsync()
    {
        try
        {
            _logger.LogInformation("Reloading fraud rules");

            // Tüm aktif kuralları getir
            var rules = await _ruleRepository.GetActiveRulesAsync();

            // Cache'i güncelle
            _cachedRules = rules.ToList();

            // Kategorilere göre sınıflandır
            _rulesByCategory.Clear();
            foreach (var category in Enum.GetValues(typeof(RuleCategory)).Cast<RuleCategory>())
                _rulesByCategory[category] = _cachedRules
                    .Where(r => r.Category == category)
                    .OrderBy(r => r.Priority)
                    .ToList();

            _logger.LogInformation("Reloaded {RuleCount} fraud rules", _cachedRules.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading fraud rules");
            throw;
        }
    }

    /// <summary>
    /// Belirli bir kuralı değerlendir
    /// </summary>
    private async Task<RuleEvaluationResult> EvaluateRuleAsync(FraudRule rule, RuleEvaluationContext context)
    {
        // Değerlendirme sonucu için temel yapı
        var result = new RuleEvaluationResult
        {
            RuleId = rule.Id,
            RuleCode = rule.RuleCode,
            RuleName = rule.Name,
            IsTriggered = false,
            TriggerScore = 0,
            Actions = new List<RuleAction>(),
            ActionDuration = rule.ActionDuration,
            EventCreated = false
        };

        // Kural geçerlilik kontrolü
        var now = DateTime.Now;
        if (rule.ValidFrom.HasValue && now < rule.ValidFrom.Value) return result; // Henüz başlamamış

        if (rule.ValidTo.HasValue && now > rule.ValidTo.Value) return result; // Süresi dolmuş

        // Kural tipine göre değerlendirme
        switch (rule.Type)
        {
            case RuleType.Simple:
                result.IsTriggered = EvaluateSimpleRule(rule, context);
                result.TriggerScore = result.IsTriggered ? 1.0 : 0.0;
                break;

            case RuleType.Threshold:
                (result.IsTriggered, result.TriggerScore) = EvaluateThresholdRule(rule, context);
                break;

            case RuleType.Complex:
                result.IsTriggered = EvaluateComplexRule(rule, context);
                result.TriggerScore = result.IsTriggered ? 1.0 : 0.0;
                break;

            case RuleType.Blacklist:
                result.IsTriggered = EvaluateBlacklistRule(rule, context);
                result.TriggerScore = result.IsTriggered ? 1.0 : 0.0;
                break;

            // Diğer kural tipleri için ek değerlendirme metodları
            default:
                _logger.LogWarning("Unsupported rule type {RuleType} for rule {RuleCode}",
                    rule.Type, rule.RuleCode);
                break;
        }

        // Eğer kural tetiklendiyse aksiyonları ekle
        if (result.IsTriggered)
        {
            result.Actions = rule.Actions.ToList();
            result.TriggerDetails = $"Rule {rule.Name} triggered at {DateTime.UtcNow}";

            _logger.LogInformation("Rule {RuleCode} triggered with score {Score}",
                rule.RuleCode, result.TriggerScore);
        }

        return result;
    }

    /// <summary>
    /// Basit kuralı değerlendir
    /// </summary>
    private bool EvaluateSimpleRule(FraudRule rule, RuleEvaluationContext context)
    {
        try
        {
            if (string.IsNullOrEmpty(rule.ConfigurationJson))
            {
                Console.WriteLine(rule.ConfigurationJson);
                _logger.LogWarning("Rule {RuleCode} - {RuleName} has null or empty configuration JSON",
                    rule.RuleCode, rule.Name);
                return false;
            }


            // Konfigürasyonu deserialize et
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.ConfigurationJson);

            // Kural kategorisine göre değerlendirme yap
            switch (rule.Category)
            {
                case RuleCategory.Network:
                    return EvaluateNetworkRule(config, context);

                case RuleCategory.IP:
                    return EvaluateIpRule(config, context);

                case RuleCategory.Account:
                    return EvaluateAccountRule(config, context);

                case RuleCategory.Device:
                    return EvaluateDeviceRule(config, context);

                case RuleCategory.Session:
                    return EvaluateSessionRule(config, context);

                case RuleCategory.Transaction:
                    return EvaluateTransactionRule(config, context);

                case RuleCategory.Time:
                    return EvaluateTimeRule(config, context);

                default:
                    _logger.LogWarning("Unsupported rule category {Category} for rule {RuleCode}",
                        rule.Category, rule.RuleCode);
                    return false;
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError("JSON parsing error for rule {RuleCode} - {RuleName}: {JsonError} - JSON: {JsonContent}",
                rule.RuleCode, rule.Name, jsonEx.Message, rule.ConfigurationJson);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating simple rule {RuleCode}", rule.RuleCode);
            return false;
        }
    }

    /// <summary>
    /// Eşik tabanlı kuralı değerlendir
    /// </summary>
    private (bool IsTriggered, double Score) EvaluateThresholdRule(FraudRule rule, RuleEvaluationContext context)
    {
        try
        {
            // Konfigürasyonu deserialize et
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.ConfigurationJson);

            // Örnek eşik tabanlı değerlendirme
            // Gerçek uygulamada daha karmaşık mantık kullanılabilir
            double? currentValue = null;
            double? thresholdValue = null;

            // Kategori ve konfigürasyona göre değerlendirilecek değerleri belirle
            if (rule.Category == RuleCategory.Transaction && context.Transaction != null)
            {
                // Örnek: İşlem tutarı eşiği
                if (config.TryGetValue("valueField", out var valueField) &&
                    valueField?.ToString() == "Amount")
                    currentValue = (double)context.Transaction.Amount;

                // Eşik değeri
                if (config.TryGetValue("threshold", out var threshold)) thresholdValue = Convert.ToDouble(threshold);
            }

            // Diğer kategoriler için benzer kontroller eklenmeli

            // Değerler mevcutsa karşılaştır
            if (currentValue.HasValue && thresholdValue.HasValue)
            {
                var isGreaterThan = config.TryGetValue("operator", out var op) &&
                                    op?.ToString() == "LessThan";

                if (isGreaterThan)
                {
                    // Küçükse tetikle
                    var isTriggered = currentValue < thresholdValue;
                    var score = isTriggered ? Math.Min(1.0, 1.0 - currentValue.Value / thresholdValue.Value) : 0.0;

                    return (isTriggered, score);
                }
                else
                {
                    // Büyükse tetikle (varsayılan)
                    var isTriggered = currentValue > thresholdValue;
                    var score = isTriggered ? Math.Min(1.0, currentValue.Value / thresholdValue.Value) : 0.0;

                    return (isTriggered, score);
                }
            }

            return (false, 0.0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating threshold rule {RuleCode}", rule.RuleCode);
            return (false, 0.0);
        }
    }

    /// <summary>
    /// Karmaşık kuralı değerlendir
    /// </summary>
    private bool EvaluateComplexRule(FraudRule rule, RuleEvaluationContext context)
    {
        try
        {
            // Koşul ifadesini parse et ve değerlendir
            // Bu basit bir örnektir, gerçek uygulamada expression dili kullanılabilir

            var condition = rule.Condition;

            // Basit örnekler
            if (condition.Contains("IP.CountryCode IN") && context.IpAddress != null)
            {
                // Ülke kodu kontrolü
                string[] countries = ExtractValuesFromInClause(condition);
                return countries.Contains(context.IpAddress.CountryCode);
            }
/*
            if (condition.Contains("Transaction.Amount >") && context.Transaction != null)
            {
                // İşlem tutarı kontrolü
                var threshold = ExtractDecimalThreshold(condition);
                return context.Transaction.Amount > threshold;
            }
*/
            if (condition.Contains("Account.UniqueIpCount24h >") && context.Account != null)
            {
                // IP sayısı kontrolü
                var threshold = ExtractIntThreshold(condition);
                return context.Account.UniqueIpCount24h > threshold;
            }

            // Karmaşık karma koşullar
            if (condition.Contains(" AND "))
            {
                var subConditions = condition.Split(new[] { " AND " }, StringSplitOptions.None);
                return subConditions.All(c => EvaluateCondition(c, context));
            }

            if (condition.Contains(" OR "))
            {
                var subConditions = condition.Split(new[] { " OR " }, StringSplitOptions.None);
                return subConditions.Any(c => EvaluateCondition(c, context));
            }

            // Bilinmeyen format
            _logger.LogWarning("Unsupported condition format: {Condition}", condition);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating complex rule {RuleCode}", rule.RuleCode);
            return false;
        }
    }

    /// <summary>
    /// Kara liste kuralını değerlendir
    /// </summary>
    private bool EvaluateBlacklistRule(FraudRule rule, RuleEvaluationContext context)
    {
        try
        {
            // Konfigürasyonu deserialize et
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(rule.ConfigurationJson);

            // Kara liste tipi
            if (!config.TryGetValue("blacklistType", out var blacklistType)) return false;

            switch (blacklistType?.ToString())
            {
                case "IP":
                    // IP kara listesi kontrolü
                    if (context.IpAddress == null) return false;

                    if (config.TryGetValue("blockedIps", out var blockedIpsObj) &&
                        blockedIpsObj is JsonElement blockedIpsElement &&
                        blockedIpsElement.ValueKind == JsonValueKind.Array)
                    {
                        var blockedIps = blockedIpsElement
                            .EnumerateArray()
                            .Select(item => item.GetString())
                            .Where(ip => !string.IsNullOrEmpty(ip))
                            .ToList();

                        return blockedIps.Contains(context.IpAddress.IpAddress);
                    }

                    break;

                case "Country":
                    // Ülke kara listesi kontrolü
                    if (context.IpAddress == null) return false;

                    if (config.TryGetValue("blockedCountries", out var blockedCountriesObj) &&
                        blockedCountriesObj is JsonElement blockedCountriesElement &&
                        blockedCountriesElement.ValueKind == JsonValueKind.Array)
                    {
                        var blockedCountries = blockedCountriesElement
                            .EnumerateArray()
                            .Select(item => item.GetString())
                            .Where(country => !string.IsNullOrEmpty(country))
                            .ToList();

                        return blockedCountries.Contains(context.IpAddress.CountryCode);
                    }

                    break;

                case "Device":
                    // Cihaz kara listesi kontrolü
                    if (context.Device == null) return false;

                    if (config.TryGetValue("blockedDevices", out var blockedDevicesObj) &&
                        blockedDevicesObj is JsonElement blockedDevicesElement &&
                        blockedDevicesElement.ValueKind == JsonValueKind.Array)
                    {
                        var blockedDevices = blockedDevicesElement
                            .EnumerateArray()
                            .Select(item => item.GetString())
                            .Where(device => !string.IsNullOrEmpty(device))
                            .ToList();

                        return blockedDevices.Contains(context.Device.DeviceId);
                    }

                    break;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating blacklist rule {RuleCode}", rule.RuleCode);
            return false;
        }
    }

    #region Rule Evaluation Helper Methods

    private bool EvaluateNetworkRule(Dictionary<string, object> config, RuleEvaluationContext context)
    {
        if (context.IpAddress == null) return false;

        // Örnek: Tor ağı kontrolü
        if (config.TryGetValue("checkTorNetwork", out var checkTorObj) &&
            checkTorObj?.ToString() == "true")
            return context.IpAddress.NetworkType == "TOR";

        // Örnek: VPN kontrolü
        if (config.TryGetValue("checkVPN", out var checkVpnObj) &&
            checkVpnObj?.ToString() == "true")
            return context.IpAddress.NetworkType == "VPN";

        return false;
    }

    private bool EvaluateIpRule(Dictionary<string, object> config, RuleEvaluationContext context)
    {
        if (context.IpAddress == null) return false;

        // Örnek: Farklı hesap sayısı kontrolü
        if (config.TryGetValue("maxDifferentAccounts", out var maxAccountsObj) &&
            int.TryParse(maxAccountsObj?.ToString(), out var maxAccounts))
            if (config.TryGetValue("timeWindowMinutes", out var timeWindowObj) &&
                int.TryParse(timeWindowObj?.ToString(), out var timeWindow))
            {
                // Zaman penceresine göre hesap sayılarını kontrol et
                if (timeWindow <= 10 && context.IpAddress.UniqueAccountCount10m > maxAccounts)
                    return true;
                else if (timeWindow <= 60 && context.IpAddress.UniqueAccountCount1h > maxAccounts)
                    return true;
                else if (timeWindow <= 1440 && context.IpAddress.UniqueAccountCount24h > maxAccounts) return true;
            }

        // Örnek: Başarısız giriş sayısı kontrolü
        if (config.TryGetValue("maxFailedLogins", out var maxFailedLoginsObj) &&
            int.TryParse(maxFailedLoginsObj?.ToString(), out var maxFailedLogins))
            return context.IpAddress.FailedLoginCount10m > maxFailedLogins;

        return false;
    }

    private bool EvaluateAccountRule(Dictionary<string, object> config, RuleEvaluationContext context)
    {
        if (context.Account == null) return false;

        // Örnek: Farklı IP sayısı kontrolü
        if (config.TryGetValue("maxDifferentIps", out var maxIpsObj) &&
            int.TryParse(maxIpsObj?.ToString(), out var maxIps))
            return context.Account.UniqueIpCount24h > maxIps;

        // Örnek: Farklı ülke sayısı kontrolü
        if (config.TryGetValue("maxDifferentCountries", out var maxCountriesObj) &&
            int.TryParse(maxCountriesObj?.ToString(), out var maxCountries))
            return context.Account.UniqueCountryCount24h > maxCountries;

        // Örnek: Tipik olmayan erişim kontrolü
        if (config.TryGetValue("checkTypicalAccess", out var checkTypicalObj) &&
            checkTypicalObj?.ToString() == "true")
        {
            // Tipik erişim saati kontrolü
            if (!context.Account.TypicalAccessHours.Contains(context.EvaluationTime.Hour)) return true;

            // Tipik erişim günü kontrolü
            if (!context.Account.TypicalAccessDays.Contains(context.EvaluationTime.DayOfWeek)) return true;

            // Tipik ülke kontrolü
            if (!context.Account.TypicalCountries.Contains(context.Account.CountryCode)) return true;
        }

        return false;
    }

    private bool EvaluateDeviceRule(Dictionary<string, object> config, RuleEvaluationContext context)
    {
        if (context.Device == null) return false;

        // Örnek: Jailbreak kontrolü
        if (config.TryGetValue("checkJailbreak", out var checkJailbreakObj) &&
            checkJailbreakObj?.ToString() == "true")
            return context.Device.IsJailbroken;

        // Örnek: Emülatör kontrolü
        if (config.TryGetValue("checkEmulator", out var checkEmulatorObj) &&
            checkEmulatorObj?.ToString() == "true")
            return context.Device.IsEmulator;

        // Örnek: Farklı hesap sayısı kontrolü
        if (config.TryGetValue("maxDifferentAccounts", out var maxAccountsObj) &&
            int.TryParse(maxAccountsObj?.ToString(), out var maxAccounts))
            return context.Device.UniqueAccountCount24h > maxAccounts;

        return false;
    }

    private bool EvaluateSessionRule(Dictionary<string, object> config, RuleEvaluationContext context)
    {
        if (context.Session == null) return false;

        // Örnek: Maksimum oturum süresi kontrolü
        if (config.TryGetValue("maxSessionDuration", out var maxDurationObj) &&
            int.TryParse(maxDurationObj?.ToString(), out var maxDuration))
            return context.Session.DurationMinutes > maxDuration;

        // Örnek: Hızlı sayfa geçişleri kontrolü
        if (config.TryGetValue("maxRapidNavigations", out var maxNavigationsObj) &&
            int.TryParse(maxNavigationsObj?.ToString(), out var maxNavigations))
            return context.Session.RapidNavigationCount > maxNavigations;

        return false;
    }

    private bool EvaluateTransactionRule(Dictionary<string, object> config, RuleEvaluationContext context)
    {
        if (context.Transaction == null) return false;

        // Örnek: Maksimum işlem tutarı kontrolü
        if (config.TryGetValue("maxAmount", out var maxAmountObj) &&
            decimal.TryParse(maxAmountObj?.ToString(), out var maxAmount))
            return context.Transaction.Amount > maxAmount;

        // Örnek: Kullanıcı ortalamasına göre kontrol
        if (config.TryGetValue("maxMultipleOfAverage", out var maxMultipleObj) &&
            decimal.TryParse(maxMultipleObj?.ToString(), out var maxMultiple))
            if (context.Transaction.UserAverageTransactionAmount > 0)
                return context.Transaction.Amount > context.Transaction.UserAverageTransactionAmount * maxMultiple;

        // Örnek: Farklı alıcı sayısı kontrolü
        if (config.TryGetValue("maxDifferentRecipients", out var maxRecipientsObj) &&
            int.TryParse(maxRecipientsObj?.ToString(), out var maxRecipients))
            return context.Transaction.UniqueRecipientCount1h > maxRecipients;

        // Örnek: Uluslararası işlem kontrolü
        if (config.TryGetValue("checkInternational", out var checkIntlObj) &&
            checkIntlObj?.ToString() == "true" &&
            !string.IsNullOrEmpty(context.Transaction.RecipientCountry))
            // Bu örnekte context'te bir "ülke kodu" kavramı yok
            // Gerçek uygulamada kullanıcının ülkesi ile karşılaştırılabilir
            if (config.TryGetValue("homeCountry", out var homeCountryObj))
            {
                var homeCountry = homeCountryObj?.ToString();
                return !string.IsNullOrEmpty(homeCountry) &&
                       context.Transaction.RecipientCountry != homeCountry;
            }

        return false;
    }

    private bool EvaluateTimeRule(Dictionary<string, object> config, RuleEvaluationContext context)
    {
        // Örnek: Gece saatleri kontrolü
        if (config.TryGetValue("nightHoursStart", out var startHourObj) &&
            config.TryGetValue("nightHoursEnd", out var endHourObj) &&
            int.TryParse(startHourObj?.ToString(), out var startHour) &&
            int.TryParse(endHourObj?.ToString(), out var endHour))
        {
            var currentHour = context.EvaluationTime.Hour;

            // Gün dönümünü kapsayan saat aralığı (örn: 22-6)
            if (startHour > endHour)
                return currentHour >= startHour || currentHour <= endHour;
            // Normal saat aralığı (örn: 0-5)
            else
                return currentHour >= startHour && currentHour <= endHour;
        }

        return false;
    }

    private bool EvaluateCondition(string condition, RuleEvaluationContext context)
    {
        // IP ülke kontrolü
        if (condition.Contains("IP.CountryCode") && context.IpAddress != null)
            if (condition.Contains("="))
            {
                var value = ExtractStringValue(condition);
                return context.IpAddress.CountryCode == value;
            }

        // IP başarısız giriş sayısı kontrolü
        if (condition.Contains("IP.FailedLoginCount") && context.IpAddress != null)
            if (condition.Contains(">"))
            {
                var threshold = ExtractIntThreshold(condition);
                return context.IpAddress.FailedLoginCount10m > threshold;
            }

        // İşlem tutarı kontrolü
        if (condition.Contains("Transaction.Amount") && context.Transaction != null)
            if (condition.Contains(">"))
            {
                var threshold = ExtractDecimalThreshold(condition);
                return context.Transaction.Amount > threshold;
            }

        // Hesap IP sayısı kontrolü
        if (condition.Contains("Account.UniqueIpCount") && context.Account != null)
            if (condition.Contains(">"))
            {
                var threshold = ExtractIntThreshold(condition);
                return context.Account.UniqueIpCount24h > threshold;
            }

        // TODO: Diğer koşul tipleri için değerlendirmeler

        // Bilinmeyen koşul
        _logger.LogWarning("Unsupported condition: {Condition}", condition);
        return false;
    }

    private string[] ExtractValuesFromInClause(string condition)
    {
        // "field IN ('val1', 'val2', 'val3')" formatını parse et
        var startIndex = condition.IndexOf("(");
        var endIndex = condition.IndexOf(")");

        if (startIndex >= 0 && endIndex > startIndex)
        {
            var valuesString = condition.Substring(startIndex + 1, endIndex - startIndex - 1);
            return valuesString
                .Split(',')
                .Select(v => v.Trim().Trim('\'', '"'))
                .ToArray();
        }

        return Array.Empty<string>();
    }

    private decimal ExtractDecimalThreshold(string condition)
    {
        // "field > 100.50" formatını parse et
        var operatorIndex = Math.Max(
            condition.IndexOf(">"),
            Math.Max(condition.IndexOf("<"), condition.IndexOf("=")));

        if (operatorIndex >= 0)
        {
            var valueString = condition.Substring(operatorIndex + 1).Trim();
            if (decimal.TryParse(valueString, out var value)) return value;
        }

        return 0;
    }

    private int ExtractIntThreshold(string condition)
    {
        // "field > 100" formatını parse et
        var operatorIndex = Math.Max(
            condition.IndexOf(">"),
            Math.Max(condition.IndexOf("<"), condition.IndexOf("=")));

        if (operatorIndex >= 0)
        {
            var valueString = condition.Substring(operatorIndex + 1).Trim();
            if (int.TryParse(valueString, out var value)) return value;
        }

        return 0;
    }

    private string ExtractStringValue(string condition)
    {
        // "field = 'value'" formatını parse et
        var operatorIndex = condition.IndexOf("=");

        if (operatorIndex >= 0)
        {
            var valueString = condition.Substring(operatorIndex + 1).Trim();
            return valueString.Trim('\'', '"');
        }

        return string.Empty;
    }

    #endregion

    /// <summary>
    /// Olay oluşturulmalı mı?
    /// </summary>
    private bool ShouldCreateEvent(List<RuleAction> actions)
    {
        // Log dışındaki tüm aksiyonlar için olay oluştur
        return actions.Any(a => a != RuleAction.Log);
    }

    /// <summary>
    /// Kural olayı oluştur
    /// </summary>
    private async Task<Guid?> CreateRuleEventAsync(
        FraudRule rule, RuleEvaluationContext context, RuleEvaluationResult result)
    {
        try
        {
            // Test modunda ise gerçek olay oluşturma
            if (rule.Status == RuleStatus.TestMode && !context.IsTestMode) return null;

            // Olay detaylarını oluştur
            var eventDetails = new
            {
                RuleEvaluation = result,
                Context = new
                {
                    TransactionId = context.Transaction?.TransactionId,
                    AccountId = context.Account?.AccountId ?? context.Transaction?.AccountId,
                    IpAddress = context.IpAddress?.IpAddress ?? context.Account?.IpAddress,
                    DeviceId = context.Device?.DeviceId ?? context.Account?.DeviceId,
                    EvaluationTime = context.EvaluationTime
                }
            };

            // JSON'a dönüştür
            var eventDetailsJson = JsonSerializer.Serialize(eventDetails);

            // Olay oluştur
            var fraudEvent = FraudRuleEvent.Create(
                rule.Id,
                rule.Name,
                rule.RuleCode,
                context.Transaction?.TransactionId,
                context.Account?.AccountId ?? context.Transaction?.AccountId,
                context.IpAddress?.IpAddress ?? context.Account?.IpAddress,
                context.Device?.DeviceId ?? context.Account?.DeviceId,
                result.Actions,
                rule.ActionDuration,
                eventDetailsJson);

            // Veritabanına kaydet
            var savedEvent = await _ruleEventRepository.AddEventAsync(fraudEvent);

            _logger.LogInformation("Created fraud rule event ID {EventId} for rule {RuleCode}",
                savedEvent.Id, rule.RuleCode);

            return savedEvent.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fraud rule event for rule {RuleCode}", rule.RuleCode);
            return null;
        }
    }
}