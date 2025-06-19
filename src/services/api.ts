// API Configuration
const API_BASE_URL = 'http://localhost:5112';
const PYTHON_API_URL = 'http://localhost:5001';

// Backend'deki gerçek interface'ler
export interface ModelMetrics {
  // Temel metrikler
  accuracy: number;
  precision: number;
  recall: number;
  f1Score: number;
  auc: number;
  
  // Confusion Matrix bileşenleri
  truePositive: number;
  trueNegative: number;
  falsePositive: number;
  falseNegative: number;
  
  // Ek sınıflandırma metrikleri
  specificity: number;
  sensitivity: number;
  npv: number; // Negative Predictive Value
  fpr: number; // False Positive Rate
  fnr: number; // False Negative Rate
  fdr: number; // False Discovery Rate
  for: number; // False Omission Rate
  
  // ROC ve PR Curve metrikleri
  aucpr: number; // Area Under Precision-Recall Curve
  optimalThreshold: number;
  balancedAccuracy: number;
  
  // İstatistiksel metrikler
  matthewsCorrCoef: number;
  cohenKappa: number;
  logLoss: number;
  brierScore: number;
  
  // Model performans özellikleri
  supportClass0: number;
  supportClass1: number;
  classImbalanceRatio: number;
  
  // Anomaly Detection için özel metrikler (PCA)
  anomalyThreshold?: number;
  meanReconstructionError?: number;
  stdReconstructionError?: number;
}

export interface ModelVersion {
  id: string;
  modelName: string;
  version: string;
  type: 'LightGBM' | 'PCA' | 'Ensemble' | 'AttentionModel' | 'AutoEncoder' | 'IsolationForest';
  status: 'Training' | 'Testing' | 'Active' | 'Archived';
  metricsJson: string; // JSON string olarak gelen metrikler
  metrics: ModelMetrics; // Parse edilmiş metrikler
  configuration: string;
  trainedAt: string;
  lastUsedAt?: string;
  createdAt: string;
  createdBy: string;
  lastModifiedAt?: string;
  lastModifiedBy: string;
}

export interface TrainingRequest {
  modelName?: string;
  modelType: 'LightGBM' | 'PCA' | 'Ensemble';
  configuration?: any;
}

export interface TrainingResult {
  modelId: string;
  actualModelName?: string;
  trainingTime: string;
  metrics?: ModelMetrics;
  basicMetrics?: any;
  confusionMatrix?: any;
  extendedMetrics?: any;
  performanceSummary?: any;
  error?: string;
}

export interface ModelComparison {
  models: Array<{
    modelName: string;
    metrics: ModelMetrics;
    grade: string;
    overallScore: number;
  }>;
  recommendation: string;
  bestModel: string;
  detailedComparison: {
    [metricName: string]: Array<{
      modelName: string;
      value: number;
      rank: number;
    }>;
  };
}

// LightGBM Configuration
export interface LightGBMConfig {
  numberOfLeaves?: number;
  minDataInLeaf?: number;
  learningRate?: number;
  numberOfTrees?: number;
  featureFraction?: number;
  baggingFraction?: number;
  baggingFrequency?: number;
  l1Regularization?: number;
  l2Regularization?: number;
  earlyStoppingRound?: number;
  minGainToSplit?: number;
  useClassWeights?: boolean;
  classWeights?: { [key: string]: number };
  predictionThreshold?: number;
}

// PCA Configuration
export interface PCAConfig {
  components?: number;
  contamination?: number;
  threshold?: number;
  scaleFeatures?: boolean;
  useWhitening?: boolean;
  randomState?: number;
}

// Ensemble Configuration
export interface EnsembleConfig {
  models?: string[];
  weights?: number[];
  votingMethod?: 'majority' | 'weighted' | 'soft';
  useStackingMeta?: boolean;
  metaLearner?: string;
  crossValidationFolds?: number;
}

// Transaction Types
export interface Transaction {
  transactionId: string;
  amount: number;
  merchantCategory: string;
  hour: number;
  dayOfWeek: number;
  isWeekend: boolean;
  userId: string;
  userAge: number;
  userGender: 'M' | 'F';
  timestamp: string;
}

export interface ModelPrediction {
  transactionId: string;
  isFraudulent: 'Yes' | 'No';
  probability: number;
  score: number;
  anomalyScore: number;
  riskLevel: 'Low' | 'Medium' | 'High';
  modelType?: string;
  modelVersion?: string;
  predictionTime?: string;
  featureContributions?: { [key: string]: number };
  metadata?: { [key: string]: any };
}

export interface FraudRule {
  id: number;
  name: string;
  description: string;
  isActive: boolean;
  triggerCount: number;
  successRate: number;
}

export interface ShapExplanation {
  transactionId: string;
  modelName: string;
  prediction: number;
  expectedValue: number;
  features: Array<{
    name: string;
    value: any;
    shapValue: number;
    impact: 'positive' | 'negative';
  }>;
  summary: {
    topPositiveFeatures: Array<{ name: string; value: number }>;
    topNegativeFeatures: Array<{ name: string; value: number }>;
    totalPositiveImpact: number;
    totalNegativeImpact: number;
  };
}

class FraudDetectionAPI {
  private apiClient: any;

  constructor() {
    this.apiClient = {
      get: async (url: string) => {
        const response = await fetch(`${API_BASE_URL}${url}`);
        if (!response.ok) throw new Error(`API Error: ${response.statusText}`);
        return response.json();
      },
      post: async (url: string, data?: any) => {
        const response = await fetch(`${API_BASE_URL}${url}`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: data ? JSON.stringify(data) : undefined,
        });
        if (!response.ok) throw new Error(`API Error: ${response.statusText}`);
        return response.json();
      },
    };
  }

  // Model Training APIs
  async trainLightGBM(config?: LightGBMConfig): Promise<TrainingResult> {
    const endpoint = config ? '/api/model/train/lightgbm-config' : '/api/model/train/lightgbm';
    return this.apiClient.post(endpoint, config);
  }

  async trainPCA(config?: PCAConfig): Promise<TrainingResult> {
    const endpoint = config ? '/api/model/train/pca-config' : '/api/model/train/pca';
    return this.apiClient.post(endpoint, config);
  }

  async trainEnsemble(config?: EnsembleConfig): Promise<TrainingResult> {
    const endpoint = config ? '/api/model/train/ensemble-config' : '/api/model/train/ensemble';
    return this.apiClient.post(endpoint, config);
  }

  async trainAttentionModel(config?: any): Promise<TrainingResult> {
    return this.apiClient.post('/api/model/train/attention', config);
  }

  async trainAutoEncoder(config?: any): Promise<TrainingResult> {
    return this.apiClient.post('/api/model/train/autoencoder', config);
  }

  async trainIsolationForest(config?: any): Promise<TrainingResult> {
    return this.apiClient.post('/api/model/train/isolation-forest', config);
  }

  // Model Information APIs
  async getModelMetrics(modelName: string): Promise<ModelMetrics> {
    return this.apiClient.get(`/api/model/${modelName}/metrics`);
  }

  async getModelPerformanceSummary(modelName: string): Promise<any> {
    return this.apiClient.get(`/api/model/${modelName}/performance-summary`);
  }

  async getModelVersions(modelName: string): Promise<ModelVersion[]> {
    return this.apiClient.get(`/api/model/${modelName}/versions`);
  }

  async getAllModels(): Promise<ModelVersion[]> {
    // Bu endpoint backend'de yoksa eklenebilir
    return this.apiClient.get('/api/model/all');
  }

  async compareModels(modelNames: string[]): Promise<ModelComparison> {
    return this.apiClient.post('/api/model/compare', modelNames);
  }

  async compareAdvancedModels(modelTypes: string[]): Promise<ModelComparison> {
    return this.apiClient.post('/api/model/compare-advanced', modelTypes);
  }

  // Model Activation APIs
  async activateModelVersion(modelName: string, version: string): Promise<any> {
    return this.apiClient.post(`/api/model/${modelName}/versions/${version}/activate`);
  }

  // Prediction APIs
  async predict(transaction: Transaction): Promise<ModelPrediction> {
    return this.apiClient.post('/api/model/predict', transaction);
  }

  async predictWithModel(modelType: string, transaction: Transaction): Promise<ModelPrediction> {
    return this.apiClient.post(`/api/model/${modelType}/predict`, transaction);
  }

  async predictAdvanced(modelType: string, transaction: Transaction): Promise<ModelPrediction> {
    return this.apiClient.post(`/api/model/predict/advanced/${modelType}`, transaction);
  }

  // SHAP Analysis (Python API)
  async getShapExplanation(transactionId: string, modelName?: string): Promise<ShapExplanation> {
    const pythonClient = {
      post: async (url: string, data?: any) => {
        const response = await fetch(`${PYTHON_API_URL}${url}`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
          },
          body: data ? JSON.stringify(data) : undefined,
        });
        if (!response.ok) throw new Error(`Python API Error: ${response.statusText}`);
        return response.json();
      }
    };

    return pythonClient.post('/analyze/shap', { 
      transactionId, 
      modelName: modelName || 'default' 
    });
  }

  // Data APIs
  async getTransactions(limit: number = 100, offset: number = 0): Promise<Transaction[]> {
    return this.apiClient.get(`/api/data/transactions?limit=${limit}&offset=${offset}`);
  }

  async getFraudRules(): Promise<FraudRule[]> {
    return this.apiClient.get('/api/rules/fraud');
  }

  // Utility APIs
  async healthCheck(): Promise<{ status: string; timestamp: string }> {
    return this.apiClient.get('/health');
  }

  async getSystemStatus(): Promise<any> {
    return this.apiClient.get('/api/system/status');
  }
}

export default new FraudDetectionAPI(); 