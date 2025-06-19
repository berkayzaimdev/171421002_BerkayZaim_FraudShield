using System.Text.Json.Serialization;

namespace Analiz.Domain.Entities.ML;

public class ModelMetrics
{
    // Temel metrikler
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public double AUC { get; set; }
    
    // Confusion Matrix bileşenleri
    public int TruePositive { get; set; }
    public int TrueNegative { get; set; }
    public int FalsePositive { get; set; }
    public int FalseNegative { get; set; }
    
    // Ek sınıflandırma metrikleri
    public double Specificity { get; set; } // True Negative Rate
    public double Sensitivity { get; set; } // True Positive Rate (Recall ile aynı)
    public double NPV { get; set; } // Negative Predictive Value
    public double FPR { get; set; } // False Positive Rate
    public double FNR { get; set; } // False Negative Rate
    public double FDR { get; set; } // False Discovery Rate
    public double FOR { get; set; } // False Omission Rate
    
    // ROC ve PR Curve metrikleri
    public double AUCPR { get; set; } // Area Under Precision-Recall Curve
    public double OptimalThreshold { get; set; }
    public double BalancedAccuracy { get; set; }
    
    // İstatistiksel metrikler
    public double MatthewsCorrCoef { get; set; } // Matthews Correlation Coefficient
    public double CohenKappa { get; set; }
    public double LogLoss { get; set; }
    public double BrierScore { get; set; }
    
    // Model performans özellikleri
    public int SupportClass0 { get; set; } // Sınıf 0'ın örnek sayısı
    public int SupportClass1 { get; set; } // Sınıf 1'in örnek sayısı
    public double ClassImbalanceRatio { get; set; }
    
    // Anomaly Detection için özel metrikler (PCA)
    public double AnomalyThreshold { get; set; }
    public double MeanReconstructionError { get; set; }
    public double StdReconstructionError { get; set; }
    public double ExplainedVarianceRatio { get; set; }
    
    // Ek metrikler sözlüğü
    public Dictionary<string, double> AdditionalMetrics { get; set; }
    
    // Sınıflandırma raporu detayları
    public ClassificationReport ClassificationReport { get; set; }

    public ModelMetrics()
    {
        AdditionalMetrics = new Dictionary<string, double>();
        ClassificationReport = new ClassificationReport();
    }

    /// <summary>
    /// Confusion Matrix'ten türetilen metrikleri hesapla
    /// </summary>
    public void CalculateDerivedMetrics()
    {
        var total = TruePositive + TrueNegative + FalsePositive + FalseNegative;
        
        if (total == 0) return;

        // Temel metrikler
        Accuracy = (double)(TruePositive + TrueNegative) / total;
        Precision = TruePositive + FalsePositive > 0 ? (double)TruePositive / (TruePositive + FalsePositive) : 0;
        Recall = TruePositive + FalseNegative > 0 ? (double)TruePositive / (TruePositive + FalseNegative) : 0;
        Sensitivity = Recall; // Aynı şey
        
        // F1 Score
        F1Score = Precision + Recall > 0 ? 2 * (Precision * Recall) / (Precision + Recall) : 0;
        
        // Specificity (True Negative Rate)
        Specificity = TrueNegative + FalsePositive > 0 ? (double)TrueNegative / (TrueNegative + FalsePositive) : 0;
        
        // Negative Predictive Value
        NPV = TrueNegative + FalseNegative > 0 ? (double)TrueNegative / (TrueNegative + FalseNegative) : 0;
        
        // False Positive Rate
        FPR = TrueNegative + FalsePositive > 0 ? (double)FalsePositive / (TrueNegative + FalsePositive) : 0;
        
        // False Negative Rate
        FNR = TruePositive + FalseNegative > 0 ? (double)FalseNegative / (TruePositive + FalseNegative) : 0;
        
        // False Discovery Rate
        FDR = TruePositive + FalsePositive > 0 ? (double)FalsePositive / (TruePositive + FalsePositive) : 0;
        
        // False Omission Rate
        FOR = TrueNegative + FalseNegative > 0 ? (double)FalseNegative / (TrueNegative + FalseNegative) : 0;
        
        // Balanced Accuracy
        BalancedAccuracy = (Sensitivity + Specificity) / 2;
        
        // Matthews Correlation Coefficient
        var numerator = (double)(TruePositive * TrueNegative) - (double)(FalsePositive * FalseNegative);
        var denominator = Math.Sqrt((double)(TruePositive + FalsePositive) * (TruePositive + FalseNegative) * 
                                   (TrueNegative + FalsePositive) * (TrueNegative + FalseNegative));
        MatthewsCorrCoef = denominator > 0 ? numerator / denominator : 0;
        
        // Class Imbalance Ratio
        SupportClass0 = TrueNegative + FalsePositive;
        SupportClass1 = TruePositive + FalseNegative;
        ClassImbalanceRatio = SupportClass0 > 0 ? (double)SupportClass1 / SupportClass0 : 0;
    }

    /// <summary>
    /// Confusion Matrix'i 2D array olarak döndür
    /// </summary>
    public int[,] GetConfusionMatrix()
    {
        return new int[,] 
        {
            { TrueNegative, FalsePositive },
            { FalseNegative, TruePositive }
        };
    }

    /// <summary>
    /// Tüm metrikleri dictionary olarak döndür
    /// </summary>
    public Dictionary<string, object> ToDictionary()
    {
        var metrics = new Dictionary<string, object>
        {
            // Temel metrikler
            ["Accuracy"] = Accuracy,
            ["Precision"] = Precision,
            ["Recall"] = Recall,
            ["F1Score"] = F1Score,
            ["AUC"] = AUC,
            ["AUCPR"] = AUCPR,
            
            // Confusion Matrix
            ["ConfusionMatrix"] = new
            {
                TruePositive,
                TrueNegative,
                FalsePositive,
                FalseNegative,
                Matrix = GetConfusionMatrix()
            },
            
            // Ek metrikler
            ["Specificity"] = Specificity,
            ["Sensitivity"] = Sensitivity,
            ["NPV"] = NPV,
            ["FPR"] = FPR,
            ["FNR"] = FNR,
            ["FDR"] = FDR,
            ["FOR"] = FOR,
            ["BalancedAccuracy"] = BalancedAccuracy,
            ["MatthewsCorrCoef"] = MatthewsCorrCoef,
            ["CohenKappa"] = CohenKappa,
            ["LogLoss"] = LogLoss,
            ["BrierScore"] = BrierScore,
            ["OptimalThreshold"] = OptimalThreshold,
            
            // Sınıf bilgileri
            ["ClassDistribution"] = new
            {
                Class0Support = SupportClass0,
                Class1Support = SupportClass1,
                ImbalanceRatio = ClassImbalanceRatio
            },
            
            // PCA özel metrikleri
            ["AnomalyDetection"] = new
            {
                AnomalyThreshold,
                MeanReconstructionError,
                StdReconstructionError,
                ExplainedVarianceRatio
            }
        };

        // Ek metrikleri ekle
        foreach (var metric in AdditionalMetrics)
        {
            metrics[metric.Key] = metric.Value;
        }

        return metrics;
    }

    /// <summary>
    /// Metriklerin performans özetini döndür
    /// </summary>
    public ModelPerformanceSummary GetPerformanceSummary()
    {
        return new ModelPerformanceSummary
        {
            OverallScore = (Accuracy + F1Score + AUC) / 3,
            IsGoodModel = Accuracy > 0.8 && F1Score > 0.7 && AUC > 0.8,
            PrimaryWeakness = GetPrimaryWeakness(),
            RecommendedActions = GetRecommendations()
        };
    }

    private string GetPrimaryWeakness()
    {
        if (Precision < 0.7) return "Yüksek False Positive oranı - Precision düşük";
        if (Recall < 0.7) return "Yüksek False Negative oranı - Recall düşük";
        if (Specificity < 0.7) return "Normal işlemleri fraud olarak işaretliyor";
        if (ClassImbalanceRatio > 100) return "Ciddi sınıf dengesizliği problemi";
        return "Genel performans kabul edilebilir";
    }

    private List<string> GetRecommendations()
    {
        var recommendations = new List<string>();
        
        if (Precision < 0.7)
            recommendations.Add("Class weights ayarlarını gözden geçirin");
        
        if (Recall < 0.7)
            recommendations.Add("Fraud sınıfı için daha fazla özellik mühendisliği yapın");
        
        if (AUC < 0.8)
            recommendations.Add("Model karmaşıklığını artırın veya ensemble yöntemleri deneyin");
        
        if (ClassImbalanceRatio > 50)
            recommendations.Add("SMOTE veya benzeri oversampling teknikleri kullanın");
            
        return recommendations;
    }
}

/// <summary>
/// Sınıflandırma raporu detayları
/// </summary>
public class ClassificationReport
{
    public ClassMetrics Class0 { get; set; } = new();
    public ClassMetrics Class1 { get; set; } = new();
    public ClassMetrics MacroAvg { get; set; } = new();
    public ClassMetrics WeightedAvg { get; set; } = new();
}

/// <summary>
/// Sınıf bazlı metrikler
/// </summary>
public class ClassMetrics
{
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public int Support { get; set; }
}

/// <summary>
/// Model performans özeti
/// </summary>
public class ModelPerformanceSummary
{
    public double OverallScore { get; set; }
    public bool IsGoodModel { get; set; }
    public string PrimaryWeakness { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
}