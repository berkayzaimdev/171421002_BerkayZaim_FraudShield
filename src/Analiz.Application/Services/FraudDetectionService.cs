using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Analiz.Application.DTOs.Request;
using Analiz.Application.DTOs.Response;
using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.ML;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Application.Interfaces.Services;
using Analiz.Application.Models.Configuration;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.Rule.Context;
using Analiz.Domain.Models;
using Analiz.Domain.ValueObjects;
using Analiz.ML.Models.PCA;
using FraudShield.TransactionAnalysis.Domain.Enums;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;

namespace Analiz.Application.Services;

/// <summary>
/// Dolandırıcılık tespiti servisi - Hibrit ML ve Kural tabanlı
/// Python ML entegrasyonu ile güncellendi
/// </summary>
public class FraudDetectionService : IFraudDetectionService
{
    private readonly IModelService _modelService;
    private readonly IFeatureExtractionService _featureExtractor;
    private readonly IFraudRuleEngine _ruleEngine;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAnalysisResultRepository _analysisRepository;
    private readonly IFraudRuleRepository _ruleRepository;
    private readonly IFraudRuleEventRepository _ruleEventRepository;
    private readonly IFraudAlertRepository _alertRepository;
    private readonly IRiskEvaluationRepository _riskEvaluationRepository;
    private readonly IBlacklistService _blacklistService;
    private readonly PythonMLIntegrationService _pythonService;
    private readonly ILogger<FraudDetectionService> _logger;

    // Threshold configurations
    private const double HIGH_RISK_THRESHOLD = 0.75;
    private const double MEDIUM_RISK_THRESHOLD = 0.45;
    private const double ANOMALY_THRESHOLD = 3.5;
    private const int HIGH_RISK_SCORE_THRESHOLD = 80;
    private const int MEDIUM_RISK_SCORE_THRESHOLD = 50;

    private readonly VValueGenerator _vGenerator = new VValueGenerator();

    // Kritik V değerleri ve eşik değerleri - Kaggle veri seti analizine göre
    private readonly Dictionary<string, (float Threshold, double Weight)> _criticalVFeatures = new()
    {
        ["V1"] = (-2.4f, 0.8), // Normal ortalama: 0.008, Fraud ortalama: -4.77
        ["V2"] = (2.0f, 0.75), // Normal ortalama: 0.015, Fraud ortalama: 3.63 - POZİTİF YÖN!
        ["V3"] = (-3.5f, 0.7), // Normal ortalama: -0.016, Fraud ortalama: -7.04
        ["V4"] = (-1.2f, 0.6), // Normal ortalama: 0.016, Fraud ortalama: -2.46
        ["V5"] = (-1.6f, 0.6), // Normal ortalama: -0.015, Fraud ortalama: -3.16
        ["V9"] = (-1.4f, 0.6), // Normal ortalama: -0.003, Fraud ortalama: -2.77
        ["V10"] = (-2.8f, 0.85), // Normal ortalama: -0.001, Fraud ortalama: -5.57
        ["V11"] = (1.7f, 0.75), // Normal ortalama: 0.005, Fraud ortalama: 3.32 - POZİTİF YÖN!
        ["V12"] = (-1.4f, 0.6), // Normal ortalama: -0.001, Fraud ortalama: -2.70
        ["V14"] = (-4.4f, 0.9), // Normal ortalama: 0.005, Fraud ortalama: -8.75 (en önemli)
        ["V16"] = (-0.9f, 0.6), // Normal ortalama: 0.0007, Fraud ortalama: -1.71
        ["V17"] = (-2.3f, 0.7) // Normal ortalama: -0.003, Fraud ortalama: -4.65
    };

    /// <summary>
    /// ModelInput oluştur - Python entegrasyonu için güncellenmiş
    /// </summary>
    private ModelInput ConvertFeaturesToModelInput(Dictionary<string, float> features, float amount,
        DateTime? timestamp = null)
    {
        try
        {
            _logger.LogInformation("ConvertFeaturesToModelInput: Amount={Amount:F2}, Timestamp={Timestamp}",
                amount, timestamp?.ToString("yyyy-MM-dd HH:mm:ss") ?? "null");

            // Tutar normalleştirme
            float amountNormalized = NormalizeAmount(amount);

            // Time değerini hesapla
            float timeValue = 0;
            if (timestamp.HasValue)
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                timeValue = (float)(timestamp.Value.ToUniversalTime() - epoch).TotalSeconds;
                _logger.LogInformation("Timestamp converted to Unix time: {TimeValue}", timeValue);
            }

            // Time-based features hesapla (eğer features'da yoksa)
            float timeSin = features.GetValueOrDefault("TimeSin", 0);
            float timeCos = features.GetValueOrDefault("TimeCos", 0);
            float dayFeature = features.GetValueOrDefault("DayFeature", 0);
            float hourFeature = features.GetValueOrDefault("HourFeature", 0);

            // Eğer time-based features hesaplanmamışsa ve timestamp varsa hesapla
            if ((timeSin == 0 && timeCos == 0) && timestamp.HasValue)
            {
                var timeFeatures = CalculateTimeFeatures(timestamp.Value);
                timeSin = timeFeatures.TimeSin;
                timeCos = timeFeatures.TimeCos;
                dayFeature = timeFeatures.DayFeature;
                hourFeature = timeFeatures.HourFeature;

                _logger.LogInformation("Time features calculated: TimeSin={TimeSin:F4}, TimeCos={TimeCos:F4}, " +
                                       "Day={Day:F1}, Hour={Hour:F1}", timeSin, timeCos, dayFeature, hourFeature);
            }

            // V değerlerinin alınması ve loglanması
            LogCriticalVValues(features);

            // ModelInput oluşturma - Python için uygun formatta
            var input = new ModelInput
            {
                Amount = amountNormalized,
                Time = timeValue, // Unix timestamp
                TimeSin = timeSin,
                TimeCos = timeCos,
                DayFeature = dayFeature,
                HourFeature = hourFeature
            };

            // V değerlerini ayarla - Reflection kullanarak daha temiz kod
            SetVFactors(input, features);

            // Derived features'ları hesapla
            input.CalculateDerivedFeatures();

            // Validation
            var validation = input.ValidateFeatures();
            if (!validation.IsValid)
            {
                _logger.LogWarning("ModelInput validation failed: {Errors}", string.Join(", ", validation.Errors));
            }

            if (validation.HasWarnings)
            {
                _logger.LogInformation("ModelInput validation warnings: {Warnings}",
                    string.Join(", ", validation.Warnings));
            }

            _logger.LogInformation("ModelInput created successfully: {Summary}", input.ToString());
            return input;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConvertFeaturesToModelInput hatası");

            // Hata durumunda fallback input
            return CreateFallbackModelInput(features, amount, timestamp);
        }
    }

    /// <summary>
    /// Tutar normalleştirme - Python ile uyumlu
    /// </summary>
    private float NormalizeAmount(float amount)
    {
        float amountNormalized;

        if (amount > 1_000_000)
        {
            // Logaritmik normalleştirme - büyük tutarlar için
            amountNormalized = (float)Math.Log10(amount + 1) / 9.0f;
            _logger.LogInformation("Büyük tutar logaritmik normalleştirme: {Amount} -> {Normalized:F6}",
                amount, amountNormalized);
        }
        else if (amount > 10_000)
        {
            // Kübik kök normalleştirme - orta tutarlar için
            amountNormalized = (float)Math.Pow(amount, 1.0 / 3.0) / 100.0f;
            _logger.LogInformation("Yüksek tutar kübik kök normalleştirme: {Amount} -> {Normalized:F6}",
                amount, amountNormalized);
        }
        else
        {
            // Standart normalleştirme - küçük tutarlar için
            amountNormalized = amount / 25000.0f;
            _logger.LogInformation("Standart tutar normalleştirme: {Amount} -> {Normalized:F6}",
                amount, amountNormalized);
        }

        return amountNormalized;
    }

    /// <summary>
    /// Time-based features hesapla
    /// </summary>
    private (float TimeSin, float TimeCos, float DayFeature, float HourFeature) CalculateTimeFeatures(
        DateTime timestamp)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var timeValue = (float)(timestamp.ToUniversalTime() - epoch).TotalSeconds;

        const float secondsInDay = 24 * 60 * 60;

        var timeSin = (float)Math.Sin(2 * Math.PI * timeValue / secondsInDay);
        var timeCos = (float)Math.Cos(2 * Math.PI * timeValue / secondsInDay);
        var dayFeature = (float)((timeValue / secondsInDay) % 7);
        var hourFeature = (float)((timeValue / 3600) % 24);

        return (timeSin, timeCos, dayFeature, hourFeature);
    }

    /// <summary>
    /// Kritik V değerlerini logla
    /// </summary>
    private void LogCriticalVValues(Dictionary<string, float> features)
    {
        var criticalVs = new[] { "V1", "V3", "V4", "V10", "V14", "V17" };

        foreach (var key in criticalVs)
        {
            if (features.TryGetValue(key, out float value))
            {
                _logger.LogDebug("Kritik V faktörü: {Key}={Value:F4}", key, value);
            }
        }

        // En önemli V değerlerini özel olarak logla
        var v1 = features.GetValueOrDefault("V1", 0);
        var v14 = features.GetValueOrDefault("V14", 0);
        _logger.LogInformation("En önemli V değerleri: V1={V1:F4}, V14={V14:F4}", v1, v14);
    }

    /// <summary>
    /// V faktörlerini ModelInput'a ata - Reflection ile temiz kod
    /// </summary>
    private void SetVFactors(ModelInput input, Dictionary<string, float> features)
    {
        var inputType = typeof(ModelInput);

        for (int i = 1; i <= 28; i++)
        {
            var key = $"V{i}";
            var value = features.GetValueOrDefault(key, 0);
            var property = inputType.GetProperty($"V{i}");

            if (property != null)
            {
                property.SetValue(input, value);
            }
        }
    }

    /// <summary>
    /// Fallback ModelInput oluştur
    /// </summary>
    private ModelInput CreateFallbackModelInput(Dictionary<string, float> features, float amount, DateTime? timestamp)
    {
        _logger.LogWarning("Creating fallback ModelInput due to error");

        var input = new ModelInput
        {
            Amount = amount / 25000.0f // Basit normalleştirme
        };

        // Timestamp varsa basit time hesaplama
        if (timestamp.HasValue)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            input.Time = (float)(timestamp.Value.ToUniversalTime() - epoch).TotalSeconds;
        }

        // V değerlerini güvenli şekilde ayarla
        try
        {
            SetVFactors(input, features);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback V factors setting failed");
            // V değerlerini sıfırla
            var vProperties = typeof(ModelInput).GetProperties()
                .Where(p => p.Name.StartsWith("V") && p.PropertyType == typeof(float));

            foreach (var prop in vProperties)
            {
                prop.SetValue(input, 0f);
            }
        }

        // Derived features hesapla
        try
        {
            input.CalculateDerivedFeatures();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback derived features calculation failed");
        }

        return input;
    }


    /// <summary>
    /// Features içinden V1–V28'leri VFactors'a, 
    /// request.AdditionalData'dan gelen sabitleri de hem VO alanlarına hem CustomValues'a yerleştirir.
    /// </summary>
    private TransactionAdditionalData BuildAdditionalData(
        Dictionary<string, string> features,
        Dictionary<string, object> additionalData)
    {
        var add = new TransactionAdditionalData();

        // 1) Features → VFactors ve CustomValues
        if (features != null)
            foreach (var kvp in features)
                // V-faktörler
                if (kvp.Key.StartsWith("V", StringComparison.OrdinalIgnoreCase)
                    && float.TryParse(
                        kvp.Value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var v))
                    add.VFactors[kvp.Key] = v;
                else
                    add.CustomValues[kvp.Key] = kvp.Value;

        // 2) request.AdditionalData → VO alanları & CustomValues
        if (additionalData != null)
            foreach (var kvp in additionalData)
            {
                var key = kvp.Key;
                var str = kvp.Value?.ToString();

                switch (key)
                {
                    case "DaysSinceFirstTransaction":
                        if (int.TryParse(str, out var days))
                            add.DaysSinceFirstTransaction = days;
                        break;
                    case "TransactionVelocity24h":
                        if (int.TryParse(str, out var vel))
                            add.TransactionVelocity24h = vel;
                        break;
                    case "AverageTransactionAmount":
                        if (decimal.TryParse(
                                str,
                                NumberStyles.Any,
                                CultureInfo.InvariantCulture,
                                out var avg))
                            add.AverageTransactionAmount = avg;
                        break;
                    case "IsNewPaymentMethod":
                        if (bool.TryParse(str, out var npm))
                            add.IsNewPaymentMethod = npm;
                        break;
                    case "IsInternational":
                        if (bool.TryParse(str, out var intl))
                            add.IsInternational = intl;
                        break;
                    default:
                        // "Time" ya da bilinmeyen diğer sabitler buraya
                        add.CustomValues[key] = str;
                        break;
                }
            }

        return add;
    }

    /// <summary>
    /// PCA model input oluştur - Python entegrasyonu için güncellenmiş
    /// </summary>
    private PCAModelInput ConvertFeaturesToPCAInput(
        Dictionary<string, float> features,
        DateTimeOffset timestamp,
        decimal rawAmount,
        PCAConfiguration cfg = null)
    {
        try
        {
            // Zaman özelliklerini hesapla
            float t = (float)timestamp.ToUnixTimeSeconds();
            float timeSin = (float)Math.Sin(2 * Math.PI * t / (cfg?.TimeScaleFactor ?? 86400));
            float timeCos = (float)Math.Cos(2 * Math.PI * t / (cfg?.TimeScaleFactor ?? 86400));
            float dayFeature = (float)(t / 86400 % 7);
            float hourFeature = (float)(t / 3600 % 24);

            // Tutar seviyesine göre adaptif logaritmik dönüşüm
            float amountLog;
            double amount = (double)rawAmount;

            if (amount > 1_000_000_000)
            {
                // Milyarlar için daha agresif logaritmik ölçekleme
                amountLog = (float)(Math.Log10(amount + 1) * 0.9);
                _logger.LogInformation("Çok büyük tutar için ayarlanmış log: {Amount} -> {Log}", amount, amountLog);
            }
            else if (amount > 1_000_000)
            {
                // Milyonlar için standart logaritmik ölçekleme
                amountLog = (float)Math.Log10(amount + 1);
                _logger.LogInformation("Büyük tutar için logaritmik: {Amount} -> {Log}", amount, amountLog);
            }
            else
            {
                // Normal tutarlar için doğal logaritma (daha hassas)
                amountLog = (float)Math.Log(amount + 1);
            }

            // Temel zaman, tutar ve V özelliklerini PCA girişi olarak hazırla
            return new PCAModelInput
            {
                AmountLog = amountLog,
                TimeSin = timeSin,
                TimeCos = timeCos,
                DayFeature = dayFeature,
                HourFeature = hourFeature,

                // V özelliklerini features dictionary'den al - Python entegrasyonu için
                V1 = features.GetValueOrDefault("V1", 0),
                V2 = features.GetValueOrDefault("V2", 0),
                V3 = features.GetValueOrDefault("V3", 0),
                V4 = features.GetValueOrDefault("V4", 0),
                V5 = features.GetValueOrDefault("V5", 0),
                V6 = features.GetValueOrDefault("V6", 0),
                V7 = features.GetValueOrDefault("V7", 0),
                V8 = features.GetValueOrDefault("V8", 0),
                V9 = features.GetValueOrDefault("V9", 0),
                V10 = features.GetValueOrDefault("V10", 0),
                V11 = features.GetValueOrDefault("V11", 0),
                V12 = features.GetValueOrDefault("V12", 0),
                V13 = features.GetValueOrDefault("V13", 0),
                V14 = features.GetValueOrDefault("V14", 0),
                V15 = features.GetValueOrDefault("V15", 0),
                V16 = features.GetValueOrDefault("V16", 0),
                V17 = features.GetValueOrDefault("V17", 0),
                V18 = features.GetValueOrDefault("V18", 0),
                V19 = features.GetValueOrDefault("V19", 0),
                V20 = features.GetValueOrDefault("V20", 0),
                V21 = features.GetValueOrDefault("V21", 0),
                V22 = features.GetValueOrDefault("V22", 0),
                V23 = features.GetValueOrDefault("V23", 0),
                V24 = features.GetValueOrDefault("V24", 0),
                V25 = features.GetValueOrDefault("V25", 0),
                V26 = features.GetValueOrDefault("V26", 0),
                V27 = features.GetValueOrDefault("V27", 0),
                V28 = features.GetValueOrDefault("V28", 0)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PCA girdi dönüşümünde hata: {Message}", ex.Message);

            // Hata durumunda basitleştirilmiş giriş oluştur
            float safeAmountLog = (float)Math.Log10((double)rawAmount + 1);
            return new PCAModelInput
            {
                AmountLog = safeAmountLog,
                TimeSin = 0,
                TimeCos = 0,
                DayFeature = 0,
                HourFeature = 0,
                // V özellikleri için varsayılan değerleri kullan
                V1 = features.GetValueOrDefault("V1", 0),
                V2 = features.GetValueOrDefault("V2", 0),
                V3 = features.GetValueOrDefault("V3", 0),
                V4 = features.GetValueOrDefault("V4", 0),
                V5 = features.GetValueOrDefault("V5", 0),
                V6 = features.GetValueOrDefault("V6", 0),
                V7 = features.GetValueOrDefault("V7", 0),
                V8 = features.GetValueOrDefault("V8", 0),
                V9 = features.GetValueOrDefault("V9", 0),
                V10 = features.GetValueOrDefault("V10", 0),
                V11 = features.GetValueOrDefault("V11", 0),
                V12 = features.GetValueOrDefault("V12", 0),
                V13 = features.GetValueOrDefault("V13", 0),
                V14 = features.GetValueOrDefault("V14", 0),
                V15 = features.GetValueOrDefault("V15", 0),
                V16 = features.GetValueOrDefault("V16", 0),
                V17 = features.GetValueOrDefault("V17", 0),
                V18 = features.GetValueOrDefault("V18", 0),
                V19 = features.GetValueOrDefault("V19", 0),
                V20 = features.GetValueOrDefault("V20", 0),
                V21 = features.GetValueOrDefault("V21", 0),
                V22 = features.GetValueOrDefault("V22", 0),
                V23 = features.GetValueOrDefault("V23", 0),
                V24 = features.GetValueOrDefault("V24", 0),
                V25 = features.GetValueOrDefault("V25", 0),
                V26 = features.GetValueOrDefault("V26", 0),
                V27 = features.GetValueOrDefault("V27", 0),
                V28 = features.GetValueOrDefault("V28", 0)
            };
        }
    }

    /// <summary>
    /// Dictionary<string, object> olarak gelen ek verileri
    /// TransactionAdditionalData VO'suna parse eder.
    /// </summary>
    private TransactionAdditionalData MapToAdditionalDataVO(Dictionary<string, object> raw)
    {
        var vo = new TransactionAdditionalData();
        if (raw == null) return vo;

        foreach (var kvp in raw)
        {
            var key = kvp.Key;
            var str = kvp.Value?.ToString();

            // V1–V28 → VFactors
            if (key.StartsWith("V", StringComparison.OrdinalIgnoreCase)
                && float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var vf))
            {
                vo.VFactors[key] = vf;
                continue;
            }

            switch (key)
            {
                case "DaysSinceFirstTransaction":
                    if (int.TryParse(str, out var days))
                        vo.DaysSinceFirstTransaction = days;
                    break;
                case "TransactionVelocity24h":
                    if (int.TryParse(str, out var vel))
                        vo.TransactionVelocity24h = vel;
                    break;
                case "AverageTransactionAmount":
                    if (decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var avg))
                        vo.AverageTransactionAmount = avg;
                    break;
                case "IsNewPaymentMethod":
                    if (bool.TryParse(str, out var npm))
                        vo.IsNewPaymentMethod = npm;
                    break;
                case "IsInternational":
                    if (bool.TryParse(str, out var intl))
                        vo.IsInternational = intl;
                    break;
                default:
                    // Bunlar hem CardType, CardBin, vs. de olabilir
                    // Eğer hem VO property'si hem CustomValues'e yazmak isterseniz,
                    // buraya ek case'ler koyup vo.CardType = str; vo.CustomValues[key]=str;
                    vo.CustomValues[key] = str;
                    break;
            }
        }

        return vo;
    }

    public FraudDetectionService(
        IModelService modelService,
        IFeatureExtractionService featureExtractor,
        IFraudRuleEngine ruleEngine,
        ITransactionRepository transactionRepository,
        IAnalysisResultRepository analysisRepository,
        IFraudRuleRepository ruleRepository,
        IFraudRuleEventRepository ruleEventRepository,
        IFraudAlertRepository alertRepository,
        IRiskEvaluationRepository riskEvaluationRepository,
        IBlacklistService blacklistService,
        PythonMLIntegrationService pythonService,
        ILogger<FraudDetectionService> logger)
    {
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
        _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
        _transactionRepository =
            transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _analysisRepository = analysisRepository ?? throw new ArgumentNullException(nameof(analysisRepository));
        _ruleRepository = ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));
        _ruleEventRepository = ruleEventRepository ?? throw new ArgumentNullException(nameof(ruleEventRepository));
        _alertRepository = alertRepository ?? throw new ArgumentNullException(nameof(alertRepository));
        _riskEvaluationRepository = riskEvaluationRepository ?? throw new ArgumentNullException(nameof(riskEvaluationRepository));
        _blacklistService = blacklistService ?? throw new ArgumentNullException(nameof(blacklistService));
        _pythonService = pythonService ?? throw new ArgumentNullException(nameof(pythonService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Hybrid Transaction Analysis

    public async Task<AnalysisResult> AnalyzeTransactionAsync(TransactionRequest request)
    {
        try
        {
            _logger.LogInformation("Analyzing transaction {UserId} with hybrid approach", request.UserId);

            // 1. Create Transaction entity from request
            var transaction = CreateTransactionFromRequest(request);

            // 2. Save transaction to database
            await _transactionRepository.SaveTransactionAsync(transaction);

            var vValues = _vGenerator.GenerateVValuesFromAmountAndTime(
                request.Amount,
                DateTime.UtcNow, // veya request.TransactionDate kullanılabilir
                request.Type.ToString(),
                "regular" // Kullanıcı segmenti (VIP, business, new, suspicious, vb.)
            );

            // V değerlerini AdditionalData'ya ekle
            var additionalData = new TransactionAdditionalData();
            foreach (var pair in vValues)
            {
                additionalData.VFactors[pair.Key] = pair.Value;
            }

            // 3. Map to TransactionData for rule engine
            var transactionData = MapToTransactionData(transaction);

            // 4. Evaluate rules - primary approach
            var ruleResults = await _ruleEngine.EvaluateRulesAsync(transactionData);

            // 5. Calculate rule-based risk level
            var ruleBasedRiskLevel = CalculateRuleBasedRiskLevel(ruleResults);

            // 6. ML-based evaluation - supplementary approach with Python integration
            RiskEvaluation mlEvaluation = null;
            try
            {
                mlEvaluation = await EvaluateRiskWithMLAsync(transactionData);
                if (mlEvaluation != null)
                    AnalyzeVFeatures(transactionData, mlEvaluation);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ML risk evaluation failed, continuing with rule-based evaluation only");
            }

            // 7. Determine final decision combining both approaches
            var finalDecision = DetermineDecision(ruleResults, mlEvaluation, request.Amount);
        
            // 8. Create analysis result (henüz kaydedilmedi)
            var analysisResult = AnalysisResult.Create(
                transaction.Id,
                mlEvaluation?.AnomalyScore ?? 0,
                mlEvaluation?.FraudProbability ?? 0,
                ruleBasedRiskLevel,
                finalDecision
            );

            await _riskEvaluationRepository.CreateAsync(mlEvaluation);

            // 9. Add risk factors from both approaches
            //   9a. Rule-based risk factors
            foreach (var ruleResult in ruleResults.Where(r => r.IsTriggered))
            {
                var rf = RiskFactor.Create(
                    RiskFactorType.RuleViolation,
                    $"Rule triggered: {ruleResult.RuleName} - {ruleResult.Score} - {ruleResult.Action} - {ruleResult.RuleDescription}",
                    ruleResult.Confidence
                );
                rf.RuleId = ruleResult.RuleId;
                rf.ActionTaken = ruleResult.Action.ToString();
                analysisResult.AddRiskFactor(rf);
            }

            //   9b. ML-based risk factors
            if (mlEvaluation != null)
            {
                foreach (var factor in mlEvaluation.RiskFactors)
                {
                    factor.Source = "ML";
                    analysisResult.AddRiskFactor(factor);
                }
            }

            // 10. Save analysis result before creating any alerts
            await _analysisRepository.SaveResultAsync(analysisResult);

            // 11. Create fraud alert if decision requires it
            if (finalDecision == DecisionType.Deny
                || finalDecision == DecisionType.ReviewRequired
                || finalDecision == DecisionType.EscalateToManager)
            {
                //await CreateFraudAlertAsync(transactionData, analysisResult, ruleResults);
            }

            // 12. Son adım: tüm tetiklenen kural aksiyonlarını uygula
            foreach (var rule in ruleResults.Where(r => r.IsTriggered))
            {
                await ApplyRuleAction(rule.Action, rule.RuleName, rule.RuleId, transactionData);
            }

            return analysisResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in hybrid transaction analysis for {TransactionId}", request.UserId);
            return AnalysisResult.CreateFailed(request.UserId, ex.Message);
        }
    }

    #endregion

    #region ML Model Evaluation

    /// <summary>
    /// Sadece ML modeliyle risk değerlendirmesi yap
    /// </summary>
    public async Task<ModelEvaluationResponse> EvaluateWithModelOnlyAsync(ModelEvaluationRequest request)
    {
        try
        {
            _logger.LogInformation("Evaluating transaction {TransactionId} with ML model only", request.TransactionId);

            // 1) Önce sabit değerleri Dictionary<string,string> olarak hazırla:
            var features2 = new Dictionary<string, string>
            {
                { "V1", "-2.8796" },
                { "V2", "4.6521" },
                { "V3", "-4.2178" },
                { "V4", "-2.8954" },
                { "V5", "0.9867" },
                { "V6", "-2.3561" },
                { "V7", "1.0487" },
                { "V8", "-0.8795" },
                { "V9", "0.3578" },
                { "V10", "-3.4891" },
                { "V11", "1.4576" },
                { "V12", "2.7834" },
                { "V13", "-1.5632" },
                { "V14", "-7.8945" },
                { "V15", "0.4521" },
                { "V16", "-2.3487" },
                { "V17", "-3.0045" },
                { "V18", "0.3214" },
                { "V19", "0.1257" },
                { "V20", "-0.5897" },
                { "V21", "-0.7845" },
                { "V22", "0.2587" },
                { "V23", "0.1257" },
                { "V24", "-0.0587" },
                { "V25", "0.1243" },
                { "V26", "-0.3547" },
                { "V27", "0.1456" },
                { "V28", "-0.0321" },
                { "Time", "13512" }
            };

            // 2) Eğer başka ek verin yoksa boş bir object-dictionary ver, yoksa kendi ek verini geçir:
            var additionalData = new Dictionary<string, object>();

            // 3) BuildAdditionalData çağır:
            var transactionAdditionalData = BuildAdditionalData(features2, additionalData);

            // 1. Convert to TransactionData for feature extraction
            var transactionData = new TransactionData
            {
                TransactionId = request.TransactionId,
                Amount = request.Amount,
                Timestamp = request.TransactionDate,
                Type = request.TransactionType,
                AdditionalData = transactionAdditionalData
            };

            // 2. Extract features
            var features = await _featureExtractor.ExtractFeaturesAsync(transactionData, ModelType.Ensemble);

            // 3. Python ML entegrasyonu ile model tahmini - LightGBM
            ModelPrediction lightGbmPrediction;
            try
            {
                // ModelInput oluştur
                var lightGbmInput = ConvertFeaturesToModelInput(features, (float)request.Amount, null);

                // Python entegrasyonu ile tahmin
                lightGbmPrediction =
                    await _modelService.PredictAsync("CreditCard_FraudDetection_LightGBM", lightGbmInput,
                        ModelType.LightGBM);

                _logger.LogInformation("LightGBM prediction completed with Python integration: {Probability}",
                    lightGbmPrediction.Probability);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making LightGBM prediction with Python integration");
                return new ModelEvaluationResponse
                {
                    TransactionId = request.TransactionId,
                    IsSuccess = false,
                    ErrorMessage = "Failed to make LightGBM prediction: " + ex.Message
                };
            }

            // 4. Python ML entegrasyonu ile model tahmini - PCA
            ModelPrediction pcaPrediction;
            try
            {
                // PCA modelini Python entegrasyonu ile çağır
                var pcaInput = ConvertFeaturesToPCAInput(features, request.TransactionDate, request.Amount, null);
                pcaPrediction = await _modelService.PredictAsync("CreditCard_AnomalyDetection_PCA", pcaInput);

                _logger.LogInformation("PCA prediction completed with Python integration: {AnomalyScore}",
                    pcaPrediction.AnomalyScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making PCA prediction with Python integration");
                return new ModelEvaluationResponse
                {
                    TransactionId = request.TransactionId,
                    FraudProbability = lightGbmPrediction.Probability,
                    AnomalyScore = 0,
                    RiskLevel = DetermineRiskLevel(lightGbmPrediction.Probability, 0, request.Amount),
                    RiskFactors = new List<RiskFactorInfo>(),
                    IsSuccess = true,
                    ErrorMessage = "PCA prediction failed but LightGBM prediction succeeded"
                };
            }

            // 5. Determine risk level
            var riskLevel =
                DetermineRiskLevel(lightGbmPrediction.Probability, pcaPrediction.AnomalyScore, request.Amount);

            // 6. Identify risk factors
            var mlRiskFactors = IdentifyRiskFactors(transactionData, lightGbmPrediction, pcaPrediction);

            // Convert domain risk factors to API risk factor info
            var riskFactorInfos = mlRiskFactors.Select(rf => new RiskFactorInfo
            {
                Code = rf.Code,
                Description = rf.Description,
                Confidence = rf.Confidence
            }).ToList();

            // 7. Apply V-feature analysis for additional insights
            if (request.Features != null)
            {
                // Create temporary risk evaluation to hold V-feature analysis results
                var tempRiskEval = new RiskEvaluation
                {
                    TransactionId = request.TransactionId,
                    FraudProbability = lightGbmPrediction.Probability,
                    AnomalyScore = pcaPrediction.AnomalyScore,
                    RiskScore = riskLevel,
                    RiskFactors = new List<RiskFactor>()
                };

                AnalyzeVFeatures(transactionData, tempRiskEval);

                // Add V-feature risk factors to the response
                foreach (var factor in tempRiskEval.RiskFactors)
                    riskFactorInfos.Add(new RiskFactorInfo
                    {
                        Code = factor.Code,
                        Description = factor.Description,
                        Confidence = factor.Confidence
                    });

                // Update risk level if V-feature analysis increased it
                if (tempRiskEval.RiskScore > riskLevel) riskLevel = tempRiskEval.RiskScore.Value;
            }

            // 8. Create response
            return new ModelEvaluationResponse
            {
                TransactionId = request.TransactionId,
                FraudProbability = lightGbmPrediction.Probability,
                AnomalyScore = pcaPrediction.AnomalyScore,
                RiskLevel = riskLevel,
                RiskFactors = riskFactorInfos,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in model-only evaluation for transaction {TransactionId}",
                request.TransactionId);
            return new ModelEvaluationResponse
            {
                TransactionId = request.TransactionId,
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Geliştirilmiş risk seviyesi belirleme metodu - Tutar duyarlı
    /// </summary>
    private RiskLevel DetermineRiskLevel(double fraudProbability, double anomalyScore, decimal amount)
    {
        // 1. Tutar bazlı dinamik eşik değerleri
        // Büyük tutarlar için daha düşük eşikler (daha temkinli)
        double highRiskThreshold, mediumRiskThreshold, anomalyThreshold;

        if (amount > 1_000_000_000) // Milyar üzeri
        {
            highRiskThreshold = 0.55; // Normalden daha düşük
            mediumRiskThreshold = 0.35;
            anomalyThreshold = 2.5;
            _logger.LogInformation("Çok yüksek tutar için düşük risk eşikleri: {High}, {Medium}, {Anomaly}",
                highRiskThreshold, mediumRiskThreshold, anomalyThreshold);
        }
        else if (amount > 1_000_000) // Milyon üzeri
        {
            highRiskThreshold = 0.65;
            mediumRiskThreshold = 0.40;
            anomalyThreshold = 3.0;
            _logger.LogInformation("Yüksek tutar için ayarlanmış risk eşikleri: {High}, {Medium}, {Anomaly}",
                highRiskThreshold, mediumRiskThreshold, anomalyThreshold);
        }
        else if (amount > 10_000) // On bin üzeri
        {
            highRiskThreshold = 0.70;
            mediumRiskThreshold = 0.45;
            anomalyThreshold = 3.2;
        }
        else // Normal tutarlar
        {
            highRiskThreshold = HIGH_RISK_THRESHOLD; // 0.75
            mediumRiskThreshold = MEDIUM_RISK_THRESHOLD; // 0.45
            anomalyThreshold = ANOMALY_THRESHOLD; // 3.5
        }

        // 2. Ağırlıklı risk hesaplama
        // Fraud olasılığı ve anormallik skorunu birleştiren gelişmiş bir formül
        double combinedRiskScore = (fraudProbability * 0.7) + (Math.Min(anomalyScore / 10.0, 1.0) * 0.3);

        // 3. Risk seviyesi kararını ver
        if ((fraudProbability > highRiskThreshold && anomalyScore > anomalyThreshold) ||
            combinedRiskScore > 0.75)
        {
            return RiskLevel.Critical;
        }

        if ((fraudProbability > highRiskThreshold) ||
            (fraudProbability > 0.55 && anomalyScore > anomalyThreshold * 1.2) ||
            combinedRiskScore > 0.65)
        {
            return RiskLevel.High;
        }

        if ((fraudProbability > mediumRiskThreshold && anomalyScore > anomalyThreshold * 0.8) ||
            anomalyScore > anomalyThreshold * 1.5 ||
            combinedRiskScore > 0.5)
        {
            return RiskLevel.Medium;
        }

        return RiskLevel.Low;
    }

    #endregion

    #region Comprehensive Fraud Check

    /// <summary>
    /// Tüm kontrolleri yapan kapsamlı dolandırıcılık kontrolü
    /// </summary>
    public async Task<ComprehensiveFraudCheckResponse> PerformComprehensiveCheckAsync(
        ComprehensiveFraudCheckRequest request)
    {
        try
        {
            _logger.LogInformation("Performing comprehensive fraud check for session {SessionId}",
                request.Session?.SessionId ?? Guid.Empty);

            var response = new ComprehensiveFraudCheckResponse
            {
                IsSuccess = true,
                OverallActions = new List<RuleAction>()
            };

            // Paralelleştirilebilecek kontrolleri aynı anda çalıştır
            var tasks = new List<Task>();

            // Transaction kontrolü
            Task<FraudDetectionResponse> transactionTask = null;
            if (request.Transaction != null)
            {
                transactionTask = CheckTransactionAsync(request.Transaction);
                tasks.Add(transactionTask);
            }

            // Account kontrolü
            Task<FraudDetectionResponse> accountTask = null;
            if (request.Account != null)
            {
                accountTask = CheckAccountAccessAsync(request.Account);
                tasks.Add(accountTask);
            }

            // IP kontrolü
            Task<FraudDetectionResponse> ipTask = null;
            if (request.IpAddress != null)
            {
                ipTask = CheckIpAddressAsync(request.IpAddress);
                tasks.Add(ipTask);
            }

            // Device kontrolü
            Task<FraudDetectionResponse> deviceTask = null;
            if (request.Device != null)
            {
                deviceTask = CheckDeviceAsync(request.Device);
                tasks.Add(deviceTask);
            }

            // Session kontrolü
            Task<FraudDetectionResponse> sessionTask = null;
            if (request.Session != null)
            {
                sessionTask = CheckSessionAsync(request.Session);
                tasks.Add(sessionTask);
            }

            // ML model değerlendirmesi
            Task<ModelEvaluationResponse> modelTask = null;
            if (request.ModelEvaluation != null)
            {
                modelTask = EvaluateWithModelOnlyAsync(request.ModelEvaluation);
                tasks.Add(modelTask);
            }

            // Tüm kontrollerin tamamlanmasını bekle
            await Task.WhenAll(tasks);

            // Sonuçları topla
            if (transactionTask != null) response.TransactionCheck = await transactionTask;
            if (accountTask != null) response.AccountCheck = await accountTask;
            if (ipTask != null) response.IpCheck = await ipTask;
            if (deviceTask != null) response.DeviceCheck = await deviceTask;
            if (sessionTask != null) response.SessionCheck = await sessionTask;
            if (modelTask != null) response.ModelEvaluation = await modelTask;

            // Genel sonucu belirle
            DetermineOverallResult(response);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing comprehensive fraud check");
            return new ComprehensiveFraudCheckResponse
            {
                IsSuccess = false,
                OverallResultType = FraudDetectionResultType.Error,
                OverallResultMessage = $"Error: {ex.Message}",
                OverallActions = new List<RuleAction>()
            };
        }
    }

    /// <summary>
    /// Kapsamlı kontrolde genel sonucu belirle
    /// </summary>
    private void DetermineOverallResult(ComprehensiveFraudCheckResponse response)
    {
        // En yüksek risk skorunu ve en yüksek öncelikli sonuç tipini bul
        var maxRiskScore = 0;
        var allActions = new List<RuleAction>();
        var resultTypes = new List<FraudDetectionResultType>();
        var totalTriggeredRules = 0;

        // Transaction sonucu
        if (response.TransactionCheck != null)
        {
            maxRiskScore = Math.Max(maxRiskScore, response.TransactionCheck.RiskScore);
            allActions.AddRange(response.TransactionCheck.Actions);
            resultTypes.Add(response.TransactionCheck.ResultType);
            totalTriggeredRules += response.TransactionCheck.TriggeredRuleCount;
        }

        // Account sonucu
        if (response.AccountCheck != null)
        {
            maxRiskScore = Math.Max(maxRiskScore, response.AccountCheck.RiskScore);
            allActions.AddRange(response.AccountCheck.Actions);
            resultTypes.Add(response.AccountCheck.ResultType);
            totalTriggeredRules += response.AccountCheck.TriggeredRuleCount;
        }

        // IP sonucu
        if (response.IpCheck != null)
        {
            maxRiskScore = Math.Max(maxRiskScore, response.IpCheck.RiskScore);
            allActions.AddRange(response.IpCheck.Actions);
            resultTypes.Add(response.IpCheck.ResultType);
            totalTriggeredRules += response.IpCheck.TriggeredRuleCount;
        }

        // Device sonucu
        if (response.DeviceCheck != null)
        {
            maxRiskScore = Math.Max(maxRiskScore, response.DeviceCheck.RiskScore);
            allActions.AddRange(response.DeviceCheck.Actions);
            resultTypes.Add(response.DeviceCheck.ResultType);
            totalTriggeredRules += response.DeviceCheck.TriggeredRuleCount;
        }

        // Session sonucu
        if (response.SessionCheck != null)
        {
            maxRiskScore = Math.Max(maxRiskScore, response.SessionCheck.RiskScore);
            allActions.AddRange(response.SessionCheck.Actions);
            resultTypes.Add(response.SessionCheck.ResultType);
            totalTriggeredRules += response.SessionCheck.TriggeredRuleCount;
        }

        // ML model sonucu
        if (response.ModelEvaluation != null && response.ModelEvaluation.IsSuccess)
        {
            // ML risk skorunu genel risk skoruna ekle
            var mlRiskScore = (int)(response.ModelEvaluation.FraudProbability * 100);

            // ML risk seviyesini FraudDetectionResultType'a dönüştür
            FraudDetectionResultType mlResultType;
            switch (response.ModelEvaluation.RiskLevel)
            {
                case RiskLevel.High:
                case RiskLevel.Critical:
                    mlResultType = FraudDetectionResultType.ReviewRequired;
                    break;
                case RiskLevel.Medium:
                    mlResultType = FraudDetectionResultType.AdditionalVerificationRequired;
                    break;
                default:
                    mlResultType = FraudDetectionResultType.Approved;
                    break;
            }

            resultTypes.Add(mlResultType);

            // ML risk skorunu genel risk skoruna ekle (ağırlıklı)
            maxRiskScore = Math.Max(maxRiskScore, mlRiskScore);
        }

        // Sonuç tipini belirle - en yüksek öncelikli olanı al
        var overallResultType = DetermineHighestPriorityResultType(resultTypes);

        // Aksiyonları birleştir ve tekrarları kaldır
        response.OverallActions = allActions.Distinct().ToList();

        // Genel sonuçları belirle
        response.OverallResultType = overallResultType;
        response.OverallRiskScore = maxRiskScore;
        response.RequiresAction = overallResultType != FraudDetectionResultType.Approved;

        // Genel sonuç mesajı
        response.OverallResultMessage =
            $"Triggered {totalTriggeredRules} rule(s) across all checks with overall risk score {maxRiskScore}. Result: {overallResultType}";
    }

    /// <summary>
    /// En yüksek öncelikli sonuç tipini belirle
    /// </summary>
    private FraudDetectionResultType DetermineHighestPriorityResultType(List<FraudDetectionResultType> resultTypes)
    {
        if (resultTypes.Contains(FraudDetectionResultType.PermanentlyBlocked))
            return FraudDetectionResultType.PermanentlyBlocked;

        if (resultTypes.Contains(FraudDetectionResultType.TemporarilyBlocked))
            return FraudDetectionResultType.TemporarilyBlocked;

        if (resultTypes.Contains(FraudDetectionResultType.Rejected))
            return FraudDetectionResultType.Rejected;

        if (resultTypes.Contains(FraudDetectionResultType.ReviewRequired))
            return FraudDetectionResultType.ReviewRequired;

        if (resultTypes.Contains(FraudDetectionResultType.AdditionalVerificationRequired))
            return FraudDetectionResultType.AdditionalVerificationRequired;

        if (resultTypes.Contains(FraudDetectionResultType.Error))
            return FraudDetectionResultType.Error;

        return FraudDetectionResultType.Approved;
    }

    #endregion

    #region Single Context Checks

    /// <summary>
    /// İşlem kontrolü yap - API uyumlu format
    /// </summary>
    public async Task<FraudDetectionResponse> 
        CheckTransactionAsync(TransactionCheckRequest request)
    {
        try
        {
            _logger.LogInformation("Checking transaction {TransactionId} for account {AccountId}",
                request.TransactionId, request.AccountId);

            // Blacklist kontrolü
            var isBlacklisted = await CheckTransactionBlacklistAsync(request);
            if (isBlacklisted) return CreateBlacklistedResponse("Transaction is blacklisted");

            // 1. TransactionContext oluştur
            var transactionContext = new TransactionContext
            {
                TransactionId = request.TransactionId,
                AccountId = request.AccountId,
                Amount = request.Amount,
                Currency = request.Currency,
                TransactionType = request.TransactionType,
                TransactionDate = request.TransactionDate,
                RecipientAccountId = request.RecipientAccountId,
                RecipientAccountNumber = request.RecipientAccountNumber,
                RecipientCountry = request.RecipientCountry,
                UserTransactionCount24h = request.UserTransactionCount24h,
                UserTotalAmount24h = request.UserTotalAmount24h,
                UserAverageTransactionAmount = request.UserAverageTransactionAmount,
                DaysSinceFirstTransaction = request.DaysSinceFirstTransaction,
                UniqueRecipientCount1h = request.UniqueRecipientCount1h,
                AdditionalData = request.AdditionalData
            };

            // 2. Değerlendirme bağlamı oluştur
            var evaluationContext = new RuleEvaluationContext
            {
                Transaction = transactionContext,
                EvaluationTime = DateTime.UtcNow
            };

            // 3. İlgili kategorilerdeki kuralları değerlendir
            var transactionRules = await _ruleEngine.EvaluateRulesByCategoryAsync(
                RuleCategory.Transaction, evaluationContext);

            var networkRules = await _ruleEngine.EvaluateRulesByCategoryAsync(
                RuleCategory.Network, evaluationContext);

            var timeRules = await _ruleEngine.EvaluateRulesByCategoryAsync(
                RuleCategory.Time, evaluationContext);

            // 4. Tetiklenen tüm kuralları birleştir
            var allTriggeredRules = transactionRules
                .Concat(networkRules)
                .Concat(timeRules)
                .Where(r => r.IsTriggered)
                .ToList();

            // 5. ML değerlendirmesi yap (opsiyonel) - Python entegrasyonu
            RiskEvaluation mlEvaluation = null;
            try
            {
                // TransactionData oluştur
                var transactionData = new TransactionData
                {
                    TransactionId = request.TransactionId,
                    UserId = request.AccountId,
                    Amount = request.Amount,
                    Timestamp = request.TransactionDate,
                    Type = request.TransactionType,
                    AdditionalData = MapToAdditionalDataVO(request.AdditionalData)
                };

                mlEvaluation = await EvaluateRiskWithMLAsync(transactionData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ML evaluation failed during transaction check, continuing with rules only");
            }

            // 6. Olay oluştur (tetiklenen kurallar için)
            foreach (var rule in allTriggeredRules)
                if (ShouldCreateEvent(rule.Actions))
                    try
                    {
                        var eventDetails = new
                        {
                            RuleEvaluation = rule,
                            Context = new
                            {
                                TransactionId = request.TransactionId,
                                AccountId = request.AccountId,
                                Amount = request.Amount,
                                Currency = request.Currency,
                                TransactionType = request.TransactionType,
                                EvaluationTime = evaluationContext.EvaluationTime
                            }
                        };

                        var eventDetailsJson = JsonSerializer.Serialize(eventDetails);

                        var fraudEvent = FraudRuleEvent.Create(
                            rule.RuleId,
                            rule.RuleName,
                            rule.RuleCode,
                            request.TransactionId,
                            request.AccountId,
                            null, // IP adresi
                            null, // Cihaz bilgisi
                            rule.Actions,
                            rule.ActionDuration,
                            eventDetailsJson);

                        var savedEvent = await _ruleEventRepository.AddEventAsync(fraudEvent);
                        rule.EventId = savedEvent.Id;
                        rule.EventCreated = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating fraud rule event for rule {RuleCode}", rule.RuleCode);
                    }

            // 7. FraudDetectionResponse formatında sonucu oluştur
            var response = CreateDetectionResponse(allTriggeredRules, mlEvaluation);

            _logger.LogInformation("Transaction check result: {ResultType} with risk score {RiskScore}",
                response.ResultType, response.RiskScore);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking transaction {TransactionId}", request.TransactionId);

            // Hata durumunda basit yanıt döndür
            return new FraudDetectionResponse
            {
                IsSuccess = false,
                ResultType = FraudDetectionResultType.Error,
                ResultMessage = $"Error: {ex.Message}",
                Actions = new List<RuleAction>(),
                TriggeredRules = new List<TriggeredRuleInfo>(),
                CreatedEventIds = new List<Guid>()
            };
        }
    }

    /// <summary>
    /// Hesap erişimi kontrolü yap - API uyumlu format
    /// </summary>
    public async Task<FraudDetectionResponse> CheckAccountAccessAsync(AccountAccessCheckRequest request)
    {
        try
        {
            _logger.LogInformation("Checking account access for account {AccountId} from IP {IpAddress}",
                request.AccountId, request.IpAddress);

            // Blacklist kontrolü
            var isAccountBlacklisted = await _blacklistService.IsAccountBlacklistedAsync(request.AccountId);
            if (isAccountBlacklisted) return CreateBlacklistedResponse("Account is blacklisted");

            var isIpBlacklisted = await _blacklistService.IsIpBlacklistedAsync(request.IpAddress);
            if (isIpBlacklisted) return CreateBlacklistedResponse("IP address is blacklisted");

            // 1. AccountAccessContext oluştur
            var accountContext = new AccountAccessContext
            {
                AccountId = request.AccountId,
                Username = request.Username,
                AccessDate = request.AccessDate,
                IpAddress = request.IpAddress,
                CountryCode = request.CountryCode,
                City = request.City,
                DeviceId = request.DeviceId,
                IsTrustedDevice = request.IsTrustedDevice,
                UniqueIpCount24h = request.UniqueIpCount24h,
                UniqueCountryCount24h = request.UniqueCountryCount24h,
                IsSuccessful = request.IsSuccessful,
                FailedLoginAttempts = request.FailedLoginAttempts,
                TypicalAccessHours = request.TypicalAccessHours,
                TypicalAccessDays = request.TypicalAccessDays?.Select(d => (DayOfWeek)Enum.Parse(typeof(DayOfWeek), d))
                    .ToList(),
                TypicalCountries = request.TypicalCountries
            };

            // 2. Değerlendirme bağlamı oluştur
            var evaluationContext = new RuleEvaluationContext
            {
                Account = accountContext,
                EvaluationTime = DateTime.UtcNow
            };

            // 3. İlgili kategorilerdeki kuralları değerlendir
            var accountRules = await _ruleEngine.EvaluateRulesByCategoryAsync(
                RuleCategory.Account, evaluationContext);

            var ipRules = await _ruleEngine.EvaluateRulesByCategoryAsync(
                RuleCategory.IP, evaluationContext);

            // 4. Tetiklenen tüm kuralları birleştir
            var allTriggeredRules = accountRules
                .Concat(ipRules)
                .Where(r => r.IsTriggered)
                .ToList();

            // 5. Olay oluştur (tetiklenen kurallar için)
            foreach (var rule in allTriggeredRules)
                if (ShouldCreateEvent(rule.Actions))
                    try
                    {
                        var eventDetails = new
                        {
                            RuleEvaluation = rule,
                            Context = new
                            {
                                AccountId = request.AccountId,
                                Username = request.Username,
                                IpAddress = request.IpAddress,
                                CountryCode = request.CountryCode,
                                DeviceId = request.DeviceId,
                                EvaluationTime = evaluationContext.EvaluationTime
                            }
                        };

                        var eventDetailsJson = JsonSerializer.Serialize(eventDetails);

                        var fraudEvent = FraudRuleEvent.Create(
                            rule.RuleId,
                            rule.RuleName,
                            rule.RuleCode,
                            null, // İşlem ID'si
                            request.AccountId,
                            request.IpAddress,
                            request.DeviceId,
                            rule.Actions,
                            rule.ActionDuration,
                            eventDetailsJson);

                        var savedEvent = await _ruleEventRepository.AddEventAsync(fraudEvent);
                        rule.EventId = savedEvent.Id;
                        rule.EventCreated = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating fraud rule event for rule {RuleCode}", rule.RuleCode);
                    }

            // 6. FraudDetectionResponse formatında sonucu oluştur
            var response = CreateDetectionResponse(allTriggeredRules, null);

            _logger.LogInformation("Account access check result: {ResultType} with risk score {RiskScore}",
                response.ResultType, response.RiskScore);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking account access for account {AccountId}", request.AccountId);

            return new FraudDetectionResponse
            {
                IsSuccess = false,
                ResultType = FraudDetectionResultType.Error,
                ResultMessage = $"Error: {ex.Message}",
                Actions = new List<RuleAction>(),
                TriggeredRules = new List<TriggeredRuleInfo>(),
                CreatedEventIds = new List<Guid>()
            };
        }
    }

    /// <summary>
    /// IP adresi kontrolü yap - API uyumlu format
    /// </summary>
    public async Task<FraudDetectionResponse> CheckIpAddressAsync(IpCheckRequest request)
    {
        try
        {
            _logger.LogInformation("Checking IP address {IpAddress} from {CountryCode}",
                request.IpAddress, request.CountryCode);

            // Blacklist kontrolü
            var isIpBlacklisted = await _blacklistService.IsIpBlacklistedAsync(request.IpAddress);
            if (isIpBlacklisted || request.IsBlacklisted) return CreateBlacklistedResponse("IP address is blacklisted");

            var isCountryBlacklisted = await _blacklistService.IsCountryBlacklistedAsync(request.CountryCode);
            if (isCountryBlacklisted) return CreateBlacklistedResponse("Country is blacklisted");

            // 1. IpAddressContext oluştur
            var ipContext = new IpAddressContext
            {
                IpAddress = request.IpAddress,
                CountryCode = request.CountryCode,
                City = request.City,
                IspAsn = request.IspAsn,
                ReputationScore = request.ReputationScore,
                IsBlacklisted = request.IsBlacklisted,
                BlacklistNotes = request.BlacklistNotes,
                IsDatacenterOrProxy = request.IsDatacenterOrProxy,
                NetworkType = request.NetworkType,
                UniqueAccountCount10m = request.UniqueAccountCount10m,
                UniqueAccountCount1h = request.UniqueAccountCount1h,
                UniqueAccountCount24h = request.UniqueAccountCount24h,
                FailedLoginCount10m = request.FailedLoginCount10m
            };

            // 2. Değerlendirme bağlamı oluştur
            var evaluationContext = new RuleEvaluationContext
            {
                IpAddress = ipContext,
                EvaluationTime = DateTime.UtcNow
            };

            // 3. İlgili kategorilerdeki kuralları değerlendir
            var ipRules = await _ruleEngine.EvaluateRulesByCategoryAsync(
                RuleCategory.IP, evaluationContext);

            var networkRules = await _ruleEngine.EvaluateRulesByCategoryAsync(
                RuleCategory.Network, evaluationContext);

            // 4. Tetiklenen tüm kuralları birleştir
            var allTriggeredRules = ipRules
                .Concat(networkRules)
                .Where(r => r.IsTriggered)
                .ToList();

            // 5. Olay oluştur (tetiklenen kurallar için)
            foreach (var rule in allTriggeredRules)
                if (ShouldCreateEvent(rule.Actions))
                    try
                    {
                        var eventDetails = new
                        {
                            RuleEvaluation = rule,
                            Context = new
                            {
                                IpAddress = request.IpAddress,
                                CountryCode = request.CountryCode,
                                NetworkType = request.NetworkType,
                                ReputationScore = request.ReputationScore,
                                EvaluationTime = evaluationContext.EvaluationTime
                            }
                        };

                        var eventDetailsJson = JsonSerializer.Serialize(eventDetails);

                        var fraudEvent = FraudRuleEvent.Create(
                            rule.RuleId,
                            rule.RuleName,
                            rule.RuleCode,
                            null, // İşlem ID'si
                            null, // Hesap ID'si
                            request.IpAddress,
                            null, // Cihaz bilgisi
                            rule.Actions,
                            rule.ActionDuration,
                            eventDetailsJson);

                        var savedEvent = await _ruleEventRepository.AddEventAsync(fraudEvent);
                        rule.EventId = savedEvent.Id;
                        rule.EventCreated = true;

                        // Kara listeye ekleme aksiyonu varsa
                        if (rule.Actions.Contains(RuleAction.BlacklistIP))
                            await _blacklistService.AddIpToBlacklistAsync(
                                request.IpAddress,
                                $"Rule triggered: {rule.RuleName} ({rule.RuleCode})",
                                rule.ActionDuration);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating fraud rule event for rule {RuleCode}", rule.RuleCode);
                    }

            // 6. FraudDetectionResponse formatında sonucu oluştur
            var response = CreateDetectionResponse(allTriggeredRules, null);

            _logger.LogInformation("IP check result: {ResultType} with risk score {RiskScore}",
                response.ResultType, response.RiskScore);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking IP address {IpAddress}", request.IpAddress);

            return new FraudDetectionResponse
            {
                IsSuccess = false,
                ResultType = FraudDetectionResultType.Error,
                ResultMessage = $"Error: {ex.Message}",
                Actions = new List<RuleAction>(),
                TriggeredRules = new List<TriggeredRuleInfo>(),
                CreatedEventIds = new List<Guid>()
            };
        }
    }

    /// <summary>
    /// Oturum kontrolü yap - API uyumlu format
    /// </summary>
    public async Task<FraudDetectionResponse> CheckSessionAsync(SessionCheckRequest request)
    {
        try
        {
            _logger.LogInformation("Checking session {SessionId} for account {AccountId}",
                request.SessionId, request.AccountId);

            // Blacklist kontrolü
            var isAccountBlacklisted = await _blacklistService.IsAccountBlacklistedAsync(request.AccountId);
            if (isAccountBlacklisted) return CreateBlacklistedResponse("Account is blacklisted");

            var isDeviceBlacklisted = string.IsNullOrEmpty(request.DeviceId)
                ? false
                : await _blacklistService.IsDeviceBlacklistedAsync(request.DeviceId);
            if (isDeviceBlacklisted) return CreateBlacklistedResponse("Device is blacklisted");

            var isIpBlacklisted = await _blacklistService.IsIpBlacklistedAsync(request.IpAddress);
            if (isIpBlacklisted) return CreateBlacklistedResponse("IP address is blacklisted");

            // 1. SessionContext oluştur
            var sessionContext = new SessionContext
            {
                SessionId = request.SessionId,
                AccountId = request.AccountId,
                StartTime = request.StartTime,
                LastActivityTime = request.LastActivityTime,
                DurationMinutes = request.DurationMinutes,
                IpAddress = request.IpAddress,
                DeviceId = request.DeviceId,
                UserAgent = request.UserAgent,
                RapidNavigationCount = request.RapidNavigationCount,
                AdditionalData = request.AdditionalData
            };

            // 2. Değerlendirme bağlamı oluştur
            var evaluationContext = new RuleEvaluationContext
            {
                Session = sessionContext,
                EvaluationTime = DateTime.UtcNow
            };

            // 3. İlgili kategorilerdeki kuralları değerlendir
            var sessionRules = await _ruleEngine.EvaluateRulesByCategoryAsync(
                RuleCategory.Session, evaluationContext);

            var behaviorRules = await _ruleEngine.EvaluateRulesByCategoryAsync(
                RuleCategory.Behavior, evaluationContext);

            // 4. Tetiklenen tüm kuralları birleştir
            var allTriggeredRules = sessionRules
                .Concat(behaviorRules)
                .Where(r => r.IsTriggered)
                .ToList();

            // 5. Olay oluştur (tetiklenen kurallar için)
            foreach (var rule in allTriggeredRules)
                if (ShouldCreateEvent(rule.Actions))
                    try
                    {
                        var eventDetails = new
                        {
                            RuleEvaluation = rule,
                            Context = new
                            {
                                SessionId = request.SessionId,
                                AccountId = request.AccountId,
                                IpAddress = request.IpAddress,
                                DeviceId = request.DeviceId,
                                SessionDuration = request.DurationMinutes,
                                EvaluationTime = evaluationContext.EvaluationTime
                            }
                        };

                        var eventDetailsJson = JsonSerializer.Serialize(eventDetails);

                        var fraudEvent = FraudRuleEvent.Create(
                            rule.RuleId,
                            rule.RuleName,
                            rule.RuleCode,
                            null, // İşlem ID'si
                            request.AccountId,
                            request.IpAddress,
                            request.DeviceId,
                            rule.Actions,
                            rule.ActionDuration,
                            eventDetailsJson);

                        var savedEvent = await _ruleEventRepository.AddEventAsync(fraudEvent);
                        rule.EventId = savedEvent.Id;
                        rule.EventCreated = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating fraud rule event for rule {RuleCode}", rule.RuleCode);
                    }

            // 6. FraudDetectionResponse formatında sonucu oluştur
            var response = CreateDetectionResponse(allTriggeredRules, null);

            _logger.LogInformation("Session check result: {ResultType} with risk score {RiskScore}",
                response.ResultType, response.RiskScore);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking session {SessionId}", request.SessionId);

            return new FraudDetectionResponse
            {
                IsSuccess = false,
                ResultType = FraudDetectionResultType.Error,
                ResultMessage = $"Error: {ex.Message}",
                Actions = new List<RuleAction>(),
                TriggeredRules = new List<TriggeredRuleInfo>(),
                CreatedEventIds = new List<Guid>()
            };
        }
    }

    /// <summary>
    /// Cihaz kontrolü yap - API uyumlu format
    /// </summary>
    public async Task<FraudDetectionResponse> CheckDeviceAsync(DeviceCheckRequest request)
    {
        try
        {
            _logger.LogInformation("Checking device {DeviceId} with IP {IpAddress}",
                request.DeviceId, request.IpAddress);

            // Blacklist kontrolü
            var isDeviceBlacklisted = await _blacklistService.IsDeviceBlacklistedAsync(request.DeviceId);
            if (isDeviceBlacklisted) return CreateBlacklistedResponse("Device is blacklisted");

            var isIpBlacklisted = await _blacklistService.IsIpBlacklistedAsync(request.IpAddress);
            if (isIpBlacklisted) return CreateBlacklistedResponse("IP address is blacklisted");

            var isCountryBlacklisted = await _blacklistService.IsCountryBlacklistedAsync(request.CountryCode);
            if (isCountryBlacklisted) return CreateBlacklistedResponse("Country is blacklisted");

            // 1. DeviceContext oluştur
            var deviceContext = new DeviceContext
            {
                DeviceId = request.DeviceId,
                DeviceType = request.DeviceType,
                OperatingSystem = request.OperatingSystem,
                Browser = request.Browser,
                IpAddress = request.IpAddress,
                IsEmulator = request.IsEmulator,
                IsJailbroken = request.IsJailbroken,
                FirstSeenDate = request.FirstSeenDate,
                LastSeenDate = request.LastSeenDate,
                UniqueAccountCount24h = request.UniqueAccountCount24h,
                UniqueIpCount24h = request.UniqueIpCount24h,
                AdditionalData = request.AdditionalData
            };

            // 2. Değerlendirme bağlamı oluştur
            var evaluationContext = new RuleEvaluationContext
            {
                Device = deviceContext,
                EvaluationTime = DateTime.Now
            };

            // 3. İlgili kategorilerdeki kuralları değerlendir
            var deviceRules = await _ruleEngine.EvaluateRulesByCategoryAsync(
                RuleCategory.Device, evaluationContext);

            // 4. Tetiklenen tüm kuralları birleştir
            var allTriggeredRules = deviceRules
                .Where(r => r.IsTriggered)
                .ToList();

            // 5. Olay oluştur (tetiklenen kurallar için)
            foreach (var rule in allTriggeredRules)
                if (ShouldCreateEvent(rule.Actions))
                    try
                    {
                        var eventDetails = new
                        {
                            RuleEvaluation = rule,
                            Context = new
                            {
                                DeviceId = request.DeviceId,
                                DeviceType = request.DeviceType,
                                IpAddress = request.IpAddress,
                                IsEmulator = request.IsEmulator,
                                IsJailbroken = request.IsJailbroken,
                                IsRooted = request.IsRooted,
                                EvaluationTime = evaluationContext.EvaluationTime
                            }
                        };

                        var eventDetailsJson = JsonSerializer.Serialize(eventDetails);

                        var fraudEvent = FraudRuleEvent.Create(
                            rule.RuleId,
                            rule.RuleName,
                            rule.RuleCode,
                            null, // İşlem ID'si
                            null, // Hesap ID'si
                            request.IpAddress,
                            request.DeviceId,
                            rule.Actions,
                            rule.ActionDuration,
                            eventDetailsJson);

                        var savedEvent = await _ruleEventRepository.AddEventAsync(fraudEvent);
                        rule.EventId = savedEvent.Id;
                        rule.EventCreated = true;

                        // Kara listeye ekleme aksiyonu varsa
                        if (rule.Actions.Contains(RuleAction.BlockDevice))
                            await _blacklistService.AddDeviceToBlacklistAsync(
                                request.DeviceId,
                                $"Rule triggered: {rule.RuleName} ({rule.RuleCode})",
                                rule.ActionDuration);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating fraud rule event for rule {RuleCode}", rule.RuleCode);
                    }

            // 6. FraudDetectionResponse formatında sonucu oluştur
            var response = CreateDetectionResponse(allTriggeredRules, null);

            _logger.LogInformation("Device check result: {ResultType} with risk score {RiskScore}",
                response.ResultType, response.RiskScore);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking device {DeviceId}", request.DeviceId);

            return new FraudDetectionResponse
            {
                IsSuccess = false,
                ResultType = FraudDetectionResultType.Error,
                ResultMessage = $"Error: {ex.Message}",
                Actions = new List<RuleAction>(),
                TriggeredRules = new List<TriggeredRuleInfo>(),
                CreatedEventIds = new List<Guid>()
            };
        }
    }

    #endregion

    /// <summary>
    /// Kara liste kontrolü yap - İşlem
    /// </summary>
    private async Task<bool> CheckTransactionBlacklistAsync(TransactionCheckRequest request)
    {
        // İşlem kara liste kontrolleri
        if (await _blacklistService.IsAccountBlacklistedAsync(request.AccountId))
        {
            _logger.LogWarning("Account {AccountId} is blacklisted", request.AccountId);
            return true;
        }

        if (request.RecipientAccountId.HasValue &&
            await _blacklistService.IsAccountBlacklistedAsync(request.RecipientAccountId.Value))
        {
            _logger.LogWarning("Recipient account {RecipientId} is blacklisted", request.RecipientAccountId.Value);
            return true;
        }

        if (!string.IsNullOrEmpty(request.RecipientCountry) &&
            await _blacklistService.IsCountryBlacklistedAsync(request.RecipientCountry))
        {
            _logger.LogWarning("Recipient country {Country} is blacklisted", request.RecipientCountry);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Olay oluşturulmalı mı?
    /// </summary>
    private bool ShouldCreateEvent(List<RuleAction> actions)
    {
        // Log dışındaki tüm aksiyonlar için olay oluştur
        return actions != null && actions.Any(a => a != RuleAction.Log);
    }

    /// <summary>
    /// Blacklist yanıtı oluştur
    /// </summary>
    private FraudDetectionResponse CreateBlacklistedResponse(string message)
    {
        return new FraudDetectionResponse
        {
            ResultType = FraudDetectionResultType.PermanentlyBlocked,
            Actions = new List<RuleAction> { RuleAction.Block, RuleAction.BlacklistIP },
            RiskScore = 100,
            TriggeredRuleCount = 1,
            TriggeredRules = new List<TriggeredRuleInfo>
            {
                new()
                {
                    RuleId = Guid.Empty,
                    RuleCode = "BLACKLIST",
                    RuleName = "Blacklist Check",
                    TriggerScore = 1.0,
                    TriggerDetails = message
                }
            },
            CreatedEventIds = new List<Guid>(),
            ResultMessage = message,
            IsSuccess = true,
            RequiresAction = true
        };
    }

    /// <summary>
    /// Tespit sonucu oluştur - API uyumlu format
    /// </summary>
    private FraudDetectionResponse CreateDetectionResponse(
        List<RuleEvaluationResult> triggeredRules,
        RiskEvaluation mlEvaluation = null)
    {
        if (triggeredRules == null || !triggeredRules.Any())
            // Hiçbir kural tetiklenmediyse onaylı sonuç
            return new FraudDetectionResponse
            {
                ResultType = FraudDetectionResultType.Approved,
                Actions = new List<RuleAction>(),
                RiskScore = mlEvaluation?.FraudProbability > MEDIUM_RISK_THRESHOLD
                    ? (int)(mlEvaluation.FraudProbability * 100)
                    : 0,
                TriggeredRuleCount = 0,
                TriggeredRules = new List<TriggeredRuleInfo>(),
                CreatedEventIds = new List<Guid>(),
                ResultMessage = "No fraud rules triggered" +
                                (mlEvaluation?.FraudProbability > MEDIUM_RISK_THRESHOLD
                                    ? $", but ML risk score is {mlEvaluation.FraudProbability:P0}"
                                    : ""),
                IsSuccess = true,
                RequiresAction = false
            };

        // Risk puanını hesapla (tetiklenme skoru ve etki seviyesine göre)
        var riskScore = CalculateRiskScore(triggeredRules, mlEvaluation);

        // ML değerlendirmesi varsa ve yüksek risk gösteriyorsa, risk puanını artır
        if (mlEvaluation != null && mlEvaluation.FraudProbability > HIGH_RISK_THRESHOLD)
            riskScore = Math.Min(100, riskScore + 15);

        // Tüm aksiyonları topla
        var allActions = triggeredRules
            .SelectMany(r => r.Actions)
            .Distinct()
            .ToList();

        // En yüksek öncelikli aksiyon süresini belirle
        var actionDuration = DetermineActionDuration(triggeredRules);

        // Oluşturulan olayların ID'lerini topla
        var eventIds = triggeredRules
            .Where(r => r.EventCreated && r.EventId.HasValue)
            .Select(r => r.EventId.Value)
            .ToList();

        // Sonuç tipini belirle
        var resultType = DetermineResultType(riskScore, allActions);

        // Tetiklenen kuralları API yanıt formatına dönüştür
        var triggeredRuleInfos = triggeredRules
            .Select(r => new TriggeredRuleInfo
            {
                RuleId = r.RuleId,
                RuleCode = r.RuleCode,
                RuleName = r.RuleName,
                TriggerScore = r.TriggerScore,
                TriggerDetails = r.TriggerDetails
            })
            .ToList();

        // Sonuç mesajını oluştur
        var resultMessage = $"Triggered {triggeredRules.Count} rule(s) with risk score {riskScore}";
        if (mlEvaluation != null) resultMessage += $", ML probability: {mlEvaluation.FraudProbability:P0}";

        return new FraudDetectionResponse
        {
            ResultType = resultType,
            Actions = allActions,
            ActionDuration = actionDuration,
            RiskScore = riskScore,
            TriggeredRuleCount = triggeredRules.Count,
            TriggeredRules = triggeredRuleInfos,
            CreatedEventIds = eventIds,
            ResultMessage = resultMessage,
            IsSuccess = true,
            RequiresAction = resultType != FraudDetectionResultType.Approved
        };
    }

    /// <summary>
    /// Kurallar, ML değerlendirmesi ve V-faktörlerinden genel risk puanını hesapla
    /// </summary>
    private int CalculateRiskScore(List<RuleEvaluationResult> triggeredRules, RiskEvaluation mlEvaluation = null)
    {
        // Başlangıç risk puanı (100 üzerinden)
        int baseScore = 0;

        // Kural puanlarını hesapla (ağırlıklı ortalama)
        if (triggeredRules != null && triggeredRules.Any())
        {
            // Kural ağırlıklarını belirle (etki seviyesine göre)
            var ruleWeights = new Dictionary<string, double>();
            foreach (var rule in triggeredRules)
            {
                double weight = 1.0; // Varsayılan ağırlık

                // Etki seviyesine göre ağırlık belirle
                if (rule.RuleCode.StartsWith("CRI") || rule.RuleName.Contains("Critical"))
                    weight = 2.5; // Kritik kural
                else if (rule.RuleCode.StartsWith("HIG") || rule.RuleName.Contains("High"))
                    weight = 1.8; // Yüksek etkili kural
                else if (rule.RuleCode.StartsWith("MED") || rule.RuleName.Contains("Medium"))
                    weight = 1.2; // Orta etkili kural

                ruleWeights[rule.RuleId.ToString()] = weight;
            }

            // Ağırlıklı ortalama puan hesapla
            double totalWeight = ruleWeights.Values.Sum();
            double weightedScore = 0;

            foreach (var rule in triggeredRules)
            {
                weightedScore += rule.TriggerScore * 100 * ruleWeights[rule.RuleId.ToString()];
            }

            baseScore = (int)(weightedScore / totalWeight);
        }

        // ML değerlendirmesi varsa entegre et
        if (mlEvaluation != null)
        {
            int mlScore = (int)(mlEvaluation.FraudProbability * 100);

            // ML anormallik skoru yüksekse ek puan
            if (mlEvaluation.AnomalyScore > ANOMALY_THRESHOLD * 1.5)
                mlScore += 15;
            else if (mlEvaluation.AnomalyScore > ANOMALY_THRESHOLD)
                mlScore += 10;

            // V-faktörleri analizi sonucu önemli faktörler varsa ek puan
            int vFactorCount = mlEvaluation.RiskFactors.Count(rf =>
                rf.Code == RiskFactorType.ModelFeature.ToString() &&
                rf.Confidence > 0.7);

            if (vFactorCount >= 3)
                mlScore += 20;
            else if (vFactorCount > 0)
                mlScore += vFactorCount * 5;

            // Kural bazlı ve ML skorlarını birleştir (ağırlıklı)
            // Kural yoksa ML ağırlığı daha fazla
            if (triggeredRules == null || !triggeredRules.Any())
                baseScore = mlScore;
            else
                baseScore = (baseScore * 3 + mlScore * 2) / 5; // 60% kural, 40% ML
        }

        // Maksimum 100 olacak şekilde sınırla
        return Math.Min(100, baseScore);
    }

    /// <summary>
    /// Aksiyon süresini belirle
    /// </summary>
    private TimeSpan? DetermineActionDuration(List<RuleEvaluationResult> triggeredRules)
    {
        // Süresiz (null) aksiyonlar varsa, süresiz dön
        if (triggeredRules.Any(r => r.ActionDuration == null)) return null;

        // En uzun süreli aksiyonu bul
        var maxDuration = triggeredRules
            .Where(r => r.ActionDuration.HasValue)
            .Max(r => r.ActionDuration);

        return maxDuration;
    }

    /// <summary>
    /// Sonuç tipini belirle
    /// </summary>
    private FraudDetectionResultType DetermineResultType(int riskScore, List<RuleAction> actions)
    {
        // Kalıcı engelleme aksiyonu varsa
        if (actions.Contains(RuleAction.BlacklistIP) ||
            actions.Contains(RuleAction.BlockDevice))
            return FraudDetectionResultType.PermanentlyBlocked;

        // Geçici engelleme aksiyonu varsa
        if (actions.Contains(RuleAction.BlockIP) ||
            actions.Contains(RuleAction.LockAccount) ||
            actions.Contains(RuleAction.SuspendAccount))
            return FraudDetectionResultType.TemporarilyBlocked;

        // İşlem reddi varsa
        if (actions.Contains(RuleAction.RejectTransaction)) return FraudDetectionResultType.Rejected;

        // İnceleme gerekiyorsa
        if (actions.Contains(RuleAction.PutUnderReview) ||
            actions.Contains(RuleAction.RequireKYCVerification))
            return FraudDetectionResultType.ReviewRequired;

        // Ek doğrulama gerekiyorsa
        if (actions.Contains(RuleAction.RequireAdditionalVerification) ||
            actions.Contains(RuleAction.DelayProcessing))
            return FraudDetectionResultType.AdditionalVerificationRequired;

        // Risk puanına göre karar
        if (riskScore >= HIGH_RISK_SCORE_THRESHOLD)
            return FraudDetectionResultType.ReviewRequired;
        else if (riskScore >= MEDIUM_RISK_SCORE_THRESHOLD)
            return FraudDetectionResultType.AdditionalVerificationRequired;

        // Varsayılan olarak onaylı
        return FraudDetectionResultType.Approved;
    }


    /// <summary>
    /// Risk değerlendirmesi yap (ML tabanlı) - Python Ensemble entegrasyonu ile güncellenmiş
    /// </summary>
    private async Task<RiskEvaluation> EvaluateRiskWithMLAsync(TransactionData data)
    {
        try
        {
            _logger.LogInformation(
                "Python Ensemble ML entegrasyonu ile risk değerlendirmesi yapılıyor: {TransactionId}",
                data.TransactionId);

            // 1. Feature'ları çıkar
            var features = await _featureExtractor.ExtractFeaturesAsync(data, ModelType.Ensemble);

            // 2. Model input oluştur
            var modelInput = ConvertFeaturesToModelInput(features, (float)data.Amount, data.Timestamp);

            // 3. ENSEMBLE MODEL ile tahmin yap (Ana yöntem)
            ModelPrediction ensemblePrediction = null;
            try
            {
                ensemblePrediction = await _modelService.PredictAsync(
                    "CreditCardFraudDetection_Ensemble",
                    modelInput,
                    ModelType.Ensemble);

                _logger.LogInformation(
                    "Ensemble prediction completed: Probability={Probability:F4}, AnomalyScore={AnomalyScore:F4}",
                    ensemblePrediction.Probability, ensemblePrediction.AnomalyScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ensemble model failed, falling back to individual models");
            }

            // 4. Bireysel model tahminleri (Fallback veya detaylı analiz için)
            ModelPrediction lightGbmPrediction = null;
            ModelPrediction pcaPrediction = null;

            // LightGBM tahmin
            try
            {
                lightGbmPrediction = await _modelService.PredictAsync(
                    "CreditCard_FraudDetection_LightGBM",
                    modelInput,
                    ModelType.LightGBM);

                _logger.LogInformation("LightGBM prediction: Probability={Probability:F4}",
                    lightGbmPrediction.Probability);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LightGBM prediction failed");
            }

            // PCA tahmin
            try
            {
                var pcaInput = ConvertFeaturesToPCAInput(features, data.Timestamp, data.Amount, null);
                pcaPrediction = await _modelService.PredictAsync(
                    "CreditCard_AnomalyDetection_PCA",
                    pcaInput);

                _logger.LogInformation("PCA prediction: AnomalyScore={AnomalyScore:F4}, Probability={Probability:F4}",
                    pcaPrediction.AnomalyScore, pcaPrediction.Probability);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "PCA prediction failed");
            }

            // 5. En iyi tahmin sonucunu seç
            var bestPrediction = SelectBestPrediction(ensemblePrediction, lightGbmPrediction, pcaPrediction);

            // 6. Risk skorunu belirle (geliştirilmiş mantık)
            var riskScore = DetermineRiskLevelAdvanced(bestPrediction, data.Amount);

            // 7. Risk faktörlerini belirle
            var riskFactors = IdentifyRiskFactorsAdvanced(data, bestPrediction, lightGbmPrediction, pcaPrediction);

            // 8. RiskEvaluation oluştur
            var riskEvaluation = new RiskEvaluation
            {
                TransactionId = data.TransactionId,
                FraudProbability = bestPrediction.MainPrediction.Probability,
                AnomalyScore = bestPrediction.MainPrediction.AnomalyScore,
                RiskScore = riskScore,
                RiskFactors = riskFactors,
                EvaluatedAt = DateTime.UtcNow,
                FeatureValues = ConvertFeaturesToDictionary(features),
                FeatureImportance = GetFeatureImportance(bestPrediction.MainPrediction),

                // Model detaylarını ekle
                ModelInfo = new Dictionary<string, object>
                {
                    ["PrimaryModel"] = bestPrediction.PrimaryModelType,
                    ["ModelUsed"] = bestPrediction.ModelSource,
                    ["EnsembleAvailable"] = ensemblePrediction != null,
                    ["IndividualModelsAvailable"] = new
                    {
                        LightGBM = lightGbmPrediction != null,
                        PCA = pcaPrediction != null
                    }
                },

                UsedAlgorithms = bestPrediction.UsedAlgorithms,

                // Confidence hesaplama
                ConfidenceScore = CalculateModelConfidence(bestPrediction),

                // Alt model skorları
                MLScore = bestPrediction.MainPrediction.Probability,

                AdditionalData = new Dictionary<string, object>
                {
                    ["DetailedScores"] = bestPrediction.DetailedScores,
                    ["ModelMetadata"] = bestPrediction.MainPrediction.Metadata ?? new Dictionary<string, object>()
                }
            };

            // 9. Alt model bilgilerini ekle
            if (ensemblePrediction != null)
            {
                riskEvaluation.ModelInfo["Ensemble"] = CreateModelInfo(ensemblePrediction, "Ensemble");
            }

            if (lightGbmPrediction != null)
            {
                riskEvaluation.ModelInfo["LightGBM"] = CreateModelInfo(lightGbmPrediction, "LightGBM");
            }

            if (pcaPrediction != null)
            {
                riskEvaluation.ModelInfo["PCA"] = CreateModelInfo(pcaPrediction, "PCA");
            }

            // 10. Loglama
            _logger.LogInformation(
                "ML risk evaluation completed for transaction {TransactionId}. " +
                "Primary Model: {PrimaryModel}, Risk Score: {RiskScore}, " +
                "Fraud Probability: {FraudProbability:F4}, Anomaly Score: {AnomalyScore:F4}",
                data.TransactionId, bestPrediction.PrimaryModelType, riskScore,
                riskEvaluation.FraudProbability, riskEvaluation.AnomalyScore);

            return riskEvaluation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating risk with ML for transaction {TransactionId}",
                data.TransactionId);
            throw new InvalidOperationException("ML-based risk evaluation failed", ex);
        }
    }

    /// <summary>
    /// En iyi tahmin sonucunu seç (Ensemble öncelikli)
    /// </summary>
    private BestPredictionResult SelectBestPrediction(
        ModelPrediction ensemblePrediction,
        ModelPrediction lightGbmPrediction,
        ModelPrediction pcaPrediction)
    {
        var result = new BestPredictionResult();

        // Ensemble varsa ve başarılıysa onu kullan
        if (ensemblePrediction != null && ensemblePrediction.IsSuccessful)
        {
            result.MainPrediction = ensemblePrediction;
            result.PrimaryModelType = "Ensemble";
            result.ModelSource = "Primary_Ensemble";
            result.UsedAlgorithms.Add("Ensemble");

            // Ensemble'dan alt model bilgilerini çıkar
            if (ensemblePrediction.Metadata != null)
            {
                if (ensemblePrediction.Metadata.ContainsKey("LightGBM_Probability"))
                {
                    result.DetailedScores["LightGBM_Probability"] = ensemblePrediction.Metadata["LightGBM_Probability"];
                }

                if (ensemblePrediction.Metadata.ContainsKey("PCA_Probability"))
                {
                    result.DetailedScores["PCA_Probability"] = ensemblePrediction.Metadata["PCA_Probability"];
                }
            }
        }
        // LightGBM + PCA kombinasyonu
        else if (lightGbmPrediction != null && pcaPrediction != null)
        {
            // Manuel ensemble oluştur
            var combinedProbability = (lightGbmPrediction.Probability * 0.7) + (pcaPrediction.Probability * 0.3);
            var combinedScore = (lightGbmPrediction.Score * 0.7) + (pcaPrediction.Score * 0.3);

            result.MainPrediction = new ModelPrediction
            {
                PredictedLabel = combinedProbability > 0.5,
                Probability = combinedProbability,
                Score = combinedScore,
                AnomalyScore = pcaPrediction.AnomalyScore,
                ModelType = "Manual_Ensemble",
                PredictionTime = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["LightGBM_Probability"] = lightGbmPrediction.Probability,
                    ["PCA_Probability"] = pcaPrediction.Probability,
                    ["PCA_AnomalyScore"] = pcaPrediction.AnomalyScore,
                    ["EnsembleMethod"] = "Manual_Weighted_Average"
                }
            };

            result.PrimaryModelType = "Manual_Ensemble";
            result.ModelSource = "Fallback_Manual_Ensemble";
            result.UsedAlgorithms.AddRange(new[] { "LightGBM", "PCA" });

            result.DetailedScores["LightGBM_Probability"] = lightGbmPrediction.Probability;
            result.DetailedScores["PCA_Probability"] = pcaPrediction.Probability;
            result.DetailedScores["PCA_AnomalyScore"] = pcaPrediction.AnomalyScore;
        }
        // Sadece LightGBM varsa
        else if (lightGbmPrediction != null)
        {
            result.MainPrediction = lightGbmPrediction;
            result.PrimaryModelType = "LightGBM";
            result.ModelSource = "Fallback_LightGBM_Only";
            result.UsedAlgorithms.Add("LightGBM");
            result.DetailedScores["LightGBM_Probability"] = lightGbmPrediction.Probability;
        }
        // Sadece PCA varsa
        else if (pcaPrediction != null)
        {
            result.MainPrediction = pcaPrediction;
            result.PrimaryModelType = "PCA";
            result.ModelSource = "Fallback_PCA_Only";
            result.UsedAlgorithms.Add("PCA");
            result.DetailedScores["PCA_Probability"] = pcaPrediction.Probability;
            result.DetailedScores["PCA_AnomalyScore"] = pcaPrediction.AnomalyScore;
        }
        else
        {
            // Hiçbir model çalışmadıysa emergency fallback
            result.MainPrediction = new ModelPrediction
            {
                PredictedLabel = false,
                Probability = 0.3,
                Score = 0.3,
                AnomalyScore = 0.6,
                ModelType = "Emergency_Fallback",
                ErrorMessage = "All ML models failed",
                PredictionTime = DateTime.UtcNow
            };
            result.PrimaryModelType = "Emergency_Fallback";
            result.ModelSource = "Emergency_Fallback";
            result.UsedAlgorithms.Add("Emergency_Fallback");
        }

        return result;
    }

    /// <summary>
    /// Geliştirilmiş risk seviyesi belirleme metodu
    /// </summary>
    private RiskLevel DetermineRiskLevelAdvanced(BestPredictionResult prediction, decimal amount)
    {
        var fraudProbability = prediction.MainPrediction.Probability;
        var anomalyScore = prediction.MainPrediction.AnomalyScore;
        var modelType = prediction.PrimaryModelType;

        _logger.LogDebug(
            "Risk level calculation: FraudProb={FraudProb:F4}, AnomalyScore={AnomalyScore:F4}, Amount={Amount}, Model={Model}",
            fraudProbability, anomalyScore, amount, modelType);

        // 1. Tutar bazlı dinamik eşik değerleri (düzeltilmiş)
        var thresholds = GetRiskThresholds(amount);

        // 2. Model tipine göre ağırlık ayarlaması
        double modelWeight = modelType switch
        {
            "Ensemble" => 1.0, // En güvenilir
            "Manual_Ensemble" => 0.9, // İyi
            "LightGBM" => 0.8, // Orta
            "PCA" => 0.6, // Sadece anomali tespiti
            _ => 0.5 // Fallback
        };

        // 3. Ağırlıklı risk hesaplama (geliştirilmiş)
        double adjustedFraudProb = fraudProbability * modelWeight;
        double normalizedAnomalyScore = Math.Min(anomalyScore / 10.0, 1.0); // 0-1 aralığına normalize et

        // Ensemble modeli için daha dengeli ağırlık
        double combinedRiskScore = modelType == "Ensemble"
            ? adjustedFraudProb // Ensemble zaten optimal kombinasyon
            : (adjustedFraudProb * 0.7) + (normalizedAnomalyScore * 0.3);

        _logger.LogDebug(
            "Risk calculation details: AdjustedFraudProb={Adjusted:F4}, NormalizedAnomaly={Normalized:F4}, Combined={Combined:F4}, ModelWeight={Weight:F2}",
            adjustedFraudProb, normalizedAnomalyScore, combinedRiskScore, modelWeight);

        // 4. Risk seviyesi kararı (düzeltilmiş mantık)
        if (combinedRiskScore >= thresholds.Critical ||
            (fraudProbability >= thresholds.HighFraud && anomalyScore >= thresholds.HighAnomaly))
        {
            _logger.LogInformation(
                "CRITICAL risk determined: Combined={Combined:F4}, Thresholds=Critical:{Critical:F2}",
                combinedRiskScore, thresholds.Critical);
            return RiskLevel.Critical;
        }

        if (combinedRiskScore >= thresholds.High ||
            fraudProbability >= thresholds.HighFraud ||
            (fraudProbability >= thresholds.MediumFraud && anomalyScore >= thresholds.HighAnomaly))
        {
            _logger.LogInformation(
                "HIGH risk determined: Combined={Combined:F4}, FraudProb={Fraud:F4}, Thresholds=High:{High:F2}",
                combinedRiskScore, fraudProbability, thresholds.High);
            return RiskLevel.High;
        }

        if (combinedRiskScore >= thresholds.Medium ||
            fraudProbability >= thresholds.MediumFraud ||
            anomalyScore >= thresholds.HighAnomaly)
        {
            _logger.LogInformation("MEDIUM risk determined: Combined={Combined:F4}, Thresholds=Medium:{Medium:F2}",
                combinedRiskScore, thresholds.Medium);
            return RiskLevel.Medium;
        }

        _logger.LogInformation("LOW risk determined: Combined={Combined:F4}", combinedRiskScore);
        return RiskLevel.Low;
    }

    /// <summary>
    /// Tutar bazlı risk eşikleri
    /// </summary>
    private RiskThresholds GetRiskThresholds(decimal amount)
    {
        if (amount >= 1_000_000_000m) // Milyar ve üzeri
        {
            return new RiskThresholds
            {
                Critical = 0.65,
                High = 0.55,
                Medium = 0.35,
                HighFraud = 0.50,
                MediumFraud = 0.30,
                HighAnomaly = 2.0
            };
        }
        else if (amount >= 1_000_000m) // Milyon ve üzeri
        {
            return new RiskThresholds
            {
                Critical = 0.70,
                High = 0.60,
                Medium = 0.40,
                HighFraud = 0.55,
                MediumFraud = 0.35,
                HighAnomaly = 2.5
            };
        }
        else if (amount >= 10_000m) // On bin ve üzeri
        {
            return new RiskThresholds
            {
                Critical = 0.75,
                High = 0.65,
                Medium = 0.45,
                HighFraud = 0.60,
                MediumFraud = 0.40,
                HighAnomaly = 3.0
            };
        }
        else // Normal tutarlar
        {
            return new RiskThresholds
            {
                Critical = 0.80,
                High = 0.70,
                Medium = 0.50,
                HighFraud = 0.65,
                MediumFraud = 0.45,
                HighAnomaly = 3.5
            };
        }
    }

    /// <summary>
    /// Geliştirilmiş risk faktörü belirleme
    /// </summary>
    private List<RiskFactor> IdentifyRiskFactorsAdvanced(
        TransactionData data,
        BestPredictionResult bestPrediction,
        ModelPrediction lightGbmPrediction,
        ModelPrediction pcaPrediction)
    {
        var riskFactors = new List<RiskFactor>();
        var mainPrediction = bestPrediction.MainPrediction;

        // Model-based risk factors
        if (mainPrediction.Probability > 0.7)
        {
            riskFactors.Add(new RiskFactor
            {
                Code = "HIGH_ML_FRAUD_PROBABILITY",
                Description =
                    $"ML model indicates high fraud probability ({mainPrediction.Probability:P2}) using {bestPrediction.PrimaryModelType}",
                Confidence = mainPrediction.Probability,
                Severity = mainPrediction.Probability > 0.85 ? RiskLevel.Critical : RiskLevel.High,
                Source = bestPrediction.PrimaryModelType,
                DetectedAt = DateTime.UtcNow
            });
        }

        if (mainPrediction.AnomalyScore > 3.0)
        {
            riskFactors.Add(new RiskFactor
            {
                Code = "HIGH_ANOMALY_SCORE",
                Description = $"Transaction shows high anomaly score ({mainPrediction.AnomalyScore:F2})",
                Confidence = Math.Min(mainPrediction.AnomalyScore / 10.0, 1.0),
                Severity = mainPrediction.AnomalyScore > 5.0 ? RiskLevel.Critical : RiskLevel.High,
                Source = "PCA_Anomaly_Detection",
                DetectedAt = DateTime.UtcNow
            });
        }

        // Amount-based risk factors
        if (data.Amount > 1_000_000m)
        {
            riskFactors.Add(new RiskFactor
            {
                Code = "HIGH_TRANSACTION_AMOUNT",
                Description = $"Very high transaction amount: {data.Amount:C}",
                Confidence = 0.9,
                Severity = data.Amount > 10_000_000m ? RiskLevel.Critical : RiskLevel.High,
                Source = "Amount_Analysis",
                DetectedAt = DateTime.UtcNow
            });
        }

        // Ensemble-specific risk factors
        if (bestPrediction.PrimaryModelType == "Ensemble" && mainPrediction.Metadata != null)
        {
            if (mainPrediction.Metadata.ContainsKey("LightGBM_Probability") &&
                mainPrediction.Metadata.ContainsKey("PCA_Probability"))
            {
                var lgbProb = Convert.ToDouble(mainPrediction.Metadata["LightGBM_Probability"]);
                var pcaProb = Convert.ToDouble(mainPrediction.Metadata["PCA_Probability"]);

                if (Math.Abs(lgbProb - pcaProb) > 0.3)
                {
                    riskFactors.Add(new RiskFactor
                    {
                        Code = "MODEL_DISAGREEMENT",
                        Description =
                            $"Significant disagreement between models (LightGBM: {lgbProb:P2}, PCA: {pcaProb:P2})",
                        Confidence = 0.6,
                        Severity = RiskLevel.Medium,
                        Source = "Ensemble_Analysis",
                        DetectedAt = DateTime.UtcNow
                    });
                }
            }
        }

        // Model fallback risk factors
        if (bestPrediction.ModelSource.Contains("Fallback"))
        {
            riskFactors.Add(new RiskFactor
            {
                Code = "MODEL_FALLBACK_USED",
                Description = $"Primary model unavailable, using fallback: {bestPrediction.ModelSource}",
                Confidence = 0.5,
                Severity = RiskLevel.Low,
                Source = "Model_Health",
                DetectedAt = DateTime.UtcNow
            });
        }

        return riskFactors;
    }

    /// <summary>
    /// Model confidence hesaplama
    /// </summary>
    private double CalculateModelConfidence(BestPredictionResult prediction)
    {
        var baseConfidence = prediction.PrimaryModelType switch
        {
            "Ensemble" => 0.95,
            "Manual_Ensemble" => 0.85,
            "LightGBM" => 0.75,
            "PCA" => 0.65,
            _ => 0.5
        };

        // Extreme değerler daha güvenilir
        var probabilityConfidence =
            prediction.MainPrediction.Probability < 0.2 || prediction.MainPrediction.Probability > 0.8
                ? 1.0
                : 0.7;

        return (baseConfidence + probabilityConfidence) / 2.0;
    }

    /// <summary>
    /// Model bilgi objesi oluştur
    /// </summary>
    private object CreateModelInfo(ModelPrediction prediction, string modelType)
    {
        return new
        {
            ModelType = modelType,
            Probability = prediction.Probability,
            Score = prediction.Score,
            AnomalyScore = prediction.AnomalyScore,
            PredictedLabel = prediction.PredictedLabel,
            IsSuccessful = prediction.IsSuccessful,
            ErrorMessage = prediction.ErrorMessage,
            PredictionTime = prediction.PredictionTime,
            Metadata = prediction.Metadata
        };
    }

// Yardımcı sınıflar
    public class BestPredictionResult
    {
        public ModelPrediction MainPrediction { get; set; }
        public string PrimaryModelType { get; set; }
        public string ModelSource { get; set; }
        public List<string> UsedAlgorithms { get; set; } = new List<string>();
        public Dictionary<string, object> DetailedScores { get; set; } = new Dictionary<string, object>();
    }

    public class RiskThresholds
    {
        public double Critical { get; set; }
        public double High { get; set; }
        public double Medium { get; set; }
        public double HighFraud { get; set; }
        public double MediumFraud { get; set; }
        public double HighAnomaly { get; set; }
    }

    /// <summary>
    /// Features'ları dictionary'e dönüştür
    /// </summary>
    /// <summary>
    /// Features'ları dictionary'e dönüştür - Düzeltilmiş reflection
    /// </summary>
    private Dictionary<string, double> ConvertFeaturesToDictionary(object features)
    {
        var result = new Dictionary<string, double>();

        try
        {
            if (features == null)
            {
                _logger.LogDebug("Features null, boş dictionary döndürülüyor");
                return result;
            }

            _logger.LogDebug("Converting features to dictionary. Type: {Type}", features.GetType().Name);

            // Reflection ile özellikleri çıkar
            var featureType = features.GetType();
            var properties = featureType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                try
                {
                    // Property'nin readable olup olmadığını kontrol et
                    if (!prop.CanRead)
                    {
                        _logger.LogTrace("Property {PropertyName} readable değil, atlanıyor", prop.Name);
                        continue;
                    }

                    // Property'nin parametre alıp almadığını kontrol et (indexer değil mi?)
                    var indexParameters = prop.GetIndexParameters();
                    if (indexParameters.Length > 0)
                    {
                        _logger.LogTrace("Property {PropertyName} indexer, atlanıyor", prop.Name);
                        continue;
                    }

                    // Property tipini kontrol et
                    var propertyType = prop.PropertyType;
                    if (!IsNumericType(propertyType))
                    {
                        _logger.LogTrace("Property {PropertyName} numeric değil ({Type}), atlanıyor",
                            prop.Name, propertyType.Name);
                        continue;
                    }

                    // Property değerini al - parametre olmadan invoke et
                    var value = prop.GetValue(features, null); // null = no index parameters

                    if (value != null)
                    {
                        try
                        {
                            var doubleValue = Convert.ToDouble(value);
                            result[prop.Name] = doubleValue;
                            _logger.LogTrace("Property {PropertyName} eklendi: {Value}", prop.Name, doubleValue);
                        }
                        catch (Exception convertEx)
                        {
                            _logger.LogTrace("Property {PropertyName} double'a çevrilemedi: {Error}",
                                prop.Name, convertEx.Message);
                        }
                    }
                    else
                    {
                        result[prop.Name] = 0.0;
                        _logger.LogTrace("Property {PropertyName} null, 0.0 olarak eklendi", prop.Name);
                    }
                }
                catch (TargetParameterCountException paramEx)
                {
                    _logger.LogWarning("Property {PropertyName} parameter count mismatch: {Error}",
                        prop.Name, paramEx.Message);
                }
                catch (Exception propEx)
                {
                    _logger.LogWarning("Property {PropertyName} işlenirken hata: {Error}",
                        prop.Name, propEx.Message);
                }
            }

            _logger.LogInformation("Features dictionary oluşturuldu: {Count} özellik", result.Count);

            // Önemli feature'ları logla
            if (result.ContainsKey("Amount"))
                _logger.LogDebug("Amount feature: {Amount}", result["Amount"]);
            if (result.ContainsKey("V1"))
                _logger.LogDebug("V1 feature: {V1}", result["V1"]);
            if (result.ContainsKey("V14"))
                _logger.LogDebug("V14 feature: {V14}", result["V14"]);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConvertFeaturesToDictionary sırasında genel hata");

            // Fallback: Bilinen feature'ları manuel ekle
            return CreateFallbackFeaturesDictionary(features);
        }
    }

    /// <summary>
    /// Numeric tip kontrolü
    /// </summary>
    private bool IsNumericType(Type type)
    {
        // Nullable type'ları kontrol et
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = Nullable.GetUnderlyingType(type);
        }

        return type == typeof(double) ||
               type == typeof(float) ||
               type == typeof(decimal) ||
               type == typeof(int) ||
               type == typeof(long) ||
               type == typeof(short) ||
               type == typeof(byte) ||
               type == typeof(uint) ||
               type == typeof(ulong) ||
               type == typeof(ushort) ||
               type == typeof(sbyte);
    }

    /// <summary>
    /// Fallback features dictionary oluştur
    /// </summary>
    private Dictionary<string, double> CreateFallbackFeaturesDictionary(object features)
    {
        var result = new Dictionary<string, double>();

        try
        {
            _logger.LogInformation("Fallback features dictionary oluşturuluyor");

            if (features == null)
            {
                return GetDefaultFeaturesDictionary();
            }

            // Bilinen feature'ları manuel olarak çıkarmaya çalış
            var featureType = features.GetType();

            // Amount kontrolü
            TryGetFeatureValue(features, featureType, "Amount", result);
            TryGetFeatureValue(features, featureType, "Time", result);

            // V feature'ları
            for (int i = 1; i <= 28; i++)
            {
                TryGetFeatureValue(features, featureType, $"V{i}", result);
            }

            // Diğer feature'lar
            TryGetFeatureValue(features, featureType, "AmountLog", result);
            TryGetFeatureValue(features, featureType, "TimeSin", result);
            TryGetFeatureValue(features, featureType, "TimeCos", result);
            TryGetFeatureValue(features, featureType, "DayOfWeek", result);
            TryGetFeatureValue(features, featureType, "HourOfDay", result);

            _logger.LogInformation("Fallback features dictionary tamamlandı: {Count} özellik", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallback features dictionary oluşturulurken hata");
            return GetDefaultFeaturesDictionary();
        }
    }

    /// <summary>
    /// Feature değerini güvenli şekilde almaya çalış
    /// </summary>
    private void TryGetFeatureValue(object features, Type featureType, string featureName,
        Dictionary<string, double> result)
    {
        try
        {
            var property = featureType.GetProperty(featureName, BindingFlags.Public | BindingFlags.Instance);
            if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
            {
                var value = property.GetValue(features, null);
                if (value != null)
                {
                    var doubleValue = Convert.ToDouble(value);
                    result[featureName] = doubleValue;
                    _logger.LogTrace("Feature {FeatureName} başarıyla eklendi: {Value}", featureName, doubleValue);
                }
                else
                {
                    result[featureName] = 0.0;
                    _logger.LogTrace("Feature {FeatureName} null, 0.0 olarak eklendi", featureName);
                }
            }
            else
            {
                result[featureName] = 0.0;
                _logger.LogTrace("Feature {FeatureName} bulunamadı, 0.0 olarak eklendi", featureName);
            }
        }
        catch (Exception ex)
        {
            result[featureName] = 0.0;
            _logger.LogTrace("Feature {FeatureName} alınırken hata: {Error}, 0.0 olarak eklendi", featureName,
                ex.Message);
        }
    }

    /// <summary>
    /// Default features dictionary
    /// </summary>
    private Dictionary<string, double> GetDefaultFeaturesDictionary()
    {
        var result = new Dictionary<string, double>
        {
            ["Amount"] = 0.0,
            ["Time"] = 0.0,
            ["AmountLog"] = 0.0,
            ["TimeSin"] = 0.0,
            ["TimeCos"] = 0.0,
            ["DayOfWeek"] = 0.0,
            ["HourOfDay"] = 12.0 // Default to noon
        };

        // V feature'ları ekle
        for (int i = 1; i <= 28; i++)
        {
            result[$"V{i}"] = 0.0;
        }

        _logger.LogInformation("Default features dictionary oluşturuldu: {Count} özellik", result.Count);
        return result;
    }

    /// <summary>
    /// Feature importance'ı çıkar
    /// </summary>
    private Dictionary<string, double> GetFeatureImportance(ModelPrediction prediction)
    {
        var importance = new Dictionary<string, double>();

        if (prediction.Metadata != null && prediction.Metadata.ContainsKey("FeatureImportance"))
        {
            if (prediction.Metadata["FeatureImportance"] is Dictionary<string, double> featureImportance)
            {
                return featureImportance;
            }
        }

        // Varsayılan feature importance değerleri
        return new Dictionary<string, double>
        {
            ["Amount"] = 0.15,
            ["V1"] = 0.08,
            ["V14"] = 0.12,
            ["Time"] = 0.05
        };
    }

    /// <summary>
    /// V-feature değerlerini analiz et
    /// </summary>
    private void AnalyzeVFeatures(TransactionData data, RiskEvaluation riskEvaluation)
    {
        var vFactors = data?.AdditionalData?.VFactors;
        if (vFactors == null || !vFactors.Any())
            return;

        double extraRiskScore = 0;
        var significantFeatureCount = 0;
        decimal amount = data.Amount;

        // Tutar seviyesine göre eşik ayarlamaları
        Dictionary<string, (float Threshold, double Weight)> criticalVFeaturesAdjusted =
            AdjustVThresholdsByAmount(_criticalVFeatures, amount);

        foreach (var feature in criticalVFeaturesAdjusted)
        {
            var key = feature.Key;
            var threshold = feature.Value.Threshold;
            var weight = feature.Value.Weight;

            if (!vFactors.TryGetValue(key, out var value))
                continue;

            var isAnomalous = false;
            double confidence = 0;

            // Tutar bazlı ölçeklendirme faktörü
            double amountFactor = Math.Min(1.0, Math.Log10((double)amount + 1) / 8.0) * 0.5;
            // Tutar arttıkça ağırlık azalır (çok büyük tutarlar için V değerleri daha az belirleyici)
            double adjustedWeight = weight * (1.0 - amountFactor);

            // V2 ve V11 gibi pozitif yönlü özellikler için farklı kontrol
            if (key == "V2" || key == "V11")
            {
                if (value > threshold)
                {
                    isAnomalous = true;
                    confidence = Math.Min(1.0, Math.Abs((value - threshold) / threshold) * adjustedWeight);

                    riskEvaluation.RiskFactors.Add(RiskFactor.Create(
                        RiskFactorType.ModelFeature,
                        $"V-özelliklerde şüpheli kalıp (${key}): {value:F2} (eşik: {threshold:F2}, anormal YÜKSEKLİK)",
                        confidence));

                    _logger.LogDebug(
                        "V-özellik anomalisi tespit edildi: {Key}={Value}, Eşik={Threshold}, Güven={Confidence}",
                        key, value, threshold, confidence);
                }
            }
            else
            {
                // Negatif yönde anormallik: eşikten KÜÇÜKSE şüpheli
                if (value < threshold)
                {
                    isAnomalous = true;
                    confidence = Math.Min(1.0, Math.Abs((value - threshold) / threshold) * adjustedWeight);

                    riskEvaluation.RiskFactors.Add(RiskFactor.Create(
                        RiskFactorType.ModelFeature,
                        $"V-özelliklerde şüpheli kalıp (${key}): {value:F2} (eşik: {threshold:F2}, anormal DÜŞÜKLÜK)",
                        confidence));

                    _logger.LogDebug(
                        "V-özellik anomalisi tespit edildi: {Key}={Value}, Eşik={Threshold}, Güven={Confidence}",
                        key, value, threshold, confidence);
                }
            }

            if (isAnomalous)
            {
                extraRiskScore += confidence * adjustedWeight;
                significantFeatureCount++;
            }
        }

        if (significantFeatureCount > 0)
        {
            var avgExtraScore = extraRiskScore / significantFeatureCount;

            // Tutar bazlı faktör - büyük tutarlar için V-feature analizini kısmen önemsizleştir
            double amountDiscountFactor = 1.0;
            if (amount > 1_000_000)
            {
                amountDiscountFactor = 0.75;
            }
            else if (amount > 100_000)
            {
                amountDiscountFactor = 0.85;
            }

            avgExtraScore *= amountDiscountFactor;

            // Anormal özellik sayısına ve tutara göre risk seviyesi güncelleme
            if (significantFeatureCount >= 4 && avgExtraScore > 0.6 && riskEvaluation.RiskScore < RiskLevel.Critical)
            {
                riskEvaluation.RiskScore = RiskLevel.Critical;
                riskEvaluation.RiskFactors.Add(RiskFactor.Create(
                    RiskFactorType.ModelFeature,
                    $"İşlem özelliklerinde çoklu kritik risk kalıpları tespit edildi (puan: {avgExtraScore:F2})",
                    Math.Min(1.0, avgExtraScore)));

                _logger.LogInformation("V-özellik analizi: Kritik risk seviyesine yükseltildi (4+ özellik, {Score:F2})",
                    avgExtraScore);
            }
            else if (significantFeatureCount >= 3 && avgExtraScore > 0.6 && riskEvaluation.RiskScore < RiskLevel.High)
            {
                riskEvaluation.RiskScore = RiskLevel.High;
                riskEvaluation.RiskFactors.Add(RiskFactor.Create(
                    RiskFactorType.ModelFeature,
                    $"İşlem özelliklerinde çoklu yüksek risk kalıpları tespit edildi (puan: {avgExtraScore:F2})",
                    Math.Min(1.0, avgExtraScore)));

                _logger.LogInformation("V-özellik analizi: Yüksek risk seviyesine yükseltildi (3+ özellik, {Score:F2})",
                    avgExtraScore);
            }
            else if (significantFeatureCount >= 2 && avgExtraScore > 0.4 && riskEvaluation.RiskScore < RiskLevel.Medium)
            {
                riskEvaluation.RiskScore = RiskLevel.Medium;
                riskEvaluation.RiskFactors.Add(RiskFactor.Create(
                    RiskFactorType.ModelFeature,
                    $"İşlem özelliklerinde dikkat çeken kalıplar tespit edildi (puan: {avgExtraScore:F2})",
                    Math.Min(0.8, avgExtraScore)));

                _logger.LogInformation("V-özellik analizi: Orta risk seviyesine yükseltildi (2+ özellik, {Score:F2})",
                    avgExtraScore);
            }

            // FraudProbability güncelleme - tutar bazlı ağırlık ayarlaması
            double vFeatureWeight = 0.35 * amountDiscountFactor;
            riskEvaluation.FraudProbability =
                riskEvaluation.FraudProbability * (1 - vFeatureWeight) +
                avgExtraScore * vFeatureWeight;

            _logger.LogDebug(
                "V-özellik analizi: Dolandırıcılık olasılığı güncellendi: {NewProb:F4}, V-ağırlık: {Weight:F2}",
                riskEvaluation.FraudProbability, vFeatureWeight);
        }
    }

    /// <summary>
    /// Tutara göre V eşik değerlerini ayarlar
    /// </summary>
    private Dictionary<string, (float Threshold, double Weight)> AdjustVThresholdsByAmount(
        Dictionary<string, (float Threshold, double Weight)> baseThresholds, decimal amount)
    {
        var adjustedThresholds = new Dictionary<string, (float Threshold, double Weight)>();

        // Tutar bazlı ölçeklendirme faktörü (0.0-1.0 aralığında)
        double amountFactor = Math.Min(1.0, Math.Log10((double)amount + 1) / 9.0);

        foreach (var pair in baseThresholds)
        {
            var key = pair.Key;
            var threshold = pair.Value.Threshold;
            var weight = pair.Value.Weight;

            // Yüksek tutarlarda eşikleri arttır (daha toleranslı hale getir)
            if (amount > 1_000_000)
            {
                // Eşik değerini tutara göre ayarla - pozitif/negatif yönlü eşiklere uygun ayarlama
                float adjustedThreshold;
                if (threshold < 0)
                    adjustedThreshold = threshold * (1.0f - (float)(amountFactor * 0.4)); // Negatif eşikleri yukarı çek
                else
                    adjustedThreshold =
                        threshold * (1.0f + (float)(amountFactor * 0.4)); // Pozitif eşikleri daha yukarı çek

                // Ağırlığı tutara göre azalt (büyük tutarlarda V-değerleri daha az belirleyici)
                double adjustedWeight = weight * (1.0 - amountFactor * 0.5);

                adjustedThresholds[key] = (adjustedThreshold, adjustedWeight);

                _logger.LogDebug(
                    "Tutar ayarlı V-eşik: {Key}, Orijinal: {Original}, Ayarlı: {Adjusted}, Ağırlık: {Weight}",
                    key, threshold, adjustedThreshold, adjustedWeight);
            }
            else
            {
                // Normal tutarlarda orijinal eşikleri kullan
                adjustedThresholds[key] = (threshold, weight);
            }
        }

        return adjustedThresholds;
    }

    /// <summary>
    /// ML risk faktörlerini belirle
    /// </summary>
    private List<RiskFactor> IdentifyRiskFactors(
        TransactionData data,
        ModelPrediction lightGbmPrediction,
        ModelPrediction pcaPrediction)
    {
        var factors = new List<RiskFactor>();

        // ML modelinden en etkili özellikleri çıkar
        if (lightGbmPrediction.Metadata != null &&
            lightGbmPrediction.Metadata.TryGetValue("TopFeatures", out var topFeaturesObj))
        {
            string[] topFeatures = topFeaturesObj.ToString().Split(',');
            foreach (var feature in topFeatures)
                factors.Add(RiskFactor.Create(
                    RiskFactorType.ModelFeature,
                    $"Contributing feature: {feature}",
                    lightGbmPrediction.Probability));
        }

        // Eğer anomali skoru önemliyse risk faktörü olarak ekle
        if (pcaPrediction.AnomalyScore > ANOMALY_THRESHOLD)
        {
            var confidence = Math.Min(pcaPrediction.AnomalyScore / (ANOMALY_THRESHOLD * 2), 1.0);
            var severityDesc = confidence > 0.8 ? "Highly unusual" : confidence > 0.5 ? "Very unusual" : "Unusual";

            factors.Add(RiskFactor.Create(
                RiskFactorType.AnomalyDetection,
                $"{severityDesc} transaction pattern detected (score: {pcaPrediction.AnomalyScore:F2})",
                confidence));
        }

        // Tutar ile ilgili riskler
        if (data.Amount > 5000) // Çok yüksek tutarlar
            factors.Add(RiskFactor.Create(
                RiskFactorType.HighValue,
                $"Very high value transaction (${data.Amount})",
                Math.Min((double)data.Amount / 10000, 1.0)));
        else if (data.Amount > 1000) // Yüksek tutarlar
            factors.Add(RiskFactor.Create(
                RiskFactorType.HighValue,
                $"High value transaction (${data.Amount})",
                Math.Min((double)data.Amount / 5000, 0.8)));

        // Lokasyon tabanlı riskler
        if (data.Location != null)
        {
            if (data.Location.IsHighRiskRegion)
                factors.Add(RiskFactor.Create(
                    RiskFactorType.Location,
                    $"Transaction from high-risk region ({data.Location.Country})",
                    0.8));

            // Coğrafi konum kontrolü - Yüksek riskli ülkeler
            string[] highRiskCountries = { "RU", "CN", "NG", "ID", "PK", "UA", "KP", "BY", "VE", "IR" };
            if (highRiskCountries.Contains(data.Location.Country))
                factors.Add(RiskFactor.Create(
                    RiskFactorType.Location,
                    $"Transaction from high-risk country ({data.Location.Country})",
                    0.7));
        }

        // Cihaz tabanlı riskler
        if (data.DeviceInfo != null)
            if (data.DeviceInfo.IpChanged)
                factors.Add(RiskFactor.Create(
                    RiskFactorType.Device,
                    "IP address recently changed",
                    0.6));

        // Zaman tabanlı riskler (gece saatlerinde işlemler daha riskli)
        var hour = data.Timestamp.Hour;
        if (hour >= 0 && hour < 5)
            factors.Add(RiskFactor.Create(
                RiskFactorType.Time,
                $"Transaction during suspicious hours ({hour:D2}:00)",
                0.6));

        return factors;
    }

    /// <summary>
    /// Son kararı belirle (Kural tabanlı + ML tabanlı entegre)
    /// </summary>
    private DecisionType DetermineDecision(
        List<RuleResult> ruleResults,
        RiskEvaluation mlEvaluation,
        decimal amount)
    {
        try
        {
            _logger.LogInformation("Kural sonuçları ve ML değerlendirmesine dayalı nihai karar belirleniyor");
            if (mlEvaluation != null)
            {
                AdjustMLEvaluationByAmount(mlEvaluation, amount);
            }

            // Kural aksiyonlarını uygula
            foreach (var rule in ruleResults.Where(r => r.IsTriggered))
            {
                // Aksiyonu uygula
                ApplyRuleAction(rule.Action, rule.RuleName, rule.RuleId);
            }

            // --------- 1. Tutar Seviyesine Göre Karar Stratejisi -----------

            // Çok büyük tutarlar için farklı karar mekanizması
            bool isVeryHighAmount = amount > 50_000;
            bool isExtremeAmount = amount > 100_000;

            // Ekstrem tutarlar için özel değerlendirme
            if (isExtremeAmount)
            {
                _logger.LogInformation("Ekstrem tutar tespiti: {Amount}. Özel değerlendirme uygulanıyor.", amount);

                // Çok büyük tutarlarda ML değerlendirmesine daha fazla güven
                if (mlEvaluation != null)
                {
                    if (mlEvaluation.FraudProbability > 0.6)
                    {
                        _logger.LogInformation("Ekstrem tutar - Yüksek ML riski: {Prob}",
                            mlEvaluation.FraudProbability);
                        return DecisionType.Deny;
                    }
                    else if (mlEvaluation.FraudProbability > 0.4)
                    {
                        return DecisionType.ReviewRequired;
                    }

                    // Kurallar tetiklense bile ML değerlendirmesi düşükse sadece inceleme gerektir
                    int highRiskRules = ruleResults.Count(r => r.IsTriggered && r.Score >= 75);
                    if (highRiskRules > 0 && mlEvaluation.FraudProbability < 0.3)
                    {
                        _logger.LogInformation("Ekstrem tutar - ML risk düşük, sadece inceleme: {Prob}",
                            mlEvaluation.FraudProbability);
                        return DecisionType.ReviewRequired;
                    }
                }
            }

            // --------- 2. Kural Tabanlı Karar Mekanizması -----------

            // 2.1 - Kritik/Spesifik aksiyonlu kuralların kontrolü
            var blockRule = ruleResults.FirstOrDefault(r =>
                r.IsTriggered && (r.Action == RuleAction.Block ||
                                  r.Action == RuleAction.BlockIP ||
                                  r.Action == RuleAction.BlockDevice ||
                                  r.Action == RuleAction.BlacklistIP ||
                                  r.Action == RuleAction.RejectTransaction));

            if (blockRule != null)
            {
                // Büyük tutarlar için yapısal engelleme kurallarına güvenme, ML değerlendirmesi kullan
                if (isVeryHighAmount && mlEvaluation != null && mlEvaluation.FraudProbability < 0.4)
                {
                    _logger.LogInformation(
                        "Büyük tutar - Engelleme kuralı tetiklendi ancak ML risk düşük: {Rule}, {Prob}",
                        blockRule.RuleName, mlEvaluation.FraudProbability);
                    return DecisionType.ReviewRequired;
                }

                _logger.LogInformation("Engelleme kuralı tetiklendi: {RuleName} - Karar: Deny", blockRule.RuleName);
                return DecisionType.Deny;
            }

            var reviewRule = ruleResults.FirstOrDefault(r =>
                r.IsTriggered && (r.Action == RuleAction.Review ||
                                  r.Action == RuleAction.PutUnderReview ||
                                  r.Action == RuleAction.RequireKYCVerification));

            if (reviewRule != null)
            {
                _logger.LogInformation("İnceleme kuralı tetiklendi: {RuleName} - Karar: ReviewRequired",
                    reviewRule.RuleName);
                return DecisionType.ReviewRequired;
            }

            var escalateRule = ruleResults.FirstOrDefault(r =>
                r.IsTriggered && r.Action == RuleAction.EscalateToManager);

            if (escalateRule != null)
            {
                _logger.LogInformation("Yükseltme kuralı tetiklendi: {RuleName} - Karar: EscalateToManager",
                    escalateRule.RuleName);
                return DecisionType.EscalateToManager;
            }

            // 2.2 - Kural risk seviyesini hesapla
            var ruleBasedRiskLevel = CalculateRuleBasedRiskLevel(ruleResults);

            // --------- 3. Kural-ML Entegrasyonu -----------

            switch (ruleBasedRiskLevel)
            {
                case RiskLevel.Critical:
                    // Büyük tutarlar için ML değerlendirmesini daha çok dikkate al
                    if (isVeryHighAmount && mlEvaluation != null && mlEvaluation.FraudProbability < 0.45)
                    {
                        _logger.LogInformation(
                            "Kritik kural risk seviyesi, ancak büyük tutar ve düşük ML riski - Karar: ReviewRequired");
                        return DecisionType.ReviewRequired;
                    }

                    _logger.LogInformation("Kural tabanlı risk seviyesi Kritik - Karar: Deny");
                    return DecisionType.Deny;

                case RiskLevel.High:
                    // ML değerlendirmesi yüksekse engelle, değilse incele
                    if (mlEvaluation != null && mlEvaluation.FraudProbability > 0.7)
                    {
                        _logger.LogInformation("Yüksek kural riski + Yüksek ML riski - Karar: Deny");
                        return DecisionType.Deny;
                    }

                    _logger.LogInformation("Yüksek kural riski - Karar: ReviewRequired");
                    return DecisionType.ReviewRequired;

                case RiskLevel.Medium:
                    // ML risk seviyesine göre kararı belirle
                    if (mlEvaluation != null)
                    {
                        if (mlEvaluation.FraudProbability > 0.65)
                        {
                            _logger.LogInformation("Orta kural riski + Yüksek ML riski - Karar: ReviewRequired");
                            return DecisionType.ReviewRequired;
                        }
                        else if (mlEvaluation.AnomalyScore > 5.0)
                        {
                            _logger.LogInformation("Orta kural riski + Yüksek anormallik - Karar: ReviewRequired");
                            return DecisionType.ReviewRequired;
                        }
                    }

                    // Tutar yüksekse daha tedbirli ol
                    if (isVeryHighAmount)
                    {
                        _logger.LogInformation("Orta kural riski + Yüksek tutar - Karar: ReviewRequired");
                        return DecisionType.ReviewRequired;
                    }

                    _logger.LogInformation("Orta kural riski - Karar: RequireAdditionalVerification");
                    return DecisionType.RequireAdditionalVerification;

                case RiskLevel.Low:
                default:
                    // 4. ML modelinden ek kontrol
                    if (mlEvaluation != null)
                    {
                        if (mlEvaluation.RiskScore >= RiskLevel.High || mlEvaluation.FraudProbability > 0.75)
                        {
                            _logger.LogInformation("Düşük kural riski, ancak yüksek ML riski - Karar: ReviewRequired");
                            return DecisionType.ReviewRequired;
                        }
                        else if (mlEvaluation.RiskScore == RiskLevel.Medium || mlEvaluation.FraudProbability > 0.5)
                        {
                            // Tutar yüksekse daha tedbirli ol
                            if (isVeryHighAmount)
                            {
                                _logger.LogInformation(
                                    "Düşük kural riski, orta ML riski, yüksek tutar - Karar: ReviewRequired");
                                return DecisionType.ReviewRequired;
                            }

                            _logger.LogInformation(
                                "Düşük kural riski, orta ML riski - Karar: RequireAdditionalVerification");
                            return DecisionType.RequireAdditionalVerification;
                        }
                        else if (mlEvaluation.AnomalyScore > 3.5 && mlEvaluation.FraudProbability > 0.4)
                        {
                            _logger.LogInformation(
                                "Düşük kural riski, yüksek anormallik - Karar: RequireAdditionalVerification");
                            return DecisionType.RequireAdditionalVerification;
                        }
                    }

                    // Tutar yüksekse ek doğrulama iste
                    if (isVeryHighAmount)
                    {
                        _logger.LogInformation("Düşük risk ancak yüksek tutar - Karar: RequireAdditionalVerification");
                        return DecisionType.RequireAdditionalVerification;
                    }

                    // Düşük risk, normal tutar
                    _logger.LogInformation("Hem kural tabanlı hem ML risk seviyeleri düşük - Karar: Approve");
                    return DecisionType.Approve;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Karar belirlenirken hata, güvenlik için ReviewRequired'a dönülüyor");
            return DecisionType.ReviewRequired; // Hata durumunda güvenli tarafta kal
        }
    }

    private void AdjustMLEvaluationByAmount(RiskEvaluation mlEvaluation, decimal amount)
    {
        if (mlEvaluation == null) return;

        // Orijinal olasılığı kaydet
        double originalProbability = mlEvaluation.FraudProbability;

        // Tutara göre olasılığı "spread out" et
        double adjustedProbability;

        // Çok büyük tutarlar
        if (amount > 500_000)
        {
            // Yüksek tutarları daha geniş aralıklı tut (0.75-0.98)
            adjustedProbability = 0.75 + (originalProbability * 0.25);
        }
        else if (amount > 100_000)
        {
            // 100K-500K arası (0.70-0.95)
            adjustedProbability = 0.70 + (originalProbability * 0.25);
        }
        else if (amount > 50_000)
        {
            // 50K-100K arası (0.60-0.90)
            adjustedProbability = 0.60 + (originalProbability * 0.30);
        }
        else if (amount > 10_000)
        {
            // 10K-50K arası (0.40-0.80)
            adjustedProbability = 0.40 + (originalProbability * 0.40);
        }
        else if (amount > 1_000)
        {
            // 1K-10K arası (0.20-0.70)
            adjustedProbability = 0.20 + (originalProbability * 0.50);
        }
        else
        {
            // 1K altı (0.10-0.60)
            adjustedProbability = 0.10 + (originalProbability * 0.50);
        }

        // Rastgele varyasyon ekle (±%5)
        double randomVariation = (new Random().NextDouble() - 0.5) * 0.1;
        adjustedProbability = Math.Min(0.98, Math.Max(0.01, adjustedProbability + randomVariation));

        _logger.LogInformation(
            "ML olasılığı tutara göre düzeltildi: Orijinal={Original:F4}, Tutar={Amount}, Düzeltilmiş={Adjusted:F4}",
            originalProbability, amount, adjustedProbability);

        // ML değerlendirmesini güncelle
        mlEvaluation.FraudProbability = adjustedProbability;
    }

    private async Task ApplyRuleAction(RuleAction action, string ruleName, string ruleId, TransactionData data = null)
    {
        try
        {
            _logger.LogInformation("Applying action {Action} for rule {RuleName}", action, ruleName);

            switch (action)
            {
                case RuleAction.BlacklistIP:
                    if (data?.DeviceInfo?.IpAddress != null)
                    {
                        await _blacklistService.AddIpToBlacklistAsync(
                            data.DeviceInfo.IpAddress,
                            $"Rule triggered: {ruleName} (ID: {ruleId})",
                            TimeSpan.FromDays(7));
                        _logger.LogInformation("IP {IpAddress} blacklisted due to rule: {RuleName}",
                            data.DeviceInfo.IpAddress, ruleName);
                    }

                    break;

                case RuleAction.BlockIP:
                    if (data?.DeviceInfo?.IpAddress != null)
                    {
                        await _blacklistService.AddIpToBlacklistAsync(
                            data.DeviceInfo.IpAddress,
                            $"Rule triggered: {ruleName} (ID: {ruleId})",
                            TimeSpan.FromHours(24)); // Temporary block
                        _logger.LogInformation("IP {IpAddress} temporarily blocked due to rule: {RuleName}",
                            data.DeviceInfo.IpAddress, ruleName);
                    }

                    break;

                case RuleAction.BlockDevice:
                    if (data?.DeviceInfo?.DeviceId != null)
                    {
                        await _blacklistService.AddDeviceToBlacklistAsync(
                            data.DeviceInfo.DeviceId,
                            $"Rule triggered: {ruleName} (ID: {ruleId})",
                            TimeSpan.FromDays(30));
                        _logger.LogInformation("Device {DeviceId} blocked due to rule: {RuleName}",
                            data.DeviceInfo.DeviceId, ruleName);
                    }

                    break;

                case RuleAction.LockAccount:
                    if (data?.UserId != Guid.Empty)
                    {
                        await _blacklistService.AddAccountToBlacklistAsync(
                            data.UserId,
                            $"Rule triggered: {ruleName} (ID: {ruleId})",
                            TimeSpan.FromDays(3));
                        _logger.LogInformation("Account {UserId} locked due to rule: {RuleName}",
                            data.UserId, ruleName);
                    }

                    break;

                case RuleAction.SuspendAccount:
                    if (data?.UserId != Guid.Empty)
                    {
                        await _blacklistService.AddAccountToBlacklistAsync(
                            data.UserId,
                            $"Rule triggered: {ruleName} (ID: {ruleId})",
                            TimeSpan.FromDays(30));
                        _logger.LogInformation("Account {UserId} suspended due to rule: {RuleName}",
                            data.UserId, ruleName);
                    }

                    break;

                case RuleAction.RejectTransaction:
                    // İşlemi reddetme işlemleri
                    _logger.LogInformation("Transaction {TransactionId} rejected due to rule: {RuleName}",
                        data?.TransactionId, ruleName);
                    break;

                case RuleAction.PutUnderReview:
                    // İnceleme için işlemi işaretle
                    _logger.LogInformation("Transaction {TransactionId} put under review due to rule: {RuleName}",
                        data?.TransactionId, ruleName);
                    break;

                case RuleAction.RequireAdditionalVerification:
                    // Ek doğrulama gerektiğini işaretle
                    _logger.LogInformation(
                        "Additional verification required for transaction {TransactionId} due to rule: {RuleName}",
                        data?.TransactionId, ruleName);
                    break;

                case RuleAction.DelayProcessing:
                    // İşlemi geciktirme
                    _logger.LogInformation("Processing delayed for transaction {TransactionId} due to rule: {RuleName}",
                        data?.TransactionId, ruleName);
                    break;

                case RuleAction.RequireKYCVerification:
                    // KYC doğrulaması gerektir
                    _logger.LogInformation("KYC verification required for account {UserId} due to rule: {RuleName}",
                        data?.UserId, ruleName);
                    break;

                case RuleAction.EscalateToManager:
                    // Yöneticiye yükselt
                    _logger.LogInformation("Transaction {TransactionId} escalated to manager due to rule: {RuleName}",
                        data?.TransactionId, ruleName);
                    break;

                case RuleAction.Log:
                    // Sadece loglama
                    _logger.LogInformation("Rule {RuleName} triggered for transaction {TransactionId} - only logging",
                        ruleName, data?.TransactionId);
                    break;

                default:
                    _logger.LogWarning("Unknown action {Action} for rule {RuleName}", action, ruleName);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply action {Action} for rule {RuleId}", action, ruleId);
        }
    }

    /// <summary>
    /// Kural tabanlı risk seviyesini hesapla
    /// </summary>
    private RiskLevel CalculateRuleBasedRiskLevel(List<RuleResult> ruleResults)
    {
        if (ruleResults == null || !ruleResults.Any(r => r.IsTriggered))
            return RiskLevel.Low;

        // Sadece tetiklenenleri al
        var triggered = ruleResults
            .Where(r => r.IsTriggered)
            .ToList();

        // En yüksek skoru al (0–100)
        var maxScore = triggered.Max(r => r.Score);

        // Kritik sayılabilecek aksiyonlar
        var hasBlockIp = triggered.Any(r => r.Action == RuleAction.BlockIP);
        var hasBlockDevice = triggered.Any(r => r.Action == RuleAction.BlockDevice);
        var hasBlacklistIp = triggered.Any(r => r.Action == RuleAction.BlacklistIP);

        // Yüksek öneme sahip aksiyonlar
        var hasLockAccount = triggered.Any(r => r.Action == RuleAction.LockAccount);
        var hasRequireKyc = triggered.Any(r => r.Action == RuleAction.RequireKYCVerification);
        var hasReview = triggered.Any(r => r.Action == RuleAction.PutUnderReview);

        // Öncelikle kritik bloklama/karaliste varsa hemen Critical
        if (hasBlockIp || hasBlacklistIp || hasBlockDevice || maxScore >= 90)
            return RiskLevel.Critical;

        // Sonra yüksek öneme sahipse veya skor 75+
        if (hasLockAccount || hasRequireKyc || hasReview || maxScore >= 75)
            return RiskLevel.High;

        // Orta seviye: skor 50+
        if (maxScore >= 50)
            return RiskLevel.Medium;

        // Aksi halde düşük
        return RiskLevel.Low;
    }

    /// <summary>
    /// Fraud alarmı oluştur
    /// </summary>
    private async Task<FraudAlert> CreateFraudAlertAsync(
        TransactionData data,
        AnalysisResult analysisResult,
        IEnumerable<RuleResult> ruleResults)
    {
        try
        {
            _logger.LogInformation("Dolandırıcılık uyarısı oluşturuluyor: {TransactionId}", data.TransactionId);

            // 1. Tetiklenen kurallardan faktör listesi oluştur
            var factors = ruleResults
                .Where(r => r.IsTriggered)
                .Select(r => $"Rule: {r.RuleName} ({r.Score:F0})")
                .ToList();

            // 2. ML risk faktörlerini ekle
            factors.AddRange(analysisResult.RiskFactors.Select(rf => $"ML: {rf.Description}"));

            // 3. AnalysisResult üzerinden FraudAlert oluştur
            var alert = analysisResult.CreateFraudAlert(data.UserId, factors);

            // 4. Fraud Alert'ı veritabanına kaydet
            await _alertRepository.AddAsync(alert);

            // 5. AnalysisResult'ı güncelle (FraudAlert ilişkisi kuruldu)
            await _analysisRepository.UpdateAsync(analysisResult);

            // 6. Yüksek risk durumunda blacklist işlemleri
            if (analysisResult.Decision == DecisionType.Deny)
            {
                // Cihaz kara listeye ekleme
                if (data.DeviceInfo != null && !string.IsNullOrEmpty(data.DeviceInfo.DeviceId))
                {
                    await _blacklistService.AddDeviceToBlacklistAsync(
                        data.DeviceInfo.DeviceId,
                        $"Yüksek riskli dolandırıcılık işlemi tespit edildi: {analysisResult.RiskScore}",
                        TimeSpan.FromDays(30));

                    _logger.LogInformation("Yüksek riskli işlem nedeniyle cihaz kara listeye alındı: {DeviceId}",
                        data.DeviceInfo.DeviceId);
                }

                // IP adresi kara listeye ekleme
                if (data.DeviceInfo != null && !string.IsNullOrEmpty(data.DeviceInfo.IpAddress))
                {
                    await _blacklistService.AddIpToBlacklistAsync(
                        data.DeviceInfo.IpAddress,
                        $"Yüksek riskli dolandırıcılık işlemi tespit edildi: {analysisResult.RiskScore}",
                        TimeSpan.FromDays(7));

                    _logger.LogInformation("Yüksek riskli işlem nedeniyle IP adresi kara listeye alındı: {IpAddress}",
                        data.DeviceInfo.IpAddress);
                }
            }

            _logger.LogInformation("Dolandırıcılık uyarısı başarıyla oluşturuldu: {TransactionId}",
                data.TransactionId);

            return alert;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dolandırıcılık uyarısı oluşturulurken hata: {TransactionId}", data.TransactionId);
            throw;
        }
    }

    private RiskLevel ConvertConfidenceToSeverity(double confidence)
    {
        return confidence switch
        {
            >= 0.85 => RiskLevel.Critical,
            >= 0.6 => RiskLevel.High,
            >= 0.4 => RiskLevel.Medium,
            _ => RiskLevel.Low
        };
    }

    /// <summary>
    /// Transaction oluştur
    /// </summary>
    private Transaction CreateTransactionFromRequest(TransactionRequest request)
    {
        // Location nesnesini oluştur
        var location = Location.Create(
            request.Location.Latitude,
            request.Location.Longitude,
            request.Location.Country,
            request.Location.City,
            false);

        // Yüksek riskli ülkeleri kontrol et
        string[] highRiskCountries = { "RU", "CN", "NG", "ID", "PK", "UA", "KP", "BY", "VE", "IR" };
        if (highRiskCountries.Contains(request.Location.Country))
            location = Location.Create(
                request.Location.Latitude,
                request.Location.Longitude,
                request.Location.Country,
                request.Location.City,
                true);

        // DeviceInfo nesnesini oluştur
        var deviceInfo = new DeviceInfo
        {
            DeviceId = request.DeviceInfo.DeviceId,
            IpAddress = request.DeviceInfo.IpAddress,
            // Diğer DeviceInfo alanları...
            IpChanged = DetermineIfIpChanged(request.UserId, request.DeviceInfo.IpAddress)
        };

        // Transaction oluştur
        return Transaction.Create(
            request.UserId.ToString(),
            request.Amount,
            request.MerchantId,
            DateTime.UtcNow,
            request.Type,
            location,
            deviceInfo);
    }

    // IP değişikliği kontrolü
    private bool DetermineIfIpChanged(Guid userId, string currentIp)
    {
        // Gerçek uygulamada veritabanı sorgusu yapılacak
        return false;
    }

    // TransactionData nesnesine dönüşüm
    private TransactionData MapToTransactionData(Transaction transaction)
    {
        return new TransactionData
        {
            TransactionId = transaction.Id,
            UserId = Guid.Parse(transaction.UserId),
            Amount = transaction.Amount,
            MerchantId = transaction.MerchantId,
            Timestamp = transaction.TransactionTime,
            Type = transaction.Type,
            Location = transaction.Location,
            DeviceInfo = transaction.DeviceInfo,
            AdditionalData = transaction.AdditionalData != null
                ? transaction.AdditionalData
                : new TransactionAdditionalData()
        };
    }
}