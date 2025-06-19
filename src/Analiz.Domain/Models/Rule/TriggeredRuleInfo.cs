namespace Analiz.Domain.Models;

/// <summary>
/// Tetiklenen kural bilgisi
/// </summary>
public class TriggeredRuleInfo
{
    /// <summary>
    /// Kural ID'si
    /// </summary>
    public Guid RuleId { get; set; }

    /// <summary>
    /// Kural kodu
    /// </summary>
    public string RuleCode { get; set; }

    /// <summary>
    /// Kural adı
    /// </summary>
    public string RuleName { get; set; }

    /// <summary>
    /// Tetiklenme skoru
    /// </summary>
    public double TriggerScore { get; set; }

    /// <summary>
    /// Tetiklenme detayları
    /// </summary>
    public string TriggerDetails { get; set; }
    public string ActionTaken { get; set; } // Hangi aksiyon alındı
    public string RuleCategory { get; set; } // Kural kategorisi
    public Guid? EventId { get; set; } // Oluşturulan event ID'si
    public List<string> RelatedFactors { get; set; } = new List<string>(); 
}