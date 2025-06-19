using Analiz.Domain.ValueObjects;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.DTOs.Request;

public class TransactionAnalysisRequest
{
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string MerchantId { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public Location Location { get; set; } = new();
    public DeviceInfo DeviceInfo { get; set; } = new();
    public TransactionAdditionalData AdditionalData { get; set; } = new();
}

public class TransactionContextAnalysisRequest
{
    public Guid TransactionId { get; set; }
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public TransactionType TransactionType { get; set; }
    public DateTime TransactionDate { get; set; }
    public Guid? RecipientAccountId { get; set; }
    public string RecipientAccountNumber { get; set; } = string.Empty;
    public string RecipientCountry { get; set; } = string.Empty;
    public int UserTransactionCount24h { get; set; }
    public decimal UserTotalAmount24h { get; set; }
    public decimal UserAverageTransactionAmount { get; set; }
    public int DaysSinceFirstTransaction { get; set; }
    public int UniqueRecipientCount1h { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class AccountContextAnalysisRequest
{
    public Guid TransactionId { get; set; }
    public Guid AccountId { get; set; }
    public string Username { get; set; } = string.Empty;
    public DateTime AccessDate { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public bool IsTrustedDevice { get; set; }
    public int UniqueIpCount24h { get; set; }
    public int UniqueCountryCount24h { get; set; }
    public bool IsSuccessful { get; set; }
    public int FailedLoginAttempts { get; set; }
    public List<int> TypicalAccessHours { get; set; } = new();
    public List<string> TypicalAccessDays { get; set; } = new();
    public List<string> TypicalCountries { get; set; } = new();
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class IpContextAnalysisRequest
{
    public Guid TransactionId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string IspAsn { get; set; } = string.Empty;
    public double ReputationScore { get; set; }
    public bool IsBlacklisted { get; set; }
    public string BlacklistNotes { get; set; } = string.Empty;
    public bool IsDatacenterOrProxy { get; set; }
    public string NetworkType { get; set; } = string.Empty;
    public int UniqueAccountCount10m { get; set; }
    public int UniqueAccountCount1h { get; set; }
    public int UniqueAccountCount24h { get; set; }
    public int FailedLoginCount10m { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class DeviceContextAnalysisRequest
{
    public Guid TransactionId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public bool IsEmulator { get; set; }
    public bool IsJailbroken { get; set; }
    public bool IsRooted { get; set; }
    public DateTime? FirstSeenDate { get; set; }
    public DateTime? LastSeenDate { get; set; }
    public int UniqueAccountCount24h { get; set; }
    public int UniqueIpCount24h { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class SessionContextAnalysisRequest
{
    public Guid TransactionId { get; set; }
    public Guid SessionId { get; set; }
    public Guid AccountId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime LastActivityTime { get; set; }
    public int DurationMinutes { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public int RapidNavigationCount { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class MLModelEvaluationRequest
{
    public Guid TransactionId { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public TransactionType TransactionType { get; set; }
    public Dictionary<string, string> Features { get; set; } = new();
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

public class ComprehensiveAnalysisRequest
{
    public Guid TransactionId { get; set; }
    public TransactionCheckRequest Transaction { get; set; } = new();
    public AccountAccessCheckRequest Account { get; set; } = new();
    public IpCheckRequest IpAddress { get; set; } = new();
    public DeviceCheckRequest Device { get; set; } = new();
    public SessionCheckRequest Session { get; set; } = new();
    public ModelEvaluationRequest ModelEvaluation { get; set; } = new();
} 