using Analiz.Application.Interfaces.ML;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.Entities.ML.DataSet;
using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;

namespace Analiz.ML.Models.LightGBM;

public class LightGBMModelBuilder : IModelBuilder
{
    private readonly MLContext _mlContext;
    private readonly LightGBMConfiguration _configuration;


    public LightGBMModelBuilder(MLContext mlContext, LightGBMConfiguration configuration)
    {
        _mlContext = mlContext ?? throw new ArgumentNullException(nameof(mlContext));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public IEstimator<ITransformer> BuildPipeline()
    {
        try
        {
            var transformations = new List<IEstimator<ITransformer>>();
            var featureColumns = new List<string>();

            // 1. Amount özellikleri: Normalizasyon işlemi standart transform ile yapılabiliyor.
            transformations.Add(
                _mlContext.Transforms.NormalizeMinMax("Amount_normalized", nameof(CreditCardModelData.Amount)));
            featureColumns.Add("Amount_normalized");

            // 2. Time özellikleri: Artık veride hesaplanmış durumda, bu nedenle custom mapping adımına gerek yok.
            // Sadece hesaplanmış sütunları featureColumns'a ekleyin.
            featureColumns.AddRange(new[] { "TimeSin", "TimeCos", "DayFeature", "HourFeature" });

            // 3. V1-V28 özellikleri için normalizasyon
            for (var i = 1; i <= 28; i++)
            {
                var inputColumn = $"V{i}";
                var outputColumn = $"{inputColumn}_normalized";
                transformations.Add(
                    _mlContext.Transforms.NormalizeMeanVariance(outputColumn, inputColumn, fixZero: true));
                featureColumns.Add(outputColumn);
            }

            // 4. Sample weight ekle (varsa)
            if (_configuration.UseClassWeights)
                transformations.Add(_mlContext.Transforms.Conversion.MapValue(
                    "SampleWeight",
                    inputColumnName: "Label",
                    keyValuePairs: new[]
                    {
                        new KeyValuePair<bool, float>(false, (float)_configuration.ClassWeights["0"]),
                        new KeyValuePair<bool, float>(true, (float)_configuration.ClassWeights["1"])
                    }));

            // 5. Tüm özellikleri birleştir
            transformations.Add(_mlContext.Transforms.Concatenate("Features", featureColumns.ToArray()));

            // 6. FastTree trainer'ı ekle
            var trainerOptions = new FastTreeBinaryTrainer.Options
            {
                NumberOfLeaves = _configuration.NumberOfLeaves,
                MinimumExampleCountPerLeaf = _configuration.MinDataInLeaf,
                LearningRate = (float)_configuration.LearningRate,
                NumberOfTrees = _configuration.NumberOfTrees,
                FeatureFraction = (float)_configuration.FeatureFraction,
                LabelColumnName = "Label",
                FeatureColumnName = "Features"
            };
            if (_configuration.UseClassWeights)
                trainerOptions.ExampleWeightColumnName = "SampleWeight";

            transformations.Add(_mlContext.BinaryClassification.Trainers.FastTree(trainerOptions));

            // Pipeline'ı oluşturma
            IEstimator<ITransformer> pipeline = transformations[0];
            for (var i = 1; i < transformations.Count; i++)
                pipeline = pipeline.Append(transformations[i]);

            return pipeline;
        }
        catch (Exception ex)
        {
            throw new Exception("Error building LightGBM pipeline", ex);
        }
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
            throw new Exception("Error training model", ex);
        }
    }

    public ModelOutput Predict(IDataView data, ITransformer model)
    {
        try
        {
            var transformedData = model.Transform(data);
            var prediction = _mlContext.Data
                .CreateEnumerable<LightGBMOutput>(transformedData, false)
                .First();

            // Calculate feature importance
            var featureImportances = CalculateFeatureImportance(model, data);

            return new ModelOutput
            {
                PredictedLabel = prediction.PredictedLabel,
                Score = prediction.Score,
                Probability = prediction.Probability,
                Metadata = new Dictionary<string, object>
                {
                    ["ConfidenceScore"] = prediction.ConfidenceScore,
                    ["UncertaintyScore"] = prediction.UncertaintyScore,
                    ["TopFeatures"] = string.Join(",", prediction.TopContributingFeatures)
                }
            };
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private Dictionary<string, double> CalculateFeatureImportance(ITransformer model, IDataView data)
    {
        // Feature importance hesaplama mantığı
        return new Dictionary<string, double>();
    }

    public void SaveModel(ITransformer model, string modelPath)
    {
        try
        {
            _mlContext.Model.Save(model, null, modelPath);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error saving model to path: {modelPath}", ex);
        }
    }

    public ITransformer LoadModel(string modelPath)
    {
        try
        {
            return _mlContext.Model.Load(modelPath, out _);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error loading model from path: {modelPath}", ex);
        }
    }
}