import React, { useState, useEffect } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Stepper,
  Step,
  StepLabel,
  Button,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Slider,
  Switch,
  FormControlLabel,
  Alert,
  Chip,
  LinearProgress,
  CircularProgress,
  Paper,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  Grid,
  Divider,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions
} from '@mui/material';
import {
  PlayArrow as StartIcon,
  Stop as StopIcon,
  Settings as SettingsIcon,
  TrendingUp as TrendingUpIcon,
  Psychology as BrainIcon,
  Speed as SpeedIcon,
  Analytics as AnalyticsIcon,
  AutoGraph as AutoGraphIcon,
  ExpandMore as ExpandMoreIcon,
  Tune as TuneIcon,
  Assessment as AssessmentIcon,
  Star as StarIcon,
  Visibility as ViewIcon,
  GetApp as DownloadIcon,
  Warning as WarningIcon
} from '@mui/icons-material';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip as RechartsTooltip, ResponsiveContainer, ScatterChart, Scatter, BarChart, Bar } from 'recharts';
import { FraudDetectionAPI, HyperparameterOptimizationConfig, OptimizationResult } from '../services/api';

// ==================== INTERFACES ====================
interface TuningExperiment {
  experimentId: number;
  parameters: any;
  score: number;
  metrics: any;
  status: 'running' | 'completed' | 'failed';
  timestamp: string;
  actualModelName?: string;
}

interface TuningConfiguration {
  modelType: 'LightGBM' | 'PCA' | 'Ensemble';
  optimizationMetric: 'f1_score' | 'accuracy' | 'precision' | 'recall' | 'auc' | 'auc_pr';
  searchStrategy: 'random';
  maxExperiments: number;
  cvFolds: number;
  parameterGrid: any;
  earlyStoppingEnabled: boolean;
  earlyStoppingPatience: number;
}

interface ParameterDefinition {
  name: string;
  displayName: string;
  type: 'int' | 'float' | 'categorical' | 'boolean';
  min?: number;
  max?: number;
  values?: any[];
  step?: number;
  description: string;
  impact: 'high' | 'medium' | 'low';
}

// ==================== CONSTANTS ====================
const OPTIMIZATION_METRICS = [
  { value: 'f1_score', label: 'F1-Score (Dengeli)', description: 'Precision ve Recall\'ın harmonik ortalaması' },
  { value: 'accuracy', label: 'Accuracy (Doğruluk)', description: 'Genel doğru tahmin oranı' },
  { value: 'precision', label: 'Precision (Kesinlik)', description: 'Fraud tahminlerinin doğruluk oranı' },
  { value: 'recall', label: 'Recall (Geri Çağırma)', description: 'Fraud\'ları yakalama oranı' },
  { value: 'auc', label: 'AUC-ROC', description: 'ROC eğrisi altındaki alan' },
  { value: 'auc_pr', label: 'AUC-PR', description: 'Precision-Recall eğrisi altındaki alan' }
];

const SEARCH_STRATEGIES = [
  {
    value: 'random',
    label: 'Random Search',
    description: 'Rastgele parametre kombinasyonları dener',
    pros: ['Hızlı', 'Skalabilir'],
    cons: ['Az kapsamlı', 'Şansa bağlı']
  }
];

const LIGHTGBM_PARAMETERS: ParameterDefinition[] = [
  {
    name: 'numberOfTrees',
    displayName: 'Ağaç Sayısı',
    type: 'int',
    min: 100,
    max: 2000,
    step: 100,
    description: 'Model karar ağacı sayısı. Daha fazla ağaç = daha iyi performans ama overfitting riski',
    impact: 'high'
  },
  {
    name: 'learningRate',
    displayName: 'Öğrenme Hızı',
    type: 'float',
    min: 0.001,
    max: 0.3,
    step: 0.001,
    description: 'Her iterasyondaki öğrenme adımı. Küçük = yavaş ama güvenli',
    impact: 'high'
  },
  {
    name: 'numberOfLeaves',
    displayName: 'Yaprak Sayısı',
    type: 'int',
    min: 16,
    max: 512,
    step: 16,
    description: 'Her ağaçtaki maksimum yaprak sayısı. Model karmaşıklığını belirler',
    impact: 'medium'
  },
  {
    name: 'featureFraction',
    displayName: 'Özellik Kullanım Oranı',
    type: 'float',
    min: 0.5,
    max: 1.0,
    step: 0.05,
    description: 'Her ağaçta kullanılacak özelliklerin yüzdesi. Overfitting\'i azaltır',
    impact: 'medium'
  },
  {
    name: 'l1Regularization',
    displayName: 'L1 Düzenleme',
    type: 'float',
    min: 0,
    max: 1,
    step: 0.01,
    description: 'L1 regularization değeri. Overfitting\'i önler',
    impact: 'medium'
  },
  {
    name: 'l2Regularization',
    displayName: 'L2 Düzenleme',
    type: 'float',
    min: 0,
    max: 1,
    step: 0.01,
    description: 'L2 regularization değeri. Model kararlılığını artırır',
    impact: 'medium'
  },
  {
    name: 'useClassWeights',
    displayName: 'Sınıf Ağırlıkları',
    type: 'boolean',
    description: 'Dengesiz veri için otomatik ağırlık ayarı',
    impact: 'high'
  }
];

const PCA_PARAMETERS: ParameterDefinition[] = [
  {
    name: 'componentCount',
    displayName: 'Bileşen Sayısı',
    type: 'int',
    min: 5,
    max: 50,
    step: 5,
    description: 'Ana bileşen analizi boyut sayısı. Az = hızlı ama kayıp',
    impact: 'high'
  },
  {
    name: 'anomalyThreshold',
    displayName: 'Anomali Eşiği',
    type: 'float',
    min: 1.0,
    max: 4.0,
    step: 0.1,
    description: 'Anomali tespit hassasiyeti. Düşük = hassas, yüksek = seçici',
    impact: 'high'
  },
  {
    name: 'standardizeInput',
    displayName: 'Veri Normalizasyonu',
    type: 'boolean',
    description: 'Giriş verilerini normalize et. PCA için genellikle gerekli',
    impact: 'medium'
  }
];

const ENSEMBLE_PARAMETERS: ParameterDefinition[] = [
  {
    name: 'lightgbmWeight',
    displayName: 'LightGBM Ağırlığı',
    type: 'float',
    min: 0.1,
    max: 0.9,
    step: 0.05,
    description: 'Final kararda LightGBM\'in etkisi',
    impact: 'high'
  },
  {
    name: 'pcaWeight',
    displayName: 'PCA Ağırlığı',
    type: 'float',
    min: 0.1,
    max: 0.9,
    step: 0.05,
    description: 'Final kararda PCA\'nın etkisi',
    impact: 'high'
  },
  {
    name: 'threshold',
    displayName: 'Karar Eşiği',
    type: 'float',
    min: 0.1,
    max: 0.9,
    step: 0.05,
    description: 'Fraud kararı için minimum skor',
    impact: 'high'
  },
  {
    name: 'enableCrossValidation',
    displayName: 'Çapraz Doğrulama',
    type: 'boolean',
    description: 'K-fold cross validation uygula',
    impact: 'medium'
  }
];

// ==================== COMPONENT ====================
const HyperparameterTuning: React.FC = () => {
  // ========== STATE ==========
  const [activeStep, setActiveStep] = useState(0);
  const [tuningConfig, setTuningConfig] = useState<TuningConfiguration>({
    modelType: 'LightGBM',
    optimizationMetric: 'f1_score',
    searchStrategy: 'random',
    maxExperiments: 20,
    cvFolds: 5,
    parameterGrid: {},
    earlyStoppingEnabled: true,
    earlyStoppingPatience: 3
  });

  const [experiments, setExperiments] = useState<TuningExperiment[]>([]);
  const [isRunning, setIsRunning] = useState(false);
  const [currentExperiment, setCurrentExperiment] = useState(0);
  const [bestExperiment, setBestExperiment] = useState<TuningExperiment | null>(null);

  const [parameterRanges, setParameterRanges] = useState<any>({});
  const [showResults, setShowResults] = useState(false);
  const [showParameterDetails, setShowParameterDetails] = useState(false);

  // ========== EFFECTS ==========
  useEffect(() => {
    // Model tipine göre default parameter ranges ayarla
    const defaultRanges = getDefaultParameterRanges(tuningConfig.modelType);
    setParameterRanges(defaultRanges);
  }, [tuningConfig.modelType]);

  useEffect(() => {
    // En iyi experiment'i güncelle
    if (experiments.length > 0) {
      const best = experiments.reduce((prev, current) =>
        current.score > prev.score ? current : prev
      );
      setBestExperiment(best);
    }
  }, [experiments]);

  // ========== HELPER FUNCTIONS ==========
  // Optimizasyon skorunu çıkar
  const extractOptimizationScore = (result: any, metric: string): number => {
    console.log('🔍 extractOptimizationScore called with:', { result, metric });
    console.log('🔍 Full result structure:', JSON.stringify(result, null, 2));

    // Backend'den gelen farklı response yapılarını kontrol et
    let metrics = null;

    // 1. Direkt metrics alanı
    if (result.metrics) {
      metrics = result.metrics;
      console.log('🔍 Found metrics in result.metrics:', metrics);
    }
    // 2. BasicMetrics alanı (PascalCase)
    else if (result.BasicMetrics) {
      metrics = result.BasicMetrics;
      console.log('🔍 Found metrics in result.BasicMetrics:', metrics);
    }
    // 3. basicMetrics alanı (camelCase) - Backend'den gelen gerçek yapı
    else if (result.basicMetrics) {
      metrics = result.basicMetrics;
      console.log('🔍 Found metrics in result.basicMetrics:', metrics);
    }
    // 4. Metrics alanı (PascalCase)
    else if (result.Metrics) {
      metrics = result.Metrics;
      console.log('🔍 Found metrics in result.Metrics:', metrics);
    }
    // 5. Data içinde basicMetrics
    else if (result.data && result.data.basicMetrics) {
      metrics = result.data.basicMetrics;
      console.log('🔍 Found metrics in result.data.basicMetrics:', metrics);
    }
    // 6. Data içinde BasicMetrics
    else if (result.data && result.data.BasicMetrics) {
      metrics = result.data.BasicMetrics;
      console.log('🔍 Found metrics in result.data.BasicMetrics:', metrics);
    }
    // 7. Data içinde metrics
    else if (result.data && result.data.metrics) {
      metrics = result.data.metrics;
      console.log('🔍 Found metrics in result.data.metrics:', metrics);
    }
    // 8. Direkt result'ın kendisi metrics olabilir
    else if (typeof result === 'object' && (result.accuracy !== undefined || result.f1_score !== undefined)) {
      metrics = result;
      console.log('🔍 Using result as metrics directly:', metrics);
    }

    if (!metrics) {
      console.warn('⚠️ No metrics found in result:', result);
      return 0;
    }

    console.log('🔍 Processing metrics:', metrics);
    console.log('🔍 Metric type:', typeof metrics);

    let score = 0;

    switch (metric) {
      case 'f1_score':
        // Backend'den gelen f1Score (camelCase) ve f1_score (snake_case) kontrol et
        score = (metrics as any).f1Score || (metrics as any).f1_score || (metrics as any).F1Score || 0;
        break;
      case 'accuracy':
        score = (metrics as any).accuracy || (metrics as any).Accuracy || 0;
        break;
      case 'precision':
        score = (metrics as any).precision || (metrics as any).Precision || 0;
        break;
      case 'recall':
        score = (metrics as any).recall || (metrics as any).Recall || 0;
        break;
      case 'auc':
        score = (metrics as any).auc || (metrics as any).AUC || 0;
        break;
      case 'auc_pr':
        score = (metrics as any).aucpr || (metrics as any).auc_pr || (metrics as any).AUCPR || 0;
        break;
      default:
        score = (metrics as any).accuracy || (metrics as any).Accuracy || 0;
    }

    console.log(`🔍 Extracted score for ${metric}:`, score);
    console.log(`🔍 Score type:`, typeof score);

    // Score'u number'a çevir
    const numericScore = typeof score === 'string' ? parseFloat(score) : Number(score);
    console.log(`🔍 Final numeric score:`, numericScore);

    return isNaN(numericScore) ? 0 : numericScore;
  };

  // Parametre kombinasyonlarını üret
  const generateParameterCombinations = (config: HyperparameterOptimizationConfig) => {
    const combinations: any[] = [];
    const parameterGrid = config.parameterGrid;

    console.log('🔧 generateParameterCombinations called with:', config);
    console.log('🔧 Parameter Grid:', parameterGrid);

    // Random Search
    console.log('🔧 Random Search - Generating combinations');
    for (let i = 0; i < (config.maxExperiments || 50); i++) {
      const randomParams = generateRandomParameterSet(config.modelType, parameterGrid);
      console.log(`🔧 Random param set ${i + 1}:`, randomParams);
      combinations.push(randomParams);
    }

    console.log('🔧 Final combinations:', combinations);
    return combinations.slice(0, config.maxExperiments);
  };

  // Rastgele parametre seti üret
  const generateRandomParameterSet = (modelType: string, parameterGrid: any): any => {
    const params: any = {};
    const enabledParams = Object.entries(parameterGrid).filter(([_, config]: [string, any]) => config.enabled);

    enabledParams.forEach(([paramName, paramConfig]: [string, any]) => {
      const paramDef = getParameterDefinition(modelType, paramName);
      if (!paramDef) return;

      switch (paramDef.type) {
        case 'int':
          params[paramName] = Math.floor(Math.random() * (paramConfig.max - paramConfig.min + 1)) + paramConfig.min;
          break;
        case 'float':
          params[paramName] = Number((Math.random() * (paramConfig.max - paramConfig.min) + paramConfig.min).toFixed(3));
          break;
        case 'boolean':
          params[paramName] = Math.random() > 0.5;
          break;
        case 'categorical':
          if (paramConfig.values && paramConfig.values.length > 0) {
            params[paramName] = paramConfig.values[Math.floor(Math.random() * paramConfig.values.length)];
          }
          break;
      }
    });

    return params;
  };

  // Parametre değerlerini al
  const getParameterValues = (paramName: string, paramConfig: any): any[] => {
    const paramDef = getCurrentParameters(tuningConfig.modelType).find(p => p.name === paramName);
    if (!paramDef) return [0.1];

    switch (paramDef.type) {
      case 'int':
        const intValues = [];
        for (let i = paramConfig.min; i <= paramConfig.max; i += (paramConfig.step || 1)) {
          intValues.push(i);
          if (intValues.length >= 3) break; // Max 3 değer grid için
        }
        return intValues;

      case 'float':
        const floatValues = [];
        for (let i = paramConfig.min; i <= paramConfig.max; i += (paramConfig.step || 0.1)) {
          floatValues.push(Number(i.toFixed(3)));
          if (floatValues.length >= 3) break; // Max 3 değer
        }
        return floatValues;

      case 'boolean':
        return [true, false];

      case 'categorical':
        return paramConfig.values || ['default'];

      default:
        return [0.1];
    }
  };

  const getParameterDefinition = (modelType: string, paramName: string): ParameterDefinition | null => {
    const allParams = modelType === 'LightGBM' ? LIGHTGBM_PARAMETERS :
      modelType === 'PCA' ? PCA_PARAMETERS :
        modelType === 'Ensemble' ? ENSEMBLE_PARAMETERS : [];

    return allParams.find(p => p.name === paramName) || null;
  };

  const getCurrentParameters = (modelType: string): ParameterDefinition[] => {
    switch (modelType) {
      case 'LightGBM': return LIGHTGBM_PARAMETERS;
      case 'PCA': return PCA_PARAMETERS;
      case 'Ensemble': return ENSEMBLE_PARAMETERS;
      default: return [];
    }
  };

  const getDefaultParameterRanges = (modelType: string) => {
    switch (modelType) {
      case 'LightGBM':
        return LIGHTGBM_PARAMETERS.reduce((acc, param) => {
          acc[param.name] = {
            enabled: true,
            min: param.min || 0,
            max: param.max || 1,
            step: param.step || 0.1,
            values: param.values || []
          };
          return acc;
        }, {} as any);
      case 'PCA':
        return PCA_PARAMETERS.reduce((acc, param) => {
          acc[param.name] = {
            enabled: true,
            min: param.min || 0,
            max: param.max || 1,
            step: param.step || 0.1,
            values: param.values || []
          };
          return acc;
        }, {} as any);
      case 'Ensemble':
        return ENSEMBLE_PARAMETERS.reduce((acc, param) => {
          acc[param.name] = {
            enabled: true,
            min: param.min || 0,
            max: param.max || 1,
            step: param.step || 0.1,
            values: param.values || []
          };
          return acc;
        }, {} as any);
      default:
        return {};
    }
  };

  // ========== HANDLERS ==========
  const handleConfigChange = (field: keyof TuningConfiguration, value: any) => {
    setTuningConfig(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const handleParameterRangeChange = (paramName: string, field: string, value: any) => {
    setParameterRanges((prev: any) => ({
      ...prev,
      [paramName]: {
        ...prev[paramName],
        [field]: value
      }
    }));
  };

  const handleStartTuning = async () => {
    try {
      setIsRunning(true);
      setCurrentExperiment(0);
      setExperiments([]);
      setBestExperiment(null);
      setShowResults(false);

      console.log('🚀 Hiperparametre optimizasyonu başlatılıyor...', tuningConfig);

      // Optimizasyon config'ini hazırla
      const optimizationConfig: HyperparameterOptimizationConfig = {
        modelType: tuningConfig.modelType,
        optimizationMetric: tuningConfig.optimizationMetric,
        searchStrategy: tuningConfig.searchStrategy,
        nTrials: tuningConfig.maxExperiments,
        cvFolds: tuningConfig.cvFolds,
        parameterSpace: parameterRanges,
        parameterGrid: parameterRanges,
        scoringMetric: tuningConfig.optimizationMetric,
        maxExperiments: tuningConfig.maxExperiments,
      };

      // Manuel olarak experiment'leri çalıştır (real-time updates için)
      const tempExperiments: TuningExperiment[] = [];
      let bestScore = 0;
      let bestParams: any = {};
      let bestMetrics: any = {};

      // Parametre kombinasyonlarını üret
      const parameterCombinations = generateParameterCombinations(optimizationConfig);
      console.log(`📊 ${parameterCombinations.length} farklı parametre kombinasyonu test edilecek`);
      console.log('🔍 Parameter Ranges:', parameterRanges);
      console.log('🔍 Generated Combinations:', parameterCombinations);
      console.log('🔍 Max Experiments:', tuningConfig.maxExperiments);
      console.log('🔍 Loop will run:', Math.min(tuningConfig.maxExperiments || 0, parameterCombinations.length), 'times');

      const loopCount = Math.min(tuningConfig.maxExperiments || 0, parameterCombinations.length);

      if (loopCount === 0) {
        console.log('❌ Loop count is 0, no experiments will run!');
        throw new Error('Hiçbir parametre kombinasyonu oluşturulamadı');
      }

      for (let i = 0; i < loopCount; i++) {
        console.log(`🔄 For loop iteration ${i + 1}/${loopCount} başladı`);

        const parameters = parameterCombinations[i];

        // Progress güncelle
        setCurrentExperiment(i + 1);

        console.log(`🔬 Deneme ${i + 1}/${tuningConfig.maxExperiments}: Parametreler test ediliyor`, parameters);

        try {
          // .NET API'ye model eğitimi isteği gönder
          let trainingResult: any;

          switch (tuningConfig.modelType) {
            case 'LightGBM':
              console.log('📞 LightGBM API çağrısı yapılıyor...');
              trainingResult = await FraudDetectionAPI.trainLightGBM(parameters);
              break;
            case 'PCA':
              console.log('📞 PCA API çağrısı yapılıyor...');
              trainingResult = await FraudDetectionAPI.trainPCA(parameters);
              break;
            case 'Ensemble':
              console.log('📞 Ensemble API çağrısı yapılıyor...');
              trainingResult = await FraudDetectionAPI.trainEnsemble(parameters);
              break;
            default:
              throw new Error(`Desteklenmeyen model tipi: ${tuningConfig.modelType}`);
          }

          console.log('📥 API yanıtı alındı:', trainingResult);

          // Sonucu değerlendir
          if (trainingResult && !trainingResult.error) {
            const score = extractOptimizationScore(trainingResult, tuningConfig.optimizationMetric);
            const metrics = trainingResult.BasicMetrics || trainingResult.Metrics || {};

            console.log(`✅ Deneme ${i + 1} başarılı: Score = ${score.toFixed(4)}`);

            // En iyi sonucu güncelle
            if (score > bestScore) {
              bestScore = score;
              bestParams = parameters;
              bestMetrics = metrics;
              console.log(`🏆 Yeni en iyi skor: ${score.toFixed(4)}`);
            }

            const newExperiment: TuningExperiment = {
              experimentId: i + 1,
              parameters: parameters,
              score: score,
              metrics: metrics,
              status: 'completed',
              timestamp: new Date().toISOString(),
              actualModelName: trainingResult.actualModelName || trainingResult.modelId
            };

            tempExperiments.push(newExperiment);

            // Real-time UI güncelleme
            setExperiments([...tempExperiments]);

            // En iyi experiment'i güncelle
            if (score === bestScore) {
              setBestExperiment(newExperiment);
            }

          } else {
            console.log(`❌ Deneme ${i + 1} başarısız:`, trainingResult?.error);

            const failedExperiment: TuningExperiment = {
              experimentId: i + 1,
              parameters: parameters,
              score: 0,
              metrics: {},
              status: 'failed',
              timestamp: new Date().toISOString()
            };

            tempExperiments.push(failedExperiment);
            setExperiments([...tempExperiments]);
          }

          // Early stopping kontrolü
          if (tuningConfig.earlyStoppingEnabled && tempExperiments.length >= (tuningConfig.earlyStoppingPatience || 5)) {
            const recentScores = tempExperiments.slice(-(tuningConfig.earlyStoppingPatience || 5)).map(e => e.score);
            const improvement = Math.max(...recentScores) - Math.min(...recentScores);

            if (improvement < 0.001) { // %0.1'den az gelişme
              console.log('🛑 Early stopping: Son denemeler arasında yeterli gelişme yok');
              break;
            }
          }

          // UI refresh için kısa delay
          await new Promise(resolve => setTimeout(resolve, 800));

        } catch (error: any) {
          console.error(`🚨 Deneme ${i + 1} hatası:`, error);

          const errorExperiment: TuningExperiment = {
            experimentId: i + 1,
            parameters: parameters,
            score: 0,
            metrics: {},
            status: 'failed',
            timestamp: new Date().toISOString()
          };

          tempExperiments.push(errorExperiment);
          setExperiments([...tempExperiments]);
        }

        console.log(`✅ For loop iteration ${i + 1} tamamlandı`);
      }

      console.log(`✅ Hiperparametre optimizasyonu tamamlandı! En iyi skor: ${bestScore.toFixed(4)}`);
      console.log(`📊 Toplam ${tempExperiments.length} deneme yapıldı`);
      setShowResults(true);

    } catch (error: any) {
      console.error('❌ Optimizasyon hatası:', error);

      // Hata durumunda kullanıcıya bildirimi göster
      alert(`Hiperparametre optimizasyonu sırasında hata oluştu: ${error.message}`);
    } finally {
      setIsRunning(false);
      setCurrentExperiment(0);
      console.log('🏁 handleStartTuning tamamlandı');
    }
  };

  const handleStopTuning = () => {
    setIsRunning(false);
  };

  const handleReset = () => {
    setExperiments([]);
    setBestExperiment(null);
    setCurrentExperiment(0);
    setShowResults(false);
    setActiveStep(0);
  };

  // ========== RENDER HELPERS ==========
  const renderModelSelection = () => (
    <Box>
      <Typography variant="h5" sx={{ textAlign: 'center', mb: 4, fontWeight: 'bold' }}>
        🎯 Model ve Metrik Seçimi
      </Typography>

      {/* Model Selection */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>Optimize Edilecek Model</Typography>
          <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 2 }}>
            {['LightGBM', 'PCA', 'Ensemble'].map((model) => (
              <Card
                key={model}
                sx={{
                  cursor: 'pointer',
                  border: tuningConfig.modelType === model ? 2 : 1,
                  borderColor: tuningConfig.modelType === model ? 'primary.main' : 'divider',
                  '&:hover': { borderColor: 'primary.main' }
                }}
                onClick={() => handleConfigChange('modelType', model as any)}
              >
                <CardContent sx={{ textAlign: 'center', p: 2 }}>
                  <Box sx={{ mb: 1 }}>
                    {model === 'LightGBM' && <SpeedIcon color="primary" />}
                    {model === 'PCA' && <AnalyticsIcon color="primary" />}
                    {model === 'Ensemble' && <AutoGraphIcon color="primary" />}
                  </Box>
                  <Typography variant="h6">{model}</Typography>
                  <Typography variant="caption" color="textSecondary">
                    {model === 'LightGBM' && 'Gradient Boosting'}
                    {model === 'PCA' && 'Anomaly Detection'}
                    {model === 'Ensemble' && 'Hybrid Model'}
                  </Typography>
                </CardContent>
              </Card>
            ))}
          </Box>
        </CardContent>
      </Card>

      {/* Optimization Metric */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>Optimizasyon Metriği</Typography>
          <FormControl fullWidth>
            <InputLabel>Optimize Edilecek Metrik</InputLabel>
            <Select
              value={tuningConfig.optimizationMetric}
              onChange={(e) => handleConfigChange('optimizationMetric', e.target.value)}
            >
              {OPTIMIZATION_METRICS.map((metric) => (
                <MenuItem key={metric.value} value={metric.value}>
                  <Box>
                    <Typography variant="body1">{metric.label}</Typography>
                    <Typography variant="caption" color="textSecondary">
                      {metric.description}
                    </Typography>
                  </Box>
                </MenuItem>
              ))}
            </Select>
          </FormControl>
        </CardContent>
      </Card>
    </Box>
  );

  const renderParameterConfiguration = () => (
    <Box>
      <Typography variant="h5" sx={{ textAlign: 'center', mb: 4, fontWeight: 'bold' }}>
        ⚙️ Parametre Aralıkları
      </Typography>

      {/* Configuration Summary */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>Optimizasyon Ayarları</Typography>
          <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 2 }}>
            <TextField
              label="Maksimum Deneme Sayısı"
              type="number"
              value={tuningConfig.maxExperiments}
              onChange={(e) => handleConfigChange('maxExperiments', parseInt(e.target.value))}
              inputProps={{ min: 5, max: 100 }}
            />
            <TextField
              label="Cross-Validation Folds"
              type="number"
              value={tuningConfig.cvFolds}
              onChange={(e) => handleConfigChange('cvFolds', parseInt(e.target.value))}
              inputProps={{ min: 3, max: 10 }}
            />
            <FormControlLabel
              control={
                <Switch
                  checked={tuningConfig.earlyStoppingEnabled}
                  onChange={(e) => handleConfigChange('earlyStoppingEnabled', e.target.checked)}
                />
              }
              label="Erken Durdurma"
            />
            {tuningConfig.earlyStoppingEnabled && (
              <TextField
                label="Sabır Değeri"
                type="number"
                value={tuningConfig.earlyStoppingPatience}
                onChange={(e) => handleConfigChange('earlyStoppingPatience', parseInt(e.target.value))}
                inputProps={{ min: 1, max: 10 }}
              />
            )}
          </Box>
        </CardContent>
      </Card>

      {/* Parameter Ranges */}
      <Typography variant="h6" sx={{ mb: 2 }}>
        {tuningConfig.modelType} Parametreleri
      </Typography>

      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(400px, 1fr))', gap: 2 }}>
        {getCurrentParameters(tuningConfig.modelType).map((param) => (
          <Card key={param.name} variant="outlined">
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <FormControlLabel
                  control={
                    <Switch
                      checked={parameterRanges[param.name]?.enabled || false}
                      onChange={(e) => handleParameterRangeChange(param.name, 'enabled', e.target.checked)}
                    />
                  }
                  label={param.displayName}
                  sx={{ flex: 1 }}
                />
                <Chip
                  label={param.impact}
                  size="small"
                  color={param.impact === 'high' ? 'error' : param.impact === 'medium' ? 'warning' : 'info'}
                  onClick={(e) => e.stopPropagation()}
                  sx={{ cursor: 'default' }}
                />
              </Box>

              <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                {param.description}
              </Typography>

              {parameterRanges[param.name]?.enabled && (
                <Box>
                  {param.type === 'boolean' ? (
                    <Alert severity="info" sx={{ mt: 1 }}>
                      Boolean parametre: Otomatik olarak true/false değerleri denenecek
                    </Alert>
                  ) : param.type === 'categorical' ? (
                    <TextField
                      label="Değerler (virgülle ayırın)"
                      fullWidth
                      value={parameterRanges[param.name]?.values?.join(', ') || ''}
                      onChange={(e) => handleParameterRangeChange(param.name, 'values', e.target.value.split(',').map((v: string) => v.trim()))}
                      size="small"
                    />
                  ) : (
                    <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
                      <TextField
                        label="Minimum"
                        type="number"
                        value={parameterRanges[param.name]?.min || param.min || 0}
                        onChange={(e) => handleParameterRangeChange(param.name, 'min', parseFloat(e.target.value))}
                        size="small"
                        inputProps={{ step: param.step }}
                      />
                      <TextField
                        label="Maksimum"
                        type="number"
                        value={parameterRanges[param.name]?.max || param.max || 1}
                        onChange={(e) => handleParameterRangeChange(param.name, 'max', parseFloat(e.target.value))}
                        size="small"
                        inputProps={{ step: param.step }}
                      />
                    </Box>
                  )}
                </Box>
              )}
            </CardContent>
          </Card>
        ))}
      </Box>

      <Alert severity="info" sx={{ mt: 3 }}>
        <Typography variant="body2">
          💡 <strong>İpucu:</strong> İlk optimizasyonda geniş aralıklar kullanın, sonra en iyi değerlerin etrafında daraltın.
        </Typography>
      </Alert>
    </Box>
  );

  const renderOptimizationProgress = () => (
    <Box>
      <Typography variant="h5" sx={{ textAlign: 'center', mb: 4, fontWeight: 'bold' }}>
        🚀 Hiperparametre Optimizasyonu
      </Typography>

      {!isRunning && experiments.length === 0 && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>Optimizasyon Başlatmaya Hazır</Typography>
            <Alert severity="info" sx={{ mb: 2 }}>
              <Typography variant="body2">
                <strong>Model:</strong> {tuningConfig.modelType}<br />
                <strong>Metrik:</strong> {OPTIMIZATION_METRICS.find(m => m.value === tuningConfig.optimizationMetric)?.label}<br />
                <strong>Strateji:</strong> {SEARCH_STRATEGIES.find(s => s.value === tuningConfig.searchStrategy)?.label}<br />
                <strong>Maksimum Deneme:</strong> {tuningConfig.maxExperiments}
              </Typography>
            </Alert>

            <Button
              variant="contained"
              size="large"
              onClick={handleStartTuning}
              startIcon={<StartIcon />}
              sx={{ minWidth: 200 }}
            >
              🎯 Optimizasyonu Başlat
            </Button>
          </CardContent>
        </Card>
      )}

      {isRunning && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
              <Typography variant="h6">Optimizasyon Çalışıyor...</Typography>
              <Button
                variant="outlined"
                color="error"
                onClick={handleStopTuning}
                startIcon={<StopIcon />}
              >
                Durdur
              </Button>
            </Box>

            <LinearProgress
              variant="determinate"
              value={(currentExperiment / tuningConfig.maxExperiments) * 100}
              sx={{ mb: 2, height: 8, borderRadius: 4 }}
            />

            <Typography variant="body2" color="textSecondary">
              Deneme {currentExperiment} / {tuningConfig.maxExperiments} ({Math.round((currentExperiment / tuningConfig.maxExperiments) * 100)}% tamamlandı)
            </Typography>
          </CardContent>
        </Card>
      )}

      {experiments.length > 0 && (
        <Card sx={{ mb: 3 }}>
          <CardContent>
            <Typography variant="h6" gutterBottom>📊 Anlık Sonuçlar</Typography>

            {bestExperiment && (
              <Alert severity="success" sx={{ mb: 2 }}>
                <Typography variant="body2">
                  🏆 <strong>En İyi Skor:</strong> {bestExperiment.score.toFixed(4)}
                  (Deneme #{bestExperiment.experimentId})
                </Typography>
              </Alert>
            )}

            <Box sx={{ height: 300, mb: 2 }}>
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={experiments.filter(e => e.status === 'completed')}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="experimentId" />
                  <YAxis domain={['dataMin', 'dataMax']} />
                  <RechartsTooltip />
                  <Line type="monotone" dataKey="score" stroke="#8884d8" strokeWidth={2} dot={{ fill: '#8884d8' }} />
                </LineChart>
              </ResponsiveContainer>
            </Box>

            <TableContainer component={Paper} sx={{ maxHeight: 300 }}>
              <Table stickyHeader size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Deneme</TableCell>
                    <TableCell>Skor</TableCell>
                    <TableCell>Durum</TableCell>
                    <TableCell>Parametreler</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {experiments.slice(-10).reverse().map((exp) => (
                    <TableRow key={exp.experimentId}>
                      <TableCell>#{exp.experimentId}</TableCell>
                      <TableCell>
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                          {exp.score.toFixed(4)}
                          {exp.experimentId === bestExperiment?.experimentId && (
                            <StarIcon color="warning" sx={{ ml: 1, fontSize: 16 }} />
                          )}
                        </Box>
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={exp.status}
                          size="small"
                          color={exp.status === 'completed' ? 'success' : exp.status === 'failed' ? 'error' : 'info'}
                        />
                      </TableCell>
                      <TableCell>
                        <Tooltip title={JSON.stringify(exp.parameters, null, 2)}>
                          <IconButton size="small">
                            <ViewIcon />
                          </IconButton>
                        </Tooltip>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </CardContent>
        </Card>
      )}

      {showResults && experiments.length > 0 && (
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>🎉 Optimizasyon Tamamlandı!</Typography>

            {bestExperiment && (
              <Box>
                <Alert severity="success" sx={{ mb: 2 }}>
                  <Typography variant="body1" sx={{ fontWeight: 'bold', mb: 1 }}>
                    🏆 En İyi Konfigürasyon Bulundu!
                  </Typography>
                  <Typography variant="body2">
                    <strong>Skor:</strong> {bestExperiment.score.toFixed(4)}<br />
                    <strong>Deneme:</strong> #{bestExperiment.experimentId}<br />
                    <strong>Model:</strong> {bestExperiment.actualModelName}
                  </Typography>
                </Alert>

                <Accordion>
                  <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                    <Typography variant="h6">En İyi Parametreler</Typography>
                  </AccordionSummary>
                  <AccordionDetails>
                    <TableContainer component={Paper}>
                      <Table size="small">
                        <TableBody>
                          {Object.entries(bestExperiment.parameters).map(([key, value]) => (
                            <TableRow key={key}>
                              <TableCell><strong>{key}</strong></TableCell>
                              <TableCell>{typeof value === 'object' ? JSON.stringify(value) : String(value)}</TableCell>
                            </TableRow>
                          ))}
                        </TableBody>
                      </Table>
                    </TableContainer>
                  </AccordionDetails>
                </Accordion>

                <Box sx={{ display: 'flex', gap: 2, mt: 3 }}>
                  <Button
                    variant="outlined"
                    onClick={handleReset}
                    startIcon={<AssessmentIcon />}
                  >
                    Yeni Optimizasyon
                  </Button>
                </Box>
              </Box>
            )}
          </CardContent>
        </Card>
      )}
    </Box>
  );

  const steps = ['Model Seçimi', 'Parametre Aralıkları', 'Optimizasyon'];

  return (
    <Box sx={{ width: '100%', maxWidth: '100%' }}>
      {/* Header */}
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 1 }}>
          🔧 Hiperparametre Optimizasyonu
        </Typography>
        <Typography variant="body1" color="textSecondary">
          Akıllı arama algoritmaları ile model performansını maksimuma çıkarın
        </Typography>
      </Box>

      {/* Stepper */}
      <Stepper activeStep={activeStep} sx={{ mb: 4 }}>
        {steps.map((label) => (
          <Step key={label}>
            <StepLabel>{label}</StepLabel>
          </Step>
        ))}
      </Stepper>

      {/* Content */}
      <Card sx={{ minHeight: 600, width: '100%' }}>
        <CardContent sx={{ p: 4 }}>
          {activeStep === 0 && renderModelSelection()}
          {activeStep === 1 && renderParameterConfiguration()}
          {activeStep === 2 && renderOptimizationProgress()}
        </CardContent>
      </Card>

      {/* Navigation */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 3 }}>
        <Button
          disabled={activeStep === 0}
          onClick={() => setActiveStep(prev => prev - 1)}
        >
          Geri
        </Button>

        {activeStep < steps.length - 1 && (
          <Button
            variant="contained"
            onClick={() => setActiveStep(prev => prev + 1)}
          >
            İleri
          </Button>
        )}
      </Box>
    </Box>
  );
};

export default HyperparameterTuning; 