namespace Analiz.ML.Models.PCA;

public class PCAConfiguration
{
    public int ComponentCount { get; set; } = 15;
    public double ExplainedVarianceThreshold { get; set; } = 0.98;
    public bool StandardizeInput { get; set; } = true;
    public double AnomalyThreshold { get; set; } = 2.5;

    public double MinAmount { get; set; } = 0;
    public double MaxAmount { get; set; } = 25000;
    public double TimeScaleFactor { get; set; } = 24 * 60 * 60;

    public List<string> FeatureColumns { get; set; }
    public Dictionary<string, double> FeatureThresholds { get; set; }

    public PCAConfiguration()
    {
        // Burada etkileşim sütunları hariç, sadece veri kümenizde bulunan sütun adlarını kullanıyoruz.
        FeatureColumns = new List<string>
        {
            "Amount", // [LoadColumn(29)]
            "AmountLog", // [ColumnName("AmountLog")]
            "TimeSin", // [ColumnName("TimeSin")]
            "TimeCos", // [ColumnName("TimeCos")]
            "DayFeature", // [ColumnName("DayFeature")]
            "HourFeature", // [ColumnName("HourFeature")]
            "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9", "V10",
            "V11", "V12", "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20",
            "V21", "V22", "V23", "V24", "V25", "V26", "V27", "V28"
        };
        FeatureThresholds = new Dictionary<string, double>
        {
            ["Amount"] = 2.5,
            ["TimeVariance"] = 0.05,
            ["PCASimilarity"] = 0.85
        };
    }
}