using Analiz.ML.Models.LightGBM;
using Analiz.ML.Models.PCA;
using FraudShield.TransactionAnalysis.Domain.Enums;

namespace Analiz.Application.Models.Ensemble;

public class EnsembleConfiguration
{
    public LightGBMConfiguration LightGBMConfig { get; set; }
    public PCAConfiguration PCAConfig { get; set; }
    public double VotingThreshold { get; set; }
    public double WeightLightGBM { get; set; }
    public double WeightPCA { get; set; }
    public bool UseWeightedVoting { get; set; }
    public double MinConfidenceThreshold { get; set; }
    public bool EnableCrossValidation { get; set; }
    public int CrossValidationFolds { get; set; }
    public List<string> FeatureColumns { get; set; }
    public ModelCombinationStrategy CombinationStrategy { get; set; }
}