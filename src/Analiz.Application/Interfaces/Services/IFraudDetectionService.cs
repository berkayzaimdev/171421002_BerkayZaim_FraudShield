using Analiz.Application.DTOs.Request;
using Analiz.Application.DTOs.Response;
using Analiz.Domain.Entities;

namespace Analiz.Application.Interfaces;

/// <summary>
/// Dolandırıcılık tespiti servisi arayüzü
/// </summary>
public interface IFraudDetectionService
{
    /// <summary>
    /// İşlemi analiz et (ML + Kural tabanlı hibrit yaklaşım)
    /// </summary>
    Task<AnalysisResult> AnalyzeTransactionAsync(TransactionRequest request);

    /// <summary>
    /// Sadece ML modeliyle risk değerlendirmesi yap
    /// </summary>
    Task<ModelEvaluationResponse> EvaluateWithModelOnlyAsync(ModelEvaluationRequest request);

    /// <summary>
    /// Tüm kontrolleri yapan kapsamlı dolandırıcılık kontrolü
    /// </summary>
    Task<ComprehensiveFraudCheckResponse> PerformComprehensiveCheckAsync(ComprehensiveFraudCheckRequest request);

    /// <summary>
    /// İşlem kontrolü yap
    /// </summary>
    Task<FraudDetectionResponse> CheckTransactionAsync(TransactionCheckRequest request);

    /// <summary>
    /// Hesap erişimi kontrolü yap
    /// </summary>
    Task<FraudDetectionResponse> CheckAccountAccessAsync(AccountAccessCheckRequest request);

    /// <summary>
    /// IP adresi kontrolü yap
    /// </summary>
    Task<FraudDetectionResponse> CheckIpAddressAsync(IpCheckRequest request);

    /// <summary>
    /// Oturum kontrolü yap
    /// </summary>
    Task<FraudDetectionResponse> CheckSessionAsync(SessionCheckRequest request);

    /// <summary>
    /// Cihaz kontrolü yap
    /// </summary>
    Task<FraudDetectionResponse> CheckDeviceAsync(DeviceCheckRequest request);
}