import { useState, useCallback, useRef, useEffect } from 'react';
import { FraudDetectionAPI } from '../services/api';

// ==================== TYPES ====================
export interface TuningExperiment {
  experimentId: number;
  parameters: Record<string, any>;
  score: number;
  metrics: Record<string, any>;
  status: 'running' | 'completed' | 'failed';
  timestamp: string;
  actualModelName?: string;
  executionTime?: number;
}

export interface TuningConfiguration {
  modelType: 'LightGBM' | 'PCA' | 'Ensemble';
  optimizationMetric: 'f1_score' | 'accuracy' | 'precision' | 'recall' | 'auc' | 'auc_pr';
  searchStrategy: 'grid' | 'random' | 'bayesian';
  maxExperiments: number;
  cvFolds: number;
  parameterRanges: Record<string, any>;
  earlyStoppingEnabled: boolean;
  earlyStoppingPatience: number;
}

export interface TuningProgress {
  isRunning: boolean;
  currentExperiment: number;
  totalExperiments: number;
  progress: number;
  elapsedTime: number;
  estimatedTimeRemaining: number;
}

export interface TuningResults {
  experiments: TuningExperiment[];
  bestExperiment: TuningExperiment | null;
  convergenceData: Array<{ iteration: number; bestScore: number; currentScore: number; }>;
  parameterImportance: Record<string, number>;
}

interface UseHyperparameterTuningReturn {
  // State
  configuration: TuningConfiguration;
  progress: TuningProgress;
  results: TuningResults;
  error: string | null;
  
  // Actions
  updateConfiguration: (updates: Partial<TuningConfiguration>) => void;
  updateParameterRanges: (paramName: string, ranges: any) => void;
  startTuning: () => Promise<void>;
  stopTuning: () => void;
  resetTuning: () => void;
  
  // Computed
  isConfigurationValid: boolean;
  canStartTuning: boolean;
  hasResults: boolean;
  
  // Analysis
  getParameterCorrelations: () => Record<string, Record<string, number>>;
  getConvergenceAnalysis: () => { isConverging: boolean; stabilityScore: number; };
  getTopParameterSets: (count: number) => TuningExperiment[];
}

// ==================== CONSTANTS ====================
const OPTIMIZATION_METRICS = {
  f1_score: { label: 'F1-Score', description: 'Precision ve Recall\'ın harmonik ortalaması', weight: 1 },
  accuracy: { label: 'Accuracy', description: 'Genel doğru tahmin oranı', weight: 0.8 },
  precision: { label: 'Precision', description: 'Fraud tahminlerinin doğruluk oranı', weight: 0.9 },
  recall: { label: 'Recall', description: 'Fraud\'ları yakalama oranı', weight: 0.9 },
  auc: { label: 'AUC-ROC', description: 'ROC eğrisi altındaki alan', weight: 0.9 },
  auc_pr: { label: 'AUC-PR', description: 'Precision-Recall eğrisi altındaki alan', weight: 0.95 }
};

const SEARCH_STRATEGIES = {
  grid: { label: 'Grid Search', estimatedTimeMultiplier: 1.5 },
  random: { label: 'Random Search', estimatedTimeMultiplier: 1.0 },
  bayesian: { label: 'Bayesian Optimization', estimatedTimeMultiplier: 0.7 }
};

// ==================== INITIAL STATE ====================
const initialConfiguration: TuningConfiguration = {
  modelType: 'LightGBM',
  optimizationMetric: 'f1_score',
  searchStrategy: 'random',
  maxExperiments: 20,
  cvFolds: 5,
  parameterRanges: {},
  earlyStoppingEnabled: true,
  earlyStoppingPatience: 3
};

const initialProgress: TuningProgress = {
  isRunning: false,
  currentExperiment: 0,
  totalExperiments: 0,
  progress: 0,
  elapsedTime: 0,
  estimatedTimeRemaining: 0
};

const initialResults: TuningResults = {
  experiments: [],
  bestExperiment: null,
  convergenceData: [],
  parameterImportance: {}
};

// ==================== HOOK ====================
export const useHyperparameterTuning = (): UseHyperparameterTuningReturn => {
  const [configuration, setConfiguration] = useState<TuningConfiguration>(initialConfiguration);
  const [progress, setProgress] = useState<TuningProgress>(initialProgress);
  const [results, setResults] = useState<TuningResults>(initialResults);
  const [error, setError] = useState<string | null>(null);

  // Refs for tracking and cleanup
  const startTimeRef = useRef<number>(0);
  const abortControllerRef = useRef<AbortController | null>(null);
  const timerIntervalRef = useRef<NodeJS.Timeout | null>(null);

  // ========== COMPUTED VALUES ==========
  const isConfigurationValid = configuration.modelType && 
                              configuration.optimizationMetric &&
                              configuration.maxExperiments > 0 &&
                              Object.keys(configuration.parameterRanges).length > 0;

  const canStartTuning = isConfigurationValid && !progress.isRunning;
  const hasResults = results.experiments.length > 0;

  // ========== ACTIONS ==========
  const updateConfiguration = useCallback((updates: Partial<TuningConfiguration>) => {
    setConfiguration(prev => {
      const newConfig = { ...prev, ...updates };
      
      // Auto-set parameter ranges when model type changes
      if (updates.modelType && updates.modelType !== prev.modelType) {
        newConfig.parameterRanges = getDefaultParameterRanges(updates.modelType);
      }
      
      return newConfig;
    });
    setError(null);
  }, []);

  const updateParameterRanges = useCallback((paramName: string, ranges: any) => {
    setConfiguration(prev => ({
      ...prev,
      parameterRanges: {
        ...prev.parameterRanges,
        [paramName]: ranges
      }
    }));
  }, []);

  const startTuning = useCallback(async () => {
    // Validation
    if (!isConfigurationValid) {
      setError('Konfigürasyon eksik veya hatalı');
      return;
    }

    try {
      setError(null);
      startTimeRef.current = Date.now();
      
      // Initialize progress
      setProgress({
        isRunning: true,
        currentExperiment: 0,
        totalExperiments: configuration.maxExperiments,
        progress: 0,
        elapsedTime: 0,
        estimatedTimeRemaining: 0
      });

      // Reset results
      setResults({
        experiments: [],
        bestExperiment: null,
        convergenceData: [],
        parameterImportance: {}
      });

      // Create abort controller
      abortControllerRef.current = new AbortController();

      // Start timer
      timerIntervalRef.current = setInterval(() => {
        const elapsed = Date.now() - startTimeRef.current;
        setProgress(prev => {
          const progressRatio = prev.currentExperiment / prev.totalExperiments;
          const estimatedTotal = progressRatio > 0 ? elapsed / progressRatio : 0;
          const remaining = Math.max(0, estimatedTotal - elapsed);
          
          return {
            ...prev,
            elapsedTime: elapsed,
            estimatedTimeRemaining: remaining,
            progress: (prev.currentExperiment / prev.totalExperiments) * 100
          };
        });
      }, 1000);

      console.log('🚀 Hiperparametre optimizasyonu başlatılıyor...', configuration);

      // Generate parameter combinations
      const parameterCombinations = generateParameterCombinations(configuration);
      console.log(`📊 ${parameterCombinations.length} farklı parametre kombinasyonu test edilecek`);

      if (parameterCombinations.length === 0) {
        throw new Error('Hiçbir parametre kombinasyonu oluşturulamadı');
      }

      const experimentsToRun = Math.min(configuration.maxExperiments, parameterCombinations.length);
      let bestScore = -Infinity;
      const tempExperiments: TuningExperiment[] = [];
      const convergenceData: Array<{ iteration: number; bestScore: number; currentScore: number; }> = [];

      // Run experiments
      for (let i = 0; i < experimentsToRun; i++) {
        // Check if aborted
        if (abortControllerRef.current?.signal.aborted) {
          break;
        }

        const experimentStartTime = Date.now();
        const parameters = parameterCombinations[i];
        
        setProgress(prev => ({ ...prev, currentExperiment: i + 1 }));
        
        console.log(`🔬 Deneme ${i + 1}/${experimentsToRun}: Parametreler test ediliyor`, parameters);
        
        try {
          // Make API call based on model type
          let trainingResult: any;
          
          switch (configuration.modelType) {
            case 'LightGBM':
              trainingResult = await FraudDetectionAPI.trainLightGBM(parameters as any);
              break;
            case 'PCA':
              trainingResult = await FraudDetectionAPI.trainPCA(parameters as any);
              break;
            case 'Ensemble':
              trainingResult = await FraudDetectionAPI.trainEnsemble(parameters as any);
              break;
            default:
              throw new Error(`Desteklenmeyen model tipi: ${configuration.modelType}`);
          }

          const executionTime = Date.now() - experimentStartTime;

          if (trainingResult && !trainingResult.error) {
            const score = extractOptimizationScore(trainingResult, configuration.optimizationMetric);
            const metrics = trainingResult.BasicMetrics || trainingResult.Metrics || {};
            
            console.log(`✅ Deneme ${i + 1} başarılı: Score = ${score.toFixed(4)}`);
            
            // Track best score
            if (score > bestScore) {
              bestScore = score;
              console.log(`🏆 Yeni en iyi skor: ${score.toFixed(4)}`);
            }

            const experiment: TuningExperiment = {
              experimentId: i + 1,
              parameters,
              score,
              metrics,
              status: 'completed',
              timestamp: new Date().toISOString(),
              actualModelName: trainingResult.actualModelName || trainingResult.modelId,
              executionTime
            };

            tempExperiments.push(experiment);
            convergenceData.push({ iteration: i + 1, bestScore, currentScore: score });
            
            // Update results in real-time
            setResults(prev => {
              const newBest = tempExperiments.reduce((best, exp) => 
                exp.score > (best?.score || -Infinity) ? exp : best, prev.bestExperiment);
              
              return {
                experiments: [...tempExperiments],
                bestExperiment: newBest,
                convergenceData: [...convergenceData],
                parameterImportance: calculateParameterImportance(tempExperiments)
              };
            });

          } else {
            console.log(`❌ Deneme ${i + 1} başarısız:`, trainingResult?.error);
            
            const failedExperiment: TuningExperiment = {
              experimentId: i + 1,
              parameters,
              score: -Infinity,
              metrics: {},
              status: 'failed',
              timestamp: new Date().toISOString(),
              executionTime
            };
            
            tempExperiments.push(failedExperiment);
            convergenceData.push({ iteration: i + 1, bestScore, currentScore: -Infinity });
          }

          // Early stopping check
          if (shouldEarlyStop(tempExperiments, configuration)) {
            console.log('🛑 Early stopping: Yeterli gelişme yok');
            break;
          }

          // Small delay for UI responsiveness
          await new Promise(resolve => setTimeout(resolve, 100));

        } catch (experimentError: any) {
          console.error(`🚨 Deneme ${i + 1} hatası:`, experimentError);
          
          const errorExperiment: TuningExperiment = {
            experimentId: i + 1,
            parameters,
            score: -Infinity,
            metrics: {},
            status: 'failed',
            timestamp: new Date().toISOString(),
            executionTime: Date.now() - experimentStartTime
          };
          
          tempExperiments.push(errorExperiment);
          convergenceData.push({ iteration: i + 1, bestScore, currentScore: -Infinity });
        }
      }

      console.log(`✅ Hiperparametre optimizasyonu tamamlandı! En iyi skor: ${bestScore.toFixed(4)}`);
      console.log(`📊 Toplam ${tempExperiments.length} deneme yapıldı`);

    } catch (tuningError: any) {
      console.error('❌ Optimizasyon hatası:', tuningError);
      setError(tuningError.message);
    } finally {
      // Cleanup
      setProgress(prev => ({ ...prev, isRunning: false }));
      
      if (timerIntervalRef.current) {
        clearInterval(timerIntervalRef.current);
        timerIntervalRef.current = null;
      }
    }
  }, [configuration, isConfigurationValid]);

  const stopTuning = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }
    
    if (timerIntervalRef.current) {
      clearInterval(timerIntervalRef.current);
      timerIntervalRef.current = null;
    }
    
    setProgress(prev => ({ ...prev, isRunning: false }));
    console.log('🛑 Hiperparametre optimizasyonu durduruldu');
  }, []);

  const resetTuning = useCallback(() => {
    stopTuning();
    setConfiguration(initialConfiguration);
    setProgress(initialProgress);
    setResults(initialResults);
    setError(null);
  }, [stopTuning]);

  // ========== ANALYSIS FUNCTIONS ==========
  const getParameterCorrelations = useCallback(() => {
    const correlations: Record<string, Record<string, number>> = {};
    
    if (results.experiments.length < 3) return correlations;
    
    const paramNames = Object.keys(results.experiments[0].parameters);
    
    for (const param1 of paramNames) {
      correlations[param1] = {};
      for (const param2 of paramNames) {
        if (param1 !== param2) {
          correlations[param1][param2] = calculateCorrelation(
            results.experiments.map(exp => exp.parameters[param1]),
            results.experiments.map(exp => exp.parameters[param2])
          );
        }
      }
    }
    
    return correlations;
  }, [results.experiments]);

  const getConvergenceAnalysis = useCallback(() => {
    if (results.convergenceData.length < 5) {
      return { isConverging: false, stabilityScore: 0 };
    }
    
    const recentScores = results.convergenceData.slice(-5).map(d => d.bestScore);
    const improvement = Math.max(...recentScores) - Math.min(...recentScores);
    const isConverging = improvement < 0.01; // %1'den az gelişme
    
    const stability = calculateStability(recentScores);
    
    return { isConverging, stabilityScore: stability };
  }, [results.convergenceData]);

  const getTopParameterSets = useCallback((count: number) => {
    return [...results.experiments]
      .filter(exp => exp.status === 'completed')
      .sort((a, b) => b.score - a.score)
      .slice(0, count);
  }, [results.experiments]);

  // ========== CLEANUP ==========
  useEffect(() => {
    return () => {
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
      if (timerIntervalRef.current) {
        clearInterval(timerIntervalRef.current);
      }
    };
  }, []);

  return {
    // State
    configuration,
    progress,
    results,
    error,
    
    // Actions
    updateConfiguration,
    updateParameterRanges,
    startTuning,
    stopTuning,
    resetTuning,
    
    // Computed
    isConfigurationValid,
    canStartTuning,
    hasResults,
    
    // Analysis
    getParameterCorrelations,
    getConvergenceAnalysis,
    getTopParameterSets
  };
};

// ==================== HELPER FUNCTIONS ====================
function getDefaultParameterRanges(modelType: string): Record<string, any> {
  switch (modelType) {
    case 'LightGBM':
      return {
        numberOfTrees: { min: 100, max: 2000, step: 100 },
        learningRate: { min: 0.001, max: 0.3, step: 0.001 },
        numberOfLeaves: { min: 16, max: 512, step: 16 },
        featureFraction: { min: 0.5, max: 1.0, step: 0.05 },
        l1Regularization: { min: 0, max: 1, step: 0.01 },
        l2Regularization: { min: 0, max: 1, step: 0.01 },
        useClassWeights: { values: [true, false] }
      };
    
    case 'PCA':
      return {
        components: { min: 2, max: 50, step: 1 },
        whiten: { values: [true, false] },
        threshold: { min: 1.5, max: 4.0, step: 0.1 }
      };
    
    case 'Ensemble':
      return {
        lightgbmWeight: { min: 0.1, max: 0.9, step: 0.1 },
        pcaWeight: { min: 0.1, max: 0.9, step: 0.1 },
        votingStrategy: { values: ['weighted', 'majority', 'soft'] }
      };
    
    default:
      return {};
  }
}

function generateParameterCombinations(config: TuningConfiguration): Record<string, any>[] {
  const combinations: Record<string, any>[] = [];
  const { parameterRanges, searchStrategy, maxExperiments } = config;
  
  if (searchStrategy === 'random') {
    for (let i = 0; i < maxExperiments; i++) {
      const combination: Record<string, any> = {};
      
      for (const [paramName, range] of Object.entries(parameterRanges)) {
        if (range.values) {
          // Categorical parameter
          combination[paramName] = range.values[Math.floor(Math.random() * range.values.length)];
        } else {
          // Numerical parameter
          const min = range.min || 0;
          const max = range.max || 1;
          combination[paramName] = Math.random() * (max - min) + min;
        }
      }
      
      combinations.push(combination);
    }
  }
  // Grid search and Bayesian optimization implementations would go here
  
  return combinations;
}

function extractOptimizationScore(result: any, metric: string): number {
  const metrics = result.BasicMetrics || result.Metrics || result;
  return metrics[metric] || metrics[metric.replace('_', '')] || 0;
}

function shouldEarlyStop(experiments: TuningExperiment[], config: TuningConfiguration): boolean {
  if (!config.earlyStoppingEnabled || experiments.length < config.earlyStoppingPatience) {
    return false;
  }
  
  const recentExperiments = experiments.slice(-config.earlyStoppingPatience);
  const scores = recentExperiments.map(exp => exp.score);
  const improvement = Math.max(...scores) - Math.min(...scores);
  
  return improvement < 0.001; // %0.1'den az gelişme
}

function calculateParameterImportance(experiments: TuningExperiment[]): Record<string, number> {
  const importance: Record<string, number> = {};
  
  if (experiments.length < 3) return importance;
  
  const paramNames = Object.keys(experiments[0].parameters);
  const scores = experiments.map(exp => exp.score);
  
  for (const paramName of paramNames) {
    const paramValues = experiments.map(exp => exp.parameters[paramName]);
    const correlation = Math.abs(calculateCorrelation(paramValues, scores));
    importance[paramName] = correlation;
  }
  
  return importance;
}

function calculateCorrelation(x: number[], y: number[]): number {
  const n = x.length;
  const sumX = x.reduce((a, b) => a + b, 0);
  const sumY = y.reduce((a, b) => a + b, 0);
  const sumXY = x.reduce((sum, xi, i) => sum + xi * y[i], 0);
  const sumX2 = x.reduce((sum, xi) => sum + xi * xi, 0);
  const sumY2 = y.reduce((sum, yi) => sum + yi * yi, 0);
  
  const numerator = n * sumXY - sumX * sumY;
  const denominator = Math.sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));
  
  return denominator === 0 ? 0 : numerator / denominator;
}

function calculateStability(scores: number[]): number {
  if (scores.length < 2) return 0;
  
  const mean = scores.reduce((a, b) => a + b, 0) / scores.length;
  const variance = scores.reduce((sum, score) => sum + Math.pow(score - mean, 2), 0) / scores.length;
  const stdDev = Math.sqrt(variance);
  
  return 1 - (stdDev / Math.max(mean, 0.001)); // Normalize by mean
} 