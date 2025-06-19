namespace Analiz.ML.Models.LightGBM;

public class LightGBMConfiguration
{
    // Mevcut özellikler
    public int NumberOfLeaves { get; set; } = 128; // Artırıldı
    public int MinDataInLeaf { get; set; } = 10; // Azaltıldı
    public double LearningRate { get; set; } = 0.005; // Yavaşlatıldı
    public int NumberOfTrees { get; set; } = 1000; // Artırıldı

    public double FeatureFraction { get; set; } = 0.8;
    public double BaggingFraction { get; set; } = 0.8;
    public int BaggingFrequency { get; set; } = 5;

    public double MinAmount { get; set; } = 0;
    public double MaxAmount { get; set; } = 25000;
    public double TimeScaleFactor { get; set; } = 24 * 60 * 60;

    public double L1Regularization { get; set; } = 0.01;
    public double L2Regularization { get; set; } = 0.01;

    public int EarlyStoppingRound { get; set; } = 100; // Artırıldı
    public double MinGainToSplit { get; set; } = 0.0005; // Hassaslaştırıldı

    public bool UseClassWeights { get; set; } = true;
    public Dictionary<string, double> ClassWeights { get; set; }

    public double PredictionThreshold { get; set; } = 0.5;
    public bool UseDynamicThreshold { get; set; } = true;
    public double DynamicThresholdPercentile { get; set; } = 95;

    public List<string> FeatureColumns { get; set; }

    public LightGBMConfiguration()
    {
        FeatureColumns = new List<string>
        {
            "Amount", "AmountLog", "TimeSin", "TimeCos", "DayOfWeek", "HourOfDay",
            "V1", "V2", "V3", "V4", "V5", "V6", "V7", "V8", "V9", "V10",
            "V11", "V12", "V13", "V14", "V15", "V16", "V17", "V18", "V19", "V20",
            "V21", "V22", "V23", "V24", "V25", "V26", "V27", "V28"
        };

        ClassWeights = new Dictionary<string, double>
        {
            { "0", 1.0 },
            { "1", 75.0 } // 250 yerine daha dengeli bir değer
        };
    }
}