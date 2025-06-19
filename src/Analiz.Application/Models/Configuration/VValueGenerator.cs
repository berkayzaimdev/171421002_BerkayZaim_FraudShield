namespace Analiz.Application.Models.Configuration;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

public class VValueGenerator
{
    private readonly Random _random;
    private readonly ILogger<VValueGenerator> _logger;

    // Fraud örüntüleri profilleri - gerçek fraud işlemler için tipik V değeri örüntüleri
    private readonly Dictionary<string, Dictionary<string, (float baseLine, float multiplier)>> _fraudProfiles;

    // Farklı tutar aralıkları için risk çarpanları
    private readonly Dictionary<(decimal min, decimal max), double> _amountRiskMultipliers;

    // Farklı işlem tipleri için risk profilleri
    private readonly Dictionary<string, double> _transactionTypeRisks;

    public VValueGenerator(ILogger<VValueGenerator> logger = null)
    {
        _random = new Random(Guid.NewGuid().GetHashCode());
        _logger = logger;

        // Farklı fraud profilleri tanımla (örneğin: kart hırsızlığı, kimlik bilgisi hırsızlığı, vb.)
        _fraudProfiles = InitializeFraudProfiles();

        // Tutar aralıkları için risk çarpanları
        _amountRiskMultipliers = new Dictionary<(decimal min, decimal max), double>
        {
            { (0, 100), 0.15 },
            { (100, 500), 0.25 },
            { (500, 1_000), 0.35 },
            { (1_000, 5_000), 0.45 },
            { (5_000, 10_000), 0.55 },
            { (10_000, 50_000), 0.65 },
            { (50_000, 100_000), 0.75 },
            { (100_000, 500_000), 0.85 },
            { (500_000, decimal.MaxValue), 0.95 }
        };

        // İşlem tiplerine göre risk faktörleri
        _transactionTypeRisks = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            { "transfer", 0.85 },
            { "wire", 0.80 },
            { "withdrawal", 0.70 },
            { "payment", 0.50 },
            { "purchase", 0.40 },
            { "deposit", 0.20 },
            { "refund", 0.15 },
            { "default", 0.50 }
        };
    }

    // Fraud profilleri için başlangıç değerleri
    private Dictionary<string, Dictionary<string, (float baseLine, float multiplier)>> InitializeFraudProfiles()
    {
        return new Dictionary<string, Dictionary<string, (float, float)>>
        {
            // Kart çalıntı profili
            {
                "STOLEN_CARD", new Dictionary<string, (float, float)>
                {
                    { "V1", (-3.5f, 1.8f) }, // V1 değeri düşük = Yüksek risk
                    { "V3", (-4.2f, 1.5f) },
                    { "V4", (-2.3f, 1.2f) },
                    { "V10", (-4.0f, 1.7f) },
                    { "V14", (-6.5f, 2.0f) }, // En önemli
                    { "V2", (3.8f, 1.5f) }, // V2 değeri yüksek = Yüksek risk
                    { "V11", (3.0f, 1.3f) }
                }
            },

            // Kimlik hırsızlığı profili
            {
                "IDENTITY_THEFT", new Dictionary<string, (float, float)>
                {
                    { "V1", (-2.8f, 1.3f) },
                    { "V3", (-3.5f, 1.2f) },
                    { "V4", (-1.8f, 1.0f) },
                    { "V10", (-3.2f, 1.4f) },
                    { "V14", (-5.0f, 1.8f) },
                    { "V2", (2.7f, 1.2f) },
                    { "V11", (2.3f, 1.1f) }
                }
            },

            // Hesap ele geçirme profili
            {
                "ACCOUNT_TAKEOVER", new Dictionary<string, (float, float)>
                {
                    { "V1", (-3.2f, 1.5f) },
                    { "V3", (-3.8f, 1.4f) },
                    { "V4", (-2.1f, 1.1f) },
                    { "V10", (-3.6f, 1.6f) },
                    { "V14", (-5.8f, 1.9f) },
                    { "V2", (3.2f, 1.4f) },
                    { "V11", (2.7f, 1.2f) }
                }
            }
        };
    }

    // Tutar ve zamana bağlı V değerleri üret
    public Dictionary<string, float> GenerateVValuesFromAmountAndTime(
        decimal amount,
        DateTime timestamp,
        string transactionType = "Default",
        string userSegment = "regular",
        bool randomizeFraudProfile = true)
    {
        try
        {
            var result = new Dictionary<string, float>();

            // 1. Temel risk faktörü hesapla
            double baseRiskFactor = CalculateRiskFactorFromAmount(amount);

            // 2. Zaman bazlı risk faktörü ekle
            double timeRiskFactor = CalculateRiskFactorFromTime(timestamp);

            // 3. İşlem tipi riski ekle
            double typeRiskFactor = CalculateRiskFactorFromType(transactionType);

            // 4. Kullanıcı segmenti ayarlaması
            double segmentAdjustment = GetSegmentAdjustment(userSegment);

            // 5. Toplam risk faktörü hesapla (ağırlıklı ortalama)
            double totalRiskFactor = (baseRiskFactor * 0.55) +
                                     (timeRiskFactor * 0.25) +
                                     (typeRiskFactor * 0.15) +
                                     (segmentAdjustment * 0.05);

            // Risk faktörünü 0.05 ile 0.95 arasında sınırla
            totalRiskFactor = Math.Min(0.95, Math.Max(0.05, totalRiskFactor));

            // 6. Fraud profili seç
            string fraudProfile = SelectFraudProfile(transactionType, randomizeFraudProfile);

            // 7. V değerlerini seçilen fraud profiline göre oluştur
            GenerateVValuesFromProfile(result, fraudProfile, totalRiskFactor);

            // 8. Eksik kalan V değerlerini oluştur
            FillRemainingVValues(result);

            // 9. Loglama yap
            LogGeneratedValues(result, amount, timestamp, transactionType, totalRiskFactor, fraudProfile);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "V değerleri üretilirken hata oluştu: {Error}", ex.Message);

            // Hata durumunda basit rastgele değerler döndür
            return GenerateDefaultVValues();
        }
    }

    // Fraud profilinden V değerlerini üret
    private void GenerateVValuesFromProfile(
        Dictionary<string, float> result,
        string fraudProfile,
        double riskFactor)
    {
        if (!_fraudProfiles.TryGetValue(fraudProfile, out var profile))
        {
            profile = _fraudProfiles["STOLEN_CARD"]; // Varsayılan profil
        }

        foreach (var item in profile)
        {
            string featureName = item.Key;
            var (baseLine, multiplier) = item.Value;

            // Risk faktörü ve baseline değerine göre V değerini hesapla
            float baseValue = baseLine * (float)riskFactor;

            // Rastgele varyasyon ekle (daha gerçekçi dağılım için)
            double randomizer = (_random.NextDouble() - 0.5) * 0.4;
            float finalValue = baseValue + (float)(randomizer * multiplier);

            result[featureName] = finalValue;
        }
    }

    // Kalan V değerlerini doldur
    private void FillRemainingVValues(Dictionary<string, float> result)
    {
        for (int i = 1; i <= 28; i++)
        {
            string key = $"V{i}";
            if (!result.ContainsKey(key))
            {
                // Normal dağılıma yakın rastgele değerler (Box-Muller Transformasyonu)
                double u1 = 1.0 - _random.NextDouble();
                double u2 = 1.0 - _random.NextDouble();
                double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

                // Standart normal değeri -1 ile 1 arasında ölçekle
                float randValue = (float)(randStdNormal * 0.3);

                result[key] = randValue;
            }
        }
    }

    // Varsayılan V değerlerini oluştur
    private Dictionary<string, float> GenerateDefaultVValues()
    {
        var defaults = new Dictionary<string, float>();

        for (int i = 1; i <= 28; i++)
        {
            string key = $"V{i}";
            float randomVal = (float)((_random.NextDouble() - 0.5) * 0.6);
            defaults[key] = randomVal;
        }

        return defaults;
    }

    // İşlem tipine uygun fraud profili seç
    private string SelectFraudProfile(string transactionType, bool randomize)
    {
        if (!randomize)
        {
            // İşlem tipine göre belirli profil seç
            switch (transactionType.ToLower())
            {
                case "transfer":
                case "wire":
                    return "ACCOUNT_TAKEOVER";

                case "withdrawal":
                    return "STOLEN_CARD";

                default:
                    return "IDENTITY_THEFT";
            }
        }
        else
        {
            // Rastgele profil seç
            string[] profiles = _fraudProfiles.Keys.ToArray();
            int index = _random.Next(profiles.Length);
            return profiles[index];
        }
    }

    // Tutara göre risk faktörü
    private double CalculateRiskFactorFromAmount(decimal amount)
    {
        foreach (var range in _amountRiskMultipliers)
        {
            if (amount >= range.Key.min && amount < range.Key.max)
            {
                return range.Value;
            }
        }

        return 0.5; // Varsayılan değer
    }

    // Zamana göre risk faktörü
    private double CalculateRiskFactorFromTime(DateTime timestamp)
    {
        int hour = timestamp.Hour;
        DayOfWeek day = timestamp.DayOfWeek;

        // Temel zaman risk faktörü
        double timeRisk;

        // Saat bazlı risk
        if (hour >= 0 && hour < 5) timeRisk = 0.90; // Gece yarısı-sabah 5
        else if (hour >= 5 && hour < 8) timeRisk = 0.75; // Sabah erken
        else if (hour >= 8 && hour < 18) timeRisk = 0.40; // İş saatleri
        else if (hour >= 18 && hour < 22) timeRisk = 0.60; // Akşam
        else timeRisk = 0.80; // Gece

        // Haftasonu ayarlaması
        if (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
        {
            timeRisk *= 1.2; // Haftasonları daha riskli
        }

        // Ayın son günlerinde risk artışı (maaş günleri)
        if (timestamp.Day >= 25 || timestamp.Day <= 5)
        {
            timeRisk *= 1.1;
        }

        return Math.Min(0.95, timeRisk);
    }

    // İşlem tipine göre risk faktörü
    private double CalculateRiskFactorFromType(string transactionType)
    {
        if (string.IsNullOrEmpty(transactionType))
        {
            return _transactionTypeRisks["default"];
        }

        if (_transactionTypeRisks.TryGetValue(transactionType.ToLower(), out double risk))
        {
            return risk;
        }

        return _transactionTypeRisks["default"];
    }

    // Kullanıcı segmentine göre risk ayarlaması
    private double GetSegmentAdjustment(string segment)
    {
        return segment.ToLower() switch
        {
            "vip" => -0.15, // VIP müşteriler daha az riskli
            "business" => -0.10, // İş hesapları daha az riskli
            "new" => 0.20, // Yeni hesaplar daha riskli
            "suspicious" => 0.30, // Şüpheli hesaplar çok daha riskli
            _ => 0.0 // Standart hesaplar için ayarlama yok
        };
    }

    // Loglama fonksiyonu
    private void LogGeneratedValues(
        Dictionary<string, float> values,
        decimal amount,
        DateTime timestamp,
        string transactionType,
        double riskFactor,
        string fraudProfile)
    {
        if (_logger == null) return;

        // Önemli V değerlerini logla
        var keyVValues = new StringBuilder();
        var keysToLog = new[] { "V1", "V2", "V3", "V4", "V10", "V14", "V11" };

        foreach (var key in keysToLog)
        {
            if (values.TryGetValue(key, out float val))
            {
                keyVValues.Append($"{key}={val:F4}, ");
            }
        }

        _logger.LogInformation(
            "Generated V values for Amount={Amount}, Time={Time}, Type={Type}, " +
            "RiskFactor={Risk:F2}, Profile={Profile} | Key V values: {VValues}",
            amount, timestamp.ToString("HH:mm"), transactionType,
            riskFactor, fraudProfile, keyVValues.ToString().TrimEnd(',', ' '));
    }
}