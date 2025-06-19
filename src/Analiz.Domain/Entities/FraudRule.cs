using FraudShield.TransactionAnalysis.Domain.Common;
using FraudShield.TransactionAnalysis.Domain.Enums.Rule;

namespace Analiz.Domain.Entities;

/// <summary>
/// Fraud tespiti için kullanılan kurallar
/// </summary>
public class FraudRule : Entity
{
    /// <summary>
    /// Kuralın tekil kodu (UI'da görüntülenecek)
    /// </summary>
    public string RuleCode { get;  set; }

    /// <summary>
    /// Kural adı
    /// </summary>
    public string Name { get;  set; }

    /// <summary>
    /// Kural açıklaması
    /// </summary>
    public string Description { get;  set; }

    /// <summary>
    /// Kural kategorisi (Ağ, IP, Hesap, Cihaz, Oturum vs.)
    /// </summary>
    public RuleCategory Category { get;  set; }

    /// <summary>
    /// Kural tipi
    /// </summary>
    public RuleType Type { get;  set; }

    /// <summary>
    /// Kural etki seviyesi
    /// </summary>
    public ImpactLevel ImpactLevel { get;  set; }

    /// <summary>
    /// Kural durumu (Aktif, Pasif, Test Modu)
    /// </summary>
    public RuleStatus Status { get; set; }

    /// <summary>
    /// Kural tetiklendiğinde yapılacak aksiyonlar (Birden fazla olabilir)
    /// </summary>
    public List<RuleAction> Actions { get;  set; }

    /// <summary>
    /// Aksiyon süresi (null ise süresiz)
    /// </summary>
    public TimeSpan? ActionDuration { get;  set; }

    /// <summary>
    /// Kuralın geçerlilik başlangıç tarihi
    /// </summary>
    public DateTime? ValidFrom { get;  set; }

    /// <summary>
    /// Kuralın geçerlilik bitiş tarihi
    /// </summary>
    public DateTime? ValidTo { get;  set; }

    /// <summary>
    /// Kuralın önceliği (düşük değerler daha öncelikli)
    /// </summary>
    public int Priority { get;  set; }

    /// <summary>
    /// Kural koşul ifadesi - karmaşık kurallar için
    /// </summary>
    public string Condition { get;  set; }

    /// <summary>
    /// Kuralın JSON formatındaki detaylı konfigürasyonu
    /// </summary>
    public string ConfigurationJson { get;  set; }

    /// <summary>
    /// Son değişiklik tarihi
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Değişikliği yapan kişi/sistem
    /// </summary>
    public string ModifiedBy { get; set; }

    /// <summary>
    /// Kural oluşturma factory metodu
    /// </summary>
    public static FraudRule Create(
        string name,
        string description,
        RuleCategory category,
        RuleType type,
        ImpactLevel impactLevel,
        List<RuleAction> actions,
        TimeSpan? actionDuration,
        int priority,
        string condition,
        string configurationJson,
        string createdBy)
    {
        var rule = new FraudRule
        {
            Id = Guid.NewGuid(),
            RuleCode = GenerateRuleCode(category, name),
            Name = name,
            Description = description,
            Category = category,
            Type = type,
            ImpactLevel = impactLevel,
            Status = RuleStatus.Draft, // Başlangıçta taslak olarak oluşturulur
            Actions = actions ?? new List<RuleAction>(),
            ActionDuration = actionDuration,
            Priority = priority,
            Condition = condition,
            ConfigurationJson = configurationJson,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            ModifiedBy = createdBy,
            CreatedBy = "Yasin"
        };

        return rule;
    }

    /// <summary>
    /// Kuralı aktifleştir
    /// </summary>
    public void Activate(string modifiedBy)
    {
        Status = RuleStatus.Active;
        LastModified = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Kuralı deaktif et
    /// </summary>
    public void Deactivate(string modifiedBy)
    {
        Status = RuleStatus.Inactive;
        LastModified = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Kuralı test moduna al
    /// </summary>
    public void SetTestMode(string modifiedBy)
    {
        Status = RuleStatus.TestMode;
        LastModified = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Kural konfigürasyonunu güncelle
    /// </summary>
    public void UpdateConfiguration(
        string name,
        string description,
        RuleCategory category,
        RuleType type,
        ImpactLevel impactLevel,
        List<RuleAction> actions,
        TimeSpan? actionDuration,
        int priority,
        string condition,
        string configurationJson,
        DateTime? validFrom,
        DateTime? validTo,
        string modifiedBy)
    {
        Name = name;
        Description = description;
        Category = category;
        Type = type;
        ImpactLevel = impactLevel;
        Actions = actions ?? new List<RuleAction>();
        ActionDuration = actionDuration;
        Priority = priority;
        Condition = condition;
        ConfigurationJson = configurationJson;
        ValidFrom = validFrom;
        ValidTo = validTo;
        LastModified = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
    }

    /// <summary>
    /// Kural kodu oluştur
    /// </summary>
    /// <summary>
    /// Kural kodu oluştur
    /// </summary>
    private static string GenerateRuleCode(RuleCategory category, string name)
    {
        // 1) Kategori kodunu al, 3 harfe kes ama eğer kısa ise olduğu gibi kullan
        var cat = category.ToString().ToUpperInvariant();
        var catPrefix = cat.Length > 3 ? cat.Substring(0, 3) : cat;

        // 2) Türkçe karakter düzeltmesi
        var normalizedName = name
            .Replace("ı", "i").Replace("İ", "I")
            .Replace("ğ", "g").Replace("Ğ", "G")
            .Replace("ü", "u").Replace("Ü", "U")
            .Replace("ş", "s").Replace("Ş", "S")
            .Replace("ö", "o").Replace("Ö", "O")
            .Replace("ç", "c").Replace("Ç", "C");

        // 3) Sadece harf/rakam ve boşluk bırak
        var cleanName = new string(normalizedName
            .Where(c => char.IsLetterOrDigit(c) || c == ' ')
            .ToArray());

        // 4) Kelime baş harflerinden kısaltma oluştur
        var abbreviation = string.Join("",
                cleanName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s[0]))
            .ToUpperInvariant();

        if (abbreviation.Length > 5)
            abbreviation = abbreviation.Substring(0, 5);

        // 5) Eğer yine boş kaldıysa rastgele 5 haneli ID ata
        if (string.IsNullOrWhiteSpace(abbreviation))
            abbreviation = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();

        // 6) Zaman damgası (yyMMddHHmmss)
        var timestamp = DateTime.UtcNow.ToString("yyMMddHHmmss");

        return $"{catPrefix}_{abbreviation}_{timestamp}";
    }

}