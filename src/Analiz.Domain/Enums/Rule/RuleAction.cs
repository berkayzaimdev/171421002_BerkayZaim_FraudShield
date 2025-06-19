namespace FraudShield.TransactionAnalysis.Domain.Enums.Rule;

/// <summary>
/// Kural aksiyonları
/// </summary>
public enum RuleAction
{
    /// <summary>
    /// Sadece loglama yapılır
    /// </summary>
    Log,

    /// <summary>
    /// Bildirim gönderilir
    /// </summary>
    Notify,

    /// <summary>
    /// İlave doğrulama istenir (OTP, Email, vb.)
    /// </summary>
    RequireAdditionalVerification,

    /// <summary>
    /// İşlemi belirli bir süre geciktir
    /// </summary>
    DelayProcessing,

    /// <summary>
    /// İşlemi incelemeye al (manuel onay gerektirir)
    /// </summary>
    PutUnderReview,

    /// <summary>
    /// İşlemi reddet
    /// </summary>
    RejectTransaction,

    /// <summary>
    /// Oturumu sonlandır
    /// </summary>
    TerminateSession,

    /// <summary>
    /// Hesabı geçici olarak kilitler
    /// </summary>
    LockAccount,

    /// <summary>
    /// Hesabı belirli bir süre askıya alır
    /// </summary>
    SuspendAccount,

    /// <summary>
    /// Hesabı KYC doğrulama statüsüne çeker
    /// </summary>
    RequireKYCVerification,

    /// <summary>
    /// Cihazı engeller
    /// </summary>
    BlockDevice,

    /// <summary>
    /// IP adresini engeller
    /// </summary>
    BlockIP,

    /// <summary>
    /// IP adresini kara listeye alır (daha kalıcı)
    /// </summary>
    BlacklistIP,
    Block,
    Review,
    EscalateToManager
}