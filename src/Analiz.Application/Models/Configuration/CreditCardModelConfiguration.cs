using System.Globalization;
using System.Text.Json;
using Analiz.Application.Converter;
using Analiz.Application.Exceptions;
using Analiz.Application.Extensions;
using Analiz.Application.Interfaces;
using Analiz.Application.Models.Ensemble;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.DataSet;
using Analiz.Domain.ValueObjects;
using Analiz.ML.Models.LightGBM;
using Analiz.ML.Models.PCA;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace Analiz.Application.Models.Configuration;
/*
public class CreditCardModelConfiguration
{
    private const string LIGHTGBM_MODEL_NAME = "CreditCard_FraudDetection_LightGBM";
    private const string PCA_MODEL_NAME = "CreditCard_AnomalyDetection_PCA";
    private const string ENSEMBLE_MODEL_NAME = "CreditCard_FraudDetection_Ensemble";
    private const double TEST_FRACTION = 0.2;
    private const int RANDOM_SEED = 42;

    #region Training Request & Data Preparation

    public static async Task<TrainingRequest> CreateTrainingRequest(
        List<CreditCardModelData> data,
        ModelType modelType,
        IFeatureExtractionService featureService,
        ILogger logger)
    {
        try
        {
            var mlContext = new MLContext(RANDOM_SEED);

            // 1. Veri dağılımını logla
            var fraudCases = data.Count(x => x.Label);
            var totalCases = data.Count;
            logger.LogInformation("Initial data distribution - Total: {Total}, Fraud: {Fraud}, Ratio: {Ratio:P2}",
                totalCases, fraudCases, (double)fraudCases / totalCases);

            // 2. Feature konfigürasyonu oluştur
            var featureConfig = new FeatureConfig
            {
                EnabledFeatures = new Dictionary<string, bool>
                {
                    ["TimeFeatures"] = true,
                    ["TransactionFeatures"] = true,
                    ["PCAFeatures"] = true
                },
                FeatureSettings = new Dictionary<string, FeatureSetting>
                {
                    // Zaman Özellikleri
                    ["TimeSin"] = new()
                    {
                        Name = "TimeSin",
                        Type = FeatureType.Numeric,
                        IsRequired = true,
                        Category = FeatureCategory.Time,
                        TransformationType = "Trigonometric",
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["Range"] = "[-1,1]"
                        }
                    },
                    ["TimeCos"] = new()
                    {
                        Name = "TimeCos",
                        Type = FeatureType.Numeric,
                        IsRequired = true,
                        Category = FeatureCategory.Time,
                        TransformationType = "Trigonometric",
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["Range"] = "[-1,1]"
                        }
                    },
                    ["DayFeature"] = new()
                    {
                        Name = "DayFeature",
                        Type = FeatureType.Categorical,
                        IsRequired = true,
                        Category = FeatureCategory.Time,
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["Range"] = "[0,6]"
                        }
                    },
                    ["HourFeature"] = new()
                    {
                        Name = "HourFeature",
                        Type = FeatureType.Categorical,
                        IsRequired = true,
                        Category = FeatureCategory.Time,
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["Range"] = "[0,23]"
                        }
                    },

                    // İşlem Özellikleri
                    ["Amount"] = new()
                    {
                        Name = "Amount",
                        Type = FeatureType.Numeric,
                        IsRequired = true,
                        Category = FeatureCategory.Transaction,
                        TransformationType = "None",
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["MinValue"] = "0"
                        }
                    },
                    ["Amount_normalized"] = new()
                    {
                        Name = "Amount_normalized",
                        Type = FeatureType.Numeric,
                        IsRequired = true,
                        Category = FeatureCategory.Transaction,
                        TransformationType = "MinMax",
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["Range"] = "[0,1]"
                        }
                    },
                    ["LogAmount"] = new()
                    {
                        Name = "LogAmount",
                        Type = FeatureType.Numeric,
                        IsRequired = true,
                        Category = FeatureCategory.Transaction,
                        TransformationType = "Log",
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["MinValue"] = "0"
                        }
                    }
                },
                NormalizationParameters = new Dictionary<string, double>
                {
                    ["AmountMin"] = data.Min(x => x.Amount),
                    ["AmountMax"] = data.Max(x => x.Amount),
                    ["TimeScaleFactor"] = 24 * 60 * 60
                }
            };

            // PCA özellikleri için dinamik ayarlar ekle
            for (var i = 1; i <= 28; i++)
            {
                var featureName = $"V{i}";
                featureConfig.FeatureSettings[featureName] = new FeatureSetting
                {
                    Name = featureName,
                    Type = FeatureType.Numeric,
                    IsRequired = true,
                    Category = FeatureCategory.Derived,
                    TransformationType = "None"
                };

                featureConfig.FeatureSettings[$"{featureName}_normalized"] = new FeatureSetting
                {
                    Name = $"{featureName}_normalized",
                    Type = FeatureType.Numeric,
                    IsRequired = true,
                    Category = FeatureCategory.Derived,
                    TransformationType = "MinMax",
                    ValidationRules = new Dictionary<string, string>
                    {
                        ["Range"] = "[-1,1]"
                    }
                };
            }

            // 3. Feature'ları çıkar
            var features = await featureService.ExtractBatchFeaturesAsync(
                data.Select(d => new TransactionData
                {
                    TransactionId = Guid.NewGuid(),
                    Amount = (decimal)d.Amount,
                    IsFraudulent = d.Label,
                    Timestamp = DateTime.FromFileTimeUtc((long)d.Time),
                    AdditionalData = CreateAdditionalData(d)
                }).ToList(),
                modelType);

            // 4. ML.NET DataView oluştur
            var enrichedData = EnrichData(data);
            var mlData = CreditCardMLConverter.ConvertToMLDataView(mlContext, enrichedData);

            // 5. Veri dengeleme
            var balancedData = BalanceDataWithUndersampling(mlContext, mlData, logger);

            // 6. Train-test split
            var (trainData, testData) = SplitDataStratified(mlContext, balancedData, TEST_FRACTION, logger);

            // 7. Model konfigürasyonunu oluştur
            var config = modelType switch
            {
                ModelType.LightGBM =>
                    JsonSerializer.Serialize(CreateFraudDetectionConfig(trainData, mlContext, logger)),
                ModelType.PCA => JsonSerializer.Serialize(CreateAnomalyDetectionConfig(data)),
                _ => throw new NotSupportedException($"Model type {modelType} not supported")
            };

            var modelName = modelType switch
            {
                ModelType.LightGBM => LIGHTGBM_MODEL_NAME,
                ModelType.PCA => PCA_MODEL_NAME,
                _ => throw new NotSupportedException($"Model type {modelType} not supported")
            };

            // 8. Training ve validation verilerini dönüştür
            var trainingDataList = CreditCardMLConverter.ToTransactionDataList(trainData, mlContext);
            var validationDataList = CreditCardMLConverter.ToTransactionDataList(testData, mlContext);

            // 9. Veri split'ini doğrula
            ValidateDataSplit(trainingDataList, validationDataList);

            return new TrainingRequest
            {
                ModelName = modelName,
                ModelType = modelType,
                Configuration = config,
                TrainingData = trainingDataList,
                ValidationData = validationDataList,
                Labels = data.Select(x => x.Label).ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating training request");
            throw new ModelPreparationException("Failed to create training request", ex);
        }
    }


    public static EnsembleConfiguration CreateEnsembleConfig(IDataView trainData, MLContext mlContext, ILogger logger)
    {
        // LightGBM ve PCA konfigürasyonlarını al
        var lightGbmConfig = CreateFraudDetectionConfig(trainData, mlContext, logger);
        var pcaConfig = CreateAnomalyDetectionConfig(mlContext.Data
            .CreateEnumerable<CreditCardModelData>(trainData, false)
            .ToList());

        return new EnsembleConfiguration
        {
            LightGBMConfig = lightGbmConfig,
            PCAConfig = pcaConfig,
            // Ensemble-specific settings
            VotingThreshold = 0.5, // Both models must agree for positive prediction
            WeightLightGBM = 0.7, // LightGBM predictions weighted more heavily
            WeightPCA = 0.3, // PCA predictions weighted less
            UseWeightedVoting = true,
            MinConfidenceThreshold = 0.8,
            EnableCrossValidation = true,
            CrossValidationFolds = 5,
            // Feature selection for ensemble
            FeatureColumns = GetEnhancedFeatureColumns(),
            // Model combination strategy
            CombinationStrategy = ModelCombinationStrategy.WeightedAverage
        };
    }

    public static async Task<TrainingRequest> CreateEnsembleTrainingRequest(
        List<CreditCardModelData> data,
        IFeatureExtractionService featureService,
        ILogger logger)
    {
        var mlContext = new MLContext(RANDOM_SEED);

        try
        {
            // 1. Veri hazırlama ve zenginleştirme
            logger.LogInformation("Starting ensemble training request preparation...");
            var enrichedData = EnrichData(data);

            // 2. Feature konfigürasyonunu al
            var featureConfig = new FeatureConfig
            {
                EnabledFeatures = new Dictionary<string, bool>
                {
                    ["TimeFeatures"] = true,
                    ["TransactionFeatures"] = true,
                    ["PCAFeatures"] = true
                },
                FeatureSettings = new Dictionary<string, FeatureSetting>
                {
                    // Zaman Özellikleri
                    ["TimeSin"] = new()
                    {
                        Name = "TimeSin",
                        Type = FeatureType.Numeric,
                        IsRequired = true,
                        Category = FeatureCategory.Time,
                        TransformationType = "Trigonometric",
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["Range"] = "[-1,1]"
                        }
                    },
                    ["TimeCos"] = new()
                    {
                        Name = "TimeCos",
                        Type = FeatureType.Numeric,
                        IsRequired = true,
                        Category = FeatureCategory.Time,
                        TransformationType = "Trigonometric",
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["Range"] = "[-1,1]"
                        }
                    },
                    ["DayFeature"] = new()
                    {
                        Name = "DayFeature",
                        Type = FeatureType.Categorical,
                        IsRequired = true,
                        Category = FeatureCategory.Time,
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["Range"] = "[0,6]"
                        }
                    },
                    ["HourFeature"] = new()
                    {
                        Name = "HourFeature",
                        Type = FeatureType.Categorical,
                        IsRequired = true,
                        Category = FeatureCategory.Time,
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["Range"] = "[0,23]"
                        }
                    },

                    // İşlem Özellikleri
                    ["Amount"] = new()
                    {
                        Name = "Amount",
                        Type = FeatureType.Numeric,
                        IsRequired = true,
                        Category = FeatureCategory.Transaction,
                        TransformationType = "None",
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["MinValue"] = "0"
                        }
                    },
                    ["Amount_normalized"] = new()
                    {
                        Name = "Amount_normalized",
                        Type = FeatureType.Numeric,
                        IsRequired = true,
                        Category = FeatureCategory.Transaction,
                        TransformationType = "MinMax",
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["Range"] = "[0,1]"
                        }
                    },
                    ["LogAmount"] = new()
                    {
                        Name = "LogAmount",
                        Type = FeatureType.Numeric,
                        IsRequired = true,
                        Category = FeatureCategory.Transaction,
                        TransformationType = "Log",
                        ValidationRules = new Dictionary<string, string>
                        {
                            ["MinValue"] = "0"
                        }
                    }
                },
                NormalizationParameters = new Dictionary<string, double>
                {
                    ["AmountMin"] = data.Min(x => x.Amount),
                    ["AmountMax"] = data.Max(x => x.Amount),
                    ["TimeScaleFactor"] = 24 * 60 * 60
                }
            };

            // 3. Feature'ları çıkar
            var features = await featureService.ExtractBatchFeaturesAsync(
                data.Select(d => new TransactionData
                {
                    Amount = (decimal)d.Amount,
                    IsFraudulent = d.Label,
                    Timestamp = DateTime.FromFileTimeUtc((long)d.Time),
                    AdditionalData = CreateAdditionalData(d)
                }).ToList(),
                ModelType.Ensemble
            );

            // 4. ML.NET DataView oluştur
            var mlData = ConvertToMLDataView(mlContext, enrichedData, features);

            // 5. Veri dengeleme
            var balancedData = BalanceDataWithUndersampling(mlContext, mlData, logger);

            // 6. Train-test split
            var (trainData, testData) = SplitDataStratified(mlContext, balancedData, TEST_FRACTION, logger);

            // 7. Ensemble config oluştur
            var config = CreateEnsembleConfig(trainData, mlContext, logger);

            // 8. Training ve validation verilerini dönüştür
            var trainingDataList = CreditCardMLConverter.ToTransactionDataList(trainData, mlContext);
            var validationDataList = CreditCardMLConverter.ToTransactionDataList(testData, mlContext);

            // 9. Veri split'ini doğrula
            ValidateDataSplit(trainingDataList, validationDataList);

            return new TrainingRequest
            {
                ModelName = ENSEMBLE_MODEL_NAME,
                ModelType = ModelType.Ensemble,
                Configuration = JsonSerializer.Serialize(config),
                TrainingData = trainingDataList,
                ValidationData = validationDataList,
                Labels = data.Select(x => x.Label).ToList()
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating ensemble training request");
            throw new ModelPreparationException("Failed to create ensemble training request", ex);
        }
    }

    private static TransactionAdditionalData CreateAdditionalData(CreditCardModelData data)
    {
        var vo = new TransactionAdditionalData();

        // V1–V28 özelliklerini VFactors olarak ekle
        for (var i = 1; i <= 28; i++)
        {
            var prop = typeof(CreditCardModelData).GetProperty($"V{i}");
            if (prop != null && prop.GetValue(data) is float f) vo.VFactors[$"V{i}"] = f;
        }

        // “Time” değerini CustomValues’a ekle
        vo.CustomValues["Time"] = data.Time.ToString(CultureInfo.InvariantCulture);

        return vo;
    }

    private static IDataView ConvertToMLDataView(
        MLContext mlContext,
        List<CreditCardModelData> data,
        List<Dictionary<string, float>> features)
    {
        // Data ve feature'ları birleştir
        var combined = data.Zip(features, (d, f) => new
        {
            d.Label,
            Features = f.Values.ToArray()
        });

        return mlContext.Data.LoadFromEnumerable(combined);
    }

    #endregion

    #region Data Preparation & Validation

    private static IDataView BalanceDataWithUndersampling(MLContext mlContext, IDataView data, ILogger logger)
    {
        var schema = data.Schema;
        logger.LogInformation("Checking schema before undersampling...");
        foreach (var col in schema) logger.LogInformation($"Column {col.Name}: {col.Type}");

        var records = mlContext.Data.CreateEnumerable<CreditCardModelData>(
                data,
                false,
                true)
            .ToList();

        var fraudRecords = records.Where(x => x.Label).ToList();
        var nonFraudRecords = records.Where(x => !x.Label).ToList();

        logger.LogInformation($"Before undersampling - Fraud: {fraudRecords.Count}, NonFraud: {nonFraudRecords.Count}");

        // Random undersampling
        var targetNonFraudCount = Math.Min(fraudRecords.Count * 5, nonFraudRecords.Count);
        var random = new Random(RANDOM_SEED);
        var sampledNonFraudRecords = nonFraudRecords.OrderBy(x => random.Next()).Take(targetNonFraudCount).ToList();

        var balancedRecords = fraudRecords.Concat(sampledNonFraudRecords).OrderBy(x => random.Next()).ToList();
        logger.LogInformation(
            $"After undersampling - Total: {balancedRecords.Count}, Fraud: {fraudRecords.Count}, NonFraud: {sampledNonFraudRecords.Count}");

        return mlContext.Data.LoadFromEnumerable(balancedRecords);
    }

    private static (IDataView trainSet, IDataView testSet) SplitDataStratified(MLContext mlContext, IDataView data,
        double testFraction, ILogger logger)
    {
        var records = mlContext.Data.CreateEnumerable<CreditCardModelData>(data, false).ToList();
        var random = new Random(RANDOM_SEED);

        var fraudRecords = records.Where(x => x.Label).ToList();
        var nonFraudRecords = records.Where(x => !x.Label).ToList();

        var fraudTestCount = (int)(fraudRecords.Count * testFraction);
        var nonFraudTestCount = (int)(nonFraudRecords.Count * testFraction);

        fraudRecords = fraudRecords.OrderBy(x => random.Next()).ToList();
        nonFraudRecords = nonFraudRecords.OrderBy(x => random.Next()).ToList();

        var trainFraud = fraudRecords.Skip(fraudTestCount).ToList();
        var testFraud = fraudRecords.Take(fraudTestCount).ToList();
        var trainNonFraud = nonFraudRecords.Skip(nonFraudTestCount).ToList();
        var testNonFraud = nonFraudRecords.Take(nonFraudTestCount).ToList();

        var trainData = mlContext.Data.LoadFromEnumerable(trainFraud.Concat(trainNonFraud));
        var testData = mlContext.Data.LoadFromEnumerable(testFraud.Concat(testNonFraud));

        LogSplitDistribution(trainFraud.Count, trainNonFraud.Count, testFraud.Count, testNonFraud.Count, logger);
        return (trainData, testData);
    }

    private static void LogSplitDistribution(int trainFraud, int trainNonFraud, int testFraud, int testNonFraud,
        ILogger logger)
    {
        logger.LogInformation("Train set - Fraud: {TrainFraud}, NonFraud: {TrainNonFraud}, Ratio: {TrainRatio:P2}",
            trainFraud, trainNonFraud, (double)trainFraud / (trainFraud + trainNonFraud));
        logger.LogInformation("Test set - Fraud: {TestFraud}, NonFraud: {TestNonFraud}, Ratio: {TestRatio:P2}",
            testFraud, testNonFraud, (double)testFraud / (testFraud + testNonFraud));
    }

    private static void ValidateDataSplit(List<TransactionData> trainData, List<TransactionData> testData)
    {
        var trainFraud = trainData.Count(x => x.IsFraudulent);
        var testFraud = testData.Count(x => x.IsFraudulent);
        if (trainFraud == 0)
            throw new InvalidOperationException("Training data contains no fraud cases");
        if (testFraud == 0)
            throw new InvalidOperationException("Test data contains no fraud cases");
    }

    private static List<CreditCardModelData> EnrichData(List<CreditCardModelData> data)
    {
        // Gerekli ek özellik hesaplamalarını burada yapabilirsiniz.
        foreach (var record in data)
            if (float.IsNaN(record.AmountLog) || float.IsInfinity(record.AmountLog))
                throw new InvalidDataException($"Invalid AmountLog value for record with Amount: {record.Amount}");

        return data;
    }

    #endregion

    #region Hiperparametre Optimizasyonu (LightGBM için)

    /// <summary>
    /// Basit grid search yöntemiyle LightGBM hiperparametrelerini optimize eder.
    /// </summary>
    public static (LightGBMConfiguration bestConfig, double bestMetric) OptimizeLightGBMHyperparameters(
        MLContext mlContext, IDataView trainData, ILogger logger)
    {
        var numberOfLeavesOptions = new[] { 64, 128, 256 };
        var learningRateOptions = new[] { 0.005, 0.01 };
        var numberOfTreesOptions = new[] { 500, 1000 };

        var bestMetric = double.MinValue;
        LightGBMConfiguration bestConfig = null;

        foreach (var leaves in numberOfLeavesOptions)
        foreach (var lr in learningRateOptions)
        foreach (var trees in numberOfTreesOptions)
        {
            var config = new LightGBMConfiguration
            {
                NumberOfLeaves = leaves,
                LearningRate = lr,
                NumberOfTrees = trees,
                MinDataInLeaf = 10,
                FeatureFraction = 0.8,
                BaggingFraction = 0.8,
                BaggingFrequency = 5,
                L1Regularization = 0.01,
                L2Regularization = 0.01,
                EarlyStoppingRound = 100,
                MinGainToSplit = 0.0005,
                UseClassWeights = true,
                ClassWeights = new Dictionary<string, double> { { "0", 1.0 }, { "1", 250.0 } },
                MinAmount = 0,
                MaxAmount = 25000,
                TimeScaleFactor = 24 * 60 * 60,
                // Eğer etkileşim özelliklerini kullanmak istemiyorsanız:
                FeatureColumns =
                    GetEnhancedFeatureColumns() // Veya GetFeatureColumns() kullanarak etkileşim sütunlarını çıkartabilirsiniz.
            };

            var builder = new LightGBMModelBuilder(mlContext, config);
            var pipeline = builder.BuildPipeline();

            // 5-fold çapraz doğrulama ile AUC değerini hesapla
            var cvResults = mlContext.BinaryClassification.CrossValidate(
                trainData,
                pipeline,
                5,
                "Label");

            var avgAUC = cvResults.Average(r => r.Metrics.AreaUnderRocCurve);
            logger.LogInformation($"Config: Leaves={leaves}, LR={lr}, Trees={trees}, AUC={avgAUC:F4}");

            if (avgAUC > bestMetric)
            {
                bestMetric = avgAUC;
                bestConfig = config;
            }
        }

        return (bestConfig, bestMetric);
    }

    #endregion

    #region Konfigürasyon Oluşturma

    public static LightGBMConfiguration CreateFraudDetectionConfig(IDataView trainData, MLContext mlContext,
        ILogger logger)
    {
        // Hiperparametre optimizasyonu uygulayabilirsiniz.
        var (optimizedConfig, bestMetric) = OptimizeLightGBMHyperparameters(mlContext, trainData, logger);
        logger.LogInformation($"Best LightGBM config found with AUC = {bestMetric:F4}");

        return optimizedConfig;
    }

    public static PCAConfiguration CreateAnomalyDetectionConfig(List<CreditCardModelData> data)
    {
        var stdDev = CalculateAmountStdDev(data);
        var maxAmount = data.Max(x => x.Amount);

        return new PCAConfiguration
        {
            ComponentCount = 15,
            ExplainedVarianceThreshold = 0.98,
            StandardizeInput = true,
            AnomalyThreshold = 2.5,
            MinAmount = 0,
            MaxAmount = maxAmount,
            TimeScaleFactor = 24 * 60 * 60,
            // Etkileşim sütunları kullanmayacaksanız, GetFeatureColumns() metodunu kullanın.
            //FeatureColumns = GetFeatureColumns(),
            FeatureThresholds = new Dictionary<string, double>
            {
                ["Amount"] = 2.5 * stdDev,
                ["TimeVariance"] = 0.05,
                ["PCASimilarity"] = 0.85
            }
        };
    }

    /// <summary>
    /// Eğer etkileşim sütunları kullanmak istiyorsanız, bunları üreten metot.
    /// </summary>
    private static List<string> GetEnhancedFeatureColumns()
    {
        var baseFeatures = new List<string>
        {
            // Zaman bazlı özellikler
            "TimeSin",
            "TimeCos",

            // İşlem bazlı özellikler
            "Amount",
            "Amount_normalized",
            "LogAmount",

            // İstatistiksel özellikler
            "DayFeature",
            "HourFeature"
        };

        // V1-V28 ana özellikler
        for (var i = 1; i <= 28; i++) baseFeatures.Add($"V{i}");

        // V1-V28 normalize edilmiş özellikler
        for (var i = 1; i <= 28; i++) baseFeatures.Add($"V{i}_normalized");

        return baseFeatures;
    }

    /// <summary>
    /// Etkileşim sütunlarını kullanmayacaksanız temel özellik listesi.
    /// </summary>
    private static List<string> GetFeatureColumns()
    {
        var baseFeatures = new List<string>
        {
            "TimeSin",
            "TimeCos",
            "LogAmount"
        };

        // V1-V28 özelliklerini ekle
        for (var i = 1; i <= 28; i++) baseFeatures.Add($"V{i}");

        return baseFeatures;
    }

    private static double CalculateAmountStdDev(List<CreditCardModelData> data)
    {
        var mean = data.Average(x => x.Amount);
        return Math.Sqrt(data.Average(x => Math.Pow(x.Amount - mean, 2)));
    }

    #endregion
}*/