using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Application.DTOs.Response;

/// <summary>
/// Kapsamlı dolandırıcılık kontrolü yanıtı
/// </summary>
public class ComprehensiveFraudCheckResponse
{
    /// <summary>
    /// İşlem kontrolü sonucu
    /// </summary>
    public FraudDetectionResponse TransactionCheck { get; set; }

    /// <summary>
    /// Hesap erişimi kontrolü sonucu
    /// </summary>
    public FraudDetectionResponse AccountCheck { get; set; }

    /// <summary>
    /// IP adresi kontrolü sonucu
    /// </summary>
    public FraudDetectionResponse IpCheck { get; set; }

    /// <summary>
    /// Cihaz kontrolü sonucu
    /// </summary>
    public FraudDetectionResponse DeviceCheck { get; set; }

    /// <summary>
    /// Oturum kontrolü sonucu
    /// </summary>
    public FraudDetectionResponse SessionCheck { get; set; }

    /// <summary>
    /// ML modeli değerlendirme sonucu
    /// </summary>
    public ModelEvaluationResponse ModelEvaluation { get; set; }

    /// <summary>
    /// Genel sonuç tipi
    /// </summary>
    public FraudDetectionResultType OverallResultType { get; set; }

    /// <summary>
    /// Genel risk puanı
    /// </summary>
    public int OverallRiskScore { get; set; }

    /// <summary>
    /// Genel risk seviyesi
    /// </summary>
    public string OverallRiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Dolandırıcılık olasılığı
    /// </summary>
    public double FraudProbability { get; set; }

    /// <summary>
    /// Anomali skoru
    /// </summary>
    public double AnomalyScore { get; set; }

    /// <summary>
    /// Karar
    /// </summary>
    public string Decision { get; set; } = string.Empty;

    /// <summary>
    /// Toplam tetiklenen kural sayısı
    /// </summary>
    public int TotalTriggeredRules { get; set; }

    /// <summary>
    /// İşlem süresi (ms)
    /// </summary>
    public int ProcessingTime { get; set; }

    /// <summary>
    /// Context sonuçları
    /// </summary>
    public Dictionary<string, object> ContextResults { get; set; } = new();

    /// <summary>
    /// ML analiz sonucu
    /// </summary>
    public object? MLAnalysis { get; set; }

    /// <summary>
    /// Genel aksiyon listesi
    /// </summary>
    public List<RuleAction> OverallActions { get; set; } = new();

    /// <summary>
    /// Genel sonuç mesajı
    /// </summary>
    public string OverallResultMessage { get; set; } = string.Empty;

    /// <summary>
    /// Başarılı mı?
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Aksiyon gerekli mi?
    /// </summary>
    public bool RequiresAction { get; set; }
}