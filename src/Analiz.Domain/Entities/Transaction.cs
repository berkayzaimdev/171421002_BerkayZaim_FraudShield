using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Transactions;
using Analiz.Domain.Events;
using Analiz.Domain.ValueObjects;
using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums;
using TransactionStatus = FraudShield.TransactionAnalysis.Domain.Enums.TransactionStatus;

namespace Analiz.Domain.Entities;

public class Transaction : Entity
{
    public string UserId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime TransactionTime { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public TransactionDetails? Details { get; private set; }
    public RiskScore? RiskScore { get; private set; }

    // Pointer kullanımı (ayrıca _flags var) yerine doğrudan private liste kullanımı
    private List<TransactionFlag> _flags = new();

    [NotMapped] public IReadOnlyCollection<TransactionFlag> Flags => _flags.AsReadOnly();

    public string? FlagsJson
    {
        get => _flags.Any() ? JsonSerializer.Serialize(_flags) : null;
        private set
        {
            _flags.Clear();
            if (!string.IsNullOrEmpty(value))
            {
                var deserialized = JsonSerializer.Deserialize<List<TransactionFlag>>(value);
                if (deserialized != null) _flags.AddRange(deserialized);
            }
        }
    }

    public string MerchantId { get; private set; }
    public DeviceInfo? DeviceInfo { get; private set; }
    public Location? Location { get; private set; }

    // Navigation Properties - İlişkiler
    public ICollection<AnalysisResult> AnalysisResults { get; private set; } = new List<AnalysisResult>();
    public ICollection<RiskEvaluation> RiskEvaluations { get; private set; } = new List<RiskEvaluation>();

    // Standartlaştırılmış ek veri
    [NotMapped] 
    public string? AdditionalDataJson { get; private set; }

    [NotMapped] public TransactionAdditionalData AdditionalData { get; private set; }

    private Transaction()
    {
        _flags = new List<TransactionFlag>();
        AdditionalData = new TransactionAdditionalData();
    }

    public static Transaction Create(
        string userId,
        decimal amount,
        string merchantId,
        DateTime timestamp,
        TransactionType type,
        Location location,
        DeviceInfo deviceInfo,
        TransactionAdditionalData additionalData = null)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(), // Backend'de ID oluşturma
            UserId = userId,
            Amount = amount,
            TransactionTime = timestamp,
            Type = type,
            Status = TransactionStatus.Pending,
            MerchantId = merchantId,
            Location = location,
            DeviceInfo = deviceInfo,
            AdditionalData = additionalData ?? new TransactionAdditionalData()
        };

        transaction.AdditionalDataJson = JsonSerializer.Serialize(transaction.AdditionalData);
        return transaction;
    }

    public void SetAdditionalData(TransactionAdditionalData data)
    {
        AdditionalData = data ?? new TransactionAdditionalData();
        AdditionalDataJson = JsonSerializer.Serialize(AdditionalData);
    }

    public void SetRiskScore(RiskScore score)
    {
        RiskScore = score;
        Status = DetermineStatus(score);
        AddDomainEvent(new TransactionRiskScoreUpdatedEvent(Id, score));
    }

    public void UpdateStatus(TransactionStatus newStatus)
    {
        if (Status != newStatus)
        {
            Status = newStatus;
            AddDomainEvent(new TransactionStatusUpdatedEvent(Id, newStatus));
        }
    }

    public void AddFlag(TransactionFlag flag)
    {
        _flags.Add(flag);
        FlagsJson = JsonSerializer.Serialize(_flags);
    }

    public void RemoveFlag(TransactionFlag flag)
    {
        _flags.Remove(flag);
        FlagsJson = JsonSerializer.Serialize(_flags);
    }

    public void ClearFlags()
    {
        _flags.Clear();
        FlagsJson = JsonSerializer.Serialize(_flags);
    }

    private TransactionStatus DetermineStatus(RiskScore score)
    {
        return score.Level switch
        {
            RiskLevel.Critical => TransactionStatus.Blocked,
            RiskLevel.High => TransactionStatus.RequiresReview,
            _ => TransactionStatus.Approved
        };
    }
}