using Microsoft.ML.Data;

namespace Analiz.Domain.Entities.ML;

/// <summary>
/// Model tahmini için girdi verisi - Geliştirilmiş feature engineering ile
/// </summary>
public class ModelInput
{
    // Temel özellikler
    public float Time { get; set; }
    public float Amount { get; set; }

    // Derived features - Time-based
    public float TimeSin { get; set; }
    public float TimeCos { get; set; }
    public float DayFeature { get; set; }
    public float HourFeature { get; set; }
    
    // Derived features - Amount-based
    public float AmountLog { get; set; }

    // V1-V28 özellikleri (PCA transformed features)
    public float V1 { get; set; }
    public float V2 { get; set; }
    public float V3 { get; set; }
    public float V4 { get; set; }
    public float V5 { get; set; }
    public float V6 { get; set; }
    public float V7 { get; set; }
    public float V8 { get; set; }
    public float V9 { get; set; }
    public float V10 { get; set; }
    public float V11 { get; set; }
    public float V12 { get; set; }
    public float V13 { get; set; }
    public float V14 { get; set; }
    public float V15 { get; set; }
    public float V16 { get; set; }
    public float V17 { get; set; }
    public float V18 { get; set; }
    public float V19 { get; set; }
    public float V20 { get; set; }
    public float V21 { get; set; }
    public float V22 { get; set; }
    public float V23 { get; set; }
    public float V24 { get; set; }
    public float V25 { get; set; }
    public float V26 { get; set; }
    public float V27 { get; set; }
    public float V28 { get; set; }

    /// <summary>
    /// Derived feature'ları hesapla
    /// </summary>
    public void CalculateDerivedFeatures()
    {
        // Amount-based features
        AmountLog = (float)Math.Log(1 + Amount);

        // Time-based features
        if (Time > 0)
        {
            const float secondsInDay = 24 * 60 * 60;
            
            // Sinusoidal encoding for cyclical time features
            TimeSin = (float)Math.Sin(2 * Math.PI * Time / secondsInDay);
            TimeCos = (float)Math.Cos(2 * Math.PI * Time / secondsInDay);
            
            // Day of week (0-6)
            DayFeature = (float)((Time / secondsInDay) % 7);
            
            // Hour of day (0-23)
            HourFeature = (float)((Time / 3600) % 24);
        }
    }

    /// <summary>
    /// TransactionData'dan ModelInput oluşturur - Geliştirilmiş feature engineering ile
    /// </summary>
    public static ModelInput FromTransactionData(TransactionData transaction)
    {
        var input = new ModelInput
        {
            Amount = (float)transaction.Amount
        };

        // Timestamp'i Time'a dönüştür (saniye cinsinden)
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        input.Time = (float)(transaction.Timestamp - epoch).TotalSeconds;

        // V değerlerini AdditionalData'dan al
        if (transaction.AdditionalData?.VFactors != null)
        {
            PopulateVFactors(input, transaction.AdditionalData.VFactors);
        }
        else
        {
            // V faktörleri yoksa sıfırla (güvenli fallback)
            SetDefaultVFactors(input);
        }

        // Derived features'ları hesapla
        input.CalculateDerivedFeatures();

        return input;
    }

    /// <summary>
    /// PCAModelInput'dan ModelInput'a dönüştürücü - Geliştirilmiş
    /// </summary>
    public static ModelInput FromPCAModelInput(PCAModelInput pcaInput)
    {
        var input = new ModelInput
        {
            Amount = pcaInput.Amount,
            Time = pcaInput.Time,
            
            // V değerlerini kopyala
            V1 = pcaInput.V1, V2 = pcaInput.V2, V3 = pcaInput.V3, V4 = pcaInput.V4,
            V5 = pcaInput.V5, V6 = pcaInput.V6, V7 = pcaInput.V7, V8 = pcaInput.V8,
            V9 = pcaInput.V9, V10 = pcaInput.V10, V11 = pcaInput.V11, V12 = pcaInput.V12,
            V13 = pcaInput.V13, V14 = pcaInput.V14, V15 = pcaInput.V15, V16 = pcaInput.V16,
            V17 = pcaInput.V17, V18 = pcaInput.V18, V19 = pcaInput.V19, V20 = pcaInput.V20,
            V21 = pcaInput.V21, V22 = pcaInput.V22, V23 = pcaInput.V23, V24 = pcaInput.V24,
            V25 = pcaInput.V25, V26 = pcaInput.V26, V27 = pcaInput.V27, V28 = pcaInput.V28
        };

        // Eğer derived feature'lar zaten hesaplanmışsa onları kullan
        if (pcaInput.TimeSin != 0 || pcaInput.TimeCos != 0)
        {
            input.TimeSin = pcaInput.TimeSin;
            input.TimeCos = pcaInput.TimeCos;
            input.DayFeature = pcaInput.DayFeature;
            input.HourFeature = pcaInput.HourFeature;
            input.AmountLog = pcaInput.AmountLog;
        }
        else
        {
            // Yoksa hesapla
            input.CalculateDerivedFeatures();
        }

        return input;
    }

    /// <summary>
    /// Test/demo amaçlı minimal input oluştur
    /// </summary>
    public static ModelInput CreateSample(decimal amount, DateTime? timestamp = null, Dictionary<string, float>? vFactors = null)
    {
        var time = timestamp ?? DateTime.UtcNow;
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        var input = new ModelInput
        {
            Amount = (float)amount,
            Time = (float)(time - epoch).TotalSeconds
        };

        // V faktörleri varsa kullan, yoksa default değerler
        if (vFactors != null)
        {
            PopulateVFactors(input, vFactors);
        }
        else
        {
            SetDefaultVFactors(input);
        }

        // Derived features'ları hesapla
        input.CalculateDerivedFeatures();

        return input;
    }

    /// <summary>
    /// Feature validation - modele gönderilmeden önce kontrol
    /// </summary>
    public ValidationResult ValidateFeatures()
    {
        var result = new ValidationResult();

        // Temel validasyonlar
        if (Amount < 0)
        {
            result.AddError("Amount cannot be negative");
        }

        if (Time < 0)
        {
            result.AddError("Time cannot be negative");
        }

        // NaN ve Infinity kontrolleri
        var properties = typeof(ModelInput).GetProperties()
            .Where(p => p.PropertyType == typeof(float))
            .ToList();

        foreach (var prop in properties)
        {
            var value = (float)prop.GetValue(this);
            
            if (float.IsNaN(value))
            {
                result.AddError($"Property {prop.Name} is NaN");
            }
            else if (float.IsInfinity(value))
            {
                result.AddError($"Property {prop.Name} is Infinity");
            }
        }

        // V değerleri için aşırı değer kontrolü
        var vProperties = properties.Where(p => p.Name.StartsWith("V") && p.Name.Length <= 3).ToList();
        foreach (var vProp in vProperties)
        {
            var value = (float)vProp.GetValue(this);
            if (Math.Abs(value) > 100) // V değerleri genelde -10 ile +10 arasında
            {
                result.AddWarning($"V feature {vProp.Name} has extreme value: {value}");
            }
        }

        // Derived feature'lar hesaplanmış mı?
        if (Amount > 0 && AmountLog == 0)
        {
            result.AddWarning("AmountLog not calculated, call CalculateDerivedFeatures()");
        }

        if (Time > 0 && TimeSin == 0 && TimeCos == 0)
        {
            result.AddWarning("Time features not calculated, call CalculateDerivedFeatures()");
        }

        return result;
    }

    /// <summary>
    /// Feature'ları dictionary'ye dönüştür (debugging/logging için)
    /// </summary>
    public Dictionary<string, float> ToFeatureDictionary()
    {
        var features = new Dictionary<string, float>();
        
        var properties = typeof(ModelInput).GetProperties()
            .Where(p => p.PropertyType == typeof(float))
            .ToList();

        foreach (var prop in properties)
        {
            features[prop.Name] = (float)prop.GetValue(this);
        }

        return features;
    }

    /// <summary>
    /// V faktörlerini dictionary'den doldur
    /// </summary>
    private static void PopulateVFactors(ModelInput input, Dictionary<string, float> vFactors)
    {
        input.V1 = vFactors.GetValueOrDefault("V1", 0);
        input.V2 = vFactors.GetValueOrDefault("V2", 0);
        input.V3 = vFactors.GetValueOrDefault("V3", 0);
        input.V4 = vFactors.GetValueOrDefault("V4", 0);
        input.V5 = vFactors.GetValueOrDefault("V5", 0);
        input.V6 = vFactors.GetValueOrDefault("V6", 0);
        input.V7 = vFactors.GetValueOrDefault("V7", 0);
        input.V8 = vFactors.GetValueOrDefault("V8", 0);
        input.V9 = vFactors.GetValueOrDefault("V9", 0);
        input.V10 = vFactors.GetValueOrDefault("V10", 0);
        input.V11 = vFactors.GetValueOrDefault("V11", 0);
        input.V12 = vFactors.GetValueOrDefault("V12", 0);
        input.V13 = vFactors.GetValueOrDefault("V13", 0);
        input.V14 = vFactors.GetValueOrDefault("V14", 0);
        input.V15 = vFactors.GetValueOrDefault("V15", 0);
        input.V16 = vFactors.GetValueOrDefault("V16", 0);
        input.V17 = vFactors.GetValueOrDefault("V17", 0);
        input.V18 = vFactors.GetValueOrDefault("V18", 0);
        input.V19 = vFactors.GetValueOrDefault("V19", 0);
        input.V20 = vFactors.GetValueOrDefault("V20", 0);
        input.V21 = vFactors.GetValueOrDefault("V21", 0);
        input.V22 = vFactors.GetValueOrDefault("V22", 0);
        input.V23 = vFactors.GetValueOrDefault("V23", 0);
        input.V24 = vFactors.GetValueOrDefault("V24", 0);
        input.V25 = vFactors.GetValueOrDefault("V25", 0);
        input.V26 = vFactors.GetValueOrDefault("V26", 0);
        input.V27 = vFactors.GetValueOrDefault("V27", 0);
        input.V28 = vFactors.GetValueOrDefault("V28", 0);
    }

    /// <summary>
    /// Default V faktörleri (güvenli fallback)
    /// </summary>
    private static void SetDefaultVFactors(ModelInput input)
    {
        // Tüm V değerlerini 0 yap
        input.V1 = input.V2 = input.V3 = input.V4 = input.V5 = input.V6 = input.V7 = 
        input.V8 = input.V9 = input.V10 = input.V11 = input.V12 = input.V13 = input.V14 = 
        input.V15 = input.V16 = input.V17 = input.V18 = input.V19 = input.V20 = input.V21 = 
        input.V22 = input.V23 = input.V24 = input.V25 = input.V26 = input.V27 = input.V28 = 0;
    }

    /// <summary>
    /// Feature özeti string'e dönüştür
    /// </summary>
    public override string ToString()
    {
        return $"ModelInput: Amount={Amount:F2}, Time={Time:F0}, " +
               $"Features=[AmountLog:{AmountLog:F3}, TimeSin:{TimeSin:F3}, TimeCos:{TimeCos:F3}], " +
               $"V-Summary=[V1:{V1:F2}, V2:{V2:F2}, V14:{V14:F2}]";
    }
}

/// <summary>
/// Validation sonucu
/// </summary>
public class ValidationResult
{
    public List<string> Errors { get; set; } = new List<string>();
    public List<string> Warnings { get; set; } = new List<string>();
    
    public bool IsValid => Errors.Count == 0;
    public bool HasWarnings => Warnings.Count > 0;

    public void AddError(string error)
    {
        Errors.Add(error);
    }

    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }

    public override string ToString()
    {
        var result = $"Valid: {IsValid}";
        if (Errors.Count > 0)
        {
            result += $", Errors: {string.Join(", ", Errors)}";
        }
        if (Warnings.Count > 0)
        {
            result += $", Warnings: {string.Join(", ", Warnings)}";
        }
        return result;
    }
}

/// <summary>
/// PCA Model Input - Geliştirilmiş
/// </summary>
public class PCAModelInput
{
    // Raw değerler
    public float Amount { get; set; }
    public float Time { get; set; }

    // V1-V28 değerleri
    public float V1 { get; set; }
    public float V2 { get; set; }
    public float V3 { get; set; }
    public float V4 { get; set; }
    public float V5 { get; set; }
    public float V6 { get; set; }
    public float V7 { get; set; }
    public float V8 { get; set; }
    public float V9 { get; set; }
    public float V10 { get; set; }
    public float V11 { get; set; }
    public float V12 { get; set; }
    public float V13 { get; set; }
    public float V14 { get; set; }
    public float V15 { get; set; }
    public float V16 { get; set; }
    public float V17 { get; set; }
    public float V18 { get; set; }
    public float V19 { get; set; }
    public float V20 { get; set; }
    public float V21 { get; set; }
    public float V22 { get; set; }
    public float V23 { get; set; }
    public float V24 { get; set; }
    public float V25 { get; set; }
    public float V26 { get; set; }
    public float V27 { get; set; }
    public float V28 { get; set; }

    // Pipeline'ın FeatureColumns listesinde yer alan türetilmiş sütunlar
    [ColumnName("AmountLog")] public float AmountLog { get; set; }
    [ColumnName("TimeSin")] public float TimeSin { get; set; }
    [ColumnName("TimeCos")] public float TimeCos { get; set; }
    [ColumnName("DayFeature")] public float DayFeature { get; set; }
    [ColumnName("HourFeature")] public float HourFeature { get; set; }

    /// <summary>
    /// ModelInput'a dönüştürücü
    /// </summary>
    public ModelInput ToModelInput()
    {
        return ModelInput.FromPCAModelInput(this);
    }

    /// <summary>
    /// Derived feature'ları hesapla (PCA için)
    /// </summary>
    public void CalculateDerivedFeatures()
    {
        // Amount-based features
        AmountLog = (float)Math.Log(1 + Amount);

        // Time-based features
        if (Time > 0)
        {
            const float secondsInDay = 24 * 60 * 60;
            
            TimeSin = (float)Math.Sin(2 * Math.PI * Time / secondsInDay);
            TimeCos = (float)Math.Cos(2 * Math.PI * Time / secondsInDay);
            DayFeature = (float)((Time / secondsInDay) % 7);
            HourFeature = (float)((Time / 3600) % 24);
        }
    }
}