using Analiz.Domain.ValueObjects;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Domain.Entities;

public class TransactionData
{
    public Guid TransactionId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string MerchantId { get; set; }
    public DateTime Timestamp { get; set; }
    public TransactionType Type { get; set; }
    public Location Location { get; set; }
    public DeviceInfo DeviceInfo { get; set; }
    public TransactionAdditionalData AdditionalData { get; set; }
    public bool IsFraudulent { get; set; }
}