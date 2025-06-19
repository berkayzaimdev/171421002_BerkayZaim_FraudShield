namespace Analiz.Domain.Entities.RuleConfigurations;

/// <summary>
/// İşlem Tabanlı kural konfigürasyonu
/// </summary>
public class TransactionBasedRuleConfig
{
    /// <summary>
    /// Bu zaman dilimi içinde (saniye)
    /// </summary>
    public int TimeWindowSeconds { get; set; }

    /// <summary>
    /// Maksimum izin verilen işlem tutarı
    /// </summary>
    public decimal MaxTransactionAmount { get; set; }

    /// <summary>
    /// Maksimum izin verilen toplam işlem tutarı
    /// </summary>
    public decimal MaxTotalAmount { get; set; }

    /// <summary>
    /// Maksimum izin verilen işlem sayısı
    /// </summary>
    public int MaxTransactionCount { get; set; }

    /// <summary>
    /// Kullanıcı ortalamasının maksimum katı
    /// </summary>
    public decimal MaxMultipleOfAverage { get; set; }

    /// <summary>
    /// İzin verilen işlem tipleri
    /// </summary>
    public List<string> AllowedTransactionTypes { get; set; }

    /// <summary>
    /// İzin verilen alıcı ülkeler
    /// </summary>
    public List<string> AllowedRecipientCountries { get; set; }
}