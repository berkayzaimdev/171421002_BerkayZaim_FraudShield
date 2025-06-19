using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.DTOs.Response;

/// <summary>
/// ML modeli değerlendirme yanıtı
/// </summary>
public class ModelEvaluationResponse
{
    /// <summary>
    /// İşlem ID'si
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Model adı
    /// </summary>
    public string ModelName { get; set; } = string.Empty;

    /// <summary>
    /// Model versiyonu
    /// </summary>
    public string ModelVersion { get; set; } = string.Empty;

    /// <summary>
    /// Dolandırıcılık olasılığı (0-1 arası)
    /// </summary>
    public double FraudProbability { get; set; }

    /// <summary>
    /// Anomali skoru
    /// </summary>
    public double AnomalyScore { get; set; }

    /// <summary>
    /// Risk seviyesi
    /// </summary>
    public RiskLevel RiskLevel { get; set; }

    /// <summary>
    /// Güven skoru
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Ensemble kullanıldı mı?
    /// </summary>
    public bool EnsembleUsed { get; set; }

    /// <summary>
    /// İşlem süresi (ms)
    /// </summary>
    public int ProcessingTime { get; set; }

    /// <summary>
    /// Feature önem dereceleri
    /// </summary>
    public Dictionary<string, double> FeatureImportance { get; set; } = new();

    /// <summary>
    /// Model metrikleri
    /// </summary>
    public Dictionary<string, double> ModelMetrics { get; set; } = new();

    /// <summary>
    /// Risk faktörleri
    /// </summary>
    public List<RiskFactorInfo> RiskFactors { get; set; } = new();

    /// <summary>
    /// Başarılı mı?
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Hata mesajı (varsa)
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
}