using System.Text.Json.Serialization;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities.ML;

public class TrainingRequest
{
    /// <summary>
    /// Model adı
    /// </summary>
    public string ModelName { get; set; }
        
    /// <summary>
    /// Model tipi (LightGBM, PCA, Ensemble)
    /// </summary>
    public ModelType ModelType { get; set; }
        
    /// <summary>
    /// Model konfigürasyonu (JSON formatında)
    /// </summary>
    public string Configuration { get; set; }
        
    /// <summary>
    /// Veri seti yolu (opsiyonel)
    /// Belirtilmezse varsayılan CSV kullanılır
    /// </summary>
    public string DatasetPath { get; set; }
        
    /// <summary>
    /// Test veri seti oranı (0-1 arası)
    /// </summary>
    public float TestDataRatio { get; set; } = 0.2f;
        
    /// <summary>
    /// Eğitim parametreleri
    /// </summary>
    public Dictionary<string, object> TrainingParameters { get; set; }
        
    /// <summary>
    /// Temel özellikler listesi
    /// </summary>
    [JsonIgnore]
    public List<string> BaseFeatureColumns { get; set; } = new List<string>
    {
        "Amount", "Time", 
        "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9", "V10",
        "V11", "V12", "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20",
        "V21", "V22", "V23", "V24", "V25", "V26", "V27", "V28"
    };
        
    public TrainingRequest()
    {
        // Varsayılan değerler
        TrainingParameters = new Dictionary<string, object>();
    }
}