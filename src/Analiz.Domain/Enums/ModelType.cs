namespace FraudShield.TransactionAnalysis.Domain.Enums;

public enum ModelType
{
    PCA,
    LightGBM,
    Ensemble,
    Normalizer,
    // Advanced model types
    Attention = 10,
    AutoEncoder = 11,
    IsolationForest = 12,
    GraphBased = 13,
    GANAugmented = 14,
        
    // Data balancing variants
    LightGBM_SMOTE = 20,
    LightGBM_ADASYN = 21,
    Ensemble_SMOTE = 22,
    Ensemble_ADASYN = 23,
    NeuralNetwork
}