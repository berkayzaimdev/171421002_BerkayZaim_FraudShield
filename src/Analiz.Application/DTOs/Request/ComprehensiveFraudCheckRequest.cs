namespace Analiz.Application.DTOs.Request;

/// <summary>
/// Kapsamlı dolandırıcılık kontrolü isteği
/// </summary>
public class ComprehensiveFraudCheckRequest
{
    /// <summary>
    /// İşlem kontrolü verileri
    /// </summary>
    public TransactionCheckRequest Transaction { get; set; }

    /// <summary>
    /// Hesap erişimi verileri
    /// </summary>
    public AccountAccessCheckRequest Account { get; set; }

    /// <summary>
    /// IP adresi verileri
    /// </summary>
    public IpCheckRequest IpAddress { get; set; }

    /// <summary>
    /// Cihaz verileri
    /// </summary>
    public DeviceCheckRequest Device { get; set; }

    /// <summary>
    /// Oturum verileri
    /// </summary>
    public SessionCheckRequest Session { get; set; }

    /// <summary>
    /// ML modeli değerlendirme isteği
    /// </summary>
    public ModelEvaluationRequest ModelEvaluation { get; set; }
}