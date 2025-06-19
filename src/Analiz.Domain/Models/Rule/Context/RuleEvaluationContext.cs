namespace Analiz.Domain.Entities.Rule.Context;

/// <summary>
/// Kural değerlendirme bağlamı
/// </summary>
public class RuleEvaluationContext
{
    /// <summary>
    /// İşlem bağlamı
    /// </summary>
    public TransactionContext Transaction { get; set; }

    /// <summary>
    /// Hesap bağlamı
    /// </summary>
    public AccountAccessContext Account { get; set; }

    /// <summary>
    /// Oturum bağlamı
    /// </summary>
    public SessionContext Session { get; set; }

    /// <summary>
    /// Cihaz bağlamı
    /// </summary>
    public DeviceContext Device { get; set; }

    /// <summary>
    /// IP adresi bağlamı
    /// </summary>
    public IpAddressContext IpAddress { get; set; }

    /// <summary>
    /// Değerlendirme zamanı
    /// </summary>
    public DateTime EvaluationTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Test modu mu?
    /// </summary>
    public bool IsTestMode { get; set; }
}