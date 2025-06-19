namespace Analiz.Domain.ValueObjects;

public class TransactionAdditionalData
{
    public string CardType { get; set; }
    public string CardBin { get; set; }
    public string CardLast4 { get; set; }
    public int? CardExpiryMonth { get; set; }
    public int? CardExpiryYear { get; set; }

    // Banka bilgileri
    public string BankName { get; set; }
    public string BankCountry { get; set; }

    // V-Faktör Değerleri (Kaggle veri setine benzer)
    public Dictionary<string, float> VFactors { get; set; } = new();

    // Diğer önemli fraud inceleme faktörleri
    public int? DaysSinceFirstTransaction { get; set; }
    public int? TransactionVelocity24h { get; set; }
    public decimal? AverageTransactionAmount { get; set; }
    public bool? IsNewPaymentMethod { get; set; }
    public bool? IsInternational { get; set; }

    // Diğer ekstra veri noktaları
    public Dictionary<string, string> CustomValues { get; set; } = new();
}