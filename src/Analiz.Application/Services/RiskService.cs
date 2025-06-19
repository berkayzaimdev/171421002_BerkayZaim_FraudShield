using Analiz.Application.Interfaces;
using Analiz.Application.Interfaces.ML;
using Analiz.Application.Interfaces.Repositories;
using Analiz.Domain.Entities;
using Analiz.Domain.Entities.ML;
using Analiz.Domain.ValueObjects;
using FraudShield.TransactionAnalysis.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Analiz.Application.Interfaces.Services;
using Analiz.Domain;
using Microsoft.ML.Transforms;

namespace Analiz.Application.Services;
/*
public class RiskService : IRiskService
{
    private readonly IModelService _modelService;
    private readonly IFeatureExtractionService _featureExtractor;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILogger<RiskService> _logger;

    // Threshold configurations
    private const double HIGH_RISK_THRESHOLD = 0.8;
    private const double MEDIUM_RISK_THRESHOLD = 0.5;
    private const double ANOMALY_THRESHOLD = 2.5;
    private const double HIGH_VALUE_THRESHOLD = 1000;

    public RiskService(
        IModelService modelService,
        IFeatureExtractionService featureExtractor,
        ITransactionRepository transactionRepository,
        IUserProfileRepository userProfileRepository,
        ILogger<RiskService> logger)
    {
        _modelService = modelService ?? throw new ArgumentNullException(nameof(modelService));
        _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
        _transactionRepository =
            transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _userProfileRepository =
            userProfileRepository ?? throw new ArgumentNullException(nameof(userProfileRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RiskEvaluation> EvaluateRiskAsync(TransactionData data)
    {
        try
        {
            _logger.LogInformation("Evaluating risk for transaction {TransactionId}", data.TransactionId);

            // 1. Extract features
            var features = await _featureExtractor.ExtractFeaturesAsync(data, ModelType.Ensemble);
            var normalizerTransformer = await _modelService.GetModelTransformerAsync("CreditCard_Normalizer");

            // 2. Create model input - Convert features to float array
            var modelInput = ConvertFeaturesToModelInput(features, (float)data.Amount,
                (NormalizingTransformer)normalizerTransformer);

            // 3. Get LightGBM prediction (supervised - probability based)
            var lightGbmTransformer =
                await _modelService.GetModelTransformerAsync("CreditCard_FraudDetection_LightGBM");
            var lightGbmPrediction = _modelService.PredictSingle(lightGbmTransformer, modelInput);

            // 4. Get PCA prediction (unsupervised - anomaly based)
            var pcaTransformer = await _modelService.GetModelTransformerAsync("CreditCard_AnomalyDetection_PCA");
            var pcaPrediction = _modelService.PredictSingle(pcaTransformer, modelInput);

            // 5. Get ensemble prediction (if available)
            double ensembleProbability = 0;
            try
            {
                var ensembleTransformer =
                    await _modelService.GetModelTransformerAsync("CreditCard_FraudDetection_Ensemble");
                var ensemblePrediction = _modelService.PredictSingle(ensembleTransformer, modelInput);
                ensembleProbability = ensemblePrediction.Probability;
            }
            catch
            {
                // If ensemble model is not available, use weighted average
                ensembleProbability = lightGbmPrediction.Probability * 0.7 +
                                      (pcaPrediction.AnomalyScore > ANOMALY_THRESHOLD ? 1.0 : 0.0) * 0.3;
            }

            // 6. Get user risk profile - Convert UserId to string
            var userRiskProfile = await GetUserRiskProfileAsync(data.UserId);

            // 7. Determine risk score
            var combinedScore = CalculateCombinedRiskScore(ensembleProbability, pcaPrediction.AnomalyScore);
            var riskFactors = await IdentifyRiskFactorsAsync(data, lightGbmPrediction);
            var riskFactorDescriptions = riskFactors.Select(rf => rf.Description).ToList();

            // Create risk score using the proper constructor
            var riskScore = RiskScore.Create(combinedScore, riskFactorDescriptions);

            // 8. Calculate feature importance
            var featureImportance = await CalculateFeatureImportanceAsync(data, lightGbmPrediction);

            // 9. Create and return risk evaluation
            return new RiskEvaluation
            {
                TransactionId = data.TransactionId,
                UserRiskProfile = userRiskProfile,
                EvaluatedAt = DateTime.UtcNow,
                FraudProbability = ensembleProbability,
                AnomalyScore = pcaPrediction.AnomalyScore,
                RiskScore = riskScore.Level,
                RiskFactors = riskFactors,
                FeatureValues = features.ToDictionary(kv => kv.Key, kv => (double)kv.Value),
                FeatureImportance = featureImportance
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating risk for transaction {TransactionId}", data.TransactionId);
            throw new InvalidOperationException("Failed to evaluate transaction risk", ex);
        }
    }

    public async Task<RiskScore> DetermineRiskScoreAsync(double fraudProbability, double anomalyScore)
    {
        try
        {
            _logger.LogDebug("Determining risk score - FraudProb: {FraudProb}, AnomalyScore: {AnomalyScore}",
                fraudProbability, anomalyScore);

            // Calculate a normalized score between 0 and 1
            var normalizedFraudScore = fraudProbability;
            var normalizedAnomalyScore = Math.Min(anomalyScore / ANOMALY_THRESHOLD, 1.0);

            // Use the higher of the two scores
            var combinedScore = Math.Max(normalizedFraudScore, normalizedAnomalyScore);

            // Create risk factors list based on scores
            var factors = new List<string>();

            if (fraudProbability > HIGH_RISK_THRESHOLD)
                factors.Add($"High fraud probability: {fraudProbability:P2}");

            if (anomalyScore > ANOMALY_THRESHOLD)
                factors.Add($"High anomaly score: {anomalyScore:F2}");

            // Create risk score with the proper constructor
            return RiskScore.Create(combinedScore, factors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining risk score");

            // Return default medium risk in case of error
            return RiskScore.Create(0.5, new List<string> { "Error calculating risk score" });
        }
    }

    public async Task<bool> IsHighRiskTransactionAsync(TransactionData data)
    {
        try
        {
            _logger.LogDebug("Checking if transaction {TransactionId} is high risk", data.TransactionId);

            // 1. Extract features
            var features = await _featureExtractor.ExtractFeaturesAsync(data, ModelType.Ensemble);

            // 2. Create model input
            var normalizerTransformer = await _modelService.GetModelTransformerAsync("CreditCard_Normalizer");

            // 2. Create model input - Convert features to float array
            var modelInput = ConvertFeaturesToModelInput(features, (float)data.Amount,
                (NormalizingTransformer)normalizerTransformer);


            // 3. Get LightGBM prediction
            var lightGbmTransformer =
                await _modelService.GetModelTransformerAsync("CreditCard_FraudDetection_LightGBM");
            var lightGbmPrediction = _modelService.PredictSingle(lightGbmTransformer, modelInput);

            // 4. Get PCA prediction
            var pcaTransformer = await _modelService.GetModelTransformerAsync("CreditCard_AnomalyDetection_PCA");
            var pcaPrediction = _modelService.PredictSingle(pcaTransformer, modelInput);

            // 5. Calculate combined score
            var combinedScore = CalculateCombinedRiskScore(lightGbmPrediction.Probability, pcaPrediction.AnomalyScore);

            // 6. Check if high risk (score > 0.75)
            return combinedScore >= 0.75;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if transaction is high risk, defaulting to high risk");
            return true; // Default to high risk when errors occur
        }
    }

    public async Task<List<RiskFactor>> IdentifyRiskFactorsAsync(TransactionData data, ModelPrediction prediction)
    {
        var factors = new List<RiskFactor>();

        try
        {
            // Extract model features from metadata
            if (prediction.Metadata != null && prediction.Metadata.TryGetValue("TopFeatures", out var topFeaturesObj))
            {
                string[] modelFeatures = topFeaturesObj.ToString().Split(',');
                foreach (var feature in modelFeatures)
                    factors.Add(RiskFactor.Create(
                        RiskFactorType.ModelFeature,
                        $"Contributing feature: {feature}",
                        prediction.Probability));
            }

            // Add anomaly detection as a risk factor if significant
            if (prediction.AnomalyScore > ANOMALY_THRESHOLD)
                factors.Add(RiskFactor.Create(
                    RiskFactorType.AnomalyDetection,
                    $"Unusual transaction pattern detected (score: {prediction.AnomalyScore:F2})",
                    Math.Min(prediction.AnomalyScore / (ANOMALY_THRESHOLD * 2), 1.0)));

            // Check for amount-related risks
            if (data.Amount > (decimal)HIGH_VALUE_THRESHOLD)
                factors.Add(RiskFactor.Create(
                    RiskFactorType.HighValue,
                    $"High value transaction (${data.Amount})",
                    Math.Min((double)data.Amount / 10000, 1.0)));

            // Add location-based risks if applicable
            if (data.Location != null && data.Location.IsHighRiskRegion)
                factors.Add(RiskFactor.Create(
                    RiskFactorType.Location,
                    $"Transaction from high-risk region ({data.Location.Country})",
                    0.7));

            // Check device info additionalInfo for suspicious indicators
            if (data.DeviceInfo != null && data.DeviceInfo.AdditionalInfo != null)
            {
                // Check for emulator
                if (data.DeviceInfo.AdditionalInfo.TryGetValue("isEmulator", out var isEmulatorStr) &&
                    bool.TryParse(isEmulatorStr, out var isEmulator) && isEmulator)
                    factors.Add(RiskFactor.Create(
                        RiskFactorType.Device,
                        "Transaction from emulator device",
                        0.8));

                // Check for rooted device
                if (data.DeviceInfo.AdditionalInfo.TryGetValue("isRooted", out var isRootedStr) &&
                    bool.TryParse(isRootedStr, out var isRooted) && isRooted)
                    factors.Add(RiskFactor.Create(
                        RiskFactorType.Device,
                        "Transaction from rooted/jailbroken device",
                        0.9));

                // Check IP risk score if available
                if (data.DeviceInfo.AdditionalInfo.TryGetValue("ipRiskScore", out var ipRiskScoreStr) &&
                    double.TryParse(ipRiskScoreStr, out var ipRiskScore) && ipRiskScore > 0.7)
                    factors.Add(RiskFactor.Create(
                        RiskFactorType.IPAddress,
                        $"Transaction from high-risk IP (score: {ipRiskScore:F2})",
                        ipRiskScore));
            }

            // Check user history for risks
            var userRiskProfile = await GetUserRiskProfileAsync(data.UserId);
            if (userRiskProfile.AverageRiskScore > 0.7)
                factors.Add(RiskFactor.Create(
                    RiskFactorType.UserBehavior,
                    $"User has high risk history (avg score: {userRiskProfile.AverageRiskScore:F2})",
                    userRiskProfile.AverageRiskScore));

            // Check for transaction frequency/velocity if implemented in repository
            try
            {
                var recentTransactionsCount = await _transactionRepository.GetTransactionCountAsync(
                    data.UserId, DateTime.UtcNow.AddHours(-24), DateTime.UtcNow);

                if (recentTransactionsCount > 10)
                    factors.Add(RiskFactor.Create(
                        RiskFactorType.Velocity,
                        $"High transaction velocity ({recentTransactionsCount} txns in 24h)",
                        Math.Min(recentTransactionsCount / 20.0, 1.0)));
            }
            catch
            {
                // Silently handle if repository doesn't implement this method
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying risk factors");
        }

        return factors;
    }

    public async Task<Dictionary<string, double>> CalculateFeatureImportanceAsync(TransactionData data,
        ModelPrediction prediction)
    {
        try
        {
            var features = await _featureExtractor.ExtractFeaturesAsync(data, ModelType.LightGBM);
            var featureImportance = new Dictionary<string, double>();

            // If the model provides feature importance, use it
            if (prediction.Metadata != null && prediction.Metadata.TryGetValue("TopFeatures", out var topFeaturesObj))
            {
                string[] importantFeatures = topFeaturesObj.ToString().Split(',');

                // Try to get feature weights if available
                double[] featureWeights = null;

                if (prediction.Metadata.TryGetValue("FeatureWeights", out var featureWeightsObj))
                    featureWeights = featureWeightsObj.ToString()
                        .Split(',')
                        .Select(w => double.TryParse(w, out var result) ? result : 0)
                        .ToArray();

                // Assign importance values
                for (var i = 0; i < importantFeatures.Length; i++)
                {
                    var weight = featureWeights != null && i < featureWeights.Length
                        ? featureWeights[i]
                        : 1.0 - (double)i /
                        Math.Max(1, importantFeatures.Length); // Decreasing importance if weights not available

                    featureImportance[importantFeatures[i]] = weight;
                }

                // Normalize to sum to 1
                var sum = featureImportance.Values.Sum();
                if (sum > 0)
                    foreach (var key in featureImportance.Keys.ToList())
                        featureImportance[key] /= sum;

                return featureImportance;
            }

            // Otherwise, use a simple heuristic approach based on feature values
            var featureDict = features.ToDictionary(kv => kv.Key, kv => Math.Abs((double)kv.Value));

            // Get the top 10 features by value
            var topFeaturesByValue = featureDict
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            // Normalize the values
            var totalSum = topFeaturesByValue.Values.Sum();
            if (totalSum > 0)
                foreach (var key in topFeaturesByValue.Keys)
                    featureImportance[key] = topFeaturesByValue[key] / totalSum;

            return featureImportance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating feature importance");
            return new Dictionary<string, double>();
        }
    }

    public async Task<RiskProfile> GetUserRiskProfileAsync(Guid userId)
    {
        try
        {
            // Try to get existing profile
            var profile = await _userProfileRepository.GetUserRiskProfileAsync(userId);

            // If profile exists and was updated recently, return it
            if (profile != null && (DateTime.UtcNow - profile.LastUpdated).TotalHours < 24) return profile;

            // Otherwise, calculate a new profile
            // Note: Implementation depends on your repository structure
            var transactions = await _transactionRepository.GetUserTransactionHistoryAsync(userId, 100);

            if (transactions == null || !transactions.Any())
            {
                // No transaction history, create new profile with default low risk
                var newProfile = new RiskProfile
                {
                    UserId = userId,
                    AverageRiskScore = 0.1,
                    TransactionCount = 0,
                    HighRiskTransactionCount = 0,
                    CommonRiskFactors = new Dictionary<string, int>(),
                    LastUpdated = DateTime.UtcNow,
                    AverageTransactionAmount = 0
                };

                await _userProfileRepository.SaveUserRiskProfileAsync(newProfile);
                return newProfile;
            }

            // Calculate risk profile metrics
            var riskScoreSum = 0.0;
            var highRiskCount = 0;
            var avgAmount = transactions.Average(t => (double)t.Amount);

            // Count risk factors
            var riskFactorCount = new Dictionary<string, int>();

            // Create new profile with calculated metrics
            var updatedProfile = new RiskProfile
            {
                UserId = userId,
                AverageRiskScore = riskScoreSum / transactions.Count(),
                TransactionCount = transactions.Count(),
                HighRiskTransactionCount = highRiskCount,
                CommonRiskFactors = riskFactorCount,
                LastUpdated = DateTime.UtcNow,
                AverageTransactionAmount = avgAmount
            };

            // Save the new profile
            await _userProfileRepository.SaveUserRiskProfileAsync(updatedProfile);

            return updatedProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user risk profile for {UserId}", userId);

            // Return a default profile in case of error
            return new RiskProfile
            {
                UserId = userId,
                AverageRiskScore = 0.5, // Medium risk if we can't determine
                TransactionCount = 0,
                HighRiskTransactionCount = 0,
                CommonRiskFactors = new Dictionary<string, int>(),
                LastUpdated = DateTime.UtcNow,
                AverageTransactionAmount = 0
            };
        }
    }

    private ModelInput ConvertFeaturesToModelInput(
        Dictionary<string, float> features,
        float amount,
        NormalizingTransformer normalizer)
    {
        // Normalization transformer’dan ilgili model parametrelerini al
        var modelParams = normalizer.GetNormalizerModelParameters(0);

        float minAmount = 0;
        float maxAmount = 1;

        if (modelParams is NormalizingTransformer.AffineNormalizerModelParameters<float> affineParams)
        {
            minAmount = affineParams.Offset;
            maxAmount = affineParams.Scale + affineParams.Offset;
        }

        return new ModelInput
        {
            Amount = (amount - minAmount) / (maxAmount - minAmount),

            TimeSin = features.GetValueOrDefault("TimeSin", 0),
            TimeCos = features.GetValueOrDefault("TimeCos", 0),
            DayFeature = features.GetValueOrDefault("DayFeature", 0),
            HourFeature = features.GetValueOrDefault("HourFeature", 0),

            V1 = features.GetValueOrDefault("V1", 0),
            V2 = features.GetValueOrDefault("V2", 0),
            V3 = features.GetValueOrDefault("V3", 0),
            V4 = features.GetValueOrDefault("V4", 0),
            V5 = features.GetValueOrDefault("V5", 0),
            V6 = features.GetValueOrDefault("V6", 0),
            V7 = features.GetValueOrDefault("V7", 0),
            V8 = features.GetValueOrDefault("V8", 0),
            V9 = features.GetValueOrDefault("V9", 0),
            V10 = features.GetValueOrDefault("V10", 0),
            V11 = features.GetValueOrDefault("V11", 0),
            V12 = features.GetValueOrDefault("V12", 0),
            V13 = features.GetValueOrDefault("V13", 0),
            V14 = features.GetValueOrDefault("V14", 0),
            V15 = features.GetValueOrDefault("V15", 0),
            V16 = features.GetValueOrDefault("V16", 0),
            V17 = features.GetValueOrDefault("V17", 0),
            V18 = features.GetValueOrDefault("V18", 0),
            V19 = features.GetValueOrDefault("V19", 0),
            V20 = features.GetValueOrDefault("V20", 0),
            V21 = features.GetValueOrDefault("V21", 0),
            V22 = features.GetValueOrDefault("V22", 0),
            V23 = features.GetValueOrDefault("V23", 0),
            V24 = features.GetValueOrDefault("V24", 0),
            V25 = features.GetValueOrDefault("V25", 0),
            V26 = features.GetValueOrDefault("V26", 0),
            V27 = features.GetValueOrDefault("V27", 0),
            V28 = features.GetValueOrDefault("V28", 0)
        };
    }

    // Helper methods

    private float[] ConvertFeaturesToFloatArray(Dictionary<string, float> features)
    {
        return features.Values.ToArray();
    }

    private double CalculateCombinedRiskScore(double fraudProbability, double anomalyScore)
    {
        // Normalize the anomaly score to 0-1 range
        var normalizedAnomalyScore = Math.Min(anomalyScore / ANOMALY_THRESHOLD, 1.0);

        // Take the higher of the two scores
        return Math.Max(fraudProbability, normalizedAnomalyScore);
    }
}*/