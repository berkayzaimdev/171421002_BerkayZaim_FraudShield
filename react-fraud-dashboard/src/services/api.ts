import axios, { AxiosResponse } from 'axios';

// ==================== BASE CONFIGURATION ====================
const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'http://localhost:5112';

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 300000, // 5 dakika genel timeout
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  }
});

// Model eğitimi için özel timeout ayarları
const trainingApi = axios.create({
  baseURL: API_BASE_URL,
  timeout: 600000, // 10 dakika model eğitimi için
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  }
});

// ==================== REQUEST/RESPONSE INTERCEPTORS ====================
api.interceptors.request.use(
  (config) => {
    console.log(`🚀 API Request: ${config.method?.toUpperCase()} ${config.url}`, config.data);
    return config;
  },
  (error) => {
    console.error('❌ API Request Error:', error);
    return Promise.reject(error);
  }
);

api.interceptors.response.use(
  (response) => {
    console.log(`✅ API Response: ${response.status} ${response.config.url}`, response.data);
    return response;
  },
  (error) => {
    console.error('❌ API Response Error:', error.response?.status, error.response?.data || error.message);

    // Detaylı hata bilgisi
    if (error.response?.data) {
      console.error('🔍 Detailed Error Response:', {
        status: error.response.status,
        statusText: error.response.statusText,
        url: error.config?.url,
        method: error.config?.method,
        requestData: error.config?.data,
        responseData: error.response.data,
        validationErrors: error.response.data?.errors,
        title: error.response.data?.title,
        message: error.response.data?.message
      });
    }

    return Promise.reject(error);
  }
);

// ==================== TYPE DEFINITIONS ====================
export interface ApiResponse<T = any> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}

// ========== .NET BACKEND RESPONSE MODELS ==========
export interface FraudDetectionResponse {
  riskScore: number;
  riskLevel: string;
  decision: string;
  actions: string[];
  triggeredRules: Array<{
    ruleId: string;
    ruleName: string;
    ruleCode: string;
    isTriggered: boolean;
    confidence: number;
    actions: string[];
  }>;
  resultMessage: string;
}

export interface AnalysisResult {
  id: string;
  transactionId: string;
  anomalyScore: number;
  fraudProbability: number;
  riskScore: RiskScore;
  riskFactors: RiskFactor[];
  decision: string;
  analyzedAt: string;
  status: string;
  error?: string;
  totalRuleCount: number;
  triggeredRuleCount: number;
  appliedActions: string[];
  triggeredRules: TriggeredRuleInfo[];
  mlAnalysis?: MLAnalysisResult;
  fraudAlert?: FraudAlert;
  transaction?: Transaction;
}

export interface RiskScore {
  value: number;
  level: string;
  factors: string[];
}

export interface TriggeredRuleInfo {
  ruleId: string;
  ruleName: string;
  ruleCode: string;
  isTriggered: boolean;
  confidence: number;
  actions: string[];
  context?: string;
}

export interface MLAnalysisResult {
  primaryModel: PrimaryModelInfo;
  modelScores: Record<string, ModelScoreInfo>;
  modelHealth: ModelHealthInfo;
  confidence: number;
  processingTimeMs: number;
  featureImportance: Record<string, number>;
  usedAlgorithms: string[];
  additionalInfo: Record<string, any>;
}

export interface PrimaryModelInfo {
  modelType: string;
  modelSource: string;
  fraudProbability: number;
  anomalyScore: number;
  isEnsemble: boolean;
  isSuccessful: boolean;
}

export interface ModelScoreInfo {
  probability: number;
  score: number;
  anomalyScore: number;
  isAvailable: boolean;
  source: string;
}

export interface ModelHealthInfo {
  ensembleAvailable: boolean;
  lightGBMAvailable: boolean;
  pcaAvailable: boolean;
  fallbackUsed: boolean;
  errorCount: number;
  warningCount: number;
}

export interface AnalysisSummary {
  transactionId: string;
  overallRisk: string;
  fraudProbability: number;
  anomalyScore: number;
  decision: string;
  ruleBasedAnalysis: RuleBasedSummary;
  mlBasedAnalysis?: MLBasedSummary;
  riskFactorsSummary: RiskFactorsSummary;
  isSuccessful: boolean;
  analysisTime: string;
}

export interface RuleBasedSummary {
  totalRules: number;
  triggeredRules: number;
  appliedActions: string[];
}

export interface MLBasedSummary {
  primaryModel: string;
  confidence: number;
  ensembleUsed: boolean;
  modelHealth: string;
  processingTime: number;
}

export interface RiskFactorsSummary {
  totalCount: number;
  highSeverityCount: number;
  mlBasedCount: number;
  ruleBasedCount: number;
}

export interface FraudAlert {
  id: string;
  transactionId: string;
  userId: string;
  riskScore: RiskScore;
  factors: string[];
  createdAt: string;
  status: string;
}

export interface ModelEvaluationResponse {
  transactionId: string;
  fraudProbability: number;
  anomalyScore: number;
  riskLevel: string;
  modelVersion?: string;
  confidence: number;
  evaluatedAt: string;
  processingTimeMs?: number;
  modelInfo?: Record<string, any>;
  features?: Record<string, number>;
  featureImportance?: Record<string, number>;
}

export interface ComprehensiveFraudCheckResponse {
  transactionId: string;
  overallRiskScore: number;
  overallRiskLevel: string;
  overallDecision: string;
  fraudProbability: number;
  anomalyScore: number;
  processingTime: number;
  contextResults: {
    account: FraudDetectionResponse;
    ip: FraudDetectionResponse;
    device: FraudDetectionResponse;
    session: FraudDetectionResponse;
  };
  triggeredRules: Array<{
    ruleId: string;
    ruleName: string;
    ruleCode: string;
    context: string;
    isTriggered: boolean;
    confidence: number;
    actions: string[];
  }>;
  appliedActions: string[];
  resultMessage: string;
}

export interface RiskFactor {
  id: string;
  code: string;
  description: string;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  confidence: number;
  source: string;
  ruleId?: string | null;
  detectedAt: string;
  metadata: Record<string, any>;
  analysisResultId?: string;
  actionTaken?: string | null;
  type?: string;
}

// ========== ENUMS ==========
export enum ModelStatus {
  Training = 'Training',
  Active = 'Active',
  Inactive = 'Inactive',
  Failed = 'Failed'
}

export enum ModelType {
  LightGBM = 'LightGBM',
  PCA = 'PCA',
  Ensemble = 'Ensemble',
  AttentionModel = 'AttentionModel',
  AutoEncoder = 'AutoEncoder',
  IsolationForest = 'IsolationForest'
}

export enum TransactionType {
  Purchase = 1,
  Withdrawal = 2,
  Transfer = 3,
  Deposit = 4,
  CreditCard = 5
}

export enum DecisionType {
  Approve = 'Approve',
  Deny = 'Deny',
  Review = 'Review'
}

export enum RiskLevel {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical'
}

// ========== ENTITY TYPES ==========
export interface Transaction {
  id: string;
  transactionId: string;
  userId: string;
  amount: number;
  merchantId?: string;
  type: 'Purchase' | 'Withdrawal' | 'Transfer' | 'Deposit' | 'CreditCard';
  status: 'pending' | 'analyzing' | 'completed' | 'failed';
  timestamp: string;
  ipAddress: string;
  deviceId: string;
  location: {
    country: string;
    city: string;
    latitude?: number;
    longitude?: number;
  };
  deviceInfo: {
    deviceType: string;
    userAgent: string;
  };
  // Additional properties for compatibility
  merchantCategory?: string;
  hour?: number;
  dayOfWeek?: number;
  isWeekend?: boolean;
  userAge?: number;
  userGender?: string;
}

export interface ModelMetadata {
  id: string;
  ModelName: string;
  Version: string;
  Type: ModelType;
  Status: ModelStatus;
  Configuration: string;
  TrainedAt: string;
  LastUsedAt?: string;
  MetricsJson?: string;
  Metrics: { [key: string]: number };
  CreatedAt: string;
  CreatedBy: string;
  LastModifiedBy: string;
  // Backend'den gelen yeni response yapısı için
  data?: {
    basicMetrics?: {
      accuracy?: number;
      precision?: number;
      recall?: number;
      f1Score?: number;
      f1_score?: number;
      auc?: number;
      aucpr?: number;
    };
    modelId?: string;
    modelName?: string;
    actualModelName?: string;
  };
}

export type ModelVersion = ModelMetadata;

export interface ModelMetrics {
  auc?: number;
  recall?: number;
  pca_auc?: number;
  accuracy?: number;
  f1_score?: number;
  precision?: number;
  lightgbm_auc?: number;
  balancedAccuracy?: number;
  fdr?: number;
  aucpr?: number;
  anomalyThreshold?: number;
  meanReconstructionError?: number;
  stdReconstructionError?: number;
  fpr?: number;
}

export interface FraudRule {
  id: string;
  name: string;
  description: string;
  condition?: string;
  action?: string;
  severity?: 'Low' | 'Medium' | 'High' | 'Critical';
  isActive: boolean;
  triggerCount: number;
  successRate: number;
  createdAt?: string;
}

export interface ModelPrediction {
  probability: number;
  score: number;
  anomalyScore?: number;
  confidence?: number;
  transactionId?: string;
  isFraudulent?: string;
  riskLevel?: string;
  modelType?: string;
}

export interface ShapExplanation {
  featureImportance: Record<string, number>;
  shapValues: Record<string, number>;
  transactionId?: string;
  businessExplanation?: string;
  plotUrls?: string[];
  baseValue?: number;
  prediction?: number;
}

export interface TrainingResult {
  success?: boolean;
  data?: any;
  modelId: string;
  actualModelName?: string;
  metrics?: ModelMetrics;
  BasicMetrics?: any;
  trainingTime?: number;
  error?: string;
}

export interface LightGBMConfig {
  numLeaves?: number;
  learningRate?: number;
  featureFraction?: number;
  baggingFraction?: number;
  baggingFreq?: number;
  minChildSamples?: number;
  numIterations?: number;
  numberOfTrees?: number;
  numberOfLeaves?: number;
  useClassWeights?: boolean;
  l1Regularization?: number;
}

export interface PCAConfig {
  nComponents?: number;
  whiten?: boolean;
  svdSolver?: string;
  randomState?: number;
  componentCount?: number;
  anomalyThreshold?: number;
  standardizeInput?: boolean;
}

export interface EnsembleConfig {
  models?: string[];
  weights?: Record<string, number>;
  votingMethod?: 'hard' | 'soft';
  lightgbmWeight?: number;
  pcaWeight?: number;
  threshold?: number;
  enableCrossValidation?: boolean;
}

export interface HyperparameterOptimizationConfig {
  modelType: string;
  parameterSpace: Record<string, any>;
  nTrials: number;
  cvFolds: number;
  scoringMetric: string;
  optimizationMetric?: string;
  searchStrategy?: 'grid' | 'random' | 'bayesian';
  maxExperiments?: number;
  parameterGrid?: Record<string, any>;
}

export interface OptimizationResult {
  bestParams: Record<string, any>;
  bestScore: number;
  trials: any[];
  optimizationTime: number;
}

// ========== REQUEST MODELS ==========
export interface SessionCheckRequest {
  sessionId: string;
  accountId: string;
  startTime: string;
  lastActivityTime: string;
  durationMinutes: number;
  ipAddress: string;
  deviceId: string;
  userAgent: string;
  rapidNavigationCount: number;
  additionalData?: Record<string, any>;
}

export interface ModelEvaluationRequest {
  transactionId: string;
  amount: number;
  transactionDate: string;
  transactionType: TransactionType;
  features: Record<string, number>;
  additionalData?: Record<string, any>;
}

export interface ComprehensiveFraudCheckRequest {
  transactionId: string;
  transaction: {
    transactionId: string;
    accountId: string;
    amount: number;
    currency: string;
    transactionType: TransactionType;
    transactionDate: string;
    userTransactionCount24h: number;
    userTotalAmount24h: number;
    userAverageTransactionAmount: number;
    daysSinceFirstTransaction: number;
    uniqueRecipientCount1h: number;
    additionalData?: Record<string, any>;
  };
  account: {
    accountId: string;
    username: string;
    accessDate: string;
    ipAddress: string;
    countryCode: string;
    city: string;
    deviceId: string;
    isTrustedDevice: boolean;
    uniqueIpCount24h: number;
    uniqueCountryCount24h: number;
    isSuccessful: boolean;
    failedLoginAttempts: number;
    typicalAccessHours: number[];
    typicalAccessDays: string[];
    typicalCountries: string[];
  };
  ipAddress: {
    ipAddress: string;
    countryCode: string;
    city: string;
    ispAsn: string;
    reputationScore: number;
    isBlacklisted: boolean;
    blacklistNotes: string;
    isDatacenterOrProxy: boolean;
    networkType: string;
    uniqueAccountCount10m: number;
    uniqueAccountCount1h: number;
    uniqueAccountCount24h: number;
    failedLoginCount10m: number;
  };
  device: {
    deviceId: string;
    deviceType: string;
    operatingSystem: string;
    browser: string;
    ipAddress: string;
    countryCode: string;
    isEmulator: boolean;
    isJailbroken: boolean;
    isRooted: boolean;
    firstSeenDate: string;
    lastSeenDate: string;
    uniqueAccountCount24h: number;
    uniqueIpCount24h: number;
  };
  session: {
    sessionId: string;
    accountId: string;
    startTime: string;
    lastActivityTime: string;
    durationMinutes: number;
    ipAddress: string;
    deviceId: string;
    userAgent: string;
    rapidNavigationCount: number;
  };
  modelEvaluation: {
    transactionId: string;
    amount: number;
    transactionDate: string;
    transactionType: TransactionType;
    features: Record<string, number>;
    additionalData?: Record<string, any>;
  };
}

export interface TransactionCheckRequest {
  transactionId: string;
  accountId: string;
  amount: number;
  currency: string;
  transactionType: TransactionType;
  transactionDate: string;
  recipientAccountId: string;
  recipientAccountNumber: string;
  recipientCountry: string;
  userTransactionCount24h: number;
  userTotalAmount24h: number;
  userAverageTransactionAmount: number;
  daysSinceFirstTransaction: number;
  uniqueRecipientCount1h: number;
  additionalData?: Record<string, any>;
}

export interface AccountAccessCheckRequest {
  accountId: string;
  username: string;
  accessDate: string;
  ipAddress: string;
  countryCode: string;
  city: string;
  deviceId: string;
  isTrustedDevice: boolean;
  uniqueIpCount24h: number;
  uniqueCountryCount24h: number;
  isSuccessful: boolean;
  failedLoginAttempts: number;
  typicalAccessHours: number[];
  typicalAccessDays: string[];
  typicalCountries: string[];
  additionalData?: Record<string, any>;
}

export interface IpCheckRequest {
  ipAddress: string;
  countryCode: string;
  city: string;
  ispAsn: string;
  reputationScore: number;
  isBlacklisted: boolean;
  blacklistNotes: string;
  isDatacenterOrProxy: boolean;
  networkType: string;
  uniqueAccountCount10m: number;
  uniqueAccountCount1h: number;
  uniqueAccountCount24h: number;
  failedLoginCount10m: number;
  additionalData?: Record<string, any>;
}

export interface DeviceCheckRequest {
  deviceId: string;
  deviceType: string;
  operatingSystem: string;
  browser: string;
  ipAddress: string;
  countryCode: string;
  isEmulator: boolean;
  isJailbroken: boolean;
  isRooted: boolean;
  firstSeenDate: string;
  lastSeenDate: string;
  uniqueAccountCount24h: number;
  uniqueIpCount24h: number;
  additionalData?: Record<string, any>;
}

// ========== FRAUD RULE MANAGEMENT TYPES ==========
export interface FraudRuleResponse {
  id: string;
  ruleCode: string;
  name: string;
  description: string;
  category: RuleCategory;
  type: RuleType;
  impactLevel: ImpactLevel;
  status: RuleStatus;
  actions: RuleAction[];
  actionDuration?: string; // TimeSpan as string
  priority: number;
  condition: string;
  configurationJson: string;
  validFrom?: string;
  validTo?: string;
  createdDate: string;
  lastModified: string;
  modifiedBy: string;
}

export interface FraudEventResponse {
  id: string;
  ruleId: string;
  ruleName: string;
  ruleCode: string;
  transactionId?: string;
  accountId?: string;
  ipAddress: string;
  deviceInfo: string;
  actions: RuleAction[];
  actionDuration?: string; // TimeSpan as string
  actionEndDate?: string;
  eventDetailsJson: string;
  createdDate: string;
  resolvedDate?: string;
  resolvedBy?: string;
  resolutionNotes?: string;
}

export interface FraudRuleCreateRequest {
  name: string;
  description: string;
  category: RuleCategory;
  type: RuleType;
  impactLevel: ImpactLevel;
  actions: RuleAction[];
  actionDuration?: string;
  priority: number;
  condition: string;
  configuration: string;
  validFrom?: string;
  validTo?: string;
}

export interface FraudRuleUpdateRequest {
  name: string;
  description: string;
  category: RuleCategory;
  type: RuleType;
  impactLevel: ImpactLevel;
  actions: RuleAction[];
  actionDuration?: string;
  priority: number;
  condition: string;
  configuration: string;
  validFrom?: string;
  validTo?: string;
}

export interface FraudEventResolveRequest {
  status: string;
  resolutionNotes: string;
}

// ========== ENUMS FROM BACKEND ==========
export enum RuleCategory {
  Network = 'Network',
  IP = 'IP',
  Account = 'Account',
  Device = 'Device',
  Session = 'Session',
  Transaction = 'Transaction',
  Behavior = 'Behavior',
  Time = 'Time',
  Location = 'Location',
  Other = 'Other',
  Complex = 'Complex'
}

export enum RuleType {
  Simple = 'Simple',
  Threshold = 'Threshold',
  Complex = 'Complex',
  Sequential = 'Sequential',
  Behavioral = 'Behavioral',
  Anomaly = 'Anomaly',
  Blacklist = 'Blacklist',
  Whitelist = 'Whitelist',
  Custom = 'Custom'
}

export enum ImpactLevel {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical'
}

export enum RuleStatus {
  Draft = 'Draft',
  Active = 'Active',
  Inactive = 'Inactive',
  TestMode = 'TestMode',
  Archived = 'Archived'
}

export enum RuleAction {
  Log = 'Log',
  Notify = 'Notify',
  RequireAdditionalVerification = 'RequireAdditionalVerification',
  DelayProcessing = 'DelayProcessing',
  PutUnderReview = 'PutUnderReview',
  RejectTransaction = 'RejectTransaction',
  TerminateSession = 'TerminateSession',
  LockAccount = 'LockAccount',
  SuspendAccount = 'SuspendAccount',
  RequireKYCVerification = 'RequireKYCVerification',
  BlockDevice = 'BlockDevice',
  BlockIP = 'BlockIP',
  BlacklistIP = 'BlacklistIP',
  Block = 'Block',
  Review = 'Review',
  EscalateToManager = 'EscalateToManager'
}

// ========== RISK FACTOR TYPES ==========
export interface RiskFactorResponse {
  id: string;
  transactionId: string;
  code: string;
  type: string;
  description: string;
  confidence: number;
  severity: string;
  analysisResultId: string;
  ruleId?: string | null;
  actionTaken?: string | null;
  source: string;
  detectedAt: string;
}

export interface RiskFactorSummary {
  totalCount: number;
  criticalCount: number;
  highCount: number;
  mediumCount: number;
  lowCount: number;
  typeDistribution: Record<string, number>;
  sourceDistribution: Record<string, number>;
  averageConfidence: number;
}

// ========== RISK FACTOR ENUMS ==========
export enum RiskFactorType {
  ModelFeature = 'ModelFeature',
  AnomalyDetection = 'AnomalyDetection',
  HighValue = 'HighValue',
  Location = 'Location',
  Frequency = 'Frequency',
  TimePattern = 'TimePattern',
  RuleViolation = 'RuleViolation',
  UserBehavior = 'UserBehavior',
  DeviceAnomaly = 'DeviceAnomaly',
  Time = 'Time',
  Device = 'Device',
  IPAddress = 'IPAddress',
  Velocity = 'Velocity',
  RecurringPattern = 'RecurringPattern'
}

// ========== BLACKLIST TYPES ==========
export interface BlacklistItemResponse {
  id: string;
  type: string;
  value: string;
  reason: string;
  status: string;
  addedBy: string;
  createdAt: string;
  expiryDate?: string | null;
  invalidatedBy?: string | null;
  invalidatedAt?: string | null;
  isActive: boolean;
  isExpired: boolean;
}

export interface BlacklistItemCreateRequest {
  type: BlacklistType;
  value: string;
  reason: string;
  durationHours?: number | null;
  addedBy?: string | null;
}

export interface BlacklistInvalidateRequest {
  invalidatedBy?: string | null;
}

export interface BlacklistCheckRequest {
  type: BlacklistType;
  value: string;
}

export interface BlacklistSummary {
  totalIpCount: number;
  activeIpCount: number;
  totalAccountCount: number;
  activeAccountCount: number;
  totalDeviceCount: number;
  activeDeviceCount: number;
  totalCountryCount: number;
  activeCountryCount: number;
}

export enum BlacklistType {
  IpAddress = 'IpAddress',
  Account = 'Account',
  Device = 'Device',
  Country = 'Country'
}

export enum BlacklistStatus {
  Active = 'Active',
  Invalidated = 'Invalidated',
  Expired = 'Expired'
}

// ==================== UTILITY FUNCTIONS ====================
const handleApiCall = async <T>(apiCall: () => Promise<AxiosResponse<T>>): Promise<ApiResponse<T>> => {
  try {
    const response = await apiCall();
    return {
      success: true,
      data: response.data
    };
  } catch (error: any) {
    console.error('API call failed:', error);

    // Validation hatalarını detaylı logla
    if (error.response?.data?.errors) {
      console.error('🔍 Validation Errors:', error.response.data.errors);
    }

    return {
      success: false,
      error: error.response?.data?.message || error.message || 'Bilinmeyen bir hata oluştu',
      data: error.response?.data
    };
  }
};

// Model eğitimi için özel API call handler
const handleTrainingApiCall = async <T>(apiCall: () => Promise<AxiosResponse<T>>): Promise<ApiResponse<T>> => {
  try {
    console.log('🚀 Training API call starting...');
    const response = await apiCall();
    console.log('✅ Training API call completed successfully');
    return {
      success: true,
      data: response.data
    };
  } catch (error: any) {
    console.error('❌ Training API call failed:', error);
    if (error.code === 'ECONNABORTED') {
      return {
        success: false,
        error: 'Model eğitimi zaman aşımına uğradı. Lütfen daha küçük bir veri seti veya daha basit parametreler deneyin.',
        data: error.response?.data
      };
    }
    return {
      success: false,
      error: error.response?.data?.message || error.message || 'Model eğitimi sırasında bir hata oluştu',
      data: error.response?.data
    };
  }
};

// ==================== API METHODS ====================
export const FraudDetectionAPI = {
  // ========== TRANSACTION ANALYSIS ==========
  async analyzeTransaction(request: any): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.post('/api/FraudDetection/analyze', request)
    );
  },

  async analyzeTransactionContext(request: any): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.post('/api/TransactionAnalysis/analyze-transaction-context', request)
    );
  },

  async analyzeAccountContext(request: any): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.post('/api/TransactionAnalysis/analyze-account-context', request)
    );
  },

  async analyzeIpContext(request: any): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.post('/api/TransactionAnalysis/analyze-ip-context', request)
    );
  },

  async analyzeDeviceContext(request: any): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.post('/api/TransactionAnalysis/analyze-device-context', request)
    );
  },

  async analyzeSessionContext(request: any): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.post('/api/TransactionAnalysis/analyze-session-context', request)
    );
  },

  async evaluateMLModel(request: any): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.post('/api/TransactionAnalysis/evaluate-ml-model', request)
    );
  },

  async comprehensiveAnalysis(request: any): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.post('/api/TransactionAnalysis/comprehensive-analysis', request)
    );
  },

  async getTransactionDetails(transactionId: string): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.get(`/api/TransactionAnalysis/${transactionId}`)
    );
  },

  // ========== TRANSACTION MANAGEMENT ==========
  async getTransactions(limit: number = 50, offset: number = 0): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.get(`/api/Transaction`, { params: { limit, offset } })
    );
  },

  // ========== COMPLETE TRANSACTION DETAILS ==========
  async getTransactionCompleteDetails(transactionId: string): Promise<ApiResponse<{
    transaction: Transaction;
    analysisResult: AnalysisResult;
    riskFactors: RiskFactor[];
  }>> {
    return handleApiCall(() =>
      api.get(`/api/TransactionAnalysis/complete-details/${transactionId}`)
    );
  },

  // ========== ANALYSIS RESULTS ==========
  async getAnalysisResults(transactionIds?: string[], limit: number = 100): Promise<ApiResponse<any[]>> {
    const params: any = { limit };
    if (transactionIds && transactionIds.length > 0) {
      params.transactionIds = transactionIds.join(',');
    }
    return handleApiCall(() =>
      api.get(`/api/AnalysisResult/list`, { params })
    );
  },

  async getAnalysisResultByTransaction(transactionId: string): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.get(`/api/AnalysisResult/by-transaction/${transactionId}`)
    );
  },

  async getAnalysisResultsByTransactions(transactionIds: string[]): Promise<ApiResponse<any[]>> {
    return handleApiCall(() =>
      api.post('/api/AnalysisResult/by-transactions', { transactionIds })
    );
  },

  // ========== RISK EVALUATION ==========
  async getRiskEvaluations(transactionIds?: string[], limit: number = 100): Promise<ApiResponse<any[]>> {
    const params: any = { limit };
    if (transactionIds && transactionIds.length > 0) {
      params.transactionIds = transactionIds.join(',');
    }
    return handleApiCall(() =>
      api.get(`/api/RiskEvaluation/list`, { params })
    );
  },

  async getRiskEvaluationByTransaction(transactionId: string): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.get(`/api/RiskEvaluation/by-transaction/${transactionId}`)
    );
  },

  async getRiskEvaluationsByTransactions(transactionIds: string[]): Promise<ApiResponse<any[]>> {
    return handleApiCall(() =>
      api.post('/api/RiskEvaluation/by-transactions', { transactionIds })
    );
  },

  // ========== RISK FACTORS ==========
  async getRiskFactors(limit: number = 100, offset: number = 0): Promise<ApiResponse<RiskFactorResponse[]>> {
    return handleApiCall(() =>
      api.get('/api/RiskFactors', { params: { limit, offset } })
    );
  },

  async getFilteredRiskFactors(
    type?: string,
    severity?: string,
    source?: string,
    startDate?: string,
    endDate?: string,
    limit: number = 100,
    offset: number = 0
  ): Promise<ApiResponse<RiskFactorResponse[]>> {
    const params: any = { limit, offset };
    if (type) params.type = type;
    if (severity) params.severity = severity;
    if (source) params.source = source;
    if (startDate) params.startDate = startDate;
    if (endDate) params.endDate = endDate;

    return handleApiCall(() =>
      api.get('/api/RiskFactors/filtered', { params })
    );
  },

  async getRiskFactorsByTransaction(transactionId: string): Promise<ApiResponse<RiskFactorResponse[]>> {
    return handleApiCall(() =>
      api.get(`/api/RiskFactors/transaction/${transactionId}`)
    );
  },

  async getRiskFactorById(id: string): Promise<ApiResponse<RiskFactorResponse>> {
    return handleApiCall(() =>
      api.get(`/api/RiskFactors/${id}`)
    );
  },

  async getRiskFactorSummary(): Promise<ApiResponse<RiskFactorSummary>> {
    return handleApiCall(() =>
      api.get('/api/RiskFactors/summary')
    );
  },

  // ========== CONTEXT ANALYSIS METHODS ==========
  async checkTransactionContext(request: TransactionCheckRequest): Promise<ApiResponse<FraudDetectionResponse>> {
    return handleApiCall(() =>
      api.post('/api/FraudDetection/check-transaction', request)
    );
  },

  async checkAccountContext(request: AccountAccessCheckRequest): Promise<ApiResponse<FraudDetectionResponse>> {
    return handleApiCall(() =>
      api.post('/api/FraudDetection/check-account-access', request)
    );
  },

  async checkIpContext(request: IpCheckRequest): Promise<ApiResponse<FraudDetectionResponse>> {
    return handleApiCall(() =>
      api.post('/api/FraudDetection/check-ip', request)
    );
  },

  async checkDeviceContext(request: DeviceCheckRequest): Promise<ApiResponse<FraudDetectionResponse>> {
    return handleApiCall(() =>
      api.post('/api/FraudDetection/check-device', request)
    );
  },

  async checkSessionContext(request: SessionCheckRequest): Promise<ApiResponse<FraudDetectionResponse>> {
    return handleApiCall(() =>
      api.post('/api/FraudDetection/check-session', request)
    );
  },

  async evaluateWithModel(request: ModelEvaluationRequest): Promise<ApiResponse<ModelEvaluationResponse>> {
    return handleApiCall(() =>
      api.post('/api/FraudDetection/evaluate-model', request)
    );
  },

  async performComprehensiveCheck(request: ComprehensiveFraudCheckRequest): Promise<ApiResponse<ComprehensiveFraudCheckResponse>> {
    return handleApiCall(() =>
      api.post('/api/FraudDetection/comprehensive-check', request)
    );
  },

  // ========== MODEL MANAGEMENT ==========
  async getAllModels(): Promise<ModelVersion[]> {
    const response = await handleApiCall(() => api.get<ModelVersion[]>('/api/Model/all'));
    return response.data || [];
  },

  async updateModelStatus(modelId: string, status: string): Promise<ApiResponse<any>> {
    return handleApiCall(() => api.put(`/api/Model/${modelId}/status`, { status }));
  },

  async updateModel(modelId: string, updates: Partial<ModelMetadata>): Promise<ApiResponse<ModelMetadata>> {
    return handleApiCall(() => api.put(`/api/Model/${modelId}`, updates));
  },

  async deleteModel(modelId: string): Promise<ApiResponse<any>> {
    return handleApiCall(() => api.delete(`/api/Model/${modelId}`));
  },

  // ========== MODEL TRAINING ==========
  async trainLightGBM(config: LightGBMConfig): Promise<ApiResponse<TrainingResult>> {
    return handleTrainingApiCall(() =>
      trainingApi.post('/api/Model/train/lightgbm-config', config)
    );
  },

  async trainPCA(config: PCAConfig): Promise<ApiResponse<TrainingResult>> {
    return handleTrainingApiCall(() =>
      trainingApi.post('/api/Model/train/pca-config', config)
    );
  },

  async trainEnsemble(config: EnsembleConfig): Promise<ApiResponse<TrainingResult>> {
    return handleTrainingApiCall(() =>
      trainingApi.post('/api/Model/train/ensemble-config', config)
    );
  },

  async trainIsolationForest(config: any): Promise<ApiResponse<TrainingResult>> {
    return handleTrainingApiCall(() =>
      trainingApi.post('/api/Model/train/isolation-forest', config)
    );
  },

  async trainAutoEncoder(config: any): Promise<ApiResponse<TrainingResult>> {
    return handleTrainingApiCall(() =>
      trainingApi.post('/api/Model/train/autoencoder', config)
    );
  },

  // ========== FRAUD RULES API ==========
  async getFraudRules(): Promise<ApiResponse<FraudRuleResponse[]>> {
    return handleApiCall(() =>
      api.get('/api/FraudRules')
    );
  },

  async getActiveFraudRules(): Promise<ApiResponse<FraudRuleResponse[]>> {
    return handleApiCall(() =>
      api.get('/api/FraudRules/active')
    );
  },

  async getFraudRulesByCategory(category: RuleCategory): Promise<ApiResponse<FraudRuleResponse[]>> {
    return handleApiCall(() =>
      api.get(`/api/FraudRules/category/${category}`)
    );
  },

  async getFraudRuleById(id: string): Promise<ApiResponse<FraudRuleResponse>> {
    return handleApiCall(() =>
      api.get(`/api/FraudRules/${id}`)
    );
  },

  async createFraudRule(request: FraudRuleCreateRequest): Promise<ApiResponse<FraudRuleResponse>> {
    return handleApiCall(() =>
      api.post('/api/FraudRules', request)
    );
  },

  async updateFraudRule(id: string, request: FraudRuleUpdateRequest): Promise<ApiResponse<FraudRuleResponse>> {
    return handleApiCall(() =>
      api.put(`/api/FraudRules/${id}`, request)
    );
  },

  async activateFraudRule(id: string): Promise<ApiResponse<FraudRuleResponse>> {
    return handleApiCall(() =>
      api.patch(`/api/FraudRules/${id}/activate`)
    );
  },

  async deactivateFraudRule(id: string): Promise<ApiResponse<FraudRuleResponse>> {
    return handleApiCall(() =>
      api.patch(`/api/FraudRules/${id}/deactivate`)
    );
  },

  async setFraudRuleTestMode(id: string): Promise<ApiResponse<FraudRuleResponse>> {
    return handleApiCall(() =>
      api.patch(`/api/FraudRules/${id}/test-mode`)
    );
  },

  async deleteFraudRule(id: string): Promise<ApiResponse<void>> {
    return handleApiCall(() =>
      api.delete(`/api/FraudRules/${id}`)
    );
  },

  // ========== FRAUD EVENTS API ==========
  async getFraudEvents(): Promise<ApiResponse<FraudEventResponse[]>> {
    return handleApiCall(() =>
      api.get('/api/FraudEvents')
    );
  },

  async getUnresolvedFraudEvents(): Promise<ApiResponse<FraudEventResponse[]>> {
    return handleApiCall(() =>
      api.get('/api/FraudEvents/unresolved')
    );
  },

  async getFraudEventsByAccountId(accountId: string): Promise<ApiResponse<FraudEventResponse[]>> {
    return handleApiCall(() =>
      api.get(`/api/FraudEvents/account/${accountId}`)
    );
  },

  async getFraudEventsByIpAddress(ipAddress: string): Promise<ApiResponse<FraudEventResponse[]>> {
    return handleApiCall(() =>
      api.get(`/api/FraudEvents/ip/${ipAddress}`)
    );
  },

  async getFraudEventById(id: string): Promise<ApiResponse<FraudEventResponse>> {
    return handleApiCall(() =>
      api.get(`/api/FraudEvents/${id}`)
    );
  },

  async resolveFraudEvent(id: string, request: FraudEventResolveRequest): Promise<ApiResponse<FraudEventResponse>> {
    return handleApiCall(() =>
      api.patch(`/api/FraudEvents/${id}/resolve`, request)
    );
  },

  // ========== UTILITY FUNCTIONS FOR ENUMS ==========
  getRuleCategoryLabel(category: RuleCategory): string {
    switch (category) {
      case RuleCategory.Network: return 'Ağ';
      case RuleCategory.Account: return 'Hesap';
      case RuleCategory.Device: return 'Cihaz';
      case RuleCategory.Session: return 'Oturum';
      case RuleCategory.Transaction: return 'İşlem';
      case RuleCategory.IP: return 'IP Adresi';
      case RuleCategory.Behavior: return 'Davranış';
      case RuleCategory.Time: return 'Zaman';
      case RuleCategory.Location: return 'Konum';
      case RuleCategory.Other: return 'Diğer';
      case RuleCategory.Complex: return 'Karmaşık';
      default: return category;
    }
  },

  getRuleTypeLabel(type: RuleType): string {
    switch (type) {
      case RuleType.Simple: return 'Basit';
      case RuleType.Threshold: return 'Eşik';
      case RuleType.Complex: return 'Karmaşık';
      case RuleType.Sequential: return 'Sıralı';
      case RuleType.Behavioral: return 'Davranış';
      case RuleType.Anomaly: return 'Anomali';
      case RuleType.Blacklist: return 'Kara Liste';
      case RuleType.Whitelist: return 'Beyaz Liste';
      case RuleType.Custom: return 'Özel';
      default: return type;
    }
  },

  getImpactLevelLabel(level: ImpactLevel): string {
    switch (level) {
      case ImpactLevel.Low: return 'Düşük';
      case ImpactLevel.Medium: return 'Orta';
      case ImpactLevel.High: return 'Yüksek';
      case ImpactLevel.Critical: return 'Kritik';
      default: return level;
    }
  },

  getRuleStatusLabel(status: RuleStatus): string {
    switch (status) {
      case RuleStatus.Draft: return 'Taslak';
      case RuleStatus.Active: return 'Aktif';
      case RuleStatus.Inactive: return 'Pasif';
      case RuleStatus.TestMode: return 'Test Modu';
      case RuleStatus.Archived: return 'Arşiv';
      default: return status;
    }
  },

  getRuleActionLabel(action: RuleAction): string {
    switch (action) {
      case RuleAction.Log: return 'Kaydet';
      case RuleAction.Notify: return 'Bildir';
      case RuleAction.RequireAdditionalVerification: return 'Ek Onayı Gerektir';
      case RuleAction.DelayProcessing: return 'İşlemi Geciktir';
      case RuleAction.PutUnderReview: return 'İncelemeye Teslim Et';
      case RuleAction.RejectTransaction: return 'İşlemi Reddet';
      case RuleAction.TerminateSession: return 'Oturumu Sonlandır';
      case RuleAction.LockAccount: return 'Hesabı Kilitle';
      case RuleAction.SuspendAccount: return 'Hesabı Askıya Al';
      case RuleAction.RequireKYCVerification: return 'KYC Doğrulaması Gerektir';
      case RuleAction.BlockDevice: return 'Cihazı Engelle';
      case RuleAction.BlockIP: return 'IP Adresini Engelle';
      case RuleAction.BlacklistIP: return 'IP Adresini Kara Listeye Ekle';
      case RuleAction.Block: return 'Engelle';
      case RuleAction.Review: return 'İnceleme';
      case RuleAction.EscalateToManager: return 'Yöneticiye Yükselt';
      default: return action;
    }
  },

  // ========== UTILITY METHODS ==========
  async healthCheck(): Promise<boolean> {
    try {
      const response = await api.get('/health');
      return response.status === 200;
    } catch (error) {
      return false;
    }
  },

  async pythonHealthCheck(): Promise<boolean> {
    try {
      const response = await api.get('/api/python/health');
      return response.status === 200;
    } catch (error) {
      return false;
    }
  },

  async getApiStatus(): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.get('/api/status')
    );
  },

  getRiskLevelValue(riskScore: any): string {
    let level = '';
    if (typeof riskScore === 'string') {
      level = riskScore;
    } else if (typeof riskScore === 'object' && riskScore !== null) {
      level = riskScore.level || riskScore.riskLevel || '';
    }
    switch ((level || '').toLowerCase()) {
      case 'low': return 'Düşük';
      case 'medium': return 'Orta';
      case 'high': return 'Yüksek';
      case 'critical': return 'Kritik';
      case 'düşük': return 'Düşük';
      case 'orta': return 'Orta';
      case 'yüksek': return 'Yüksek';
      case 'kritik': return 'Kritik';
      default: return 'Bilinmiyor';
    }
  },

  // ========== FRAUD ALERTS ==========
  async getFraudAlerts(limit: number = 10): Promise<FraudAlert[]> {
    console.log('🚀 Calling getFraudAlerts with limit:', limit);
    const response = await handleApiCall(() =>
      api.get('/api/FraudAlerts', { params: { pageSize: limit } })
    );
    console.log('📋 API Response for getFraudAlerts:', response);
    console.log('📦 Response data:', response.data);
    console.log('✅ Returning alerts array:', response.data || []);
    return response.data || [];
  },

  async getActiveAlerts(): Promise<ApiResponse<FraudAlert[]>> {
    return handleApiCall(() =>
      api.get('/api/FraudAlerts/active')
    );
  },

  async getAlertsByUserId(userId: string): Promise<ApiResponse<FraudAlert[]>> {
    return handleApiCall(() =>
      api.get(`/api/FraudAlerts/user/${userId}`)
    );
  },

  async getAlertsByStatus(status: string): Promise<ApiResponse<FraudAlert[]>> {
    return handleApiCall(() =>
      api.get(`/api/FraudAlerts/status/${status}`)
    );
  },

  async getAlertById(alertId: string): Promise<ApiResponse<FraudAlert>> {
    return handleApiCall(() =>
      api.get(`/api/FraudAlerts/${alertId}`)
    );
  },

  async resolveAlert(alertId: string, resolution: string, resolvedBy: string = 'system'): Promise<ApiResponse<void>> {
    return handleApiCall(() =>
      api.put(`/api/FraudAlerts/${alertId}/resolve`, { resolution, resolvedBy })
    );
  },

  async assignAlert(alertId: string, assignedTo: string): Promise<ApiResponse<FraudAlert>> {
    return handleApiCall(() =>
      api.put(`/api/FraudAlerts/${alertId}/assign`, { assignedTo })
    );
  },

  async createAlert(transactionId: string, userId: string, type: string, riskScore: any, factors: string[]): Promise<ApiResponse<FraudAlert>> {
    return handleApiCall(() =>
      api.post('/api/FraudAlerts', { transactionId, userId, type, riskScore, factors })
    );
  },

  async updateAlert(alertId: string, alert: Partial<FraudAlert>): Promise<ApiResponse<FraudAlert>> {
    return handleApiCall(() =>
      api.put(`/api/FraudAlerts/${alertId}`, alert)
    );
  },

  async deleteAlert(alertId: string): Promise<ApiResponse<void>> {
    return handleApiCall(() =>
      api.delete(`/api/FraudAlerts/${alertId}`)
    );
  },

  async getAlertSummary(): Promise<ApiResponse<any>> {
    return handleApiCall(() =>
      api.get('/api/FraudAlerts/summary')
    );
  },

  // ========== SHAP ANALYSIS ==========
  async getShapExplanation(transactionId: string): Promise<ApiResponse<ShapExplanation>> {
    return handleApiCall(() =>
      api.get(`/api/shap/explanation/${transactionId}`)
    );
  },

  // ========== TEST METHODS ==========
  async testEndpoint(endpoint: string, data: any): Promise<ApiResponse<any>> {
    console.log(`🧪 Testing ${endpoint} with data:`, data);
    return handleApiCall(() =>
      api.post(endpoint, data)
    );
  },

  // ========== UTILITY FUNCTIONS FOR RISK FACTORS ==========
  getRiskFactorTypeLabel(type: string): string {
    switch (type) {
      case 'ModelFeature': return 'Model Özelliği';
      case 'AnomalyDetection': return 'Anomali Tespiti';
      case 'HighValue': return 'Yüksek Değer';
      case 'Location': return 'Konum';
      case 'Frequency': return 'Sıklık';
      case 'TimePattern': return 'Zaman Deseni';
      case 'RuleViolation': return 'Kural İhlali';
      case 'UserBehavior': return 'Kullanıcı Davranışı';
      case 'DeviceAnomaly': return 'Cihaz Anomalisi';
      case 'Time': return 'Zaman';
      case 'Device': return 'Cihaz';
      case 'IPAddress': return 'IP Adresi';
      case 'Velocity': return 'Hız';
      case 'RecurringPattern': return 'Tekrarlanan Desen';
      default: return type;
    }
  },

  getRiskLevelLabel(level: string): string {
    switch (level) {
      case 'Low': return 'Düşük';
      case 'Medium': return 'Orta';
      case 'High': return 'Yüksek';
      case 'Critical': return 'Kritik';
      default: return level;
    }
  },

  getRiskLevelColor(level: string): 'success' | 'info' | 'warning' | 'error' | 'default' {
    switch (level) {
      case 'Low': return 'success';
      case 'Medium': return 'info';
      case 'High': return 'warning';
      case 'Critical': return 'error';
      default: return 'default';
    }
  },

  // ========== BLACKLIST API ==========
  async getBlacklistItems(limit: number = 100, offset: number = 0): Promise<ApiResponse<BlacklistItemResponse[]>> {
    return handleApiCall(() =>
      api.get('/api/Blacklist', { params: { limit, offset } })
    );
  },

  async getBlacklistItemsByType(type: BlacklistType): Promise<ApiResponse<BlacklistItemResponse[]>> {
    return handleApiCall(() =>
      api.get(`/api/Blacklist/type/${type}`)
    );
  },

  async getActiveBlacklistItems(type: BlacklistType): Promise<ApiResponse<BlacklistItemResponse[]>> {
    return handleApiCall(() =>
      api.get(`/api/Blacklist/active/${type}`)
    );
  },

  async getBlacklistSummary(): Promise<ApiResponse<BlacklistSummary>> {
    return handleApiCall(() =>
      api.get('/api/Blacklist/summary')
    );
  },

  async addBlacklistItem(request: BlacklistItemCreateRequest): Promise<ApiResponse<BlacklistItemResponse>> {
    return handleApiCall(() =>
      api.post('/api/Blacklist', request)
    );
  },

  async getBlacklistItemById(id: string): Promise<ApiResponse<BlacklistItemResponse>> {
    return handleApiCall(() =>
      api.get(`/api/Blacklist/${id}`)
    );
  },

  async invalidateBlacklistItem(id: string, request: BlacklistInvalidateRequest): Promise<ApiResponse<BlacklistItemResponse>> {
    return handleApiCall(() =>
      api.patch(`/api/Blacklist/${id}/invalidate`, request)
    );
  },

  async deleteBlacklistItem(id: string): Promise<ApiResponse<void>> {
    return handleApiCall(() =>
      api.delete(`/api/Blacklist/${id}`)
    );
  },

  async cleanupExpiredItems(): Promise<ApiResponse<number>> {
    return handleApiCall(() =>
      api.post('/api/Blacklist/cleanup-expired')
    );
  },

  async checkBlacklist(request: BlacklistCheckRequest): Promise<ApiResponse<boolean>> {
    return handleApiCall(() =>
      api.post('/api/Blacklist/check', request)
    );
  },

  // ========== UTILITY FUNCTIONS FOR BLACKLIST ==========
  getBlacklistTypeLabel(type: string): string {
    switch (type) {
      case 'IpAddress': return 'IP Adresi';
      case 'Account': return 'Hesap';
      case 'Device': return 'Cihaz';
      case 'Country': return 'Ülke';
      default: return type;
    }
  },

  getBlacklistStatusLabel(status: string): string {
    switch (status) {
      case 'Active': return 'Aktif';
      case 'Invalidated': return 'Geçersiz Kılınmış';
      case 'Expired': return 'Süresi Dolmuş';
      default: return status;
    }
  },

  getBlacklistStatusColor(status: string): 'success' | 'info' | 'warning' | 'error' | 'default' {
    switch (status) {
      case 'Active': return 'success';
      case 'Invalidated': return 'warning';
      case 'Expired': return 'error';
      default: return 'default';
    }
  },
};

export default FraudDetectionAPI; 