import React, { useState, useEffect, useCallback } from 'react';
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
  Tooltip,
  IconButton,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  LinearProgress,
  Paper,
  Grid,
  Divider,
  CircularProgress,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material';
import {
  School as TrainIcon,
  Settings as SettingsIcon,
  Info as InfoIcon,
  Speed as SpeedIcon,
  Memory as MemoryIcon,
  Tune as TuneIcon,
  PlayArrow as StartIcon,
  CheckCircle as SuccessIcon,
  Error as ErrorIcon,
  ExpandMore as ExpandMoreIcon,
  Psychology as BrainIcon,
  Analytics as AnalyticsIcon,
  Engineering as EngineeringIcon,
  AutoGraph as AutoGraphIcon,
  Forest as ForestIcon,
} from '@mui/icons-material';
import { FraudDetectionAPI } from '../services/api';

// ==================== INTERFACES ====================
interface ModelTrainingWizardProps {
  onTrainingComplete: (result: any) => void;
  showSnackbar: (message: string, severity: 'success' | 'error' | 'warning' | 'info') => void;
}

interface ModelConfiguration {
  name: string;
  description: string;
  type: 'LightGBM' | 'PCA' | 'Ensemble' | 'IsolationForest' | 'AutoEncoder';
  parameters: any;
  estimatedTime: string;
  complexity: 'Başlangıç' | 'Orta' | 'İleri';
  useCase: string;
}

interface ParameterConfig {
  key: string;
  label: string;
  description: string;
  type: 'number' | 'slider' | 'select' | 'boolean';
  value: any;
  min?: number;
  max?: number;
  step?: number;
  options?: Array<{ value: any; label: string }>;
  impact: string;
  recommendation: string;
}

// ==================== CONSTANTS ====================
const MODEL_TYPES = {
  LightGBM: {
    title: 'LightGBM Classifier',
    icon: <SpeedIcon />,
    color: '#ff9800',
    description: 'Hızlı ve yüksek performanslı gradient boosting algoritması',
    useCase: 'Dengeli veri setleri için ideal, yüksek accuracy',
    complexity: 'Orta' as const,
    estimatedTime: '5-15 dakika',
    strengths: ['Hızlı eğitim', 'Yüksek accuracy', 'Overfitting\'e dayanıklı'],
    weaknesses: ['Dengesiz veride sorunlu', 'Hiperparametre hassas']
  },
  PCA: {
    title: 'PCA Anomaly Detection',
    icon: <AnalyticsIcon />,
    color: '#4caf50',
    description: 'Boyut azaltma tabanlı anomali tespit algoritması',
    useCase: 'Yeni fraud türlerinin tespiti için ideal',
    complexity: 'Başlangıç' as const,
    estimatedTime: '2-8 dakika',
    strengths: ['Hızlı', 'Açıklanabilir', 'Yeni anomaliler'],
    weaknesses: ['Düşük precision', 'Lineer varsayımlar']
  },
  Ensemble: {
    title: 'Ensemble Hybrid Model',
    icon: <EngineeringIcon />,
    color: '#1976d2',
    description: 'LightGBM ve PCA\'nın birleşimi, en yüksek performans',
    useCase: 'Production ortamı için en optimal çözüm',
    complexity: 'İleri' as const,
    estimatedTime: '10-25 dakika',
    strengths: ['En yüksek accuracy', 'Dengeli performans', 'Robust'],
    weaknesses: ['Uzun eğitim', 'Kompleks parametre']
  },
  IsolationForest: {
    title: 'Isolation Forest',
    icon: <ForestIcon />,
    color: '#00bcd4',
    description: 'Ağaç tabanlı anomali tespit algoritması',
    useCase: 'Az etiketli veriler için ideal',
    complexity: 'Orta' as const,
    estimatedTime: '3-10 dakika',
    strengths: ['Label\'sız eğitim', 'Hızlı inference', 'Skalabilir'],
    weaknesses: ['Parametre hassas', 'Yüksek boyutlarda sorunlu']
  },
  AutoEncoder: {
    title: 'Neural AutoEncoder',
    icon: <BrainIcon />,
    color: '#9c27b0',
    description: 'Derin öğrenme tabanlı anomali tespit',
    useCase: 'Karmaşık fraud pattern\'ları için',
    complexity: 'İleri' as const,
    estimatedTime: '15-45 dakika',
    strengths: ['Karmaşık pattern', 'Yüksek kapasiteleri', 'Esnek'],
    weaknesses: ['Uzun eğitim', 'GPU gerekir', 'Açıklanamaz']
  }
};

const PARAMETER_CONFIGS: Record<string, ParameterConfig[]> = {
  LightGBM: [
    {
      key: 'numberOfTrees',
      label: 'Ağaç Sayısı',
      description: 'Modelde kullanılacak karar ağacı sayısı. Daha fazla ağaç daha iyi accuracy ama daha uzun eğitim demek.',
      type: 'slider',
      min: 100,
      max: 2000,
      step: 100,
      value: 1000,
      impact: 'Yüksek: Accuracy ve eğitim süresi',
      recommendation: '500-1500 arası optimal, 1000 başlangıç için ideal'
    },
    {
      key: 'numberOfLeaves',
      label: 'Yaprak Sayısı',
      description: 'Her ağaçtaki yaprak sayısı. Modelin karmaşıklığını belirler.',
      type: 'slider',
      min: 16,
      max: 256,
      step: 16,
      value: 128,
      impact: 'Orta: Model karmaşıklığı',
      recommendation: '64-128 çoğu durumda yeterli'
    },
    {
      key: 'learningRate',
      label: 'Öğrenme Hızı',
      description: 'Her iterasyondaki öğrenme adımının büyüklüğü. Küçük değerler daha iyi ama yavaş.',
      type: 'select',
      options: [
        { value: 0.001, label: '0.001 (Çok Yavaş - En Güvenli)' },
        { value: 0.005, label: '0.005 (Yavaş - Önerilen)' },
        { value: 0.01, label: '0.01 (Normal)' },
        { value: 0.05, label: '0.05 (Hızlı - Riskli)' },
        { value: 0.1, label: '0.1 (Çok Hızlı - Çok Riskli)' }
      ],
      value: 0.005,
      impact: 'Yüksek: Overfitting riski',
      recommendation: '0.005 çoğu durumda en güvenli seçim'
    },
    {
      key: 'featureFraction',
      label: 'Özellik Kullanım Oranı',
      description: 'Her ağaçta kullanılacak özelliklerin yüzdesi. Overfitting\'i azaltır.',
      type: 'slider',
      min: 0.5,
      max: 1.0,
      step: 0.1,
      value: 0.8,
      impact: 'Orta: Overfitting kontrolü',
      recommendation: '0.7-0.9 arası optimal'
    },
    {
      key: 'useClassWeights',
      label: 'Sınıf Ağırlıkları Kullan',
      description: 'Dengesiz veri için fraud sınıfına daha fazla ağırlık verir.',
      type: 'boolean',
      value: true,
      impact: 'Yüksek: Recall performansı',
      recommendation: 'Fraud detection için her zaman açık tutun'
    },
    {
      key: 'l1Regularization',
      label: 'L1 Düzenleme',
      description: 'Modelin karmaşıklığını azaltır, overfitting\'i önler.',
      type: 'select',
      options: [
        { value: 0, label: '0 (Düzenleme Yok)' },
        { value: 0.001, label: '0.001 (Minimal)' },
        { value: 0.01, label: '0.01 (Normal - Önerilen)' },
        { value: 0.1, label: '0.1 (Güçlü)' }
      ],
      value: 0.01,
      impact: 'Orta: Model karmaşıklığı',
      recommendation: '0.01 başlangıç için ideal'
    }
  ],
  PCA: [
    {
      key: 'componentCount',
      label: 'Bileşen Sayısı',
      description: 'Boyut azaltma sonrası kaç boyut kullanılacak. Az boyut = hızlı ama kayıp, çok boyut = yavaş ama detaylı.',
      type: 'slider',
      min: 5,
      max: 50,
      step: 5,
      value: 15,
      impact: 'Yüksek: Hız vs Accuracy',
      recommendation: '10-20 arası çoğu durumda optimal'
    },
    {
      key: 'anomalyThreshold',
      label: 'Anomali Eşiği',
      description: 'Bu değerin üstündeki skorlar anomali kabul edilir. Düşük değer = hassas (çok alarm), yüksek değer = kaçırma.',
      type: 'select',
      options: [
        { value: 1.5, label: '1.5 (Çok Hassas - Çok Alarm)' },
        { value: 2.0, label: '2.0 (Hassas)' },
        { value: 2.5, label: '2.5 (Dengeli - Önerilen)' },
        { value: 3.0, label: '3.0 (Seçici)' },
        { value: 3.5, label: '3.5 (Çok Seçici - Kaçırabilir)' }
      ],
      value: 2.5,
      impact: 'Yüksek: False Positive vs False Negative',
      recommendation: '2.5 dengeli başlangıç noktası'
    },
    {
      key: 'standardizeInput',
      label: 'Veri Standardizasyonu',
      description: 'Verileri 0-1 aralığına normalize eder. PCA için kritik.',
      type: 'boolean',
      value: true,
      impact: 'Kritik: Model performansı',
      recommendation: 'PCA için her zaman açık olmalı'
    }
  ],
  Ensemble: [
    {
      key: 'lightgbmWeight',
      label: 'LightGBM Ağırlığı',
      description: 'Final kararda LightGBM\'in etkisi. Yüksek değer = precision odaklı.',
      type: 'slider',
      min: 0.1,
      max: 0.9,
      step: 0.05,
      value: 0.7,
      impact: 'Yüksek: Precision vs Recall',
      recommendation: '0.6-0.8 arası dengeli, 0.7 başlangıç için ideal'
    },
    {
      key: 'pcaWeight',
      label: 'PCA Ağırlığı',
      description: 'Final kararda PCA\'nın etkisi. Yüksek değer = recall odaklı.',
      type: 'slider',
      min: 0.1,
      max: 0.9,
      step: 0.05,
      value: 0.3,
      impact: 'Yüksek: Recall vs Precision',
      recommendation: 'LightGBM ağırlığı ile toplamı 1.0 olmalı'
    },
    {
      key: 'threshold',
      label: 'Karar Eşiği',
      description: 'Bu değerin üstü fraud kabul edilir. Düşük = hassas (çok alarm), yüksek = kaçırma riski.',
      type: 'select',
      options: [
        { value: 0.3, label: '0.3 (Çok Hassas - Recall Öncelikli)' },
        { value: 0.4, label: '0.4 (Hassas)' },
        { value: 0.5, label: '0.5 (Dengeli - Standart)' },
        { value: 0.6, label: '0.6 (Seçici)' },
        { value: 0.7, label: '0.7 (Çok Seçici - Precision Öncelikli)' }
      ],
      value: 0.5,
      impact: 'Kritik: Precision vs Recall dengeleri',
      recommendation: '0.5 dengeli başlangıç, production için ayarlanabilir'
    },
    {
      key: 'enableCrossValidation',
      label: 'Çapraz Doğrulama',
      description: 'Model performansını daha güvenilir şekilde ölçer ama eğitim süresini uzatır.',
      type: 'boolean',
      value: true,
      impact: 'Orta: Eğitim süresi vs Güvenilirlik',
      recommendation: 'Production için her zaman açık olmalı'
    }
  ],
  IsolationForest: [
    {
      key: 'nEstimators',
      label: 'Ağaç Sayısı',
      description: 'Isolation Forest\'taki ağaç sayısı. Daha fazla ağaç daha kararlı ama yavaş.',
      type: 'slider',
      min: 50,
      max: 500,
      step: 50,
      value: 200,
      impact: 'Orta: Kararlılık vs Hız',
      recommendation: '100-300 arası optimal'
    },
    {
      key: 'contamination',
      label: 'Kirlilik Oranı',
      description: 'Verideki fraud oranı tahmini. Gerçek fraud oranına yakın olmalı.',
      type: 'select',
      options: [
        { value: 0.001, label: '0.1% (Çok Az Fraud)' },
        { value: 0.005, label: '0.5% (Az Fraud)' },
        { value: 0.01, label: '1% (Normal - Önerilen)' },
        { value: 0.02, label: '2% (Fazla Fraud)' },
        { value: 0.05, label: '5% (Çok Fazla Fraud)' }
      ],
      value: 0.01,
      impact: 'Kritik: Model hassasiyeti',
      recommendation: '0.5-1% çoğu durumda gerçekçi'
    },
    {
      key: 'maxSamples',
      label: 'Maksimum Örnek',
      description: 'Her ağaç için kullanılacak maksimum veri sayısı. "auto" tüm veriyi kullanır.',
      type: 'select',
      options: [
        { value: 'auto', label: 'Otomatik (Tüm Veri)' },
        { value: 1000, label: '1000 Örnek' },
        { value: 5000, label: '5000 Örnek' },
        { value: 10000, label: '10000 Örnek' }
      ],
      value: 'auto',
      impact: 'Orta: Hız vs Doğruluk',
      recommendation: 'Otomatik çoğu durumda en iyisi'
    },
    {
      key: 'randomState',
      label: 'Rastgele Durum',
      description: 'Sonuçların tekrarlanabilirliği için sabit değer.',
      type: 'number',
      min: 1,
      max: 9999,
      value: 42,
      impact: 'Düşük: Sadece tekrarlanabilirlik',
      recommendation: '42 standart değer'
    },
    {
      key: 'bootstrap',
      label: 'Bootstrap Örnekleme',
      description: 'Rastgele örnekleme ile ağaç çeşitliliği artırır.',
      type: 'boolean',
      value: true,
      impact: 'Orta: Model çeşitliliği',
      recommendation: 'Açık tutmanız önerilir'
    }
  ],
  AutoEncoder: [
    {
      key: 'hiddenLayers',
      label: 'Gizli Katman Boyutları',
      description: 'Her gizli katmandaki nöron sayısı. Daha karmaşık yapı daha iyi öğrenme ama overfitting riski.',
      type: 'select',
      options: [
        { value: [32, 16, 8], label: 'Basit: [32, 16, 8]' },
        { value: [64, 32, 16], label: 'Orta: [64, 32, 16] - Önerilen' },
        { value: [128, 64, 32], label: 'Karmaşık: [128, 64, 32]' },
        { value: [256, 128, 64], label: 'Çok Karmaşık: [256, 128, 64]' }
      ],
      value: [64, 32, 16],
      impact: 'Yüksek: Model kapasitesi vs Overfitting',
      recommendation: 'Orta seviye başlangıç için ideal'
    },
    {
      key: 'epochs',
      label: 'Eğitim Devri Sayısı',
      description: 'Modelin veri üzerinde kaç kez eğitileceği. Fazla = overfitting, az = underfitting.',
      type: 'slider',
      min: 10,
      max: 200,
      step: 10,
      value: 50,
      impact: 'Yüksek: Öğrenme vs Overfitting',
      recommendation: '30-100 arası optimal, 50 güvenli başlangıç'
    },
    {
      key: 'learningRate',
      label: 'Öğrenme Hızı',
      description: 'Adam optimizer için öğrenme hızı. Küçük = yavaş ama kararlı, büyük = hızlı ama kararsız.',
      type: 'select',
      options: [
        { value: 0.0001, label: '0.0001 (Çok Yavaş - Güvenli)' },
        { value: 0.0005, label: '0.0005 (Yavaş)' },
        { value: 0.001, label: '0.001 (Normal - Önerilen)' },
        { value: 0.005, label: '0.005 (Hızlı)' },
        { value: 0.01, label: '0.01 (Çok Hızlı - Riskli)' }
      ],
      value: 0.001,
      impact: 'Yüksek: Eğitim kararlılığı',
      recommendation: '0.001 çoğu durumda en güvenli'
    },
    {
      key: 'dropoutRate',
      label: 'Dropout Oranı',
      description: 'Overfitting\'i önlemek için nöronların kaçının rastgele kapatılacağı.',
      type: 'slider',
      min: 0.0,
      max: 0.5,
      step: 0.05,
      value: 0.2,
      impact: 'Orta: Overfitting kontrolü',
      recommendation: '0.1-0.3 arası optimal'
    },
    {
      key: 'batchSize',
      label: 'Batch Boyutu',
      description: 'Her adımda işlenecek örnek sayısı. Büyük = hızlı ama hafıza, küçük = yavaş ama kararlı.',
      type: 'select',
      options: [
        { value: 16, label: '16 (Küçük - Kararlı)' },
        { value: 32, label: '32 (Normal - Önerilen)' },
        { value: 64, label: '64 (Büyük)' },
        { value: 128, label: '128 (Çok Büyük - Hızlı)' }
      ],
      value: 32,
      impact: 'Orta: Hız vs Kararlılık',
      recommendation: '32 çoğu durumda optimal'
    },
    {
      key: 'activationFunction',
      label: 'Aktivasyon Fonksiyonu',
      description: 'Gizli katmanlar için aktivasyon fonksiyonu.',
      type: 'select',
      options: [
        { value: 'relu', label: 'ReLU (Standart)' },
        { value: 'tanh', label: 'Tanh (Smooth)' },
        { value: 'sigmoid', label: 'Sigmoid (Classic)' },
        { value: 'leaky_relu', label: 'Leaky ReLU (Robust)' }
      ],
      value: 'relu',
      impact: 'Orta: Öğrenme hızı',
      recommendation: 'ReLU çoğu durumda en iyi'
    },
    {
      key: 'earlyStopping',
      label: 'Erken Durdurma',
      description: 'Overfitting\'i önlemek için eğitimi erken durdurur.',
      type: 'boolean',
      value: true,
      impact: 'Yüksek: Overfitting kontrolü',
      recommendation: 'Her zaman açık tutun'
    },
    {
      key: 'patience',
      label: 'Sabır Değeri',
      description: 'Erken durdurma için kaç epoch bekleneceği.',
      type: 'slider',
      min: 5,
      max: 20,
      step: 1,
      value: 10,
      impact: 'Orta: Eğitim süresi',
      recommendation: '5-15 arası optimal'
    }
  ]
};

// ==================== COMPONENT ====================
const ModelTrainingWizard: React.FC<ModelTrainingWizardProps> = ({ onTrainingComplete, showSnackbar }) => {
  // ========== STATE ==========
  const [activeStep, setActiveStep] = useState(0);
  const [selectedModelType, setSelectedModelType] = useState<string>('');
  const [modelConfig, setModelConfig] = useState<ModelConfiguration>({
    name: '',
    description: '',
    type: 'LightGBM',
    parameters: {},
    estimatedTime: '',
    complexity: 'Başlangıç',
    useCase: ''
  });
  const [isTraining, setIsTraining] = useState(false);
  const [trainingProgress, setTrainingProgress] = useState(0);
  const [trainingStep, setTrainingStep] = useState('');
  const [trainingResult, setTrainingResult] = useState<any>(null);
  const [showAdvanced, setShowAdvanced] = useState(false);
  const [estimatedTimeCountdown, setEstimatedTimeCountdown] = useState(0);

  // ========== EFFECTS ==========
  useEffect(() => {
    if (selectedModelType && MODEL_TYPES[selectedModelType as keyof typeof MODEL_TYPES]) {
      const modelInfo = MODEL_TYPES[selectedModelType as keyof typeof MODEL_TYPES];
      const parametersConfig = PARAMETER_CONFIGS[selectedModelType];
      
      const defaultParams = parametersConfig?.reduce((acc: any, param: ParameterConfig) => {
        acc[param.key] = param.value;
        return acc;
      }, {}) || {};

      setModelConfig({
        name: `${modelInfo.title}_${new Date().toISOString().slice(0, 10)}`,
        description: modelInfo.description,
        type: selectedModelType as any,
        parameters: defaultParams,
        estimatedTime: modelInfo.estimatedTime,
        complexity: modelInfo.complexity,
        useCase: modelInfo.useCase
      });
    }
  }, [selectedModelType]);

  // Training progress simulation
  useEffect(() => {
    if (isTraining) {
      const progressInterval = setInterval(() => {
        setTrainingProgress(prev => {
          if (prev >= 90) return prev;
          const increment = modelConfig.type === 'AutoEncoder' ? Math.random() * 1 : Math.random() * 3;
          return prev + increment;
        });
      }, modelConfig.type === 'AutoEncoder' ? 3000 : 1000);

      const stepInterval = setInterval(() => {
        const steps = modelConfig.type === 'AutoEncoder' ? [
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
        ] : [
          'Veri yükleniyor...',
          'Veri ön işleme yapılıyor...',
          'Özellik çıkarımı...',
          'Model eğitimi başlatıldı...',
          'Hiperparametre optimizasyonu...',
          'Çapraz doğrulama...',
          'Model değerlendirme...',
          'Sonuçlar kaydediliyor...'
        ];
        
        const currentStepIndex = Math.floor((trainingProgress / 100) * steps.length);
        setTrainingStep(steps[currentStepIndex] || steps[steps.length - 1]);
      }, modelConfig.type === 'AutoEncoder' ? 5000 : 2000);

      return () => {
        clearInterval(progressInterval);
        clearInterval(stepInterval);
      };
    }
  }, [isTraining, trainingProgress, modelConfig.type]);

  // ========== HANDLERS ==========
  const handleNext = () => {
    setActiveStep(prev => prev + 1);
  };

  const handleBack = () => {
    setActiveStep(prev => prev - 1);
  };

  const handleParameterChange = (key: string, value: any) => {
    setModelConfig(prev => ({
      ...prev,
      parameters: {
        ...prev.parameters,
        [key]: value
      }
    }));
  };

  const handleStartTraining = async () => {
    try {
      setIsTraining(true);
      setTrainingProgress(0);
      setTrainingStep('Eğitim başlatılıyor...');

      // Simulate training progress
      const progressInterval = setInterval(() => {
        setTrainingProgress(prev => {
          if (prev >= 90) return prev;
          const increment = modelConfig.type === 'AutoEncoder' ? Math.random() * 1 : Math.random() * 3;
          return prev + increment;
        });
      }, modelConfig.type === 'AutoEncoder' ? 3000 : 1000);

      try {
        let result;
        const config = modelConfig.parameters;

        console.log('🚀 Model eğitimi başlatılıyor:', modelConfig.type, config);

        switch (modelConfig.type) {
          case 'LightGBM':
            result = await FraudDetectionAPI.trainLightGBM(config);
            break;
          case 'PCA':
            result = await FraudDetectionAPI.trainPCA(config);
            break;
          case 'Ensemble':
            result = await FraudDetectionAPI.trainEnsemble(config);
            break;
          case 'IsolationForest':
            result = await FraudDetectionAPI.trainIsolationForest(config);
            break;
          case 'AutoEncoder':
            result = await FraudDetectionAPI.trainAutoEncoder(config);
            break;
        }

        clearInterval(progressInterval);
        
        console.log('📊 Training sonucu alındı:', result);

        // Check if training was successful
        if (result.error) {
          throw new Error(result.error);
        }

        if (result.success === false) {
          throw new Error('Python model training failed');
        }

        setTrainingResult(result);
        setTrainingProgress(100);
        setTrainingStep('Eğitim tamamlandı!');
        
        // Debug için detaylı response log
        console.log('🔍 Training Result Full Response:', JSON.stringify(result, null, 2));
        console.log('🔍 Result.data:', result.data);
        console.log('🔍 Result keys:', Object.keys(result));
        if (result.data) {
          console.log('🔍 Result.data keys:', Object.keys(result.data));
        }
        
        // Otomatik olarak sonuçlar step'ine geç
        setTimeout(() => {
          setActiveStep(3);
        }, 1000);
        
        const modelName = (result.data as any)?.actualModelName || (result.data as any)?.modelName || modelConfig.name;
        showSnackbar(`✅ ${modelName} başarıyla eğitildi!`, 'success');
        onTrainingComplete(result);

      } catch (error: any) {
        clearInterval(progressInterval);
        console.error('❌ Training error:', error);
        setTrainingStep('Eğitim hatası!');
        setTrainingResult({ error: error.message, modelId: '', _raw: error });
        showSnackbar(`❌ Eğitim hatası: ${error.message}`, 'error');
      } finally {
        setIsTraining(false);
      }

    } catch (error: any) {
      console.error('❌ Training start error:', error);
      showSnackbar(`❌ Eğitim başlatılamadı: ${error.message}`, 'error');
      setIsTraining(false);
    }
  };

  const handleReset = () => {
    setActiveStep(0);
    setSelectedModelType('');
    setModelConfig({
      name: '',
      description: '',
      type: 'LightGBM',
      parameters: {},
      estimatedTime: '',
      complexity: 'Başlangıç',
      useCase: ''
    });
    setIsTraining(false);
    setTrainingProgress(0);
    setTrainingResult(null);
  };

  const updateModelConfig = useCallback(() => {
    if (selectedModelType && MODEL_TYPES[selectedModelType as keyof typeof MODEL_TYPES]) {
      const modelInfo = MODEL_TYPES[selectedModelType as keyof typeof MODEL_TYPES];
      const parametersConfig = PARAMETER_CONFIGS[selectedModelType];
      
      const defaultParams = parametersConfig?.reduce((acc: any, param: ParameterConfig) => {
        acc[param.key] = param.value;
        return acc;
      }, {}) || {};

      setModelConfig({
        name: `${modelInfo.title}_${new Date().toISOString().slice(0, 10)}`,
        description: modelInfo.description,
        type: selectedModelType as any,
        parameters: defaultParams,
        estimatedTime: modelInfo.estimatedTime,
        complexity: modelInfo.complexity,
        useCase: modelInfo.useCase
      });
    }
  }, [selectedModelType]);

  // ========== RENDER HELPERS ==========
  const renderModelSelection = () => (
    <Box>
      <Typography variant="h5" sx={{ textAlign: 'center', mb: 4, fontWeight: 'bold' }}>
        🎯 Model Seçimi
      </Typography>
      
      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(350px, 1fr))', gap: 3 }}>
        {Object.entries(MODEL_TYPES).map(([key, model]) => (
          <Card 
            key={key}
            sx={{ 
              cursor: 'pointer',
              height: '100%',
              transition: 'all 0.3s ease',
              border: selectedModelType === key ? 3 : 1,
              borderColor: selectedModelType === key ? model.color : 'divider',
              transform: selectedModelType === key ? 'scale(1.02)' : 'scale(1)',
              boxShadow: selectedModelType === key ? 4 : 1,
              '&:hover': {
                transform: 'scale(1.02)',
                boxShadow: 4
              }
            }}
            onClick={() => setSelectedModelType(key as any)}
          >
            <CardContent sx={{ p: 3 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
                <Box sx={{ color: model.color, mr: 2 }}>
                  {model.icon}
                </Box>
                <Typography variant="h6" sx={{ fontWeight: 'bold', flex: 1 }}>
                  {model.title}
                </Typography>
                <Chip 
                  label={model.complexity} 
                  size="small" 
                  color={
                    model.complexity === 'Başlangıç' ? 'success' : 
                    model.complexity === 'Orta' ? 'warning' : 'error'
                  }
                  onClick={(e) => e.stopPropagation()}
                  sx={{ cursor: 'default' }}
                />
              </Box>

              <Typography variant="body2" sx={{ mb: 2, minHeight: 40 }}>
                {model.description}
              </Typography>

              <Typography variant="caption" color="textSecondary" sx={{ mb: 1, display: 'block' }}>
                <strong>Kullanım Alanı:</strong> {model.useCase}
              </Typography>

              <Typography variant="caption" color="textSecondary" sx={{ mb: 2, display: 'block' }}>
                <strong>Tahmini Süre:</strong> {model.estimatedTime}
              </Typography>

              <Box sx={{ mb: 2 }}>
                <Typography variant="caption" sx={{ fontWeight: 'bold', color: 'success.main' }}>
                  ✅ Avantajlar:
                </Typography>
                <Typography variant="caption" display="block" sx={{ ml: 1 }}>
                  • {model.strengths.join(', ')}
                </Typography>
              </Box>

              <Box>
                <Typography variant="caption" sx={{ fontWeight: 'bold', color: 'warning.main' }}>
                  ⚠️ Dezavantajlar:
                </Typography>
                <Typography variant="caption" display="block" sx={{ ml: 1 }}>
                  • {model.weaknesses.join(', ')}
                </Typography>
              </Box>
            </CardContent>
          </Card>
        ))}
      </Box>

      {selectedModelType && (
        <Alert severity="info" sx={{ mt: 3 }}>
          <Typography variant="body2">
            <strong>{MODEL_TYPES[selectedModelType as keyof typeof MODEL_TYPES].title}</strong> seçildi. 
            Devam etmek için <strong>İleri</strong> butonuna tıklayın.
          </Typography>
        </Alert>
      )}
    </Box>
  );

  const renderParameterConfiguration = () => {
    const parameters = PARAMETER_CONFIGS[selectedModelType!] || [];

    return (
      <Box>
        <Typography variant="h5" sx={{ textAlign: 'center', mb: 4, fontWeight: 'bold' }}>
          ⚙️ Parametre Konfigürasyonu
        </Typography>

        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(400px, 1fr))', gap: 3 }}>
          {parameters.map((param) => (
            <Card key={param.key} variant="outlined" sx={{ p: 2, height: '100%' }}>
              <Box sx={{ display: 'flex', alignItems: 'center', mb: 1 }}>
                <Typography variant="h6" sx={{ fontWeight: 'medium', flex: 1 }}>
                  {param.label}
                </Typography>
                <Chip 
                  label={param.impact.split(':')[0]} 
                  size="small" 
                  color={
                    param.impact.includes('Kritik') ? 'error' : 
                    param.impact.includes('Yüksek') ? 'warning' : 'info'
                  }
                />
              </Box>

              <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
                {param.description}
              </Typography>

              {param.type === 'slider' && (
                <Box sx={{ px: 1 }}>
                  <Typography variant="caption" gutterBottom>
                    Değer: {modelConfig.parameters[param.key] || param.value}
                  </Typography>
                  <Slider
                    value={modelConfig.parameters[param.key] || param.value}
                    onChange={(_, value) => handleParameterChange(param.key, value)}
                    min={param.min}
                    max={param.max}
                    step={param.step}
                    marks
                    valueLabelDisplay="auto"
                    sx={{ mt: 1 }}
                  />
                </Box>
              )}

              {param.type === 'select' && (
                <FormControl fullWidth size="small">
                  <Select
                    value={modelConfig.parameters[param.key] || param.value}
                    onChange={(e) => handleParameterChange(param.key, e.target.value)}
                  >
                    {param.options?.map((option) => (
                      <MenuItem key={String(option.value)} value={option.value}>
                        {option.label}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
              )}

              {param.type === 'boolean' && (
                <FormControlLabel
                  control={
                    <Switch
                      checked={modelConfig.parameters[param.key] ?? param.value}
                      onChange={(e) => handleParameterChange(param.key, e.target.checked)}
                      color="primary"
                    />
                  }
                  label={modelConfig.parameters[param.key] ?? param.value ? 'Açık' : 'Kapalı'}
                />
              )}

              {param.type === 'number' && (
                <TextField
                  type="number"
                  value={modelConfig.parameters[param.key] || param.value}
                  onChange={(e) => handleParameterChange(param.key, Number(e.target.value))}
                  inputProps={{
                    min: param.min,
                    max: param.max,
                    step: param.step || 1
                  }}
                  size="small"
                  fullWidth
                />
              )}

              <Alert severity="info" sx={{ mt: 2 }}>
                <Typography variant="caption">
                  <strong>💡 Öneri:</strong> {param.recommendation}
                </Typography>
              </Alert>
            </Card>
          ))}
        </Box>
      </Box>
    );
  };

  const renderTrainingProgress = () => (
    <Box>
      <Typography variant="h5" gutterBottom sx={{ fontWeight: 'bold', mb: 3 }}>
        🏃‍♂️ Model Eğitimi
      </Typography>

      {!isTraining && !trainingResult && (
        <Box>
          <Alert severity="info" sx={{ mb: 3 }}>
            <Typography variant="body1" sx={{ fontWeight: 'medium', mb: 1 }}>
              Eğitim Öncesi Kontrol
            </Typography>
            <Typography variant="body2">
              Model: <strong>{modelConfig.name}</strong><br/>
              Tip: <strong>{MODEL_TYPES[selectedModelType as keyof typeof MODEL_TYPES]?.title}</strong><br/>
              Tahmini Süre: <strong>{modelConfig.estimatedTime}</strong>
            </Typography>
          </Alert>

          {/* AutoEncoder için özel uyarı */}
          {modelConfig.type === 'AutoEncoder' && (
            <Alert severity="warning" sx={{ mb: 3 }}>
              <Typography variant="body1" sx={{ fontWeight: 'bold', mb: 1 }}>
                ⚠️ AutoEncoder Eğitim Uyarısı
              </Typography>
              <Typography variant="body2" sx={{ mb: 1 }}>
                Neural network eğitimi <strong>5-15 dakika</strong> sürebilir. Eğitim sırasında:
              </Typography>
              <Typography component="ul" variant="body2" sx={{ ml: 2 }}>
                <li>Sayfayı kapatmayın</li>
                <li>Tarayıcıyı minimize etmeyin</li>
                <li>Eğer timeout alırsanız epoch sayısını azaltın ({modelConfig.parameters.epochs || 50} → 20-30)</li>
                <li>Veya hidden layer boyutlarını küçültün</li>
              </Typography>
            </Alert>
          )}

          <Paper sx={{ p: 3, mb: 3, bgcolor: '#f8f9fa' }}>
            <Typography variant="h6" gutterBottom>📋 Parametre Özeti</Typography>
            <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 2 }}>
              {Object.entries(modelConfig.parameters).map(([key, value]) => (
                <Typography key={key} variant="body2">
                  <strong>{key}:</strong> {typeof value === 'object' ? JSON.stringify(value) : String(value)}
                </Typography>
              ))}
            </Box>
            
            {/* AutoEncoder için performans önerileri */}
            {modelConfig.type === 'AutoEncoder' && (
              <Alert severity="info" sx={{ mt: 2 }}>
                <Typography variant="caption">
                  <strong>💡 Hızlı Eğitim İçin:</strong> Epochs: 20-30, Hidden Layers: [32,16,8], Batch Size: 64
                </Typography>
              </Alert>
            )}
          </Paper>

          <Button
            variant="contained"
            size="large"
            onClick={handleStartTraining}
            startIcon={<StartIcon />}
            sx={{ minWidth: 200 }}
          >
            🚀 Eğitimi Başlat
          </Button>
        </Box>
      )}

      {isTraining && (
        <Box>
          <Paper sx={{ p: 3, mb: 3 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
              <CircularProgress size={24} sx={{ mr: 2 }} />
              <Typography variant="h6">Model Eğitiliyor...</Typography>
            </Box>
            
            <LinearProgress 
              variant="determinate" 
              value={trainingProgress} 
              sx={{ mb: 2, height: 8, borderRadius: 4 }}
            />
            
            <Typography variant="body2" color="textSecondary">
              {trainingStep} ({Math.round(trainingProgress)}% tamamlandı)
            </Typography>
          </Paper>

          <Alert severity="warning">
            <Typography variant="body2">
              ⏳ Eğitim devam ediyor. Lütfen sayfayı kapatmayın. 
              Bu işlem {modelConfig.estimatedTime} sürebilir.
            </Typography>
          </Alert>
        </Box>
      )}

      {trainingResult && (
        <Box>
          <Alert severity={trainingResult.error ? "error" : "success"} sx={{ mb: 3 }}>
            <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 1 }}>
              {trainingResult.error ? '❌ Eğitim Hatası!' : '🎉 Eğitim Başarıyla Tamamlandı!'}
            </Typography>
            <Typography variant="body2">
              {trainingResult.error ? 
                `Hata: ${trainingResult.error}` :
                `Model ${trainingResult.actualModelName || modelConfig.name} başarıyla eğitildi ve sisteme kaydedildi.`
              }
            </Typography>
          </Alert>

          {!trainingResult.error && trainingResult.BasicMetrics && (
            <Paper sx={{ p: 3, mb: 3 }}>
              <Typography variant="h6" gutterBottom>📊 Model Performansı</Typography>
              
              {/* Temel Metrikler */}
              <Box sx={{ mb: 3 }}>
                <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 'bold' }}>
                  🎯 Temel Performans Metrikleri
                </Typography>
                <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: 2 }}>
                  {Object.entries(trainingResult.BasicMetrics).filter(([key]) => 
                    ['Accuracy', 'Precision', 'Recall', 'F1Score', 'AUC', 'AUCPR'].includes(key)
                  ).map(([key, value]) => {
                    if (value === undefined || value === null || typeof value !== 'number') return null;
                    
                    // Format the display name
                    let displayName = key;
                    switch(key) {
                      case 'Accuracy': displayName = 'Doğruluk'; break;
                      case 'Precision': displayName = 'Kesinlik'; break;
                      case 'Recall': displayName = 'Geri Çağırma'; break;
                      case 'F1Score': displayName = 'F1 Skor'; break;
                      case 'AUC': displayName = 'AUC Skoru'; break;
                      case 'AUCPR': displayName = 'AUC-PR'; break;
                    }
                    
                    // Format the value
                    let displayValue = `${(value * 100).toFixed(2)}%`;
                    
                    // Color coding based on value
                    let color = value > 0.9 ? 'success' : value > 0.7 ? 'warning' : 'error';

                    return (
                      <Card key={key} variant="outlined" sx={{ textAlign: 'center', p: 2 }}>
                        <Typography variant="caption" color="textSecondary">
                          {displayName}
                        </Typography>
                        <Typography variant="h6" color={`${color}.main`}>
                          {displayValue}
                        </Typography>
                      </Card>
                    );
                  })}
                </Box>
              </Box>

              {/* Confusion Matrix */}
              {trainingResult.BasicMetrics.TruePositive !== undefined && (
                <Box sx={{ mb: 3 }}>
                  <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 'bold' }}>
                    🔍 Confusion Matrix
                  </Typography>
                  <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 2, maxWidth: 400 }}>
                    <Card variant="outlined" sx={{ textAlign: 'center', p: 2, bgcolor: 'success.light' }}>
                      <Typography variant="caption" color="success.dark">True Positive</Typography>
                      <Typography variant="h5" color="success.dark">
                        {trainingResult.BasicMetrics.TruePositive}
                      </Typography>
                      <Typography variant="caption">Doğru Fraud Tespiti</Typography>
                    </Card>
                    <Card variant="outlined" sx={{ textAlign: 'center', p: 2, bgcolor: 'error.light' }}>
                      <Typography variant="caption" color="error.dark">False Positive</Typography>
                      <Typography variant="h5" color="error.dark">
                        {trainingResult.BasicMetrics.FalsePositive}
                      </Typography>
                      <Typography variant="caption">Yanlış Fraud Alarmı</Typography>
                    </Card>
                    <Card variant="outlined" sx={{ textAlign: 'center', p: 2, bgcolor: 'warning.light' }}>
                      <Typography variant="caption" color="warning.dark">False Negative</Typography>
                      <Typography variant="h5" color="warning.dark">
                        {trainingResult.BasicMetrics.FalseNegative}
                      </Typography>
                      <Typography variant="caption">Kaçırılan Fraud</Typography>
                    </Card>
                    <Card variant="outlined" sx={{ textAlign: 'center', p: 2, bgcolor: 'success.light' }}>
                      <Typography variant="caption" color="success.dark">True Negative</Typography>
                      <Typography variant="h5" color="success.dark">
                        {trainingResult.BasicMetrics.TrueNegative}
                      </Typography>
                      <Typography variant="caption">Doğru Normal Tespit</Typography>
                    </Card>
                  </Box>
                </Box>
              )}

              {/* İleri Metrikler */}
              {trainingResult.BasicMetrics.Specificity && (
                <Box sx={{ mb: 3 }}>
                  <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 'bold' }}>
                    📈 İleri Seviye Metrikler
                  </Typography>
                  <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 2 }}>
                    {Object.entries(trainingResult.BasicMetrics).filter(([key]) => 
                      ['Specificity', 'Sensitivity', 'BalancedAccuracy', 'MatthewsCorrCoef'].includes(key)
                    ).map(([key, value]) => {
                      if (value === undefined || value === null || typeof value !== 'number') return null;
                      
                      let displayName = key;
                      switch(key) {
                        case 'Specificity': displayName = 'Özgüllük (Specificity)'; break;
                        case 'Sensitivity': displayName = 'Hassaslık (Sensitivity)'; break;
                        case 'BalancedAccuracy': displayName = 'Dengeli Doğruluk'; break;
                        case 'MatthewsCorrCoef': displayName = 'Matthews Korelasyon'; break;
                      }
                      
                      let displayValue = `${(value * 100).toFixed(2)}%`;
                      let color = value > 0.8 ? 'success' : value > 0.6 ? 'warning' : 'error';

                      return (
                        <Card key={key} variant="outlined" sx={{ textAlign: 'center', p: 2 }}>
                          <Typography variant="caption" color="textSecondary">
                            {displayName}
                          </Typography>
                          <Typography variant="h6" color={`${color}.main`}>
                            {displayValue}
                          </Typography>
                        </Card>
                      );
                    })}
                  </Box>
                </Box>
              )}

              {/* Performance Summary */}
              {trainingResult.BasicMetrics.OverallScore && (
                <Alert 
                  severity={trainingResult.BasicMetrics.ModelGrade?.includes('A') ? 'success' : 'info'} 
                  sx={{ mb: 2 }}
                >
                  <Typography variant="body2">
                    <strong>🏆 Model Değerlendirmesi:</strong> {trainingResult.BasicMetrics.ModelGrade} 
                    (Genel Skor: {(trainingResult.BasicMetrics.OverallScore * 100).toFixed(1)}%)
                  </Typography>
                  <Typography variant="body2">
                    <strong>📋 Ana Zayıflık:</strong> {trainingResult.BasicMetrics.PrimaryWeakness}
                  </Typography>
                </Alert>
              )}

              {/* Özel Model Türü Bilgileri */}
              {(modelConfig.type === 'PCA' || modelConfig.type === 'IsolationForest') && (
                <Alert severity="info" sx={{ mt: 2 }}>
                  <Typography variant="body2">
                    <strong>💡 {modelConfig.type} Özel Notlar:</strong> Bu model anomali tespit algoritmasıdır. 
                    Yüksek recall değeri fraud tespitinde önemlidir, precision düşük olabilir.
                  </Typography>
                </Alert>
              )}
              
              {modelConfig.type === 'AutoEncoder' && (
                <Alert severity="info" sx={{ mt: 2 }}>
                  <Typography variant="body2">
                    <strong>💡 AutoEncoder Özel Notlar:</strong> Düşük reconstruction error değeri iyi performans gösterir. 
                    Training/Validation loss dengesine dikkat edin.
                  </Typography>
                </Alert>
              )}

              {modelConfig.type === 'LightGBM' && trainingResult.BasicMetrics.F1Score && (
                <Alert severity={trainingResult.BasicMetrics.F1Score > 0.8 ? 'success' : 'warning'} sx={{ mt: 2 }}>
                  <Typography variant="body2">
                    <strong>🚀 LightGBM Performans:</strong> F1-Score {(trainingResult.BasicMetrics.F1Score * 100).toFixed(2)}% 
                    {trainingResult.BasicMetrics.F1Score > 0.8 ? 
                      '- Mükemmel performans! Production için hazır.' : 
                      '- İyi performans, parametre optimizasyonu yapılabilir.'
                    }
                  </Typography>
                </Alert>
              )}
            </Paper>
          )}

          <Box sx={{ display: 'flex', gap: 2 }}>
            <Button variant="contained" onClick={handleReset} startIcon={<TrainIcon />}>
              Yeni Model Eğit
            </Button>
            <Button variant="outlined" onClick={() => setActiveStep(0)}>
              Başa Dön
            </Button>
          </Box>
        </Box>
      )}
    </Box>
  );

  const renderResults = () => (
    <Box>
      <Typography variant="h5" gutterBottom sx={{ fontWeight: 'bold', mb: 3 }}>
        🎉 Eğitim Sonuçları
      </Typography>

      {trainingResult && !trainingResult.error && (
        <Box>
          <Alert severity="success" sx={{ mb: 3 }}>
            <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 1 }}>
              ✅ Model Başarıyla Eğitildi!
            </Typography>
            <Typography variant="body2">
              Model <strong>{trainingResult.actualModelName || modelConfig.name}</strong> başarıyla eğitildi ve sisteme kaydedildi.
              <br />Model Tipi: <strong>{MODEL_TYPES[selectedModelType as keyof typeof MODEL_TYPES]?.title}</strong>
            </Typography>
          </Alert>

          {trainingResult.BasicMetrics && (
            <Paper sx={{ p: 3, mb: 3 }}>
              <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                📊 Detaylı Performans Analizi
              </Typography>
              
              {/* Ana Performans Metrikleri */}
              <Box sx={{ mb: 4 }}>
                <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 'bold', color: 'primary.main' }}>
                  🎯 Ana Performans Metrikleri
                </Typography>
                <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 2 }}>
                  {Object.entries(trainingResult.BasicMetrics).filter(([key]) => 
                    ['Accuracy', 'Precision', 'Recall', 'F1Score', 'AUC', 'AUCPR'].includes(key)
                  ).map(([key, value]) => {
                    if (value === undefined || value === null || typeof value !== 'number') return null;
                    
                    let displayName = key;
                    let icon = '';
                    switch(key) {
                      case 'Accuracy': displayName = 'Doğruluk'; icon = '🎯'; break;
                      case 'Precision': displayName = 'Kesinlik'; icon = '🔍'; break;
                      case 'Recall': displayName = 'Geri Çağırma'; icon = '🔄'; break;
                      case 'F1Score': displayName = 'F1 Skor'; icon = '⚖️'; break;
                      case 'AUC': displayName = 'AUC Skoru'; icon = '📈'; break;
                      case 'AUCPR': displayName = 'AUC-PR'; icon = '📊'; break;
                    }
                    
                    let displayValue = `${(value * 100).toFixed(2)}%`;
                    let color = value > 0.9 ? 'success' : value > 0.7 ? 'warning' : 'error';
                    let bgColor = value > 0.9 ? 'success.light' : value > 0.7 ? 'warning.light' : 'error.light';

  return (
                      <Card key={key} variant="outlined" sx={{ textAlign: 'center', p: 2, bgcolor: bgColor }}>
                        <Typography variant="caption" color="textSecondary" sx={{ display: 'block', mb: 0.5 }}>
                          {icon} {displayName}
                        </Typography>
                        <Typography variant="h5" color={`${color}.main`} sx={{ fontWeight: 'bold' }}>
                          {displayValue}
                        </Typography>
                      </Card>
                    );
                  })}
                </Box>
              </Box>

              {/* Confusion Matrix - Detaylı */}
              {trainingResult.BasicMetrics.TruePositive !== undefined && (
                <Box sx={{ mb: 4 }}>
                  <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 'bold', color: 'primary.main' }}>
                    🔍 Confusion Matrix Analizi
                  </Typography>
                  <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 3, maxWidth: 600, mx: 'auto' }}>
                    <Card variant="outlined" sx={{ textAlign: 'center', p: 3, bgcolor: 'success.light' }}>
                      <Typography variant="h6" color="success.dark" sx={{ fontWeight: 'bold' }}>
                        ✅ True Positive
                      </Typography>
                      <Typography variant="h4" color="success.dark" sx={{ my: 1 }}>
                        {trainingResult.BasicMetrics.TruePositive}
                      </Typography>
                      <Typography variant="body2" color="success.dark">
                        Doğru Fraud Tespiti
                      </Typography>
                    </Card>
                    <Card variant="outlined" sx={{ textAlign: 'center', p: 3, bgcolor: 'error.light' }}>
                      <Typography variant="h6" color="error.dark" sx={{ fontWeight: 'bold' }}>
                        ❌ False Positive
                      </Typography>
                      <Typography variant="h4" color="error.dark" sx={{ my: 1 }}>
                        {trainingResult.BasicMetrics.FalsePositive}
                      </Typography>
                      <Typography variant="body2" color="error.dark">
                        Yanlış Fraud Alarmı
                      </Typography>
                    </Card>
                    <Card variant="outlined" sx={{ textAlign: 'center', p: 3, bgcolor: 'warning.light' }}>
                      <Typography variant="h6" color="warning.dark" sx={{ fontWeight: 'bold' }}>
                        ⚠️ False Negative
                      </Typography>
                      <Typography variant="h4" color="warning.dark" sx={{ my: 1 }}>
                        {trainingResult.BasicMetrics.FalseNegative}
                      </Typography>
                      <Typography variant="body2" color="warning.dark">
                        Kaçırılan Fraud
                      </Typography>
                    </Card>
                    <Card variant="outlined" sx={{ textAlign: 'center', p: 3, bgcolor: 'success.light' }}>
                      <Typography variant="h6" color="success.dark" sx={{ fontWeight: 'bold' }}>
                        ✅ True Negative
                      </Typography>
                      <Typography variant="h4" color="success.dark" sx={{ my: 1 }}>
                        {trainingResult.BasicMetrics.TrueNegative}
                      </Typography>
                      <Typography variant="body2" color="success.dark">
                        Doğru Normal Tespit
                      </Typography>
                    </Card>
                  </Box>
                </Box>
              )}

              {/* İleri Seviye Metrikler */}
              {(trainingResult.BasicMetrics.Specificity || trainingResult.BasicMetrics.Sensitivity) && (
                <Box sx={{ mb: 4 }}>
                  <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 'bold', color: 'primary.main' }}>
                    📈 İleri Seviye Metrikler
                  </Typography>
                  <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 2 }}>
                    {Object.entries(trainingResult.BasicMetrics).filter(([key]) => 
                      ['Specificity', 'Sensitivity', 'BalancedAccuracy', 'MatthewsCorrCoef'].includes(key)
                    ).map(([key, value]) => {
                      if (value === undefined || value === null || typeof value !== 'number') return null;
                      
                      let displayName = key;
                      let description = '';
                      switch(key) {
                        case 'Specificity': 
                          displayName = 'Özgüllük (Specificity)'; 
                          description = 'Normal işlemleri doğru tanıma oranı';
                          break;
                        case 'Sensitivity': 
                          displayName = 'Hassaslık (Sensitivity)'; 
                          description = 'Fraud işlemleri yakalama oranı';
                          break;
                        case 'BalancedAccuracy': 
                          displayName = 'Dengeli Doğruluk'; 
                          description = 'Sınıf dengesizliğini hesaba katan doğruluk';
                          break;
                        case 'MatthewsCorrCoef': 
                          displayName = 'Matthews Korelasyon'; 
                          description = 'Genel model kalitesi (-1 ile +1 arası)';
                          break;
                      }
                      
                      let displayValue = key === 'MatthewsCorrCoef' ? 
                        value.toFixed(3) : `${(value * 100).toFixed(2)}%`;
                      let color = value > 0.8 ? 'success' : value > 0.6 ? 'warning' : 'error';

                      return (
                        <Card key={key} variant="outlined" sx={{ p: 2 }}>
                          <Typography variant="subtitle2" color="textSecondary" sx={{ fontWeight: 'bold' }}>
                            {displayName}
                          </Typography>
                          <Typography variant="h5" color={`${color}.main`} sx={{ my: 1 }}>
                            {displayValue}
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            {description}
                          </Typography>
                        </Card>
                      );
                    })}
                  </Box>
                </Box>
              )}

              {/* Model Değerlendirme ve Öneriler */}
              <Box sx={{ mb: 3 }}>
                <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 'bold', color: 'primary.main' }}>
                  🏆 Model Değerlendirmesi ve Öneriler
                </Typography>
                
                {trainingResult.BasicMetrics.OverallScore && (
                  <Alert 
                    severity={trainingResult.BasicMetrics.ModelGrade?.includes('A') ? 'success' : 'info'} 
                    sx={{ mb: 2 }}
                  >
                    <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 1 }}>
                      📋 Model Notu: {trainingResult.BasicMetrics.ModelGrade}
                    </Typography>
                    <Typography variant="body1" sx={{ mb: 1 }}>
                      <strong>Genel Performans Skoru:</strong> {(trainingResult.BasicMetrics.OverallScore * 100).toFixed(1)}%
                    </Typography>
                    {trainingResult.BasicMetrics.PrimaryWeakness && (
                      <Typography variant="body2">
                        <strong>🔍 Ana Zayıflık:</strong> {trainingResult.BasicMetrics.PrimaryWeakness}
                      </Typography>
                    )}
                  </Alert>
                )}

                {/* Model Tipine Özel Öneriler */}
                {modelConfig.type === 'LightGBM' && trainingResult.BasicMetrics.F1Score && (
                  <Alert severity={trainingResult.BasicMetrics.F1Score > 0.8 ? 'success' : 'warning'} sx={{ mb: 2 }}>
                    <Typography variant="body1" sx={{ fontWeight: 'bold', mb: 1 }}>
                      🚀 LightGBM Performans Analizi
                    </Typography>
                    <Typography variant="body2">
                      F1-Score: <strong>{(trainingResult.BasicMetrics.F1Score * 100).toFixed(2)}%</strong>
                    </Typography>
                    <Typography variant="body2">
                      {trainingResult.BasicMetrics.F1Score > 0.8 ? 
                        '✅ Mükemmel performans! Model production ortamı için hazır.' : 
                        '⚠️ İyi performans. Hiperparametre optimizasyonu ile daha da geliştirilebilir.'
                      }
                    </Typography>
                  </Alert>
                )}

                {(modelConfig.type === 'PCA' || modelConfig.type === 'IsolationForest') && (
                  <Alert severity="info" sx={{ mb: 2 }}>
                    <Typography variant="body1" sx={{ fontWeight: 'bold', mb: 1 }}>
                      💡 {modelConfig.type} Özel Notlar
                    </Typography>
                    <Typography variant="body2">
                      Bu model anomali tespit algoritmasıdır. Yüksek recall değeri fraud tespitinde önemlidir, 
                      precision'ın düşük olması normal kabul edilebilir. Threshold değeri ayarlanarak dengeli hale getirilebilir.
                    </Typography>
                  </Alert>
                )}
                
                {modelConfig.type === 'AutoEncoder' && (
                  <Alert severity="info" sx={{ mb: 2 }}>
                    <Typography variant="body1" sx={{ fontWeight: 'bold', mb: 1 }}>
                      🧠 AutoEncoder Performans Notları
                    </Typography>
                    <Typography variant="body2">
                      Neural network tabanlı bu model karmaşık pattern'ları yakalayabilir. 
                      Reconstruction error'a dikkat edin. Overfitting belirtisi için training/validation loss'u karşılaştırın.
                    </Typography>
                  </Alert>
                )}

                {modelConfig.type === 'Ensemble' && (
                  <Alert severity="success" sx={{ mb: 2 }}>
                    <Typography variant="body1" sx={{ fontWeight: 'bold', mb: 1 }}>
                      🎯 Ensemble Model Avantajları
                    </Typography>
                    <Typography variant="body2">
                      Bu hibrit model LightGBM ve PCA'nın güçlü yanlarını birleştirir. 
                      Genellikle tek modellerden daha iyi performans gösterir ve production ortamı için idealdir.
                    </Typography>
                  </Alert>
                )}
              </Box>
            </Paper>
          )}

          {/* Action Buttons */}
          <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center', mt: 4 }}>
            <Button 
              variant="contained" 
              onClick={handleReset} 
              startIcon={<TrainIcon />}
              size="large"
              color="success"
            >
              🚀 Yeni Model Eğit
            </Button>
            <Button 
              variant="outlined" 
              onClick={() => setActiveStep(0)}
              size="large"
            >
              📋 Model Seçimine Dön
            </Button>
            <Button 
              variant="outlined" 
              onClick={() => setActiveStep(1)}
              size="large"
            >
              ⚙️ Parametreleri Değiştir
            </Button>
          </Box>
        </Box>
      )}

      {trainingResult && trainingResult.error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 1 }}>
            ❌ Eğitim Hatası
          </Typography>
          <Typography variant="body2">
            <strong>Hata Detayı:</strong> {trainingResult.error}
          </Typography>
          <Box sx={{ mt: 2 }}>
            <Button variant="contained" onClick={handleReset} startIcon={<TrainIcon />}>
              Tekrar Dene
            </Button>
          </Box>
        </Alert>
      )}
    </Box>
  );

  const steps = ['Model Seçimi', 'Konfigürasyon', 'Eğitim', 'Sonuçlar'];

  return (
    <Box sx={{ width: '100%', maxWidth: '100%' }}>
      {/* Header */}
      <Box sx={{ mb: 4 }}>
        <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 1 }}>
          🧠 Akıllı Model Eğitimi
        </Typography>
        <Typography variant="body1" color="textSecondary">
          Fraud detection için optimize edilmiş machine learning modelleri eğitin
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
          {activeStep === 2 && renderTrainingProgress()}
          {activeStep === 3 && renderResults()}
        </CardContent>
      </Card>

      {/* Navigation */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 3 }}>
        <Button
          disabled={activeStep === 0}
          onClick={handleBack}
          startIcon={<InfoIcon />}
        >
          Geri
        </Button>
        
        <Box>
          {activeStep < steps.length - 1 && activeStep !== 2 && (
          <Button
            variant="contained"
            onClick={handleNext}
            disabled={activeStep === 0 && !selectedModelType}
            endIcon={<InfoIcon />}
          >
            İleri
          </Button>
        )}

          {activeStep === 3 && (
            <Button
              variant="contained"
              onClick={handleReset}
              startIcon={<TrainIcon />}
              color="success"
            >
              Yeni Model Eğit
          </Button>
        )}
        </Box>
      </Box>
    </Box>
  );
};

export default ModelTrainingWizard; 