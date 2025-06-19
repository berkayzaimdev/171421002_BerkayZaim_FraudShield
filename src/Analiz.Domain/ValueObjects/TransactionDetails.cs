using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using FraudShield.TransactionAnalysis.Domain.Common;

namespace Analiz.Domain.Entities;

public class TransactionDetails : ValueObject
{
    public string Description { get; private set; }
    public string Category { get; private set; }
    public string Currency { get; private set; }

    [NotMapped] public Dictionary<string, string> CustomData { get; private set; }

    // JSON property'si
    public string CustomDataJson
    {
        get => CustomData != null ? JsonSerializer.Serialize(CustomData) : null;
        private set => CustomData = !string.IsNullOrEmpty(value)
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(value)
            : new Dictionary<string, string>();
    }

    private TransactionDetails()
    {
        CustomData = new Dictionary<string, string>();
    }

    public static TransactionDetails Create(
        string description,
        string category,
        string currency,
        Dictionary<string, string> customData = null)
    {
        var details = new TransactionDetails
        {
            Description = description,
            Category = category,
            Currency = currency,
            CustomData = customData ?? new Dictionary<string, string>()
        };

        details.CustomDataJson = JsonSerializer.Serialize(details.CustomData);
        return details;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Description;
        yield return Category;
        yield return Currency;
        foreach (var item in CustomData.OrderBy(x => x.Key))
        {
            yield return item.Key;
            yield return item.Value;
        }
    }
}