using Analiz.Domain.Entities.ML;

namespace Analiz.Application.Interfaces.Training;

public interface IFraudModelTrainingService
{
    Task<(TrainingResult LightGBM, TrainingResult PCA)> TrainModelsAsync();
    Task<Ensemble.EnsembleModelResults> TrainAllModelsAsync();
    Task<TrainingResult> TrainEnsembleModelAsync();
}