using Analiz.Domain.Entities.ML;
using Microsoft.ML;

namespace Analiz.Application.Models.Ensemble;

public class EnsembleModel
{
    private readonly MLContext _mlContext;
    public ITransformer LightGBMModel { get; }
    public ITransformer PCAModel { get; }

    public EnsembleModel(MLContext mlContext, ITransformer lightgbmModel, ITransformer pcaModel)
    {
        _mlContext = mlContext;
        LightGBMModel = lightgbmModel;
        PCAModel = pcaModel;
    }

    /// <summary>
    /// Verilen girdi için her iki modelin tahminlerini alır ve ağırlıklı ortalama ile ensemble tahmin üretir.
    /// Varsayalım ki ModelInput sınıfı LightGBM için kullanılıyor ve PCA modeli için giriş verisini
    /// oluşturmak üzere aynı özellik vektörü kullanılıyor.
    /// </summary>
    /* public Domain.Entities.ML.Ensemble.EnsemblePrediction Predict(ModelInput input)
     {
         // LightGBM tahmini
         var lightGbmEngine =
             _mlContext.Model.CreatePredictionEngine<ModelInput, Domain.Entities.ML.Ensemble.LightGBMPrediction>(
                 LightGBMModel);
         var lightGbmPrediction = lightGbmEngine.Predict(input);

         // PCA tahmini için; ModelInput içerisindeki Features vektörünü kullanıyoruz.
         var pcaInput = new PCAPredictionInput { PCAFeatures = input.Features };
         var pcaEngine = _mlContext.Model.CreatePredictionEngine<PCAPredictionInput, PCAPredictionOutput>(PCAModel);
         var pcaPrediction = pcaEngine.Predict(pcaInput);

         // ğırlıklı ortalama ile birleştirme: %70 LightGBM, %30 PCA
         var ensembleProbability = 0.7f * lightGbmPrediction.Probability + 0.3f * pcaPrediction.Probability;
         var ensembleLabel = ensembleProbability >= 0.5f;

         return new Domain.Entities.ML.Ensemble.EnsemblePrediction
         {
             LightGBMProbability = lightGbmPrediction.Probability,
             PCAPredictionProbability = pcaPrediction.Probability,
             EnsembleProbability = ensembleProbability,
             EnsembleLabel = ensembleLabel
         };
     }*/
}