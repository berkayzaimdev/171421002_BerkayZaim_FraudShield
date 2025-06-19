using Microsoft.ML;

namespace Analiz.Application.Interfaces.ML;

public interface IModelBuilder
{
    IEstimator<ITransformer> BuildPipeline();
    ITransformer Train(IDataView trainingData);
    void SaveModel(ITransformer model, string modelPath);
    ITransformer LoadModel(string modelPath);
}