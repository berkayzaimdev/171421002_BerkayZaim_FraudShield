import { useState, useCallback, useRef, useEffect } from 'react';
import { FraudDetectionAPI } from '../services/api';

// ==================== TYPES ====================
export interface ModelConfiguration {
  name: string;
  description: string;
  type: 'LightGBM' | 'PCA' | 'Ensemble' | 'IsolationForest' | 'AutoEncoder';
  parameters: Record<string, any>;
  estimatedTime: string;
  complexity: 'Başlangıç' | 'Orta' | 'İleri';
  useCase: string;
}

export interface TrainingProgress {
  progress: number;
  step: string;
  isTraining: boolean;
  error: string | null;
  result: any | null;
}

export interface TrainingNotification {
  message: string;
  severity: 'success' | 'error' | 'warning' | 'info';
  timestamp: number;
}

interface UseModelTrainingReturn {
  // State
  configuration: ModelConfiguration;
  progress: TrainingProgress;
  notification: TrainingNotification | null;
  
  // Actions
  updateConfiguration: (updates: Partial<ModelConfiguration>) => void;
  updateParameters: (parameters: Record<string, any>) => void;
  startTraining: () => Promise<void>;
  stopTraining: () => void;
  resetTraining: () => void;
  clearNotification: () => void;
  
  // Computed
  isConfigurationValid: boolean;
  canStartTraining: boolean;
}

// ==================== CONSTANTS ====================
const MODEL_TYPES = {
  LightGBM: {
    title: 'LightGBM Classifier',
    description: 'Hızlı ve yüksek performanslı gradient boosting algoritması',
    useCase: 'Dengeli veri setleri için ideal, yüksek accuracy',
    complexity: 'Orta' as const,
    estimatedTime: '5-15 dakika',
    defaultParams: {
      numberOfTrees: 1000,
      numberOfLeaves: 128,
      learningRate: 0.005,
      featureFraction: 0.8,
      useClassWeights: true,
    }
  },
  PCA: {
    title: 'PCA Anomaly Detection',
    description: 'Boyut azaltma tabanlı anomali tespit algoritması',
    useCase: 'Yeni fraud türlerinin tespiti için ideal',
    complexity: 'Başlangıç' as const,
    estimatedTime: '2-8 dakika',
    defaultParams: {
      components: 10,
      whiten: false,
      threshold: 2.5,
    }
  },
  Ensemble: {
    title: 'Ensemble Hybrid Model',
    description: 'LightGBM ve PCA\'nın birleşimi, en yüksek performans',
    useCase: 'Production ortamı için en optimal çözüm',
    complexity: 'İleri' as const,
    estimatedTime: '10-25 dakika',
    defaultParams: {
      lightgbmWeight: 0.7,
      pcaWeight: 0.3,
      votingStrategy: 'weighted',
    }
  },
  IsolationForest: {
    title: 'Isolation Forest',
    description: 'Ağaç tabanlı anomali tespit algoritması',
    useCase: 'Az etiketli veriler için ideal',
    complexity: 'Orta' as const,
    estimatedTime: '3-10 dakika',
    defaultParams: {
      nEstimators: 100,
      contamination: 0.1,
      maxSamples: 'auto',
    }
  },
  AutoEncoder: {
    title: 'Neural AutoEncoder',
    description: 'Derin öğrenme tabanlı anomali tespit',
    useCase: 'Karmaşık fraud pattern\'ları için',
    complexity: 'İleri' as const,
    estimatedTime: '15-45 dakika',
    defaultParams: {
      hiddenLayers: [64, 32, 16, 32, 64],
      epochs: 100,
      batchSize: 32,
      learningRate: 0.001,
    }
  }
};

// ==================== INITIAL STATE ====================
const initialConfiguration: ModelConfiguration = {
  name: '',
  description: '',
  type: 'LightGBM',
  parameters: {},
  estimatedTime: '',
  complexity: 'Başlangıç',
  useCase: ''
};

const initialProgress: TrainingProgress = {
  progress: 0,
  step: '',
  isTraining: false,
  error: null,
  result: null
};

// ==================== HOOK ====================
export const useModelTraining = (): UseModelTrainingReturn => {
  const [configuration, setConfiguration] = useState<ModelConfiguration>(initialConfiguration);
  const [progress, setProgress] = useState<TrainingProgress>(initialProgress);
  const [notification, setNotification] = useState<TrainingNotification | null>(null);
  
  // Refs for cleanup
  const progressIntervalRef = useRef<NodeJS.Timeout | null>(null);
  const stepIntervalRef = useRef<NodeJS.Timeout | null>(null);
  const abortControllerRef = useRef<AbortController | null>(null);

  // ========== NOTIFICATION HANDLER ==========
  const showNotification = useCallback((message: string, severity: TrainingNotification['severity']) => {
    setNotification({
      message,
      severity,
      timestamp: Date.now()
    });
    console.log(`${severity.toUpperCase()}: ${message}`);
  }, []);

  const clearNotification = useCallback(() => {
    setNotification(null);
  }, []);

  // ========== ACTIONS ==========
  const updateConfiguration = useCallback((updates: Partial<ModelConfiguration>) => {
    setConfiguration(prev => {
      const newConfig = { ...prev, ...updates };
      
      // Auto-update parameters when type changes
      if (updates.type && updates.type !== prev.type) {
        const modelInfo = MODEL_TYPES[updates.type];
        if (modelInfo) {
          newConfig.parameters = { ...modelInfo.defaultParams };
          newConfig.name = `${modelInfo.title}_${new Date().toISOString().slice(0, 10)}`;
          newConfig.description = modelInfo.description;
          newConfig.estimatedTime = modelInfo.estimatedTime;
          newConfig.complexity = modelInfo.complexity;
          newConfig.useCase = modelInfo.useCase;
        }
      }
      
      return newConfig;
    });
  }, []);

  const updateParameters = useCallback((parameters: Record<string, any>) => {
    setConfiguration(prev => ({
      ...prev,
      parameters: { ...prev.parameters, ...parameters }
    }));
  }, []);

  const startTraining = useCallback(async () => {
    // Validation
    if (!configuration.name || !configuration.type) {
      const errorMessage = 'Model adı ve tipi seçilmelidir';
      setProgress(prev => ({
        ...prev,
        error: errorMessage
      }));
      showNotification(errorMessage, 'error');
      return;
    }

    try {
      // Clear previous notifications
      clearNotification();
      
      // Reset progress
      setProgress({
        progress: 0,
        step: 'Eğitim başlatılıyor...',
        isTraining: true,
        error: null,
        result: null
      });

      // Create abort controller for cancellation
      abortControllerRef.current = new AbortController();

      // Start progress simulation
      const progressSteps = getProgressSteps(configuration.type);
      let currentStepIndex = 0;

      progressIntervalRef.current = setInterval(() => {
        setProgress(prev => {
          if (prev.progress >= 90) return prev;
          const increment = configuration.type === 'AutoEncoder' ? 
            Math.random() * 1 : Math.random() * 3;
          return {
            ...prev,
            progress: Math.min(prev.progress + increment, 90)
          };
        });
      }, configuration.type === 'AutoEncoder' ? 3000 : 1000);

      stepIntervalRef.current = setInterval(() => {
        if (currentStepIndex < progressSteps.length) {
          setProgress(prev => ({
            ...prev,
            step: progressSteps[currentStepIndex]
          }));
          currentStepIndex++;
        }
      }, configuration.type === 'AutoEncoder' ? 5000 : 2000);

      // Start actual training
      console.log('🚀 Model eğitimi başlatılıyor:', configuration.type, configuration.parameters);
      showNotification(`🚀 ${configuration.name} eğitimi başlatıldı`, 'info');
      
      let result;
      const config = configuration.parameters;

      switch (configuration.type) {
        case 'LightGBM':
          result = await FraudDetectionAPI.trainLightGBM(config as any);
          break;
        case 'PCA':
          result = await FraudDetectionAPI.trainPCA(config as any);
          break;
        case 'Ensemble':
          result = await FraudDetectionAPI.trainEnsemble(config as any);
          break;
        case 'IsolationForest':
          result = await FraudDetectionAPI.trainIsolationForest(config as any);
          break;
        case 'AutoEncoder':
          result = await FraudDetectionAPI.trainAutoEncoder(config as any);
          break;
        default:
          throw new Error(`Desteklenmeyen model tipi: ${configuration.type}`);
      }

      // Check if training was successful
      if (result?.error) {
        throw new Error(result.error);
      }

      if (result?.success === false) {
        throw new Error('Python model training başarısız');
      }

      // Training completed successfully
      console.log('📊 Training sonucu alındı:', result);
      
      setProgress({
        progress: 100,
        step: 'Eğitim tamamlandı! ✅',
        isTraining: false,
        error: null,
        result
      });

      const successMessage = `✅ ${configuration.name} başarıyla eğitildi!`;
      console.log(successMessage);
      showNotification(successMessage, 'success');

    } catch (error: any) {
      console.error('❌ Training error:', error);
      
      const errorMessage = `❌ Eğitim hatası: ${error.message}`;
      setProgress(prev => ({
        ...prev,
        progress: prev.progress,
        step: 'Eğitim hatası! ❌',
        isTraining: false,
        error: error.message,
        result: { error: error.message }
      }));
      
      showNotification(errorMessage, 'error');
    } finally {
      // Cleanup intervals
      if (progressIntervalRef.current) {
        clearInterval(progressIntervalRef.current);
        progressIntervalRef.current = null;
      }
      if (stepIntervalRef.current) {
        clearInterval(stepIntervalRef.current);
        stepIntervalRef.current = null;
      }
    }
  }, [configuration, showNotification, clearNotification]);

  const stopTraining = useCallback(() => {
    // Abort API call if possible
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    // Clear intervals
    if (progressIntervalRef.current) {
      clearInterval(progressIntervalRef.current);
      progressIntervalRef.current = null;
    }
    if (stepIntervalRef.current) {
      clearInterval(stepIntervalRef.current);
      stepIntervalRef.current = null;
    }

    setProgress(prev => ({
      ...prev,
      isTraining: false,
      step: 'Eğitim durduruldu',
      error: 'Kullanıcı tarafından durduruldu'
    }));

    showNotification('🛑 Eğitim kullanıcı tarafından durduruldu', 'warning');
  }, [showNotification]);

  const resetTraining = useCallback(() => {
    // Stop training first
    stopTraining();
    
    // Reset all state
    setConfiguration(initialConfiguration);
    setProgress(initialProgress);
    clearNotification();
  }, [stopTraining, clearNotification]);

  // ========== COMPUTED VALUES ==========
  const isConfigurationValid = configuration.name.length > 0 && 
                              configuration.type.length > 0 &&
                              Object.keys(configuration.parameters).length > 0;

  const canStartTraining = isConfigurationValid && !progress.isTraining;

  // ========== CLEANUP ==========
  useEffect(() => {
    return () => {
      // Cleanup on unmount
      if (progressIntervalRef.current) {
        clearInterval(progressIntervalRef.current);
      }
      if (stepIntervalRef.current) {
        clearInterval(stepIntervalRef.current);
      }
      if (abortControllerRef.current) {
        abortControllerRef.current.abort();
      }
    };
  }, []);

  return {
    // State
    configuration,
    progress,
    notification,
    
    // Actions
    updateConfiguration,
    updateParameters,
    startTraining,
    stopTraining,
    resetTraining,
    clearNotification,
    
    // Computed
    isConfigurationValid,
    canStartTraining
  };
};

// ==================== HELPER FUNCTIONS ====================
function getProgressSteps(modelType: ModelConfiguration['type']): string[] {
  switch (modelType) {
    case 'AutoEncoder':
      return [
        'Neural network hazırlanıyor...',
        'Veri ön işleme...',
        'Encoder katmanları oluşturuluyor...',
        'Decoder katmanları oluşturuluyor...',
        'Weights initialization...',
        'Epoch 1/50 başlatıldı...',
        'Forward propagation...',
        'Backward propagation...',
        'Loss hesaplanıyor...',
        'Validation yapılıyor...',
        'Overfitting kontrol ediliyor...',
        'Model optimize ediliyor...',
        'Final validation...',
        'Model kaydediliyor...'
      ];
    
    default:
      return [
        'Veri yükleniyor...',
        'Veri ön işleme yapılıyor...',
        'Özellik çıkarımı...',
        'Model eğitimi başlatıldı...',
        'Hiperparametre optimizasyonu...',
        'Çapraz doğrulama...',
        'Model değerlendirme...',
        'Sonuçlar kaydediliyor...'
      ];
  }
} 