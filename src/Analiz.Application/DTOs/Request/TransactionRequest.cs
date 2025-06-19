using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.DTOs.Request;

public class TransactionRequest
{
    // ID backend tarafında otomatik oluşturulacak

    // Temel işlem bilgileri
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string MerchantId { get; set; }
    public TransactionType Type { get; set; }

    // Lokasyon bilgileri
    public LocationRequest Location { get; set; }

    // Cihaz bilgileri
    public DeviceInfoRequest DeviceInfo { get; set; }

    // Standartlaştırılmış ek bilgiler
    public TransactionAdditionalDataRequest AdditionalDataRequest { get; set; }
}