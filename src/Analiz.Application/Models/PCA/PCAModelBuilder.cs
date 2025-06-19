using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;
using Analiz.Application.Interfaces.ML;
using Analiz.Application.Services;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.DataSet;
using Analiz.ML.Models.PCA;
using Microsoft.ML.Trainers;

public class PCAModelBuilder : IModelBuilder
{
    private readonly MLContext _mlContext;
    private readonly PCAConfiguration _configuration;
    private readonly ILogger<ModelService> _logger;

    public PCAModelBuilder(
        MLContext mlContext,
        PCAConfiguration configuration,
        ILogger<ModelService> logger)
    {
        _mlContext = mlContext;
        _configuration = configuration;
        _logger = logger;
    }

    public IEstimator<ITransformer> BuildPipeline()
    {
        // 1) Tüm FeatureColumns’u Features vektöründe topla:
        var concat = _mlContext.Transforms
            .Concatenate("Features", _configuration.FeatureColumns.ToArray());

        // 2) Mean‐Variance normalize:
        var normalize = _mlContext.Transforms
            .NormalizeMeanVariance("Features");

        // 3) PCA Anomaly detector:
        var pca = _mlContext.AnomalyDetection.Trainers.RandomizedPca(
            "Features",
            rank: _configuration.ComponentCount);

        // 4) Ara çıktıyı (Score, PredictedLabel) → bizim istediğimiz sütunlara çevir:
        var toFinal = _mlContext.Transforms.CustomMapping<AnomalyPrediction, PCAPredictionOutput>(
            (src, dst) =>
            {
                // RandomizedPca’dan dönen Score/PredictedLabel
                dst.Score = src.Score;
                dst.AnomalyScore = src.Score;
                dst.PredictedLabel = src.PredictedLabel;
                dst.IsAnomaly = src.PredictedLabel;

                // Sigmoid ile probability:
                dst.Probability = 1f / (1f + (float)Math.Exp(-src.Score));
            },
            "AnomalyScoring" // **boş olursa Save sırasında patlar**
        );

        // 5) Zinciri oluştur:
        return concat
            .Append(normalize)
            .Append(pca)
            .Append(toFinal);
    }


    private float CalculateAnomalyScore(float[] pcaFeatures)
    {
        // Mahalanobis distance calculation
        return (float)Math.Sqrt(pcaFeatures.Select(x => x * x).Sum());
    }

    public ITransformer Train(IDataView trainingData)
    {
        try
        {
            var pipeline = BuildPipeline();
            return pipeline.Fit(trainingData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training PCA model");
            throw;
        }
    }

    public void SaveModel(ITransformer model, string modelPath)
    {
        throw new NotImplementedException();
    }

    public ITransformer LoadModel(string modelPath)
    {
        throw new NotImplementedException();
    }

    private DataStatistics GetDataStatistics(IDataView data)
    {
        var statistics = new DataStatistics();

        // Her kolon için istatistikleri hesapla
        foreach (var column in data.Schema)
            if (column.Type is NumberDataViewType)
            {
                var stats = _mlContext.Data.CreateEnumerable<CreditCardModelData>(data, false)
                    .Select(x => GetPropertyValue(x, column.Name))
                    .Where(x => !float.IsNaN(x))
                    .ToList();

                if (stats.Any())
                    statistics.ColumnStatistics[column.Name] = new ColumnStatistics
                    {
                        Mean = stats.Average(),
                        StdDev = (float)CalculateStdDev(stats),
                        Min = stats.Min(),
                        Max = stats.Max(),
                        MissingCount = stats.Count(float.IsNaN),
                        NonZeroCount = stats.Count(x => x != 0)
                    };
            }

        return statistics;
    }

    private void LogDataStatistics(DataStatistics statistics)
    {
        foreach (var (column, stats) in statistics.ColumnStatistics)
            _logger.LogInformation(
                "Column {Column} stats - Mean: {Mean:F2}, StdDev: {StdDev:F2}, " +
                "Range: [{Min:F2}, {Max:F2}], Missing: {Missing}, NonZero: {NonZero}",
                column, stats.Mean, stats.StdDev, stats.Min, stats.Max,
                stats.MissingCount, stats.NonZeroCount);
    }

    private static float GetPropertyValue(CreditCardModelData data, string propertyName)
    {
        return (float)data.GetType().GetProperty(propertyName)?.GetValue(data, null);
    }

    private static double CalculateStdDev(IEnumerable<float> values)
    {
        var enumerable = values as float[] ?? values.ToArray();
        var avg = enumerable.Average();
        return Math.Sqrt(enumerable.Average(v => Math.Pow(v - avg, 2)));
    }
}