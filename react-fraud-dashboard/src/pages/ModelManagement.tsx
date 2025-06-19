import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  Button,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  LinearProgress,
  Alert,
  Tabs,
  Tab,
  IconButton,
  Tooltip,
  Snackbar,
  Paper,
  TablePagination,
  Collapse,
  Grid,
  Autocomplete,
  Badge,
  FormControlLabel,
  Switch,
  Divider,
  AlertColor,
} from '@mui/material';
import {
  ModelTraining as ModelIcon,
  Add as AddIcon,
  Publish as DeployIcon,
  Archive as ArchiveIcon,
  PlayArrow as TrainIcon,
  Visibility as ViewIcon,
  Download as DownloadIcon,
  Settings as SettingsIcon,
  ExpandMore as ExpandMoreIcon,
  TrendingUp as MetricsIcon,
  Timeline as TrendIcon,
  Assessment as AnalysisIcon,
  FilterList as FilterIcon,
  CompareArrows as CompareIcon,
  Clear as ClearIcon,
  Refresh as RefreshIcon,
  CheckCircle as SuccessIcon,
  Warning as WarningIcon,
  Error as ErrorIcon,
  Info as InfoIcon,
  Tune as TuneIcon,
  Stop as StopIcon,
  Pause as PauseIcon,
} from '@mui/icons-material';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
  RadarChart,
  PolarGrid,
  PolarAngleAxis,
  PolarRadiusAxis,
  Radar,
  PieChart,
  Pie,
  Cell,
  LineChart,
  Line,
  ScatterChart,
  Scatter,
  ZAxis,
  Legend,
  Area,
  AreaChart,
} from 'recharts';
import { FraudDetectionAPI, ModelStatus, ModelType } from '../services/api';
import ModelTrainingWizard from '../components/ModelTrainingWizard';
import HyperparameterTuning from './HyperparameterTuning';
import type {
  ModelMetadata,
  ModelMetrics,
  TrainingResult,
  LightGBMConfig,
  PCAConfig,
  EnsembleConfig
} from '../services/api';

// ==================== INTERFACES & TYPES ====================
interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

interface SnackbarState {
  open: boolean;
  message: string;
  severity: 'success' | 'error' | 'warning' | 'info';
}

interface ComparisonFilters {
  minAccuracy: number;
  maxAccuracy: number;
  modelTypes: string[];
  statusTypes: string[];
  dateRange: { start: string; end: string };
}

interface ModelConfig {
  name: string;
  type: 'LightGBM' | 'PCA' | 'Ensemble';
  description: string;
  config: LightGBMConfig | PCAConfig | EnsembleConfig;
}

// ==================== CONSTANTS ====================
const MODEL_TYPE_COLORS = {
  Ensemble: '#1976d2',
  LightGBM: '#ff9800',
  PCA: '#4caf50',
  AttentionModel: '#9c27b0',
  AutoEncoder: '#f44336',
  IsolationForest: '#00bcd4',
};

const METRIC_COLORS = {
  accuracy: '#1976d2',
  precision: '#ff9800',
  recall: '#4caf50',
  f1Score: '#e91e63',
  f1_score: '#e91e63',
  auc: '#9c27b0',
};

const STATUS_CONFIG = {
  Active: { color: 'success', label: 'Aktif', icon: '🟢' },
  Inactive: { color: 'default', label: 'Pasif', icon: '⚫' },
  Training: { color: 'info', label: 'Eğitiliyor', icon: '🔵' },
  Failed: { color: 'error', label: 'Başarısız', icon: '🔴' },
};

const MODEL_TYPE_CONFIG = {
  LightGBM: { label: 'LightGBM', icon: '⚡' },
  PCA: { label: 'PCA', icon: '📊' },
  Ensemble: { label: 'Ensemble', icon: '🎯' },
  AttentionModel: { label: 'Attention', icon: '🧠' },
  AutoEncoder: { label: 'AutoEncoder', icon: '🔄' },
  IsolationForest: { label: 'Isolation Forest', icon: '🌲' },
};

// ==================== HELPER COMPONENTS ====================
function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;
  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`tabpanel-${index}`}
      aria-labelledby={`tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

// ==================== MAIN COMPONENT ====================
const ModelManagement: React.FC = () => {
  // ========== STATE MANAGEMENT ==========
  // Tab Navigation
  const [activeTab, setActiveTab] = useState(0);

  // Data States
  const [models, setModels] = useState<ModelMetadata[]>([]);
  const [isTraining, setTraining] = useState(false);
  const [selectedModel, setSelectedModel] = useState<ModelMetadata | null>(null);
  const [showDetailModal, setShowDetailModal] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [sortBy, setSortBy] = useState('name');
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [performanceTimeRange, setPerformanceTimeRange] = useState('30d');
  const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'info' as AlertColor });
  const [newModel, setNewModel] = useState({
    name: '',
    type: 'LightGBM' as const,
    description: '',
    config: { numLeaves: 31, learningRate: 0.1 }, // Fixed config type
  });

  // Dialog States
  const [detailDialog, setDetailDialog] = useState(false);
  const [trainingDialog, setTrainingDialog] = useState(false);
  const [trainingResultDialog, setTrainingResultDialog] = useState(false);
  const [trainingResult, setTrainingResult] = useState<any>(null);

  // List View States
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);

  // Comparison View States
  const [selectedModelsForComparison, setSelectedModelsForComparison] = useState<string[]>([]);
  const [comparisonMetric, setComparisonMetric] = useState<'accuracy' | 'precision' | 'recall' | 'f1_score' | 'auc'>('accuracy');
  const [comparisonView, setComparisonView] = useState<'bar' | 'radar' | 'line' | 'heatmap' | 'scatter'>('bar');
  const [showAdvancedComparison, setShowAdvancedComparison] = useState(false);
  const [comparisonFilters, setComparisonFilters] = useState<ComparisonFilters>({
    minAccuracy: 0,
    maxAccuracy: 100,
    modelTypes: [],
    statusTypes: [],
    dateRange: { start: '', end: '' }
  });

  // Training Config
  const [newModelConfig, setNewModelConfig] = useState<ModelConfig>({
    name: '',
    type: 'LightGBM',
    description: '',
    config: { numLeaves: 31, learningRate: 0.1 } as LightGBMConfig,
  });

  // Added missing state
  const [showFilters, setShowFilters] = useState(false);

  // ========== FALLBACK DATA ==========
  const fallbackModels: ModelMetadata[] = [
    {
      id: "model-1",
      ModelName: "Ensemble Model", // Backend property isimleri
      Version: "1.0.0",
      Type: ModelType.Ensemble,
      Status: ModelStatus.Active,
      MetricsJson: JSON.stringify({
        auc: 0.96,
        recall: 0.78,
        accuracy: 0.94,
        f1_score: 0.85,
        precision: 0.90
      }),
      Metrics: {
        auc: 0.96,
        recall: 0.78,
        accuracy: 0.94,
        f1_score: 0.85,
        precision: 0.90
      },
      TrainedAt: new Date().toISOString(),
      CreatedAt: new Date().toISOString(),
      CreatedBy: "System",
      LastModifiedBy: "System",
      Configuration: JSON.stringify({ ensemble: true })
    },
    {
      id: "model-2",
      ModelName: "LightGBM Model v2",
      Version: "2.1.0",
      Type: ModelType.LightGBM,
      Status: ModelStatus.Active,
      MetricsJson: JSON.stringify({
        auc: 0.92,
        recall: 0.81,
        accuracy: 0.91,
        f1_score: 0.83,
        precision: 0.88
      }),
      Metrics: {
        auc: 0.92,
        recall: 0.81,
        accuracy: 0.91,
        f1_score: 0.83,
        precision: 0.88
      },
      TrainedAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(), // 7 gün önce
      CreatedAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString(),
      CreatedBy: "System",
      LastModifiedBy: "System",
      Configuration: JSON.stringify({ lightgbm: { numLeaves: 31, learningRate: 0.1 } })
    },
    {
      id: "model-3",
      ModelName: "PCA Anomaly Detector",
      Version: "1.5.0",
      Type: ModelType.PCA,
      Status: ModelStatus.Training,
      MetricsJson: JSON.stringify({
        auc: 0.87,
        recall: 0.75,
        accuracy: 0.89,
        f1_score: 0.78,
        precision: 0.85
      }),
      Metrics: {
        auc: 0.87,
        recall: 0.75,
        accuracy: 0.89,
        f1_score: 0.78,
        precision: 0.85
      },
      TrainedAt: new Date(Date.now() - 14 * 24 * 60 * 60 * 1000).toISOString(), // 14 gün önce
      CreatedAt: new Date(Date.now() - 14 * 24 * 60 * 60 * 1000).toISOString(),
      CreatedBy: "Data Scientist",
      LastModifiedBy: "System",
      Configuration: JSON.stringify({ pca: { componentCount: 10, threshold: 0.95 } })
    },
    {
      id: "model-4",
      ModelName: "Legacy Ensemble",
      Version: "1.0.0",
      Type: ModelType.Ensemble,
      Status: ModelStatus.Inactive,
      MetricsJson: JSON.stringify({
        auc: 0.84,
        recall: 0.72,
        accuracy: 0.86,
        f1_score: 0.74,
        precision: 0.82
      }),
      Metrics: {
        auc: 0.84,
        recall: 0.72,
        accuracy: 0.86,
        f1_score: 0.74,
        precision: 0.82
      },
      TrainedAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(), // 30 gün önce
      CreatedAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
      CreatedBy: "ML Engineer",
      LastModifiedBy: "System",
      Configuration: JSON.stringify({ ensemble: { version: "legacy" } })
    }
  ];

  // ========== DATA LOADING ==========
  useEffect(() => {
    loadModels();
  }, []);

  const loadModels = async () => {
    try {
      setTraining(true);
      console.log('🔄 Model verileri yükleniyor...');

      const response = await FraudDetectionAPI.getAllModels();
      console.log('✅ Model verileri alındı:', response.length, 'model');

      if (response && response.length > 0) {
        setModels(response);
        showSnackbar(`✅ ${response.length} model başarıyla yüklendi`, 'success');
      } else {
        console.warn('⚠️ Model verisi bulunamadı, demo veriler yükleniyor');
        setModels(fallbackModels);
        showSnackbar('⚠️ Sistemde model bulunamadı, demo veriler yüklendi', 'warning');
      }

    } catch (err) {
      console.error('❌ Model yükleme hatası:', err);
      setModels(fallbackModels);
      showSnackbar('❌ API bağlantısı başarısız, demo veriler yüklendi', 'error');
    } finally {
      setTraining(false);
    }
  };

  const reloadModels = async () => {
    await loadModels();
  };

  // ========== UTILITY FUNCTIONS ==========
  const showSnackbar = (message: string, severity: SnackbarState['severity']) => {
    setSnackbar({ open: true, message, severity });
  };

  const getStatusConfig = (status: string) => STATUS_CONFIG[status as keyof typeof STATUS_CONFIG] || STATUS_CONFIG.Inactive;
  const getModelTypeConfig = (type: string) => MODEL_TYPE_CONFIG[type as keyof typeof MODEL_TYPE_CONFIG] || { label: type, icon: '📄' };

  // Model kartlarında güvenli property erişimi için helper function'lar
  const getModelProperty = (model: any, propName: string) => {
    // Null/undefined check
    if (!model) {
      return '';
    }

    // camelCase önce, sonra PascalCase dene
    const camelCase = propName.charAt(0).toLowerCase() + propName.slice(1);
    const pascalCase = propName.charAt(0).toUpperCase() + propName.slice(1);
    return model[camelCase] || model[pascalCase] || '';
  };

  const getModelMetrics = (model: any) => {
    // Null/undefined check
    if (!model) {
      return {};
    }

    // Önce direkt metrics alanını kontrol et
    if (model.metrics || model.Metrics) {
      return model.metrics || model.Metrics;
    }

    // Sonra metricsJson alanını parse etmeye çalış
    const metricsJson = model.metricsJson || model.MetricsJson;
    if (metricsJson && typeof metricsJson === 'string') {
      try {
        return JSON.parse(metricsJson);
      } catch (e) {
        console.warn('MetricsJson parse hatası:', e);
      }
    }

    // Son olarak boş obje döndür
    return {};
  };

  // ========== DATA FUNCTIONS ==========
  const getAdvancedPerformanceData = useCallback(() => {
    return models.map(model => {
      const metrics = getModelMetrics(model);

      // Backend'den gelen yeni response yapısını destekle
      let accuracy = 0, precision = 0, recall = 0, f1_score = 0, auc = 0;

      // Önce basicMetrics içindeki değerleri kontrol et
      if ((model as any).data && (model as any).data.basicMetrics) {
        const basicMetrics = (model as any).data.basicMetrics;
        accuracy = basicMetrics.accuracy || 0;
        precision = basicMetrics.precision || 0;
        recall = basicMetrics.recall || 0;
        f1_score = basicMetrics.f1Score || basicMetrics.f1_score || 0;
        auc = basicMetrics.auc || 0;
      } else {
        // Eski yapıyı destekle
        accuracy = metrics?.accuracy || 0;
        precision = metrics?.precision || 0;
        recall = metrics?.recall || 0;
        f1_score = metrics?.f1_score || metrics?.f1Score || 0;
        auc = metrics?.auc || 0;
      }

      return {
        id: model.id,
        name: getModelProperty(model, 'modelName'),
        type: getModelProperty(model, 'type'),
        status: getModelProperty(model, 'status'),
        accuracy: accuracy * 100,
        precision: precision * 100,
        recall: recall * 100,
        f1_score: f1_score * 100,
        auc: auc * 100,
        trainedAt: getModelProperty(model, 'trainedAt'),
        // Ham değerleri de sakla (filtreleme için)
        rawAccuracy: accuracy,
        rawPrecision: precision,
        rawRecall: recall,
        rawF1Score: f1_score,
        rawAuc: auc
      };
    }).filter(model => {
      // 0, null, undefined değerleri olan modelleri filtrele
      const hasValidMetrics = (
        model.rawAccuracy > 0 &&
        model.rawPrecision > 0 &&
        model.rawRecall > 0 &&
        model.rawF1Score > 0 &&
        model.rawAuc > 0
      );

      // En az bir geçerli metriği olan modelleri göster
      const hasAnyValidMetric = (
        model.rawAccuracy > 0 ||
        model.rawPrecision > 0 ||
        model.rawRecall > 0 ||
        model.rawF1Score > 0 ||
        model.rawAuc > 0
      );

      return hasAnyValidMetric;
    });
  }, [models]);

  const getRadarData = useCallback(() => {
    const metrics = ['accuracy', 'precision', 'recall', 'f1_score', 'auc'];
    return metrics.map(metric => {
      const result: any = { metric: metric.toUpperCase() };

      if (selectedModelsForComparison.length > 0) {
        selectedModelsForComparison.forEach(modelId => {
          const model = models.find(m => m.id === modelId);
          if (model) {
            const modelMetrics = getModelMetrics(model);
            const modelName = getModelProperty(model, 'modelName');
            result[modelName] = (modelMetrics?.[metric as keyof ModelMetrics] as number || 0) * 100;
          }
        });
      } else {
        models.slice(0, 3).forEach(model => {
          const modelMetrics = getModelMetrics(model);
          const modelName = getModelProperty(model, 'modelName');
          result[modelName] = (modelMetrics?.[metric as keyof ModelMetrics] as number || 0) * 100;
        });
      }

      return result;
    });
  }, [models, selectedModelsForComparison]);

  const getHeatmapData = useCallback((filterModels = false) => {
    const dataSource = filterModels ? getAdvancedPerformanceData().filter(model => {
      const passesAccuracy = model.accuracy >= comparisonFilters.minAccuracy &&
        model.accuracy <= comparisonFilters.maxAccuracy;
      const passesType = comparisonFilters.modelTypes.length === 0 ||
        comparisonFilters.modelTypes.includes(model.type);
      const passesStatus = comparisonFilters.statusTypes.length === 0 ||
        comparisonFilters.statusTypes.includes(model.status);

      let passesDate = true;
      if (comparisonFilters.dateRange.start || comparisonFilters.dateRange.end) {
        const modelDate = new Date(model.trainedAt);
        if (comparisonFilters.dateRange.start) {
          passesDate = passesDate && modelDate >= new Date(comparisonFilters.dateRange.start);
        }
        if (comparisonFilters.dateRange.end) {
          passesDate = passesDate && modelDate <= new Date(comparisonFilters.dateRange.end);
        }
      }

      return passesAccuracy && passesType && passesStatus && passesDate;
    }) : getAdvancedPerformanceData(); // Sadece getAdvancedPerformanceData kullan, models değil

    return dataSource.map(model => ({
      model: model.name.length > 30 ? model.name.substring(0, 30) + '...' : model.name,
      fullName: model.name,
      accuracy: model.accuracy, // Zaten yüzde olarak geldi
      precision: model.precision,
      recall: model.recall,
      f1_score: model.f1_score,
      auc: model.auc
    }));
  }, [getAdvancedPerformanceData, comparisonFilters]);

  const getScatterData = useCallback((filterModels = false) => {
    const dataSource = filterModels ? getAdvancedPerformanceData().filter(model => {
      const passesAccuracy = model.accuracy >= comparisonFilters.minAccuracy &&
        model.accuracy <= comparisonFilters.maxAccuracy;
      const passesType = comparisonFilters.modelTypes.length === 0 ||
        comparisonFilters.modelTypes.includes(model.type);
      const passesStatus = comparisonFilters.statusTypes.length === 0 ||
        comparisonFilters.statusTypes.includes(model.status);

      let passesDate = true;
      if (comparisonFilters.dateRange.start || comparisonFilters.dateRange.end) {
        const modelDate = new Date(model.trainedAt);
        if (comparisonFilters.dateRange.start) {
          passesDate = passesDate && modelDate >= new Date(comparisonFilters.dateRange.start);
        }
        if (comparisonFilters.dateRange.end) {
          passesDate = passesDate && modelDate <= new Date(comparisonFilters.dateRange.end);
        }
      }

      return passesAccuracy && passesType && passesStatus && passesDate;
    }) : getAdvancedPerformanceData(); // Sadece getAdvancedPerformanceData kullan

    return dataSource.map(model => ({
      x: model.precision, // Zaten yüzde olarak geldi
      y: model.recall,
      z: model.accuracy,
      type: model.type,
      status: model.status,
      fullName: model.name
    }));
  }, [getAdvancedPerformanceData, comparisonFilters]);

  const generateConfusionMatrixSafe = useCallback((model: ModelMetadata) => {
    return [
      { name: 'True Positive', value: 850, color: '#4caf50', description: 'Doğru tespit edilen fraud işlemler' },
      { name: 'True Negative', value: 8150, color: '#4caf50', description: 'Doğru tespit edilen normal işlemler' },
      { name: 'False Positive', value: 150, color: '#ff9800', description: 'Yanlış fraud olarak işaretlenen normal işlemler' },
      { name: 'False Negative', value: 50, color: '#ff9800', description: 'Kaçırılan fraud işlemler' }
    ];
  }, []);

  const getFilteredModelsForComparison = useCallback(() => {
    return getAdvancedPerformanceData().filter(model => {
      const passesAccuracy = model.accuracy >= comparisonFilters.minAccuracy &&
        model.accuracy <= comparisonFilters.maxAccuracy;
      const passesType = comparisonFilters.modelTypes.length === 0 ||
        comparisonFilters.modelTypes.includes(model.type);
      const passesStatus = comparisonFilters.statusTypes.length === 0 ||
        comparisonFilters.statusTypes.includes(model.status);

      let passesDate = true;
      if (comparisonFilters.dateRange.start || comparisonFilters.dateRange.end) {
        const modelDate = new Date(model.trainedAt);
        if (comparisonFilters.dateRange.start) {
          passesDate = passesDate && modelDate >= new Date(comparisonFilters.dateRange.start);
        }
        if (comparisonFilters.dateRange.end) {
          passesDate = passesDate && modelDate <= new Date(comparisonFilters.dateRange.end);
        }
      }

      return passesAccuracy && passesType && passesStatus && passesDate;
    });
  }, [getAdvancedPerformanceData, comparisonFilters]);

  // Fix parseMetricsJsonSafe function
  const parseMetricsJsonSafe = useCallback((model: ModelMetadata) => {
    if (!model.MetricsJson) {
      return {};
    }
    try {
      return JSON.parse(model.MetricsJson);
    } catch (e) {
      console.warn('Metrics JSON parse error:', e);
      return {};
    }
  }, []);

  // Fix formatConfiguration for undefined
  const formatConfigurationSafe = useCallback((configString: string | undefined, modelType: string) => {
    if (!configString) {
      return { 'Konfigürasyon': 'Konfigürasyon bilgisi bulunamadı' };
    }
    return formatConfiguration(configString, modelType);
  }, []);

  // ========== MODEL OPERATIONS ==========
  const handleViewDetails = (model: ModelMetadata) => {
    console.log('🔍 Selected model for details:', model);
    console.log('🔍 Model metrics structure:', getModelMetrics(model));
    setSelectedModel(model);
    setDetailDialog(true);
  };

  const handleUpdateModelStatus = async (modelId: string, newStatus: string) => {
    try {
      setTraining(true);
      await FraudDetectionAPI.updateModelStatus(modelId, newStatus);
      await reloadModels();

      if (selectedModel && selectedModel.id === modelId) {
        setSelectedModel({ ...selectedModel, Status: newStatus as any });
      }

      showSnackbar(`Model durumu ${getStatusConfig(newStatus).label} olarak güncellendi`, 'success');
    } catch (err: any) {
      const errorMessage = err.response?.data?.Error || err.message || 'Model durumu güncellenirken hata oluştu';
      showSnackbar(errorMessage, 'error');
    } finally {
      setTraining(false);
    }
  };

  const handleStartTraining = async () => {
    setTraining(true);
    try {
      console.log('🚀 Model eğitimi başlatılıyor...', newModelConfig);

      // API'den eğitim başlat (örnek olarak timeout kullanıyorum, gerçekte API çağrısı olacak)
      setTimeout(() => {
        // Model tipine göre farklı response formatları
        let mockTrainingResult;

        if (newModelConfig.type === 'LightGBM') {
          // LightGBM response format
          mockTrainingResult = {
            success: true,
            data: {
              basicMetrics: {
                accuracy: 0.9994908886626171,
                auc: 0.9767178114915072,
                aucpr: 0.8716708898507923,
                f1Score: 0.8415300546448088,
                precision: 0.9058823529411765,
                recall: 0.7857142857142857
              },
              confusionMatrix: {
                falseNegative: 21,
                falsePositive: 8,
                trueNegative: 56856,
                truePositive: 77
              },
              extendedMetrics: {
                balancedAccuracy: 0.8927867995819599,
                matthewsCorrCoef: 0.843414022099618,
                sensitivity: 0.7857142857142857,
                specificity: 0.9998593134496342
              },
              modelId: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
              modelName: `CreditCard_FraudDetection_LightGBM_${new Date().toISOString().split('T')[0]}`,
              performanceSummary: {
                isGoodModel: true,
                modelGrade: "A+",
                overallScore: 0.9392462515996444,
                primaryWeakness: "Genel performans kabul edilebilir"
              },
              recommendations: [],
              trainingTime: 10800.197564
            }
          };
        } else if (newModelConfig.type === 'PCA') {
          // PCA format - Gerçek response formatına uygun
          mockTrainingResult = {
            success: true,
            data: {
              basicMetrics: {
                accuracy: 0.9936624416277519,
                precision: 0.15844155844155844,
                recall: 0.6224489795918368,
                f1Score: 0.2525879917184265,
                auc: 0.948778969944759
              },
              confusionMatrix: {
                truePositive: 61,
                trueNegative: 56540,
                falsePositive: 324,
                falseNegative: 37
              },
              extendedMetrics: {
                specificity: 0.9943021947101857,
                sensitivity: 0.6224489795918368,
                balancedAccuracy: 0.8083755871510112,
                matthewsCorrCoef: 0.3119546068946014
              },
              modelId: `fb03af23-30ee-46d2-84c3-89fc29401257`,
              modelName: `CreditCard_AnomalyDetection_PCA_${new Date().toISOString().split('T')[0].replace(/-/g, '')}`,
              performanceSummary: {
                overallScore: 0.7316764677636458,
                isGoodModel: false,
                primaryWeakness: 'Yüksek False Positive oranı - Precision düşük',
                modelGrade: 'B'
              },
              recommendations: [
                'Class weights ayarlarını gözden geçirin',
                'Fraud sınıfı için daha fazla özellik mühendisliği yapın'
              ],
              trainingTime: 10800.144421
            }
          };
        } else {
          // Ensemble format
          mockTrainingResult = {
            success: true,
            data: {
              basicMetrics: {
                accuracy: 0.9996137776061234,
                auc: 0.9810746083745823,
                aucpr: 0.8825775965743204,
                f1Score: 0.8817204301075269,
                precision: 0.9318181818181818,
                recall: 0.8367346938775511
              },
              confusionMatrix: {
                falseNegative: 16,
                falsePositive: 6,
                trueNegative: 56858,
                truePositive: 82
              },
              extendedMetrics: {
                balancedAccuracy: 0.9183145894823883,
                matthewsCorrCoef: 0.8828085391767327,
                sensitivity: 0.8367346938775511,
                specificity: 0.9998944850872257,
                lightgbm_weight: 0.7,
                pca_weight: 0.3,
                ensemble_confidence: 0.92
              },
              modelId: `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
              modelName: `CreditCard_FraudDetection_Ensemble_${new Date().toISOString().split('T')[0]}`,
              performanceSummary: {
                isGoodModel: true,
                modelGrade: "A++",
                overallScore: 0.9601941930221161,
                primaryWeakness: "Mükemmel performans - Production ready"
              },
              recommendations: [],
              trainingTime: 15420.892156
            }
          };
        }

        console.log('🎉 Model eğitimi tamamlandı:', mockTrainingResult);

        // Sonuçları göster
        setTrainingResult(mockTrainingResult);
        setTrainingResultDialog(true);
        setTrainingDialog(false);

        // Form temizle
        setNewModelConfig({
          name: '',
          type: 'LightGBM',
          description: '',
          config: { numLeaves: 31, learningRate: 0.1 } as LightGBMConfig,
        });

        setTraining(false);

        // Modelleri yeniden yükle
        reloadModels();
      }, 3000);
    } catch (error) {
      console.error('❌ Model eğitimi sırasında hata:', error);
      showSnackbar('Model eğitimi sırasında hata oluştu', 'error');
      setTraining(false);
    }
  };

  // ========== FILTERING & SORTING ==========
  const getFilteredAndSortedModels = () => {
    let filteredModels = models.filter(model => {
      const modelName = (model as any).modelName || model.ModelName || '';
      const modelId = model.id || '';

      // Arama filtresi - model adı veya ID'de ara
      const matchesSearch = searchTerm === '' ||
        modelName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        modelId.toLowerCase().includes(searchTerm.toLowerCase());

      // Durum filtresi - hem camelCase hem PascalCase destekle
      const modelStatus = (model as any).status || model.Status || '';
      const matchesStatus = statusFilter === '' || modelStatus === statusFilter;

      // Tip filtresi - hem camelCase hem PascalCase destekle
      const modelType = (model as any).type || model.Type || '';
      const matchesType = typeFilter === '' || modelType === typeFilter;

      // Metrik filtresi - sadece geçerli metrikleri olan modelleri göster
      const metrics = getModelMetrics(model);
      const hasValidMetrics = (
        (metrics?.accuracy && metrics.accuracy > 0) ||
        (metrics?.precision && metrics.precision > 0) ||
        (metrics?.recall && metrics.recall > 0) ||
        (metrics?.f1_score && metrics.f1_score > 0) ||
        (metrics?.auc && metrics.auc > 0)
      );

      return matchesSearch && matchesStatus && matchesType && hasValidMetrics;
    });

    // Sıralama
    filteredModels.sort((a, b) => {
      let aValue: any, bValue: any;

      switch (sortBy) {
        case 'name':
          aValue = ((a as any).modelName || a.ModelName || '').toLowerCase();
          bValue = ((b as any).modelName || b.ModelName || '').toLowerCase();
          break;
        case 'type':
          aValue = (a as any).type || a.Type || '';
          bValue = (b as any).type || b.Type || '';
          break;
        case 'status':
          aValue = (a as any).status || a.Status || '';
          bValue = (b as any).status || b.Status || '';
          break;
        case 'accuracy':
          const aMetrics = getModelMetrics(a);
          const bMetrics = getModelMetrics(b);
          aValue = aMetrics?.accuracy || 0;
          bValue = bMetrics?.accuracy || 0;
          break;
        case 'date':
          aValue = new Date((a as any).trainedAt || a.TrainedAt || '');
          bValue = new Date((b as any).trainedAt || b.TrainedAt || '');
          break;
        default:
          return 0;
      }

      if (aValue < bValue) return sortDirection === 'asc' ? -1 : 1;
      if (aValue > bValue) return sortDirection === 'asc' ? 1 : -1;
      return 0;
    });

    return filteredModels;
  };

  const handleSort = (field: typeof sortBy) => {
    if (sortBy === field) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(field);
      setSortDirection('asc');
    }
  };

  const clearFilters = () => {
    setSearchTerm('');
    setStatusFilter('');
    setTypeFilter('');
    setSortBy('date');
    setSortDirection('desc');
    setPage(0);
  };

  // ========== COMPARISON FUNCTIONS ==========
  const getTimeRangeLabel = () => {
    switch (performanceTimeRange) {
      case '7d': return 'Son 7 Gün';
      case '30d': return 'Son 30 Gün';
      case '90d': return 'Son 90 Gün';
      case 'all': return 'Tüm Zamanlar';
      default: return 'Son 30 Gün';
    }
  };

  const getModelTrendData = useCallback(() => {
    // Filtrelenmiş modelleri kullan
    const filteredModels = getFilteredModelsForComparison();
    const now = new Date();
    const trendData = [];

    // Eğer filtrelenmiş model yoksa boş data döndür
    if (filteredModels.length === 0) {
      return [];
    }

    let days = 7;
    if (performanceTimeRange === '30d') days = 30;
    else if (performanceTimeRange === '90d') days = 90;
    else if (performanceTimeRange === 'all') {
      if (filteredModels.length > 0) {
        const oldestModel = filteredModels.reduce((oldest, model) => {
          const trainedAt = model.trainedAt;
          const modelDate = trainedAt ? new Date(trainedAt) : new Date();
          return modelDate < oldest ? modelDate : oldest;
        }, new Date());
        days = Math.ceil((now.getTime() - oldestModel.getTime()) / (1000 * 60 * 60 * 24));
      }
    }

    let interval = 1;
    if (days > 30 && days <= 90) interval = 3;
    else if (days > 90 && days <= 180) interval = 7;
    else if (days > 180) interval = 14;

    for (let i = days; i >= 0; i -= interval) {
      const date = new Date(now);
      date.setDate(date.getDate() - i);

      const periodEnd = new Date(date);
      const periodStart = new Date(date);
      periodStart.setDate(periodStart.getDate() - interval + 1);

      const periodModels = filteredModels.filter(m => {
        const trainedAt = m.trainedAt;
        if (!trainedAt) return false;
        const trainedDate = new Date(trainedAt);
        return trainedDate >= periodStart && trainedDate <= periodEnd;
      });

      let avgAccuracy = 0, avgPrecision = 0, avgRecall = 0, avgF1Score = 0, avgAuc = 0;

      if (periodModels.length > 0) {
        avgAccuracy = periodModels.reduce((sum, m) => sum + m.accuracy, 0) / periodModels.length;
        avgPrecision = periodModels.reduce((sum, m) => sum + m.precision, 0) / periodModels.length;
        avgRecall = periodModels.reduce((sum, m) => sum + m.recall, 0) / periodModels.length;
        avgF1Score = periodModels.reduce((sum, m) => sum + m.f1_score, 0) / periodModels.length;
        avgAuc = periodModels.reduce((sum, m) => sum + m.auc, 0) / periodModels.length;
      } else {
        // Eğer o periyotta model yoksa, o tarihe kadar olan tüm modellerin ortalamasını al
        const allModels = filteredModels.filter(m => {
          const trainedAt = m.trainedAt;
          if (!trainedAt) return false;
          return new Date(trainedAt) <= periodEnd;
        });

        if (allModels.length > 0) {
          avgAccuracy = allModels.reduce((sum, m) => sum + m.accuracy, 0) / allModels.length;
          avgPrecision = allModels.reduce((sum, m) => sum + m.precision, 0) / allModels.length;
          avgRecall = allModels.reduce((sum, m) => sum + m.recall, 0) / allModels.length;
          avgF1Score = allModels.reduce((sum, m) => sum + m.f1_score, 0) / allModels.length;
          avgAuc = allModels.reduce((sum, m) => sum + m.auc, 0) / allModels.length;
        }
      }

      trendData.push({
        date: date.toISOString().split('T')[0],
        accuracy: Number(avgAccuracy.toFixed(1)),
        precision: Number(avgPrecision.toFixed(1)),
        recall: Number(avgRecall.toFixed(1)),
        f1Score: Number(avgF1Score.toFixed(1)),
        auc: Number(avgAuc.toFixed(1)),
        modelCount: periodModels.length,
        interval: interval
      });
    }

    return trendData;
  }, [performanceTimeRange, getFilteredModelsForComparison]);

  const getComparisonStats = () => {
    const data = getAdvancedPerformanceData();
    if (data.length === 0) return null;

    const metrics = ['accuracy', 'precision', 'recall', 'f1_score', 'auc'] as const;
    const stats: any = {};

    metrics.forEach(metric => {
      const values = data.map(d => d[metric]);
      const sortedValues = [...values].sort((a, b) => a - b);
      const avg = values.reduce((a, b) => a + b, 0) / values.length;

      stats[metric] = {
        min: Math.min(...values),
        max: Math.max(...values),
        avg: avg,
        median: sortedValues[Math.floor(sortedValues.length / 2)],
        std: Math.sqrt(values.reduce((a, b) => a + Math.pow(b - avg, 2), 0) / values.length)
      };
    });

    return stats;
  };

  // Model Selection
  const handleModelSelectionForComparison = (modelId: string) => {
    setSelectedModelsForComparison(prev => {
      if (prev.includes(modelId)) {
        return prev.filter(id => id !== modelId);
      } else if (prev.length < 10) {
        return [...prev, modelId];
      } else {
        showSnackbar('⚠️ Maksimum 10 model karşılaştırabilirsiniz', 'warning');
        return prev;
      }
    });
  };

  const selectTopModels = (criterion: 'accuracy' | 'precision' | 'recall' | 'auc' | 'f1_score', count: number = 5) => {
    const data = getAdvancedPerformanceData();
    const topModels = [...data].sort((a, b) => b[criterion] - a[criterion]).slice(0, count);
    setSelectedModelsForComparison(topModels.map(m => m.id));
  };

  const clearComparison = () => {
    setSelectedModelsForComparison([]);
  };

  const resetComparisonFilters = () => {
    setComparisonFilters({
      minAccuracy: 0,
      maxAccuracy: 100,
      modelTypes: [],
      statusTypes: [],
      dateRange: { start: '', end: '' }
    });
  };

  // ========== DETAIL DIALOG FUNCTIONS ==========


  const getConfusionMatrixData = (model: ModelMetadata) => {
    let confusionData;
    try {
      const extraMetrics = parseMetricsJsonSafe(model);
      console.log('📊 Parsed metrics for confusion matrix:', extraMetrics);

      if (extraMetrics.true_positive !== undefined) {
        confusionData = [
          { name: 'True Positive (TP)', value: extraMetrics.true_positive, color: '#4caf50', description: 'Doğru tespit edilen fraud işlemler' },
          { name: 'False Positive (FP)', value: extraMetrics.false_positive, color: '#ff9800', description: 'Yanlış fraud olarak işaretlenen normal işlemler' },
          { name: 'True Negative (TN)', value: extraMetrics.true_negative, color: '#4caf50', description: 'Doğru tespit edilen normal işlemler' },
          { name: 'False Negative (FN)', value: extraMetrics.false_negative, color: '#ff9800', description: 'Kaçırılan fraud işlemler' }
        ];
        console.log('✅ Using actual confusion matrix data:', confusionData);
      }
    } catch (e) {
      console.warn('Metrics JSON parse error:', e);
    }

    if (!confusionData) {
      // Güvenli metrics erişimi için helper fonksiyonu kullan
      const metrics = getModelMetrics(model);
      const totalFraud = 1000;
      const totalNormal = 9000;
      const tp = Math.round(totalFraud * (metrics?.recall || 0));
      const fn = totalFraud - tp;
      const fp = Math.round(totalNormal * (metrics?.fpr || 0.001));
      const tn = totalNormal - fp;

      confusionData = [
        { name: 'True Positive (TP)', value: tp, color: '#4caf50', description: 'Doğru tespit edilen fraud işlemler' },
        { name: 'False Positive (FP)', value: fp, color: '#ff9800', description: 'Yanlış fraud olarak işaretlenen normal işlemler' },
        { name: 'True Negative (TN)', value: tn, color: '#4caf50', description: 'Doğru tespit edilen normal işlemler' },
        { name: 'False Negative (FN)', value: fn, color: '#ff9800', description: 'Kaçırılan fraud işlemler' }
      ];
      console.log('⚠️ Using calculated confusion matrix data:', confusionData);
    }

    return confusionData;
  };

  const getGaugeData = (value: number, label: string, color: string) => [
    { name: label, value: value * 100, fill: color },
    { name: 'Remaining', value: (1 - value) * 100, fill: '#f0f0f0' }
  ];

  const getROCCurveData = (model: ModelMetadata) => {
    const metrics = getModelMetrics(model);
    const auc = metrics?.auc || 0.5;
    let actualFPR = 0, actualTPR = 0;

    try {
      const metricsJson = getModelProperty(model, 'metricsJson');
      if (metricsJson) {
        const extraMetrics = JSON.parse(metricsJson);
        actualFPR = extraMetrics.fpr || metrics?.fpr || 0.001;
        actualTPR = metrics?.recall || 0;
      }
    } catch (e) {
      actualFPR = metrics?.fpr || 0.001;
      actualTPR = metrics?.recall || 0;
    }

    const rocData = [];
    for (let i = 0; i <= 100; i++) {
      const fpr = i / 100;
      let tpr;

      if (fpr === 0) {
        tpr = 0;
      } else if (fpr === 1) {
        tpr = 1;
      } else {
        const ratio = fpr / (actualFPR || 0.001);
        tpr = Math.min(1, actualTPR * ratio + (auc - 0.5) * 2 * (1 - fpr));
        tpr = Math.max(fpr, tpr);
      }

      rocData.push({ fpr: Math.round(fpr * 100), tpr: Math.round(tpr * 100) });
    }

    return rocData;
  };

  const getPRCurveData = (model: ModelMetadata) => {
    const metrics = getModelMetrics(model);
    const precision = metrics?.precision || 0.5;
    const recall = metrics?.recall || 0.5;
    const points = [];

    for (let i = 0; i <= 10; i++) {
      const r = i / 10;
      let p;

      if (r === 0) {
        p = 1;
      } else if (r > recall) {
        const dropRate = (r - recall) / (1 - recall);
        p = precision * (1 - dropRate * 0.8);
      } else {
        const ratio = r / recall;
        p = precision + (1 - precision) * (1 - ratio) * 0.3;
      }

      points.push({
        recall: r * 100,
        precision: Math.max(0.1, Math.min(100, p * 100)),
        threshold: (1 - r).toFixed(2)
      });
    }

    return points;
  };

  const getModelSpecificMetrics = (model: ModelMetadata) => {
    const metrics = getModelMetrics(model);
    const modelType = getModelProperty(model, 'type');

    if (modelType === 'LightGBM') {
      return [
        { label: 'Balanced Accuracy', value: metrics?.balancedAccuracy || 0, format: 'percentage' },
        { label: 'FDR (False Discovery Rate)', value: metrics?.fdr || 0, format: 'percentage' },
        { label: 'AUC-PR', value: metrics?.aucpr || 0, format: 'percentage' }
      ];
    } else if (modelType === 'PCA') {
      return [
        { label: 'Anomaly Threshold', value: metrics?.anomalyThreshold || 0, format: 'number' },
        { label: 'Mean Reconstruction Error', value: metrics?.meanReconstructionError || 0, format: 'number' },
        { label: 'Std Reconstruction Error', value: metrics?.stdReconstructionError || 0, format: 'number' }
      ];
    } else if (modelType === 'Ensemble') {
      let lightgbmAuc = 0;
      let pcaAuc = 0;

      try {
        const metricsJson = getModelProperty(model, 'metricsJson');
        if (metricsJson) {
          const extraMetrics = JSON.parse(metricsJson);
          lightgbmAuc = extraMetrics.lightgbm_auc || 0;
          pcaAuc = extraMetrics.pca_auc || 0;
        }
      } catch (e) {
        console.warn('Metrics JSON parse error:', e);
      }

      return [
        { label: 'LightGBM AUC', value: lightgbmAuc, format: 'percentage' },
        { label: 'PCA AUC', value: pcaAuc, format: 'percentage' },
        { label: 'Combined Score', value: ((metrics?.auc || 0) + (metrics?.accuracy || 0)) / 2, format: 'percentage' }
      ];
    }
    return [];
  };

  const formatConfiguration = (configString: string, modelType: string) => {
    try {
      const config = JSON.parse(configString);

      if (modelType === 'Ensemble') {
        return {
          'Ensemble Ayarları': {
            'LightGBM Ağırlığı': config.lightgbmWeight,
            'PCA Ağırlığı': config.pcaWeight,
            'Eşik Değeri': config.threshold,
            'Birleştirme Stratejisi': config.combinationStrategy
          },
          'Çapraz Doğrulama': {
            'Aktif': config.enableCrossValidation,
            'Katman Sayısı': config.crossValidationFolds
          }
        };
      } else if (modelType === 'LightGBM') {
        const lightgbmConfig = config.lightgbm || config;
        return {
          'Eğitim Parametreleri': {
            'Yaprak Sayısı': lightgbmConfig.numberOfLeaves,
            'Öğrenme Oranı': lightgbmConfig.learningRate,
            'Ağaç Sayısı': lightgbmConfig.numberOfTrees,
            'Minimum Veri': lightgbmConfig.minDataInLeaf
          },
          'Özellik Ayarları': {
            'Özellik Oranı': lightgbmConfig.featureFraction,
            'Bagging Oranı': lightgbmConfig.baggingFraction,
            'Bagging Sıklığı': lightgbmConfig.baggingFrequency
          },
          'Düzenleme': {
            'L1 Düzenleme': lightgbmConfig.l1Regularization,
            'L2 Düzenleme': lightgbmConfig.l2Regularization,
            'Minimum Kazanç': lightgbmConfig.minGainToSplit
          }
        };
      } else if (modelType === 'PCA') {
        const pcaConfig = config.pca || config;
        return {
          'Boyut Azaltma': {
            'Bileşen Sayısı': pcaConfig.componentCount,
            'Varyans Eşiği': pcaConfig.explainedVarianceThreshold
          },
          'İşleme': {
            'Standardizasyon': pcaConfig.standardizeInput,
            'Anomali Eşiği': pcaConfig.anomalyThreshold
          }
        };
      }

      return config;
    } catch (e) {
      console.error('Config parse hatası:', e);
      return { 'Ham Konfigürasyon': configString };
    }
  };

  const renderConfigSection = (data: any, level: number = 0): React.ReactNode => {
    if (typeof data !== 'object' || data === null) {
      return (
        <Typography variant="body2" sx={{ color: '#2e7d32', fontFamily: 'monospace' }}>
          {String(data)}
        </Typography>
      );
    }

    return (
      <Box sx={{ ml: level * 2 }}>
        {Object.entries(data).map(([key, value]) => (
          <Box key={key} sx={{ mb: 1 }}>
            <Typography
              variant="body2"
              sx={{
                fontWeight: level === 0 ? 'bold' : 'medium',
                color: level === 0 ? '#1976d2' : '#333',
                mb: 0.5
              }}
            >
              {key}:
            </Typography>
            {typeof value === 'object' && value !== null ? (
              renderConfigSection(value, level + 1)
            ) : (
              <Typography
                variant="body2"
                sx={{
                  ml: 2,
                  color: typeof value === 'number' ? '#d32f2f' : '#2e7d32',
                  fontFamily: 'monospace'
                }}
              >
                {Array.isArray(value) ? `[${value.join(', ')}]` : String(value)}
              </Typography>
            )}
          </Box>
        ))}
      </Box>
    );
  };

  const generateModelReport = (model: ModelMetadata) => {
    const metrics = getModelMetrics(model);
    const modelName = getModelProperty(model, 'modelName');
    const modelType = getModelProperty(model, 'type');
    const version = getModelProperty(model, 'version');
    const status = getModelProperty(model, 'status');
    const trainedAt = getModelProperty(model, 'trainedAt');
    const createdBy = getModelProperty(model, 'createdBy');
    const configuration = getModelProperty(model, 'configuration');

    let report = `
Model Raporu
=============

Model Bilgileri:
- Ad: ${modelName}
- Tip: ${getModelTypeConfig(modelType).label}
- Versiyon: ${version}
- Durum: ${getStatusConfig(status).label}
- Eğitim Tarihi: ${new Date(trainedAt).toLocaleString('tr-TR')}
- Oluşturan: ${createdBy}

Performans Metrikleri:
- Accuracy: %${((metrics?.accuracy || 0) * 100).toFixed(2)}
- Precision: %${((metrics?.precision || 0) * 100).toFixed(2)}
- Recall: %${((metrics?.recall || 0) * 100).toFixed(2)}
- F1-Score: %${((metrics?.f1_score || 0) * 100).toFixed(2)}
- AUC: %${((metrics?.auc || 0.5) * 100).toFixed(2)}

Confusion Matrix:
`;

    const confusionData = getConfusionMatrixData(model);
    confusionData.forEach((item: any) => {
      report += `- ${item.name}: ${item.value}\n`;
    });

    if (configuration) {
      report += `
Konfigürasyon:
${configuration}

Rapor Tarihi: ${new Date().toLocaleString('tr-TR')}
Sistem: Fraud Shield v2.0 Advanced Analytics
===============================================`;
    }

    return report;
  };

  const downloadModelReport = (model: ModelMetadata) => {
    const report = generateModelReport(model);
    const blob = new Blob([report], { type: 'text/plain;charset=utf-8' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${model.ModelName}_v${model.Version}_rapor_${new Date().toISOString().split('T')[0]}.txt`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);

    showSnackbar(`${model.ModelName} model raporu indirildi`, 'success');
  };

  // ========== RENDER HELPERS ==========
  const filteredModels = getFilteredAndSortedModels();
  const paginatedModels = filteredModels.slice(
    page * rowsPerPage,
    page * rowsPerPage + rowsPerPage
  );

  const uniqueStatuses = Array.from(new Set(models.map(m => getModelProperty(m, 'status')).filter(Boolean)));
  const uniqueTypes = Array.from(new Set(models.map(m => getModelProperty(m, 'type')).filter(Boolean)));

  // ========== MAIN RENDER ==========
  return (
    <Box>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <ModelIcon sx={{ fontSize: 40, color: '#1976d2' }} />
          <Box>


            <Typography variant="body2" color="textSecondary">
              Machine learning modellerinin yönetimi, eğitimi ve performans analizi
            </Typography>
          </Box>
        </Box>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button
            variant="outlined"
            color="info"
            onClick={reloadModels}
            disabled={isTraining}
            startIcon={<RefreshIcon />}
          >
            Yenile
          </Button>

        </Box>
      </Box>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={activeTab} onChange={(_, v) => setActiveTab(v)}>
          <Tab
            label="Model Listesi"
            icon={<ModelIcon />}
            iconPosition="start"
          />
          <Tab
            label="Performans Karşılaştırma"
            icon={<AnalysisIcon />}
            iconPosition="start"
          />
          <Tab
            label="Model Eğitimi"
            icon={<TrainIcon />}
            iconPosition="start"
          />
          <Tab
            label="Hiperparametre Optimizasyonu"
            icon={<TuneIcon />}
            iconPosition="start"
          />
        </Tabs>
      </Box>

      {/* Model Listesi Tab */}
      <TabPanel value={activeTab} index={0}>
        {/* Filtreleme */}
        <Card variant="outlined" sx={{ mb: 3 }}>
          <CardContent>
            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
              <Typography variant="h6" color="primary" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <FilterIcon /> Filtreleme ve Arama
              </Typography>
              <Button
                variant="text"
                color="secondary"
                onClick={() => setShowFilters(!showFilters)}
                startIcon={<ExpandMoreIcon sx={{
                  transform: showFilters ? 'rotate(180deg)' : 'rotate(0)',
                  transition: 'transform 0.3s'
                }} />}
              >
                {showFilters ? 'Filtreleri Gizle' : 'Gelişmiş Filtreler'}
              </Button>
            </Box>

            <Box sx={{ display: 'flex', gap: 2, mb: showFilters ? 2 : 0, flexWrap: 'wrap' }}>
              <TextField
                label="Model Ara"
                placeholder="Model adı veya ID ile arayın..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                sx={{ flex: 1, minWidth: 300 }}
                size="small"
              />
              <Button
                variant="outlined"
                onClick={clearFilters}
                sx={{ minWidth: 120 }}
                startIcon={<ClearIcon />}
              >
                Temizle
              </Button>
            </Box>

            <Collapse in={showFilters}>
              <Box sx={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
                gap: 2,
                pt: 2,
                borderTop: '1px solid #e0e0e0'
              }}>
                <FormControl size="small">
                  <InputLabel>Model Durumu</InputLabel>
                  <Select
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value)}
                    label="Model Durumu"
                  >
                    <MenuItem value="">Tümü</MenuItem>
                    {uniqueStatuses.map(status => (
                      <MenuItem key={status} value={status}>
                        {getStatusConfig(status).icon} {getStatusConfig(status).label}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>

                <FormControl size="small">
                  <InputLabel>Model Tipi</InputLabel>
                  <Select
                    value={typeFilter}
                    onChange={(e) => setTypeFilter(e.target.value)}
                    label="Model Tipi"
                  >
                    <MenuItem value="">Tümü</MenuItem>
                    {uniqueTypes.map(type => (
                      <MenuItem key={type} value={type}>
                        {getModelTypeConfig(type).icon} {getModelTypeConfig(type).label}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>

                <FormControl size="small">
                  <InputLabel>Sıralama</InputLabel>
                  <Select
                    value={`${sortBy}-${sortDirection}`}
                    onChange={(e) => {
                      const [field, order] = e.target.value.split('-');
                      setSortBy(field as any);
                      setSortDirection(order as 'asc' | 'desc');
                    }}
                    label="Sıralama"
                  >
                    <MenuItem value="date-desc">📅 En Yeni</MenuItem>
                    <MenuItem value="date-asc">📅 En Eski</MenuItem>
                    <MenuItem value="name-asc">📝 Ada Göre (A→Z)</MenuItem>
                    <MenuItem value="name-desc">📝 Ada Göre (Z→A)</MenuItem>
                    <MenuItem value="accuracy-desc">🎯 En Yüksek Accuracy</MenuItem>
                    <MenuItem value="accuracy-asc">🎯 En Düşük Accuracy</MenuItem>
                  </Select>
                </FormControl>

                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Typography variant="body2" color="textSecondary">
                    {filteredModels.length} / {models.length} model
                  </Typography>
                </Box>
              </Box>
            </Collapse>
          </CardContent>
        </Card>

        {/* Model Tablosu */}
        {isTraining ? (
          <Box sx={{ mb: 2 }}>
            <LinearProgress />
            <Typography variant="body2" sx={{ mt: 1 }}>
              Modeller yükleniyor...
            </Typography>
          </Box>
        ) : models.length === 0 ? (
          <Alert severity="info">
            Henüz hiç model bulunamadı. Yeni bir model eğiterek başlayabilirsiniz.
          </Alert>
        ) : filteredModels.length === 0 ? (
          <Alert severity="warning">
            Arama kriterlerinize uygun model bulunamadı.
          </Alert>
        ) : (
          <TableContainer component={Card}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell sx={{ cursor: 'pointer' }} onClick={() => handleSort('name')}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      Model Adı {sortBy === 'name' && (sortDirection === 'asc' ? '↑' : '↓')}
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      Tip
                    </Box>
                  </TableCell>
                  <TableCell>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      Durum
                    </Box>
                  </TableCell>
                  <TableCell align="center" sx={{ cursor: 'pointer' }} onClick={() => handleSort('accuracy')}>
                    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 1 }}>
                      Accuracy {sortBy === 'accuracy' && (sortDirection === 'asc' ? '↑' : '↓')}
                    </Box>
                  </TableCell>
                  <TableCell align="center">Precision</TableCell>
                  <TableCell align="center">Recall</TableCell>
                  <TableCell align="center">F1-Score</TableCell>
                  <TableCell align="center">AUC</TableCell>
                  <TableCell sx={{ cursor: 'pointer' }} onClick={() => handleSort('date')}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      Eğitim Tarihi {sortBy === 'date' && (sortDirection === 'asc' ? '↑' : '↓')}
                    </Box>
                  </TableCell>
                  <TableCell align="center">İşlemler</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {paginatedModels.map((model) => (
                  <TableRow key={model.id} hover>
                    <TableCell>
                      <Box>
                        <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
                          {getModelProperty(model, 'modelName')}
                        </Typography>
                        <Typography variant="caption" color="textSecondary">
                          ID: {model.id.substring(0, 12)}...
                        </Typography>
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Box
                        sx={{ display: 'flex', alignItems: 'center', gap: 1 }}
                        onClick={(e) => e.stopPropagation()}
                      >
                        <Typography sx={{ fontSize: 14 }}>{getModelTypeConfig(getModelProperty(model, 'type')).icon}</Typography>
                        <Chip
                          label={getModelTypeConfig(getModelProperty(model, 'type')).label}
                          size="small"
                          sx={{ cursor: 'default' }}
                          onClick={(e) => e.stopPropagation()}
                        />
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={getStatusConfig(getModelProperty(model, 'status')).label}
                        color={getStatusConfig(getModelProperty(model, 'status')).color as "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning"}
                        size="small"
                        sx={{ cursor: 'default' }}
                        onClick={(e) => e.stopPropagation()}
                      />
                    </TableCell>
                    {['accuracy', 'precision', 'recall', 'f1_score', 'auc'].map(metric => (
                      <TableCell key={metric} align="center">
                        <Typography
                          variant="body2"
                          sx={{
                            fontWeight: metric === 'accuracy' ? 'medium' : 'normal',
                            color: (getModelMetrics(model)?.[metric as keyof ModelMetrics] as number) > 0.9 ? 'success.main' : 'text.primary'
                          }}
                        >
                          {getModelMetrics(model)?.[metric as keyof ModelMetrics]
                            ? `${((getModelMetrics(model)?.[metric as keyof ModelMetrics] as number) * 100).toFixed(2)}%`
                            : 'N/A'}
                        </Typography>
                      </TableCell>
                    ))}
                    <TableCell>
                      {new Date(getModelProperty(model, 'trainedAt')).toLocaleDateString('tr-TR')}
                    </TableCell>
                    <TableCell align="center">
                      <Box sx={{ display: 'flex', gap: 1 }}>
                        <Tooltip title="Detayları Görüntüle">
                          <IconButton size="small" onClick={() => handleViewDetails(model)}>
                            <ViewIcon />
                          </IconButton>
                        </Tooltip>

                        {/* Training durumundaki modeller için Pasife Al butonu */}
                        {getModelProperty(model, 'status') === 'Training' && (
                          <Tooltip title="Pasife Al">
                            <IconButton
                              size="small"
                              onClick={() => handleUpdateModelStatus(model.id, 'Inactive')}
                              disabled={isTraining}
                              color="warning"
                            >
                              <StopIcon />
                            </IconButton>
                          </Tooltip>
                        )}

                        {/* Inactive durumundaki modeller için Aktifleştir butonu */}
                        {getModelProperty(model, 'status') === 'Inactive' && (
                          <Tooltip title="Aktifleştir">
                            <IconButton
                              size="small"
                              onClick={() => handleUpdateModelStatus(model.id, 'Active')}
                              disabled={isTraining}
                              color="success"
                            >
                              <DeployIcon />
                            </IconButton>
                          </Tooltip>
                        )}

                        {/* Active durumundaki modeller için Pasife Al butonu */}
                        {getModelProperty(model, 'status') === 'Active' && (
                          <Tooltip title="Pasife Al">
                            <IconButton
                              size="small"
                              onClick={() => handleUpdateModelStatus(model.id, 'Inactive')}
                              disabled={isTraining}
                              color="warning"
                            >
                              <PauseIcon />
                            </IconButton>
                          </Tooltip>
                        )}
                      </Box>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            <TablePagination
              rowsPerPageOptions={[10, 25, 50]}
              component="div"
              count={filteredModels.length}
              rowsPerPage={rowsPerPage}
              page={page}
              onPageChange={(_, newPage) => setPage(newPage)}
              onRowsPerPageChange={(e) => {
                setRowsPerPage(parseInt(e.target.value, 10));
                setPage(0);
              }}
              labelRowsPerPage="Sayfa başına:"
              labelDisplayedRows={({ from, to, count }) => `${from}-${to} / ${count}`}
            />
          </TableContainer>
        )}
      </TabPanel>

      {/* Performans Karşılaştırma Tab */}
      <TabPanel value={activeTab} index={1}>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
          {/* Kontrol Paneli */}
          <Card variant="outlined">
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
                <Typography variant="h6" color="primary" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <CompareIcon /> Performans Karşılaştırması
                  <Chip
                    label={`${getFilteredModelsForComparison().length} / ${models.length} Model`}
                    color="primary"
                    size="small"
                    variant="outlined"
                  />
                </Typography>
                <Box sx={{ display: 'flex', gap: 2 }}>
                  <Button
                    variant="text"
                    color="secondary"
                    onClick={() => setShowAdvancedComparison(!showAdvancedComparison)}
                    startIcon={<ExpandMoreIcon sx={{
                      transform: showAdvancedComparison ? 'rotate(180deg)' : 'rotate(0)',
                      transition: 'transform 0.3s'
                    }} />}
                  >
                    {showAdvancedComparison ? 'Basit Görünüm' : 'Gelişmiş Filtreler'}
                  </Button>
                  <Button
                    variant="outlined"
                    color="info"
                    onClick={resetComparisonFilters}
                    size="small"
                    startIcon={<RefreshIcon />}
                  >
                    Sıfırla
                  </Button>
                </Box>
              </Box>

              {/* Temel Kontroller */}
              <Box sx={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
                gap: 2,
                mb: showAdvancedComparison ? 3 : 0
              }}>
                <FormControl size="small">
                  <InputLabel>Görselleştirme</InputLabel>
                  <Select
                    value={comparisonView}
                    onChange={(e) => setComparisonView(e.target.value as any)}
                    label="Görselleştirme"
                  >
                    <MenuItem value="bar">📊 Çubuk Grafik</MenuItem>
                    <MenuItem value="radar">🕸️ Radar Grafik</MenuItem>
                    <MenuItem value="line">📈 Trend Grafiği</MenuItem>
                    <MenuItem value="heatmap">🔥 Isı Haritası</MenuItem>
                    <MenuItem value="scatter">💫 Dağılım Grafiği</MenuItem>
                  </Select>
                </FormControl>

                <FormControl size="small">
                  <InputLabel>Ana Metrik</InputLabel>
                  <Select
                    value={comparisonMetric}
                    onChange={(e) => setComparisonMetric(e.target.value as any)}
                    label="Ana Metrik"
                  >
                    <MenuItem value="accuracy">🎯 Accuracy</MenuItem>
                    <MenuItem value="precision">🔍 Precision</MenuItem>
                    <MenuItem value="recall">📋 Recall</MenuItem>
                    <MenuItem value="f1_score">⚖️ F1-Score</MenuItem>
                    <MenuItem value="auc">📐 AUC</MenuItem>
                  </Select>
                </FormControl>

                <FormControl size="small">
                  <InputLabel>Zaman Aralığı</InputLabel>
                  <Select
                    value={performanceTimeRange}
                    onChange={(e) => setPerformanceTimeRange(e.target.value as any)}
                    label="Zaman Aralığı"
                  >
                    <MenuItem value="7d">📅 Son 7 Gün</MenuItem>
                    <MenuItem value="30d">📅 Son 30 Gün</MenuItem>
                    <MenuItem value="90d">📅 Son 90 Gün</MenuItem>
                    <MenuItem value="all">📅 Tüm Zamanlar</MenuItem>
                  </Select>
                </FormControl>

                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  <Typography variant="body2" color="textSecondary">
                    Seçili: {selectedModelsForComparison.length}/10 model
                  </Typography>
                </Box>
              </Box>

              {/* Model Seçim Listesi */}
              <Box sx={{ mt: 2, p: 2, bgcolor: '#f8f9fa', borderRadius: 1 }}>
                <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 'bold' }}>
                  📋 Karşılaştırma için Model Seçin (Radar analizi için gerekli)
                </Typography>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, maxHeight: 200, overflowY: 'auto' }}>
                  {getFilteredModelsForComparison().map((model) => (
                    <Chip
                      key={model.id}
                      label={`${getModelTypeConfig(model.type).icon} ${model.name}`}
                      color={selectedModelsForComparison.includes(model.id) ? 'primary' : 'default'}
                      variant={selectedModelsForComparison.includes(model.id) ? 'filled' : 'outlined'}
                      onClick={() => handleModelSelectionForComparison(model.id)}
                      clickable
                      size="small"
                      sx={{
                        '&:hover': {
                          boxShadow: 1
                        }
                      }}
                    />
                  ))}
                </Box>
                {getFilteredModelsForComparison().length === 0 && (
                  <Alert severity="warning" sx={{ mt: 1 }}>
                    ⚠️ Filtreleme kriterlerinize uygun model bulunamadı
                  </Alert>
                )}
              </Box>

              {/* Gelişmiş Filtreler */}
              <Collapse in={showAdvancedComparison}>
                <Box sx={{ pt: 3, borderTop: '1px solid #e0e0e0' }}>
                  <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 'bold' }}>
                    Gelişmiş Filtreleme
                  </Typography>

                  <Box sx={{
                    display: 'grid',
                    gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
                    gap: 2,
                    mb: 3
                  }}>
                    <Box>
                      <Typography variant="caption" gutterBottom>Accuracy Aralığı (%)</Typography>
                      <Box sx={{ display: 'flex', gap: 1 }}>
                        <TextField
                          size="small"
                          type="number"
                          placeholder="Min"
                          value={comparisonFilters.minAccuracy}
                          onChange={(e) => setComparisonFilters(prev => ({
                            ...prev,
                            minAccuracy: Number(e.target.value)
                          }))}
                          sx={{ width: 80 }}
                        />
                        <TextField
                          size="small"
                          type="number"
                          placeholder="Max"
                          value={comparisonFilters.maxAccuracy}
                          onChange={(e) => setComparisonFilters(prev => ({
                            ...prev,
                            maxAccuracy: Number(e.target.value)
                          }))}
                          sx={{ width: 80 }}
                        />
                      </Box>
                    </Box>

                    <Autocomplete
                      multiple
                      size="small"
                      options={uniqueTypes}
                      getOptionLabel={(option) => getModelTypeConfig(option).label}
                      value={comparisonFilters.modelTypes}
                      onChange={(_, newValue) => setComparisonFilters(prev => ({
                        ...prev,
                        modelTypes: newValue
                      }))}
                      renderInput={(params) => <TextField {...params} label="Model Tipleri" />}
                      renderTags={(value, getTagProps) =>
                        value.map((option, index) => (
                          <Chip
                            label={getModelTypeConfig(option).label}
                            size="small"
                            {...getTagProps({ index })}
                          />
                        ))
                      }
                    />

                    <Autocomplete
                      multiple
                      size="small"
                      options={uniqueStatuses}
                      getOptionLabel={(option) => getStatusConfig(option).label}
                      value={comparisonFilters.statusTypes}
                      onChange={(_, newValue) => setComparisonFilters(prev => ({
                        ...prev,
                        statusTypes: newValue
                      }))}
                      renderInput={(params) => <TextField {...params} label="Model Durumları" />}
                      renderTags={(value, getTagProps) =>
                        value.map((option, index) => (
                          <Chip
                            label={getStatusConfig(option).label}
                            size="small"
                            {...getTagProps({ index })}
                          />
                        ))
                      }
                    />

                    <Box>
                      <Typography variant="caption" gutterBottom>Tarih Aralığı</Typography>
                      <Box sx={{ display: 'flex', gap: 1 }}>
                        <TextField
                          size="small"
                          type="date"
                          value={comparisonFilters.dateRange.start}
                          onChange={(e) => setComparisonFilters(prev => ({
                            ...prev,
                            dateRange: { ...prev.dateRange, start: e.target.value }
                          }))}
                          sx={{ width: 140 }}
                        />
                        <TextField
                          size="small"
                          type="date"
                          value={comparisonFilters.dateRange.end}
                          onChange={(e) => setComparisonFilters(prev => ({
                            ...prev,
                            dateRange: { ...prev.dateRange, end: e.target.value }
                          }))}
                          sx={{ width: 140 }}
                        />
                      </Box>
                    </Box>
                  </Box>

                  {/* Hızlı Seçim */}
                  <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={() => selectTopModels('accuracy', 5)}
                    >
                      🥇 En İyi 5 (Accuracy)
                    </Button>
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={() => selectTopModels('precision', 5)}
                    >
                      🔍 En İyi 5 (Precision)
                    </Button>
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={() => selectTopModels('recall', 5)}
                    >
                      📋 En İyi 5 (Recall)
                    </Button>
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={() => selectTopModels('f1_score', 5)}
                    >
                      ⚖️ En İyi 5 (F1-Score)
                    </Button>
                    <Button
                      size="small"
                      variant="outlined"
                      onClick={() => selectTopModels('auc', 5)}
                    >
                      📐 En İyi 5 (AUC)
                    </Button>
                    <Button
                      size="small"
                      variant="outlined"
                      color="error"
                      onClick={clearComparison}
                    >
                      🗑️ Temizle
                    </Button>
                  </Box>

                  {/* Aktif Filtreler Göstergesi */}
                  {(comparisonFilters.minAccuracy > 0 || comparisonFilters.maxAccuracy < 100 ||
                    comparisonFilters.modelTypes.length > 0 || comparisonFilters.statusTypes.length > 0 ||
                    comparisonFilters.dateRange.start || comparisonFilters.dateRange.end) && (
                      <Box sx={{ mt: 2, p: 2, bgcolor: 'info.main', color: 'info.contrastText', borderRadius: 1 }}>
                        <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 'bold' }}>
                          🔍 Aktif Filtreler:
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                          {(comparisonFilters.minAccuracy > 0 || comparisonFilters.maxAccuracy < 100) && (
                            <Chip
                              label={`Accuracy: ${comparisonFilters.minAccuracy}%-${comparisonFilters.maxAccuracy}%`}
                              size="small"
                              sx={{ bgcolor: 'white', color: 'primary.main' }}
                            />
                          )}
                          {comparisonFilters.modelTypes.map(type => (
                            <Chip
                              key={type}
                              label={`Tip: ${getModelTypeConfig(type).label}`}
                              size="small"
                              sx={{ bgcolor: 'white', color: 'primary.main' }}
                            />
                          ))}
                          {comparisonFilters.statusTypes.map(status => (
                            <Chip
                              key={status}
                              label={`Durum: ${getStatusConfig(status).label}`}
                              size="small"
                              sx={{ bgcolor: 'white', color: 'primary.main' }}
                            />
                          ))}
                          {comparisonFilters.dateRange.start && (
                            <Chip
                              label={`Başlangıç: ${comparisonFilters.dateRange.start}`}
                              size="small"
                              sx={{ bgcolor: 'white', color: 'primary.main' }}
                            />
                          )}
                          {comparisonFilters.dateRange.end && (
                            <Chip
                              label={`Bitiş: ${comparisonFilters.dateRange.end}`}
                              size="small"
                              sx={{ bgcolor: 'white', color: 'primary.main' }}
                            />
                          )}
                        </Box>
                      </Box>
                    )}
                </Box>
              </Collapse>
            </CardContent>
          </Card>

          {/* İstatistik Kartları */}
          {getFilteredModelsForComparison().length > 0 && (
            <Box sx={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))',
              gap: 2
            }}>
              {['accuracy', 'precision', 'recall', 'f1_score', 'auc'].map((metric) => {
                const filteredData = getFilteredModelsForComparison();
                const values = filteredData.map(d => d[metric as keyof typeof d] as number);
                const avg = values.length > 0 ? values.reduce((a, b) => a + b, 0) / values.length : 0;
                const min = values.length > 0 ? Math.min(...values) : 0;
                const max = values.length > 0 ? Math.max(...values) : 0;
                const sortedValues = [...values].sort((a, b) => a - b);
                const median = sortedValues.length > 0 ? sortedValues[Math.floor(sortedValues.length / 2)] : 0;
                const std = values.length > 0 ? Math.sqrt(values.reduce((a, b) => a + Math.pow(b - avg, 2), 0) / values.length) : 0;

                const metricConfig = {
                  accuracy: { icon: '🎯', label: 'Accuracy' },
                  precision: { icon: '🔍', label: 'Precision' },
                  recall: { icon: '📋', label: 'Recall' },
                  f1_score: { icon: '⚖️', label: 'F1-Score' },
                  auc: { icon: '📐', label: 'AUC' }
                }[metric]!;

                return (
                  <Card key={metric} variant="outlined">
                    <CardContent sx={{ textAlign: 'center' }}>
                      <Typography variant="subtitle2" gutterBottom sx={{
                        textTransform: 'uppercase',
                        fontSize: '0.75rem',
                        fontWeight: 'bold'
                      }}>
                        {metricConfig.icon} {metricConfig.label}
                      </Typography>
                      <Typography variant="h5" color="primary" sx={{ fontWeight: 'bold', mb: 1 }}>
                        {avg.toFixed(1)}%
                      </Typography>
                      <Box sx={{
                        display: 'grid',
                        gridTemplateColumns: '1fr 1fr',
                        gap: 1,
                        fontSize: '0.75rem'
                      }}>
                        <Typography variant="caption" color="success.main">
                          Min: {min.toFixed(1)}%
                        </Typography>
                        <Typography variant="caption" color="error.main">
                          Max: {max.toFixed(1)}%
                        </Typography>
                        <Typography variant="caption" color="textSecondary">
                          Med: {median.toFixed(1)}%
                        </Typography>
                        <Typography variant="caption" color="textSecondary">
                          Std: {std.toFixed(1)}
                        </Typography>
                      </Box>
                    </CardContent>
                  </Card>
                );
              })}
            </Box>
          )}

          {/* Görselleştirmeler */}
          {comparisonView === 'bar' && (
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  📊 Model Performans Karşılaştırması
                  <Chip label={`${getFilteredModelsForComparison().length} Model`} size="small" variant="outlined" />
                </Typography>
                <ResponsiveContainer width="100%" height={500}>
                  <BarChart data={getFilteredModelsForComparison().slice(0, 20)}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis
                      dataKey="name"
                      angle={-45}
                      textAnchor="end"
                      height={100}
                      interval={0}
                      fontSize={12}
                    />
                    <YAxis domain={[0, 100]} />
                    <RechartsTooltip
                      formatter={(value, name) => [`${Number(value).toFixed(2)}%`, name]}
                      labelFormatter={(label) => `Model: ${label}`}
                    />
                    <Legend />
                    <Bar dataKey="accuracy" fill={METRIC_COLORS.accuracy} name="Accuracy" />
                    <Bar dataKey="precision" fill={METRIC_COLORS.precision} name="Precision" />
                    <Bar dataKey="recall" fill={METRIC_COLORS.recall} name="Recall" />
                    <Bar dataKey="f1_score" fill={METRIC_COLORS.f1Score} name="F1-Score" />
                    <Bar dataKey="auc" fill={METRIC_COLORS.auc} name="AUC" />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          )}

          {comparisonView === 'radar' && (
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  🕸️ Model Radar Karşılaştırması
                </Typography>

                {selectedModelsForComparison.length === 0 && (
                  <Alert severity="info" sx={{ mb: 2 }}>
                    🎯 Model seçimi yapılmadı. En iyi 3 model otomatik olarak gösterilecek.
                  </Alert>
                )}

                {selectedModelsForComparison.length > 0 && (
                  <Box sx={{ mb: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="body2" color="success.main" sx={{ fontWeight: 'medium' }}>
                      ✅ {selectedModelsForComparison.length} model seçildi
                    </Typography>
                    <Button
                      size="small"
                      variant="outlined"
                      color="error"
                      onClick={() => setSelectedModelsForComparison([])}
                      startIcon={<ClearIcon />}
                    >
                      Seçimi Temizle
                    </Button>
                  </Box>
                )}

                <ResponsiveContainer width="100%" height={500}>
                  <RadarChart data={[
                    {
                      metric: 'ACCURACY',
                      ...(() => {
                        const modelsToShow = selectedModelsForComparison.length > 0
                          ? getFilteredModelsForComparison().filter(m => selectedModelsForComparison.includes(m.id))
                          : getFilteredModelsForComparison().slice(0, 3);
                        const result: any = {};
                        modelsToShow.forEach((model, index) => {
                          result[`Model${index + 1}`] = model.accuracy;
                        });
                        return result;
                      })()
                    },
                    {
                      metric: 'PRECISION',
                      ...(() => {
                        const modelsToShow = selectedModelsForComparison.length > 0
                          ? getFilteredModelsForComparison().filter(m => selectedModelsForComparison.includes(m.id))
                          : getFilteredModelsForComparison().slice(0, 3);
                        const result: any = {};
                        modelsToShow.forEach((model, index) => {
                          result[`Model${index + 1}`] = model.precision;
                        });
                        return result;
                      })()
                    },
                    {
                      metric: 'RECALL',
                      ...(() => {
                        const modelsToShow = selectedModelsForComparison.length > 0
                          ? getFilteredModelsForComparison().filter(m => selectedModelsForComparison.includes(m.id))
                          : getFilteredModelsForComparison().slice(0, 3);
                        const result: any = {};
                        modelsToShow.forEach((model, index) => {
                          result[`Model${index + 1}`] = model.recall;
                        });
                        return result;
                      })()
                    },
                    {
                      metric: 'F1-SCORE',
                      ...(() => {
                        const modelsToShow = selectedModelsForComparison.length > 0
                          ? getFilteredModelsForComparison().filter(m => selectedModelsForComparison.includes(m.id))
                          : getFilteredModelsForComparison().slice(0, 3);
                        const result: any = {};
                        modelsToShow.forEach((model, index) => {
                          result[`Model${index + 1}`] = model.f1_score;
                        });
                        return result;
                      })()
                    },
                    {
                      metric: 'AUC',
                      ...(() => {
                        const modelsToShow = selectedModelsForComparison.length > 0
                          ? getFilteredModelsForComparison().filter(m => selectedModelsForComparison.includes(m.id))
                          : getFilteredModelsForComparison().slice(0, 3);
                        const result: any = {};
                        modelsToShow.forEach((model, index) => {
                          result[`Model${index + 1}`] = model.auc;
                        });
                        return result;
                      })()
                    }
                  ]}>
                    <PolarGrid />
                    <PolarAngleAxis dataKey="metric" />
                    <PolarRadiusAxis angle={30} domain={[0, 100]} />
                    <RechartsTooltip />
                    <Legend />
                    {(() => {
                      const modelsToShow = selectedModelsForComparison.length > 0
                        ? getFilteredModelsForComparison().filter(m => selectedModelsForComparison.includes(m.id))
                        : getFilteredModelsForComparison().slice(0, 3);
                      return modelsToShow.map((model, index) => (
                        <Radar
                          key={index}
                          name={model.name.length > 15 ? model.name.substring(0, 15) + '...' : model.name}
                          dataKey={`Model${index + 1}`}
                          stroke={Object.values(METRIC_COLORS)[index % Object.values(METRIC_COLORS).length]}
                          fill={Object.values(METRIC_COLORS)[index % Object.values(METRIC_COLORS).length]}
                          fillOpacity={0.3}
                          strokeWidth={2}
                        />
                      ));
                    })()}
                  </RadarChart>
                </ResponsiveContainer>

                {/* Model Detayları */}
                <Box sx={{ mt: 2 }}>
                  <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 'bold' }}>
                    📊 Karşılaştırılan Modeller:
                  </Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                    {(() => {
                      const modelsToShow = selectedModelsForComparison.length > 0
                        ? getFilteredModelsForComparison().filter(m => selectedModelsForComparison.includes(m.id))
                        : getFilteredModelsForComparison().slice(0, 3);
                      return modelsToShow.map((model, index) => (
                        <Chip
                          key={index}
                          label={`${getModelTypeConfig(model.type).icon} ${model.name}`}
                          size="small"
                          style={{
                            backgroundColor: Object.values(METRIC_COLORS)[index % Object.values(METRIC_COLORS).length],
                            color: 'white'
                          }}
                        />
                      ));
                    })()}
                  </Box>
                </Box>
              </CardContent>
            </Card>
          )}

          {comparisonView === 'line' && (
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  📈 Model Performans Trendi ({getTimeRangeLabel()})
                  <Chip
                    label={`${getFilteredModelsForComparison().length} Model`}
                    size="small"
                    variant="outlined"
                  />
                  {getModelTrendData().length > 0 && (
                    <Chip
                      label={`${getModelTrendData().reduce((sum, day) => sum + day.modelCount, 0)} Eğitim`}
                      size="small"
                      color="success"
                      variant="outlined"
                    />
                  )}
                </Typography>

                {getFilteredModelsForComparison().length === 0 ? (
                  <Alert severity="warning" sx={{ mt: 2 }}>
                    ⚠️ Filtreleme kriterlerinize uygun model bulunamadı. Trend analizi için filtrelerinizi genişletin.
                  </Alert>
                ) : getModelTrendData().length === 0 ? (
                  <Alert severity="info" sx={{ mt: 2 }}>
                    📊 Seçilen zaman aralığında yeterli veri bulunamadı. Daha geniş bir zaman aralığı seçin.
                  </Alert>
                ) : (
                  <>
                    <ResponsiveContainer width="100%" height={400}>
                      <LineChart data={getModelTrendData()}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis
                          dataKey="date"
                          tickFormatter={(date) => new Date(date).toLocaleDateString('tr-TR', {
                            month: 'short',
                            day: 'numeric'
                          })}
                        />
                        <YAxis domain={[0, 100]} />
                        <RechartsTooltip
                          formatter={(value, name) => [
                            `${Number(value).toFixed(1)}%`,
                            name
                          ]}
                          labelFormatter={(date) => {
                            const dayData = getModelTrendData().find(d => d.date === date);
                            return `${new Date(date).toLocaleDateString('tr-TR')} (${dayData?.modelCount || 0} model)`;
                          }}
                        />
                        <Legend />
                        <Line
                          type="monotone"
                          dataKey="accuracy"
                          stroke={METRIC_COLORS.accuracy}
                          name="Accuracy"
                          strokeWidth={3}
                          dot={{ fill: METRIC_COLORS.accuracy, strokeWidth: 2, r: 4 }}
                        />
                        <Line
                          type="monotone"
                          dataKey="precision"
                          stroke={METRIC_COLORS.precision}
                          name="Precision"
                          strokeWidth={3}
                          dot={{ fill: METRIC_COLORS.precision, strokeWidth: 2, r: 4 }}
                        />
                        <Line
                          type="monotone"
                          dataKey="recall"
                          stroke={METRIC_COLORS.recall}
                          name="Recall"
                          strokeWidth={3}
                          dot={{ fill: METRIC_COLORS.recall, strokeWidth: 2, r: 4 }}
                        />
                        <Line
                          type="monotone"
                          dataKey="f1Score"
                          stroke={METRIC_COLORS.f1Score}
                          name="F1-Score"
                          strokeWidth={3}
                          dot={{ fill: METRIC_COLORS.f1Score, strokeWidth: 2, r: 4 }}
                        />
                        <Line
                          type="monotone"
                          dataKey="auc"
                          stroke={METRIC_COLORS.auc}
                          name="AUC"
                          strokeWidth={3}
                          dot={{ fill: METRIC_COLORS.auc, strokeWidth: 2, r: 4 }}
                        />
                      </LineChart>
                    </ResponsiveContainer>
                    <Typography variant="body2" color="textSecondary" sx={{ mt: 2, textAlign: 'center' }}>
                      Grafik, {getTimeRangeLabel().toLowerCase()} için filtrelenmiş model performanslarının ortalamalarını gösterir.
                      {getModelTrendData().length > 0 && ` En son ${getModelTrendData()[getModelTrendData().length - 1].accuracy.toFixed(1)}% accuracy elde edildi.`}
                    </Typography>
                  </>
                )}
              </CardContent>
            </Card>
          )}

          {comparisonView === 'heatmap' && (
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  🔥 Model Performans Isı Haritası
                  <Chip label={`${getHeatmapData(true).length} Model`} size="small" variant="outlined" />
                </Typography>

                {getHeatmapData(true).length === 0 ? (
                  <Alert severity="warning">
                    ⚠️ Filtreleme kriterlerinize uygun model bulunamadı.
                  </Alert>
                ) : (
                  <Box sx={{ overflowX: 'auto' }}>
                    {/* Header */}
                    <Box sx={{ display: 'flex', alignItems: 'center', py: 1, borderBottom: '2px solid #e0e0e0', mb: 1 }}>
                      <Typography variant="body2" sx={{ width: 200, fontSize: 12, fontWeight: 'bold' }}>
                        Model Adı
                      </Typography>
                      {['accuracy', 'precision', 'recall', 'f1_score', 'auc'].map(metric => (
                        <Typography key={metric} variant="caption" sx={{ width: 120, mx: 1, fontWeight: 'bold', textAlign: 'center' }}>
                          {metric.toUpperCase()}
                        </Typography>
                      ))}
                    </Box>

                    {/* Data Rows */}
                    <Box sx={{ minWidth: 900, maxHeight: 600, overflowY: 'auto' }}>
                      {getHeatmapData(true).slice(0, 20).map((model, index) => (
                        <Box key={index} sx={{ display: 'flex', alignItems: 'center', py: 1, '&:hover': { bgcolor: '#f5f5f5' } }}>
                          <Typography variant="body2" sx={{ width: 200, fontSize: 11 }} title={model.fullName}>
                            {model.model}
                          </Typography>
                          {['accuracy', 'precision', 'recall', 'f1_score', 'auc'].map(metric => {
                            const value = model[metric as keyof typeof model] as number || 0;
                            const intensity = Math.min(value / 100, 1);
                            const color = value >= 90 ? '#4caf50' : value >= 75 ? '#ff9800' : value >= 50 ? '#2196f3' : '#f44336';

                            return (
                              <Box key={metric} sx={{ width: 120, mx: 1 }}>
                                <Box
                                  sx={{
                                    height: 24,
                                    bgcolor: color,
                                    opacity: 0.3 + (intensity * 0.7),
                                    borderRadius: 1,
                                    display: 'flex',
                                    alignItems: 'center',
                                    justifyContent: 'center',
                                    border: '1px solid #e0e0e0'
                                  }}
                                >
                                  <Typography variant="caption" sx={{
                                    color: intensity > 0.5 ? 'white' : 'black',
                                    fontWeight: 'bold',
                                    fontSize: '10px'
                                  }}>
                                    {value.toFixed(1)}%
                                  </Typography>
                                </Box>
                              </Box>
                            );
                          })}
                        </Box>
                      ))}
                    </Box>
                  </Box>
                )}
              </CardContent>
            </Card>
          )}

          {comparisonView === 'scatter' && (
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  💫 Precision vs Recall Dağılım Grafiği
                  <Chip label={`${getScatterData(true).length} Model`} size="small" variant="outlined" />
                </Typography>

                {getScatterData(true).length === 0 ? (
                  <Alert severity="warning">
                    ⚠️ Filtreleme kriterlerinize uygun model bulunamadı.
                  </Alert>
                ) : (
                  <>
                    <ResponsiveContainer width="100%" height={500}>
                      <ScatterChart margin={{ top: 20, right: 20, bottom: 60, left: 60 }}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis
                          type="number"
                          dataKey="x"
                          domain={[0, 100]}
                          name="Precision"
                          label={{ value: 'Precision (%)', position: 'insideBottom', offset: -10 }}
                        />
                        <YAxis
                          type="number"
                          dataKey="y"
                          domain={[0, 100]}
                          name="Recall"
                          label={{ value: 'Recall (%)', angle: -90, position: 'insideLeft' }}
                        />
                        <ZAxis type="number" dataKey="z" range={[50, 400]} />
                        <RechartsTooltip
                          formatter={(value, name) => [
                            `${Number(value).toFixed(1)}%`,
                            name === 'x' ? 'Precision' : name === 'y' ? 'Recall' : 'Accuracy'
                          ]}
                          labelFormatter={(_, payload) => {
                            if (payload && payload[0]) {
                              const data = payload[0].payload;
                              return `${data.fullName} (${getModelTypeConfig(data.type).label})`;
                            }
                            return '';
                          }}
                        />
                        <Legend />
                        {/* LightGBM Models */}
                        <Scatter
                          name="LightGBM"
                          data={getScatterData(true).filter(d => d.type === 'LightGBM')}
                          fill="#ff9800"
                        />
                        {/* PCA Models */}
                        <Scatter
                          name="PCA"
                          data={getScatterData(true).filter(d => d.type === 'PCA')}
                          fill="#4caf50"
                        />
                        {/* Ensemble Models */}
                        <Scatter
                          name="Ensemble"
                          data={getScatterData(true).filter(d => d.type === 'Ensemble')}
                          fill="#1976d2"
                        />
                        {/* Other Models */}
                        <Scatter
                          name="Diğer"
                          data={getScatterData(true).filter(d => !['LightGBM', 'PCA', 'Ensemble'].includes(d.type))}
                          fill="#9c27b0"
                        />
                      </ScatterChart>
                    </ResponsiveContainer>

                    {/* Model Distribution Info */}
                    <Box sx={{ mt: 2, display: 'flex', justifyContent: 'center', gap: 3, flexWrap: 'wrap' }}>
                      {['LightGBM', 'PCA', 'Ensemble'].map(modelType => {
                        const count = getScatterData(true).filter(d => d.type === modelType).length;
                        const color = modelType === 'LightGBM' ? '#ff9800' : modelType === 'PCA' ? '#4caf50' : '#1976d2';
                        return count > 0 ? (
                          <Chip
                            key={modelType}
                            label={`${getModelTypeConfig(modelType).icon} ${modelType}: ${count} model`}
                            size="small"
                            sx={{ bgcolor: color, color: 'white' }}
                          />
                        ) : null;
                      })}
                    </Box>
                  </>
                )}
              </CardContent>
            </Card>
          )}
        </Box>
      </TabPanel>

      {/* Model Eğitimi Tab */}
      <TabPanel value={activeTab} index={2}>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
          <ModelTrainingWizard
            onTrainingComplete={(result) => {
              console.log('🎉 Model eğitimi tamamlandı:', result);

              // Training result dialog'unu göster
              setTrainingResult(result);
              setTrainingResultDialog(true);

              showSnackbar(
                `✅ Model "${result?.data?.actualModelName || result?.data?.modelName || 'Yeni Model'}" başarıyla eğitildi!`,
                'success'
              );
              // Model listesini yenile
              loadModels();
            }}
            showSnackbar={showSnackbar}
          />
        </Box>
      </TabPanel>

      {/* Hiperparametre Optimizasyonu Tab */}
      <TabPanel value={activeTab} index={3}>
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
          <HyperparameterTuning />
        </Box>
      </TabPanel>

      {/* Model Detay Dialog */}
      <Dialog
        open={detailDialog}
        onClose={() => setDetailDialog(false)}
        maxWidth="lg"
        fullWidth
        PaperProps={{
          sx: { minHeight: '80vh' }
        }}
      >
        <DialogTitle>
          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
              <ViewIcon color="primary" fontSize="large" />
              <Box>
                <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                  {selectedModel ? getModelProperty(selectedModel, 'modelName') : 'Model Detayları'}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  {selectedModel && `${getModelTypeConfig(getModelProperty(selectedModel, 'type')).icon} ${getModelTypeConfig(getModelProperty(selectedModel, 'type')).label} • v${getModelProperty(selectedModel, 'version')}`}
                </Typography>
              </Box>
            </Box>
            <Chip
              label={selectedModel ? getStatusConfig(getModelProperty(selectedModel, 'status')).label : ''}
              color={selectedModel ? getStatusConfig(getModelProperty(selectedModel, 'status')).color as any : 'default'}
              icon={<span>{selectedModel ? getStatusConfig(getModelProperty(selectedModel, 'status')).icon : ''}</span>}
            />
          </Box>
        </DialogTitle>
        <DialogContent>
          {selectedModel && (
            <Box sx={{ mt: 2 }}>
              {/* Ana Metrikler - Gauge Charts */}
              <Card variant="outlined" sx={{ mb: 3 }}>
                <CardContent>
                  <Typography variant="h6" gutterBottom color="primary" sx={{ mb: 3 }}>
                    🎯 Ana Performans Metrikleri
                  </Typography>
                  <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 3 }}>
                    {/* Accuracy Gauge */}
                    <Box sx={{ textAlign: 'center' }}>
                      <Typography variant="subtitle2" gutterBottom>Accuracy</Typography>
                      <ResponsiveContainer width="100%" height={150}>
                        <PieChart>
                          <Pie
                            data={getGaugeData(getModelMetrics(selectedModel).accuracy, 'Accuracy', '#1976d2')}
                            cx="50%"
                            cy="50%"
                            startAngle={180}
                            endAngle={0}
                            innerRadius={40}
                            outerRadius={70}
                            dataKey="value"
                          >
                            {getGaugeData(getModelMetrics(selectedModel).accuracy, 'Accuracy', '#1976d2').map((entry, index) => (
                              <Cell key={`cell-${index}`} fill={entry.fill} />
                            ))}
                          </Pie>
                        </PieChart>
                      </ResponsiveContainer>
                      <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold', mt: -2 }}>
                        {(getModelMetrics(selectedModel).accuracy * 100).toFixed(2)}%
                      </Typography>
                    </Box>

                    {/* Precision Gauge */}
                    <Box sx={{ textAlign: 'center' }}>
                      <Typography variant="subtitle2" gutterBottom>Precision</Typography>
                      <ResponsiveContainer width="100%" height={150}>
                        <PieChart>
                          <Pie
                            data={getGaugeData(getModelMetrics(selectedModel).precision, 'Precision', '#ff9800')}
                            cx="50%"
                            cy="50%"
                            startAngle={180}
                            endAngle={0}
                            innerRadius={40}
                            outerRadius={70}
                            dataKey="value"
                          >
                            {getGaugeData(getModelMetrics(selectedModel).precision, 'Precision', '#ff9800').map((entry, index) => (
                              <Cell key={`cell-${index}`} fill={entry.fill} />
                            ))}
                          </Pie>
                        </PieChart>
                      </ResponsiveContainer>
                      <Typography variant="h6" color="warning.main" sx={{ fontWeight: 'bold', mt: -2 }}>
                        {(getModelMetrics(selectedModel).precision * 100).toFixed(2)}%
                      </Typography>
                    </Box>

                    {/* Recall Gauge */}
                    <Box sx={{ textAlign: 'center' }}>
                      <Typography variant="subtitle2" gutterBottom>Recall</Typography>
                      <ResponsiveContainer width="100%" height={150}>
                        <PieChart>
                          <Pie
                            data={getGaugeData(getModelMetrics(selectedModel).recall, 'Recall', '#4caf50')}
                            cx="50%"
                            cy="50%"
                            startAngle={180}
                            endAngle={0}
                            innerRadius={40}
                            outerRadius={70}
                            dataKey="value"
                          >
                            {getGaugeData(getModelMetrics(selectedModel).recall, 'Recall', '#4caf50').map((entry, index) => (
                              <Cell key={`cell-${index}`} fill={entry.fill} />
                            ))}
                          </Pie>
                        </PieChart>
                      </ResponsiveContainer>
                      <Typography variant="h6" color="success.main" sx={{ fontWeight: 'bold', mt: -2 }}>
                        {(getModelMetrics(selectedModel).recall * 100).toFixed(2)}%
                      </Typography>
                    </Box>

                    {/* F1-Score Gauge */}
                    <Box sx={{ textAlign: 'center' }}>
                      <Typography variant="subtitle2" gutterBottom>F1-Score</Typography>
                      <ResponsiveContainer width="100%" height={150}>
                        <PieChart>
                          <Pie
                            data={getGaugeData(getModelMetrics(selectedModel).f1_score, 'F1-Score', '#9c27b0')}
                            cx="50%"
                            cy="50%"
                            startAngle={180}
                            endAngle={0}
                            innerRadius={40}
                            outerRadius={70}
                            dataKey="value"
                          >
                            {getGaugeData(getModelMetrics(selectedModel).f1_score, 'F1-Score', '#9c27b0').map((entry, index) => (
                              <Cell key={`cell-${index}`} fill={entry.fill} />
                            ))}
                          </Pie>
                        </PieChart>
                      </ResponsiveContainer>
                      <Typography variant="h6" color="secondary.main" sx={{ fontWeight: 'bold', mt: -2 }}>
                        {(getModelMetrics(selectedModel).f1_score * 100).toFixed(2)}%
                      </Typography>
                    </Box>

                    {/* AUC Gauge */}
                    <Box sx={{ textAlign: 'center' }}>
                      <Typography variant="subtitle2" gutterBottom>AUC</Typography>
                      <ResponsiveContainer width="100%" height={150}>
                        <PieChart>
                          <Pie
                            data={getGaugeData(getModelMetrics(selectedModel).auc, 'AUC', '#00bcd4')}
                            cx="50%"
                            cy="50%"
                            startAngle={180}
                            endAngle={0}
                            innerRadius={40}
                            outerRadius={70}
                            dataKey="value"
                          >
                            {getGaugeData(getModelMetrics(selectedModel).auc, 'AUC', '#00bcd4').map((entry, index) => (
                              <Cell key={`cell-${index}`} fill={entry.fill} />
                            ))}
                          </Pie>
                        </PieChart>
                      </ResponsiveContainer>
                      <Typography variant="h6" sx={{ color: '#00bcd4', fontWeight: 'bold', mt: -2 }}>
                        {(getModelMetrics(selectedModel).auc * 100).toFixed(2)}%
                      </Typography>
                    </Box>
                  </Box>
                </CardContent>
              </Card>

              {/* İlk Satır - ROC Curve ve Precision-Recall Curve */}
              <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap', mb: 3 }}>
                {/* ROC Curve */}
                <Card variant="outlined" sx={{ flex: 1, minWidth: 400 }}>
                  <CardContent>
                    <Typography variant="h6" gutterBottom color="primary">
                      📈 ROC Curve (Simulated)
                    </Typography>
                    <ResponsiveContainer width="100%" height={300}>
                      <LineChart data={getROCCurveData(selectedModel)}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="fpr" label={{ value: 'False Positive Rate (%)', position: 'insideBottom', offset: -10 }} />
                        <YAxis label={{ value: 'True Positive Rate (%)', angle: -90, position: 'insideLeft' }} />
                        <RechartsTooltip
                          formatter={(value, name) => [`${Number(value).toFixed(1)}%`, name === 'tpr' ? 'TPR' : 'FPR']}
                        />
                        <Line
                          type="monotone"
                          dataKey="tpr"
                          stroke="#1976d2"
                          strokeWidth={3}
                          dot={{ fill: '#1976d2', strokeWidth: 2, r: 4 }}
                          name="ROC Curve"
                        />
                        <Line
                          type="monotone"
                          dataKey="fpr"
                          stroke="#ff4444"
                          strokeDasharray="5 5"
                          strokeWidth={1}
                          dot={false}
                          name="Random Classifier"
                        />
                      </LineChart>
                    </ResponsiveContainer>
                    <Typography variant="body2" color="textSecondary" sx={{ mt: 1, textAlign: 'center' }}>
                      AUC: {(getModelMetrics(selectedModel).auc * 100).toFixed(2)}% (Ne kadar yüksek o kadar iyi)
                    </Typography>
                  </CardContent>
                </Card>

                {/* Precision-Recall Curve */}
                <Card variant="outlined" sx={{ flex: 1, minWidth: 400 }}>
                  <CardContent>
                    <Typography variant="h6" gutterBottom color="primary">
                      📊 Precision-Recall Curve
                    </Typography>
                    <ResponsiveContainer width="100%" height={300}>
                      <LineChart data={getPRCurveData(selectedModel)}>
                        <CartesianGrid strokeDasharray="3 3" />
                        <XAxis dataKey="recall" label={{ value: 'Recall (%)', position: 'insideBottom', offset: -10 }} />
                        <YAxis label={{ value: 'Precision (%)', angle: -90, position: 'insideLeft' }} />
                        <RechartsTooltip
                          formatter={(value, name) => [`${Number(value).toFixed(1)}%`, name === 'precision' ? 'Precision' : 'Recall']}
                        />
                        <Line
                          type="monotone"
                          dataKey="precision"
                          stroke="#4caf50"
                          strokeWidth={3}
                          dot={{ fill: '#4caf50', strokeWidth: 2, r: 4 }}
                          name="PR Curve"
                        />
                      </LineChart>
                    </ResponsiveContainer>
                    <Typography variant="body2" color="textSecondary" sx={{ mt: 1, textAlign: 'center' }}>
                      Current Point: Precision {(getModelMetrics(selectedModel).precision * 100).toFixed(1)}%, Recall {(getModelMetrics(selectedModel).recall * 100).toFixed(1)}%
                    </Typography>
                  </CardContent>
                </Card>
              </Box>

              {/* İkinci Satır - Confusion Matrix ve Model Özgü Metrikler */}
              <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap', mb: 3 }}>
                {/* Confusion Matrix */}
                <Card variant="outlined" sx={{ flex: 1, minWidth: 500 }}>
                  <CardContent>
                    <Typography variant="h6" gutterBottom color="primary">
                      🎯 Confusion Matrix (Karmaşa Matrisi)
                    </Typography>

                    {/* Matrix Grid */}
                    <Box sx={{ mb: 3 }}>
                      <Typography variant="subtitle2" gutterBottom sx={{ textAlign: 'center', mb: 2 }}>
                        Tahmin vs Gerçek Sonuçlar
                      </Typography>

                      <Box sx={{
                        display: 'grid',
                        gridTemplateColumns: '1fr 1fr 1fr',
                        gridTemplateRows: '1fr 1fr 1fr',
                        gap: 1,
                        maxWidth: 400,
                        mx: 'auto',
                        textAlign: 'center'
                      }}>
                        {/* Header */}
                        <Box></Box>
                        <Box sx={{ fontWeight: 'bold', p: 1, bgcolor: '#e3f2fd' }}>Gerçek: Normal</Box>
                        <Box sx={{ fontWeight: 'bold', p: 1, bgcolor: '#ffebee' }}>Gerçek: Fraud</Box>

                        {/* Tahmin: Normal */}
                        <Box sx={{ fontWeight: 'bold', p: 1, bgcolor: '#e3f2fd', writingMode: 'vertical-rl' }}>
                          Tahmin: Normal
                        </Box>
                        <Box sx={{
                          p: 2,
                          bgcolor: '#c8e6c9',
                          border: '2px solid #4caf50',
                          borderRadius: 1,
                          fontWeight: 'bold',
                          fontSize: '1.2rem'
                        }}>
                          {getConfusionMatrixData(selectedModel).find(d => d.name.includes('TN'))?.value?.toLocaleString('tr-TR') || '0'}
                          <Typography variant="caption" display="block">TN</Typography>
                        </Box>
                        <Box sx={{
                          p: 2,
                          bgcolor: '#ffccbc',
                          border: '2px solid #ff9800',
                          borderRadius: 1,
                          fontWeight: 'bold',
                          fontSize: '1.2rem'
                        }}>
                          {getConfusionMatrixData(selectedModel).find(d => d.name.includes('FN'))?.value?.toLocaleString('tr-TR') || '0'}
                          <Typography variant="caption" display="block">FN</Typography>
                        </Box>

                        {/* Tahmin: Fraud */}
                        <Box sx={{ fontWeight: 'bold', p: 1, bgcolor: '#ffebee', writingMode: 'vertical-rl' }}>
                          Tahmin: Fraud
                        </Box>
                        <Box sx={{
                          p: 2,
                          bgcolor: '#ffccbc',
                          border: '2px solid #ff9800',
                          borderRadius: 1,
                          fontWeight: 'bold',
                          fontSize: '1.2rem'
                        }}>
                          {getConfusionMatrixData(selectedModel).find(d => d.name.includes('FP'))?.value?.toLocaleString('tr-TR') || '0'}
                          <Typography variant="caption" display="block">FP</Typography>
                        </Box>
                        <Box sx={{
                          p: 2,
                          bgcolor: '#c8e6c9',
                          border: '2px solid #4caf50',
                          borderRadius: 1,
                          fontWeight: 'bold',
                          fontSize: '1.2rem'
                        }}>
                          {getConfusionMatrixData(selectedModel).find(d => d.name.includes('TP'))?.value?.toLocaleString('tr-TR') || '0'}
                          <Typography variant="caption" display="block">TP</Typography>
                        </Box>
                      </Box>
                    </Box>

                    {/* Açıklamalar */}
                    <Box sx={{ mt: 3 }}>
                      <Typography variant="subtitle2" gutterBottom>
                        Açıklamalar:
                      </Typography>
                      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                        {getConfusionMatrixData(selectedModel).map((item, index) => (
                          <Box key={index} sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                            <Box sx={{
                              width: 16,
                              height: 16,
                              bgcolor: item.color,
                              borderRadius: 1
                            }} />
                            <Typography variant="body2">
                              <strong>{item.name}:</strong> {item.description}
                            </Typography>
                          </Box>
                        ))}
                      </Box>
                    </Box>
                  </CardContent>
                </Card>

                {/* Model Özgü Metrikler ve Sistem Bilgileri */}
                <Card variant="outlined" sx={{ flex: 1, minWidth: 500 }}>
                  <CardContent>
                    <Typography variant="h6" gutterBottom color="primary">
                      🔧 {getModelTypeConfig(getModelProperty(selectedModel, 'type')).label} Özgü Metrikler
                    </Typography>

                    {/* Temel Bilgiler */}
                    <Box sx={{ mb: 3 }}>
                      <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 'bold', color: '#666' }}>
                        Model Bilgileri
                      </Typography>
                      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: 2 }}>
                        <Box>
                          <Typography variant="body2" color="textSecondary">Versiyon</Typography>
                          <Typography variant="body1" sx={{ fontWeight: 'medium' }}>
                            {getModelProperty(selectedModel, 'version')}
                          </Typography>
                        </Box>
                        <Box>
                          <Typography variant="body2" color="textSecondary">Durum</Typography>
                          <Chip
                            label={getStatusConfig(getModelProperty(selectedModel, 'status')).label}
                            color={getStatusConfig(getModelProperty(selectedModel, 'status')).color as any}
                            size="small"
                          />
                        </Box>
                        <Box>
                          <Typography variant="body2" color="textSecondary">Oluşturan</Typography>
                          <Typography variant="body1" sx={{ fontWeight: 'medium' }}>
                            {getModelProperty(selectedModel, 'createdBy')}
                          </Typography>
                        </Box>
                        <Box>
                          <Typography variant="body2" color="textSecondary">Eğitim Tarihi</Typography>
                          <Typography variant="body1" sx={{ fontWeight: 'medium' }}>
                            {new Date(getModelProperty(selectedModel, 'trainedAt')).toLocaleDateString('tr-TR')}
                          </Typography>
                        </Box>
                      </Box>
                    </Box>

                    {/* Özgü Metrikler */}
                    <Box sx={{ mb: 3 }}>
                      <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 'bold', color: '#666' }}>
                        İleri Seviye Metrikler
                      </Typography>
                      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                        {getModelSpecificMetrics(selectedModel).map((metric, index) => (
                          <Box key={index} sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 2, bgcolor: '#f8f9fa', borderRadius: 1 }}>
                            <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
                              {metric.label}
                            </Typography>
                            <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                              {metric.format === 'percentage'
                                ? `${(metric.value * 100).toFixed(2)}%`
                                : metric.value.toFixed(4)
                              }
                            </Typography>
                          </Box>
                        ))}
                      </Box>
                    </Box>
                  </CardContent>
                </Card>
              </Box>

              {/* Üçüncü Satır - Konfigürasyon */}
              <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
                {/* Model Konfigürasyonu */}
                <Card variant="outlined" sx={{ flex: 1, minWidth: 400 }}>
                  <CardContent>
                    <Typography variant="h6" gutterBottom color="primary">
                      🔧 Model Konfigürasyonu
                    </Typography>
                    <Paper
                      variant="outlined"
                      sx={{
                        p: 3,
                        backgroundColor: '#fafafa',
                        maxHeight: 500,
                        overflow: 'auto',
                        border: '1px solid #e0e0e0'
                      }}
                    >
                      {renderConfigSection(formatConfigurationSafe(getModelProperty(selectedModel, 'configuration'), getModelProperty(selectedModel, 'type')))}
                    </Paper>

                    {/* Konfigürasyon Özeti */}
                    <Box sx={{ mt: 2, p: 2, bgcolor: '#f0f7ff', borderRadius: 1 }}>
                      <Typography variant="subtitle2" color="primary" gutterBottom>
                        📋 Konfigürasyon Özeti
                      </Typography>
                      <Typography variant="body2" color="textSecondary">
                        {getModelProperty(selectedModel, 'type') === 'Ensemble'
                          ? 'Bu ensemble model, LightGBM ve PCA algoritmalarının weighted average kombinasyonunu kullanır. LightGBM ağırlığı %70, PCA ağırlığı %30 olarak ayarlanmıştır.'
                          : getModelProperty(selectedModel, 'type') === 'LightGBM'
                            ? 'Gradient boosting algoritması kullanarak yüksek performanslı sınıflandırma yapar. Class imbalance için 1:100 ağırlık oranı kullanılmıştır.'
                            : 'Principal Component Analysis ile anomali tespiti yapar. 15 ana bileşen kullanarak %98 varyans korunmaktadır.'
                        }
                      </Typography>
                    </Box>
                  </CardContent>
                </Card>

                {/* Model İstatistikleri ve İyileştirme Önerileri */}
                <Card variant="outlined" sx={{ flex: 1, minWidth: 400 }}>
                  <CardContent>
                    <Typography variant="h6" gutterBottom color="primary">
                      📈 Model İstatistikleri ve Öneriler
                    </Typography>

                    {/* İstatistikler */}
                    <Box sx={{ mb: 3 }}>
                      <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 'bold', color: '#666' }}>
                        Detaylı İstatistikler
                      </Typography>
                      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(120px, 1fr))', gap: 2 }}>
                        <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#e3f2fd', borderRadius: 1 }}>
                          <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                            {new Date(getModelProperty(selectedModel, 'trainedAt')).toLocaleDateString('tr-TR')}
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            Eğitim Tarihi
                          </Typography>
                        </Box>
                        <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#f3e5f5', borderRadius: 1 }}>
                          <Typography variant="h6" color="secondary.main" sx={{ fontWeight: 'bold' }}>
                            v{getModelProperty(selectedModel, 'version')}
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            Model Versiyonu
                          </Typography>
                        </Box>
                        <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#fff3e0', borderRadius: 1 }}>
                          <Typography variant="h6" color="warning.main" sx={{ fontWeight: 'bold' }}>
                            {getStatusConfig(getModelProperty(selectedModel, 'status')).label}
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            Durum
                          </Typography>
                        </Box>
                      </Box>
                    </Box>

                    {/* İyileştirme Önerileri */}
                    <Box>
                      <Typography variant="subtitle2" gutterBottom sx={{ fontWeight: 'bold', color: '#666' }}>
                        🚀 İyileştirme Önerileri
                      </Typography>
                      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                        {getModelMetrics(selectedModel).precision < 0.9 && (
                          <Box sx={{ p: 2, bgcolor: '#fff3e0', borderRadius: 1, borderLeft: '4px solid #ff9800' }}>
                            <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
                              Precision İyileştirmesi
                            </Typography>
                            <Typography variant="caption" color="textSecondary">
                              Precision %{(getModelMetrics(selectedModel).precision * 100).toFixed(1)} - False positive oranını azaltmak için threshold değerini artırabilirsiniz.
                            </Typography>
                          </Box>
                        )}

                        {getModelMetrics(selectedModel).recall < 0.85 && (
                          <Box sx={{ p: 2, bgcolor: '#ffebee', borderRadius: 1, borderLeft: '4px solid #f44336' }}>
                            <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
                              Recall İyileştirmesi
                            </Typography>
                            <Typography variant="caption" color="textSecondary">
                              Recall %{(getModelMetrics(selectedModel).recall * 100).toFixed(1)} - Kaçırılan fraud'ları azaltmak için class weight'leri artırabilirsiniz.
                            </Typography>
                          </Box>
                        )}

                        {getModelMetrics(selectedModel).auc > 0.95 && getModelMetrics(selectedModel).accuracy > 0.99 && (
                          <Box sx={{ p: 2, bgcolor: '#e8f5e8', borderRadius: 1, borderLeft: '4px solid #4caf50' }}>
                            <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
                              Mükemmel Performans
                            </Typography>
                            <Typography variant="caption" color="textSecondary">
                              Model şu anki konfigürasyonuyla optimal performans göstermektedir. Production'a alınabilir.
                            </Typography>
                          </Box>
                        )}

                        <Box sx={{ p: 2, bgcolor: '#e3f2fd', borderRadius: 1, borderLeft: '4px solid #2196f3' }}>
                          <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
                            Monitoring Önerisi
                          </Typography>
                          <Typography variant="caption" color="textSecondary">
                            Model drift'ini takip etmek için haftalık performans raporları oluşturunuz.
                          </Typography>
                        </Box>
                      </Box>
                    </Box>
                  </CardContent>
                </Card>
              </Box>
            </Box>
          )}
        </DialogContent>
        <DialogActions sx={{ p: 3 }}>
          <Button onClick={() => setDetailDialog(false)} size="large" variant="outlined">
            Kapat
          </Button>
          <Box sx={{ display: 'flex', gap: 2 }}>
            {selectedModel && getModelProperty(selectedModel, 'status') === 'Inactive' && (
              <Button
                variant="contained"
                color="success"
                size="large"
                startIcon={<DeployIcon />}
                onClick={() => selectedModel && handleUpdateModelStatus(selectedModel.id, 'Active')}
                disabled={isTraining}
              >
                Aktifleştir
              </Button>
            )}
            {selectedModel && getModelProperty(selectedModel, 'status') === 'Active' && (
              <Button
                variant="contained"
                color="warning"
                size="large"
                startIcon={<SettingsIcon />}
                onClick={() => selectedModel && handleUpdateModelStatus(selectedModel.id, 'Inactive')}
                disabled={isTraining}
              >
                Pasif Yap
              </Button>
            )}
            <Button
              variant="contained"
              size="large"
              startIcon={<DownloadIcon />}
              onClick={() => selectedModel && downloadModelReport(selectedModel)}
            >
              Rapor İndir
            </Button>
          </Box>
        </DialogActions>
      </Dialog>

      {/* Training Dialog */}
      <Dialog open={trainingDialog} onClose={() => !isTraining && setTrainingDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <TrainIcon color="primary" />
            <Typography variant="h6">Yeni Model Eğitimi</Typography>
          </Box>
        </DialogTitle>
        <DialogContent>
          <Box sx={{ mt: 2, display: 'flex', flexDirection: 'column', gap: 3 }}>
            <Alert severity="info">
              Model eğitimi başlatıldığında, sistem mevcut verileri kullanarak yeni bir model oluşturacaktır.
            </Alert>

            <TextField
              label="Model Adı"
              value={newModelConfig.name}
              onChange={(e) => setNewModelConfig(prev => ({ ...prev, name: e.target.value }))}
              fullWidth
              disabled={isTraining}
              helperText="Modeli tanımlayan açıklayıcı bir isim girin"
            />

            <FormControl fullWidth disabled={isTraining}>
              <InputLabel>Model Tipi</InputLabel>
              <Select
                value={newModelConfig.type}
                onChange={(e) => setNewModelConfig(prev => ({ ...prev, type: e.target.value as any }))}
                label="Model Tipi"
              >
                <MenuItem value="LightGBM">
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography>⚡</Typography>
                    <Box>
                      <Typography>LightGBM Classifier</Typography>
                      <Typography variant="caption" color="textSecondary">
                        Hızlı ve yüksek performanslı gradient boosting
                      </Typography>
                    </Box>
                  </Box>
                </MenuItem>
                <MenuItem value="PCA">
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography>📊</Typography>
                    <Box>
                      <Typography>PCA Anomaly Detection</Typography>
                      <Typography variant="caption" color="textSecondary">
                        Boyut azaltma tabanlı anomali tespiti
                      </Typography>
                    </Box>
                  </Box>
                </MenuItem>
                <MenuItem value="Ensemble">
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Typography>🎯</Typography>
                    <Box>
                      <Typography>Ensemble Model</Typography>
                      <Typography variant="caption" color="textSecondary">
                        Birden fazla modelin kombinasyonu
                      </Typography>
                    </Box>
                  </Box>
                </MenuItem>
              </Select>
            </FormControl>

            <TextField
              label="Açıklama"
              value={newModelConfig.description}
              onChange={(e) => setNewModelConfig(prev => ({ ...prev, description: e.target.value }))}
              multiline
              rows={3}
              fullWidth
              disabled={isTraining}
              helperText="Model hakkında detaylı bilgi (opsiyonel)"
            />

            {isTraining && (
              <Box>
                <LinearProgress />
                <Typography variant="body2" sx={{ mt: 1, textAlign: 'center' }}>
                  Model eğitimi devam ediyor...
                </Typography>
              </Box>
            )}
          </Box>
        </DialogContent>
        <DialogActions sx={{ p: 3 }}>
          <Button
            onClick={() => setTrainingDialog(false)}
            disabled={isTraining}
          >
            İptal
          </Button>
          <Button
            variant="contained"
            onClick={handleStartTraining}
            startIcon={isTraining ? null : <TrainIcon />}
            disabled={isTraining || !newModelConfig.name || !newModelConfig.type}
          >
            {isTraining ? 'Eğitim Devam Ediyor...' : 'Eğitimi Başlat'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Training Result Dialog */}
      <Dialog
        open={trainingResultDialog}
        onClose={() => setTrainingResultDialog(false)}
        maxWidth="lg"
        fullWidth
        PaperProps={{ sx: { minHeight: '70vh' } }}
      >
        <DialogTitle>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <SuccessIcon color="success" fontSize="large" />
            <Box>
              <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                🎉 Model Eğitimi Tamamlandı!
              </Typography>
              <Typography variant="body2" color="textSecondary">
                {trainingResult?.data?.modelName || 'Yeni Model'} başarıyla eğitildi
              </Typography>
            </Box>
          </Box>
        </DialogTitle>
        <DialogContent>
          {trainingResult && (
            <Box sx={{ mt: 2 }}>
              {/* Performance Summary */}
              {trainingResult.data?.performanceSummary && (
                <Card variant="outlined" sx={{ mb: 3, bgcolor: '#f8f9fa' }}>
                  <CardContent>
                    <Typography variant="h6" color="primary" gutterBottom>
                      📊 Performans Özeti
                    </Typography>
                    <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 2, mb: 2 }}>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#e8f5e8', borderRadius: 1 }}>
                        <Typography variant="h4" color="success.main" sx={{ fontWeight: 'bold' }}>
                          {trainingResult.data.performanceSummary.modelGrade}
                        </Typography>
                        <Typography variant="body2" color="textSecondary">Model Notu</Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#e3f2fd', borderRadius: 1 }}>
                        <Typography variant="h4" color="primary" sx={{ fontWeight: 'bold' }}>
                          {(trainingResult.data.performanceSummary.overallScore * 100).toFixed(1)}%
                        </Typography>
                        <Typography variant="body2" color="textSecondary">Genel Skor</Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: trainingResult.data.performanceSummary.isGoodModel ? '#e8f5e8' : '#ffebee', borderRadius: 1 }}>
                        <Typography variant="h6" color={trainingResult.data.performanceSummary.isGoodModel ? 'success.main' : 'error.main'} sx={{ fontWeight: 'bold' }}>
                          {trainingResult.data.performanceSummary.isGoodModel ? '✅ ÜRETİME HAZIR' : '⚠️ İYİLEŞTİRME GEREKLİ'}
                        </Typography>
                        <Typography variant="body2" color="textSecondary">Durum</Typography>
                      </Box>
                    </Box>
                    <Alert severity="info" sx={{ mt: 2 }}>
                      <Typography variant="body2">
                        <strong>Değerlendirme:</strong> {trainingResult.data.performanceSummary.primaryWeakness}
                      </Typography>
                    </Alert>
                  </CardContent>
                </Card>
              )}

              {/* Main Metrics for LightGBM/Ensemble */}
              {trainingResult.data?.basicMetrics && (
                <Card variant="outlined" sx={{ mb: 3 }}>
                  <CardContent>
                    <Typography variant="h6" color="primary" gutterBottom>
                      🎯 Temel Performans Metrikleri
                    </Typography>
                    <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: 2 }}>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#e3f2fd', borderRadius: 1 }}>
                        <Typography variant="h5" color="primary" sx={{ fontWeight: 'bold' }}>
                          {(trainingResult.data.basicMetrics.accuracy * 100).toFixed(2)}%
                        </Typography>
                        <Typography variant="body2" color="textSecondary">Accuracy</Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#fff3e0', borderRadius: 1 }}>
                        <Typography variant="h5" color="warning.main" sx={{ fontWeight: 'bold' }}>
                          {(trainingResult.data.basicMetrics.precision * 100).toFixed(2)}%
                        </Typography>
                        <Typography variant="body2" color="textSecondary">Precision</Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#e8f5e8', borderRadius: 1 }}>
                        <Typography variant="h5" color="success.main" sx={{ fontWeight: 'bold' }}>
                          {(trainingResult.data.basicMetrics.recall * 100).toFixed(2)}%
                        </Typography>
                        <Typography variant="body2" color="textSecondary">Recall</Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#f3e5f5', borderRadius: 1 }}>
                        <Typography variant="h5" color="secondary.main" sx={{ fontWeight: 'bold' }}>
                          {(trainingResult.data.basicMetrics.f1Score * 100).toFixed(2)}%
                        </Typography>
                        <Typography variant="body2" color="textSecondary">F1-Score</Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#e0f2f1', borderRadius: 1 }}>
                        <Typography variant="h5" sx={{ color: '#00bcd4', fontWeight: 'bold' }}>
                          {(trainingResult.data.basicMetrics.auc * 100).toFixed(2)}%
                        </Typography>
                        <Typography variant="body2" color="textSecondary">AUC</Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#fce4ec', borderRadius: 1 }}>
                        <Typography variant="h5" color="error.main" sx={{ fontWeight: 'bold' }}>
                          {trainingResult.data.basicMetrics.aucpr ? (trainingResult.data.basicMetrics.aucpr * 100).toFixed(2) : (trainingResult.data.basicMetrics.auc * 100).toFixed(2)}%
                        </Typography>
                        <Typography variant="body2" color="textSecondary">
                          {trainingResult.data.basicMetrics.aucpr ? 'AUC-PR' : 'AUC-PR (est.)'}
                        </Typography>
                      </Box>
                    </Box>
                  </CardContent>
                </Card>
              )}

              {/* Main Metrics for PCA/Isolation Forest */}
              {trainingResult.data?.metrics && !trainingResult.data?.basicMetrics && (
                <Card variant="outlined" sx={{ mb: 3 }}>
                  <CardContent>
                    <Typography variant="h6" color="primary" gutterBottom>
                      🎯 Anomali Tespit Metrikleri
                    </Typography>
                    <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(150px, 1fr))', gap: 2, mb: 3 }}>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#e3f2fd', borderRadius: 1 }}>
                        <Typography variant="h5" color="primary" sx={{ fontWeight: 'bold' }}>
                          {(trainingResult.data.metrics.accuracy * 100).toFixed(2)}%
                        </Typography>
                        <Typography variant="body2" color="textSecondary">Accuracy</Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#fff3e0', borderRadius: 1 }}>
                        <Typography variant="h5" color="warning.main" sx={{ fontWeight: 'bold' }}>
                          {(trainingResult.data.metrics.precision * 100).toFixed(2)}%
                        </Typography>
                        <Typography variant="body2" color="textSecondary">Precision</Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#e8f5e8', borderRadius: 1 }}>
                        <Typography variant="h5" color="success.main" sx={{ fontWeight: 'bold' }}>
                          {(trainingResult.data.metrics.recall * 100).toFixed(2)}%
                        </Typography>
                        <Typography variant="body2" color="textSecondary">Recall</Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#f3e5f5', borderRadius: 1 }}>
                        <Typography variant="h5" color="secondary.main" sx={{ fontWeight: 'bold' }}>
                          {(trainingResult.data.metrics.f1_score * 100).toFixed(2)}%
                        </Typography>
                        <Typography variant="body2" color="textSecondary">F1-Score</Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#e0f2f1', borderRadius: 1 }}>
                        <Typography variant="h5" sx={{ color: '#00bcd4', fontWeight: 'bold' }}>
                          {(trainingResult.data.metrics.auc * 100).toFixed(2)}%
                        </Typography>
                        <Typography variant="body2" color="textSecondary">AUC</Typography>
                      </Box>
                      <Box sx={{ textAlign: 'center', p: 2, bgcolor: '#fce4ec', borderRadius: 1 }}>
                        <Typography variant="h5" color="error.main" sx={{ fontWeight: 'bold' }}>
                          {(trainingResult.data.metrics.auc_pr * 100).toFixed(2)}%
                        </Typography>
                        <Typography variant="body2" color="textSecondary">AUC-PR</Typography>
                      </Box>
                    </Box>

                    {/* Model-specific metrics */}
                    {trainingResult.data.metrics.explained_variance_ratio && (
                      <Box>
                        <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 'bold', mt: 2 }}>
                          🔬 PCA Özgü Metrikler
                        </Typography>
                        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 2 }}>
                          <Box sx={{ p: 2, bgcolor: '#f8f9fa', borderRadius: 1 }}>
                            <Typography variant="body2" color="textSecondary">Explained Variance</Typography>
                            <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                              {(trainingResult.data.metrics.explained_variance_ratio * 100).toFixed(1)}%
                            </Typography>
                          </Box>
                          <Box sx={{ p: 2, bgcolor: '#f8f9fa', borderRadius: 1 }}>
                            <Typography variant="body2" color="textSecondary">Components</Typography>
                            <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                              {trainingResult.data.metrics.n_components}
                            </Typography>
                          </Box>
                          <Box sx={{ p: 2, bgcolor: '#f8f9fa', borderRadius: 1 }}>
                            <Typography variant="body2" color="textSecondary">Reconstruction Error (Mean)</Typography>
                            <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                              {trainingResult.data.metrics.reconstruction_error_mean?.toFixed(4) || 'N/A'}
                            </Typography>
                          </Box>
                        </Box>
                      </Box>
                    )}

                    {trainingResult.data.metrics.mean_anomaly_score && (
                      <Box>
                        <Typography variant="subtitle1" gutterBottom sx={{ fontWeight: 'bold', mt: 2 }}>
                          🌲 Isolation Forest Özgü Metrikler
                        </Typography>
                        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 2 }}>
                          <Box sx={{ p: 2, bgcolor: '#f8f9fa', borderRadius: 1 }}>
                            <Typography variant="body2" color="textSecondary">Mean Anomaly Score</Typography>
                            <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                              {trainingResult.data.metrics.mean_anomaly_score?.toFixed(4)}
                            </Typography>
                          </Box>
                          <Box sx={{ p: 2, bgcolor: '#f8f9fa', borderRadius: 1 }}>
                            <Typography variant="body2" color="textSecondary">Std Anomaly Score</Typography>
                            <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                              {trainingResult.data.metrics.std_anomaly_score?.toFixed(4)}
                            </Typography>
                          </Box>
                          <Box sx={{ p: 2, bgcolor: '#f8f9fa', borderRadius: 1 }}>
                            <Typography variant="body2" color="textSecondary">Contamination</Typography>
                            <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                              {(trainingResult.data.metrics.contamination * 100).toFixed(1)}%
                            </Typography>
                          </Box>
                          <Box sx={{ p: 2, bgcolor: '#f8f9fa', borderRadius: 1 }}>
                            <Typography variant="body2" color="textSecondary">N Estimators</Typography>
                            <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                              {trainingResult.data.metrics.n_estimators}
                            </Typography>
                          </Box>
                        </Box>
                      </Box>
                    )}
                  </CardContent>
                </Card>
              )}

              {/* Confusion Matrix for all models */}
              {trainingResult.data?.confusionMatrix && (
                <Card variant="outlined" sx={{ mb: 3 }}>
                  <CardContent>
                    <Typography variant="h6" color="primary" gutterBottom>
                      🎯 Confusion Matrix
                    </Typography>
                    <Box sx={{
                      display: 'grid',
                      gridTemplateColumns: '1fr 1fr 1fr',
                      gridTemplateRows: '1fr 1fr 1fr',
                      gap: 1,
                      maxWidth: 500,
                      mx: 'auto',
                      textAlign: 'center'
                    }}>
                      <Box></Box>
                      <Box sx={{ fontWeight: 'bold', p: 1, bgcolor: '#e3f2fd' }}>Gerçek: Normal</Box>
                      <Box sx={{ fontWeight: 'bold', p: 1, bgcolor: '#ffebee' }}>Gerçek: Fraud</Box>

                      <Box sx={{ fontWeight: 'bold', p: 1, bgcolor: '#e3f2fd', writingMode: 'vertical-rl' }}>
                        Tahmin: Normal
                      </Box>
                      <Box sx={{
                        p: 2,
                        bgcolor: '#c8e6c9',
                        border: '2px solid #4caf50',
                        borderRadius: 1,
                        fontWeight: 'bold',
                        fontSize: '1.2rem'
                      }}>
                        {trainingResult.data.confusionMatrix.trueNegative.toLocaleString('tr-TR')}
                        <Typography variant="caption" display="block">TN</Typography>
                      </Box>
                      <Box sx={{
                        p: 2,
                        bgcolor: '#ffccbc',
                        border: '2px solid #ff9800',
                        borderRadius: 1,
                        fontWeight: 'bold',
                        fontSize: '1.2rem'
                      }}>
                        {trainingResult.data.confusionMatrix.falseNegative.toLocaleString('tr-TR')}
                        <Typography variant="caption" display="block">FN</Typography>
                      </Box>

                      <Box sx={{ fontWeight: 'bold', p: 1, bgcolor: '#ffebee', writingMode: 'vertical-rl' }}>
                        Tahmin: Fraud
                      </Box>
                      <Box sx={{
                        p: 2,
                        bgcolor: '#ffccbc',
                        border: '2px solid #ff9800',
                        borderRadius: 1,
                        fontWeight: 'bold',
                        fontSize: '1.2rem'
                      }}>
                        {trainingResult.data.confusionMatrix.falsePositive.toLocaleString('tr-TR')}
                        <Typography variant="caption" display="block">FP</Typography>
                      </Box>
                      <Box sx={{
                        p: 2,
                        bgcolor: '#c8e6c9',
                        border: '2px solid #4caf50',
                        borderRadius: 1,
                        fontWeight: 'bold',
                        fontSize: '1.2rem'
                      }}>
                        {trainingResult.data.confusionMatrix.truePositive.toLocaleString('tr-TR')}
                        <Typography variant="caption" display="block">TP</Typography>
                      </Box>
                    </Box>
                  </CardContent>
                </Card>
              )}

              {/* Confusion Matrix for PCA/Isolation Forest */}
              {trainingResult.data?.metrics && !trainingResult.data?.confusionMatrix && (
                <Card variant="outlined" sx={{ mb: 3 }}>
                  <CardContent>
                    <Typography variant="h6" color="primary" gutterBottom>
                      🎯 Confusion Matrix
                    </Typography>
                    <Box sx={{
                      display: 'grid',
                      gridTemplateColumns: '1fr 1fr 1fr',
                      gridTemplateRows: '1fr 1fr 1fr',
                      gap: 1,
                      maxWidth: 500,
                      mx: 'auto',
                      textAlign: 'center'
                    }}>
                      <Box></Box>
                      <Box sx={{ fontWeight: 'bold', p: 1, bgcolor: '#e3f2fd' }}>Gerçek: Normal</Box>
                      <Box sx={{ fontWeight: 'bold', p: 1, bgcolor: '#ffebee' }}>Gerçek: Fraud</Box>

                      <Box sx={{ fontWeight: 'bold', p: 1, bgcolor: '#e3f2fd', writingMode: 'vertical-rl' }}>
                        Tahmin: Normal
                      </Box>
                      <Box sx={{
                        p: 2,
                        bgcolor: '#c8e6c9',
                        border: '2px solid #4caf50',
                        borderRadius: 1,
                        fontWeight: 'bold',
                        fontSize: '1.2rem'
                      }}>
                        {trainingResult.data.metrics.true_negative?.toLocaleString('tr-TR') || '0'}
                        <Typography variant="caption" display="block">TN</Typography>
                      </Box>
                      <Box sx={{
                        p: 2,
                        bgcolor: '#ffccbc',
                        border: '2px solid #ff9800',
                        borderRadius: 1,
                        fontWeight: 'bold',
                        fontSize: '1.2rem'
                      }}>
                        {trainingResult.data.metrics.false_negative?.toLocaleString('tr-TR') || '0'}
                        <Typography variant="caption" display="block">FN</Typography>
                      </Box>

                      <Box sx={{ fontWeight: 'bold', p: 1, bgcolor: '#ffebee', writingMode: 'vertical-rl' }}>
                        Tahmin: Fraud
                      </Box>
                      <Box sx={{
                        p: 2,
                        bgcolor: '#ffccbc',
                        border: '2px solid #ff9800',
                        borderRadius: 1,
                        fontWeight: 'bold',
                        fontSize: '1.2rem'
                      }}>
                        {trainingResult.data.metrics.false_positive?.toLocaleString('tr-TR') || '0'}
                        <Typography variant="caption" display="block">FP</Typography>
                      </Box>
                      <Box sx={{
                        p: 2,
                        bgcolor: '#c8e6c9',
                        border: '2px solid #4caf50',
                        borderRadius: 1,
                        fontWeight: 'bold',
                        fontSize: '1.2rem'
                      }}>
                        {trainingResult.data.metrics.true_positive?.toLocaleString('tr-TR') || '0'}
                        <Typography variant="caption" display="block">TP</Typography>
                      </Box>
                    </Box>
                  </CardContent>
                </Card>
              )}

              {/* Extended Metrics */}
              {trainingResult.data?.extendedMetrics && (
                <Card variant="outlined" sx={{ mb: 3 }}>
                  <CardContent>
                    <Typography variant="h6" color="primary" gutterBottom>
                      📈 Gelişmiş Metrikler
                    </Typography>
                    <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: 2 }}>
                      <Box sx={{ p: 2, bgcolor: '#f8f9fa', borderRadius: 1 }}>
                        <Typography variant="body2" color="textSecondary">Balanced Accuracy</Typography>
                        <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                          {(trainingResult.data.extendedMetrics.balancedAccuracy * 100).toFixed(2)}%
                        </Typography>
                      </Box>
                      <Box sx={{ p: 2, bgcolor: '#f8f9fa', borderRadius: 1 }}>
                        <Typography variant="body2" color="textSecondary">Matthews Correlation</Typography>
                        <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                          {trainingResult.data.extendedMetrics.matthewsCorrCoef.toFixed(4)}
                        </Typography>
                      </Box>
                      <Box sx={{ p: 2, bgcolor: '#f8f9fa', borderRadius: 1 }}>
                        <Typography variant="body2" color="textSecondary">Sensitivity</Typography>
                        <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                          {(trainingResult.data.extendedMetrics.sensitivity * 100).toFixed(2)}%
                        </Typography>
                      </Box>
                      <Box sx={{ p: 2, bgcolor: '#f8f9fa', borderRadius: 1 }}>
                        <Typography variant="body2" color="textSecondary">Specificity</Typography>
                        <Typography variant="h6" color="primary" sx={{ fontWeight: 'bold' }}>
                          {(trainingResult.data.extendedMetrics.specificity * 100).toFixed(2)}%
                        </Typography>
                      </Box>
                    </Box>
                  </CardContent>
                </Card>
              )}

              {/* Recommendations */}
              {trainingResult.data?.recommendations && trainingResult.data.recommendations.length > 0 && (
                <Card variant="outlined" sx={{ mb: 3 }}>
                  <CardContent>
                    <Typography variant="h6" color="primary" gutterBottom>
                      💡 İyileştirme Önerileri
                    </Typography>
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                      {trainingResult.data.recommendations.map((recommendation: string, index: number) => (
                        <Alert
                          key={index}
                          severity="warning"
                          variant="outlined"
                          sx={{
                            borderLeft: '4px solid #ff9800',
                            '& .MuiAlert-icon': { color: '#ff9800' }
                          }}
                        >
                          <Typography variant="body2">
                            <strong>Öneri {index + 1}:</strong> {recommendation}
                          </Typography>
                        </Alert>
                      ))}
                    </Box>
                  </CardContent>
                </Card>
              )}

              {/* Training Time and Model Info */}
              {(trainingResult.data?.trainingTime || trainingResult.data?.modelId || trainingResult.data?.modelPath) && (
                <Card variant="outlined">
                  <CardContent>
                    <Typography variant="h6" color="primary" gutterBottom>
                      ⏱️ Eğitim Bilgileri
                    </Typography>
                    <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(300px, 1fr))', gap: 2 }}>
                      {trainingResult.data.modelName && (
                        <Typography variant="body1">
                          <strong>Model İsmi:</strong> {trainingResult.data.modelName}
                        </Typography>
                      )}
                      {trainingResult.data.trainingTime && (
                        <Typography variant="body1">
                          <strong>Eğitim Süresi:</strong> {(trainingResult.data.trainingTime / 1000).toFixed(2)} saniye
                        </Typography>
                      )}
                      {trainingResult.data.modelId && (
                        <Typography variant="body1">
                          <strong>Model ID:</strong> {trainingResult.data.modelId}
                        </Typography>
                      )}
                      {trainingResult.data.modelPath && (
                        <Typography variant="body1">
                          <strong>Model Yolu:</strong> {trainingResult.data.modelPath.split('/').pop()}
                        </Typography>
                      )}
                      {trainingResult.data.modelType && (
                        <Typography variant="body1">
                          <strong>Model Tipi:</strong> {trainingResult.data.modelType}
                        </Typography>
                      )}
                      {trainingResult.data.message && (
                        <Typography variant="body1" color="success.main">
                          <strong>Durum:</strong> {trainingResult.data.message}
                        </Typography>
                      )}
                    </Box>
                  </CardContent>
                </Card>
              )}
            </Box>
          )}
        </DialogContent>
        <DialogActions sx={{ p: 3 }}>
          <Button onClick={() => setTrainingResultDialog(false)} variant="contained" size="large">
            Kapat
          </Button>
        </DialogActions>
      </Dialog>

      {/* Snackbar */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={() => setSnackbar(prev => ({ ...prev, open: false }))}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
      >
        <Alert
          severity={snackbar.severity}
          onClose={() => setSnackbar(prev => ({ ...prev, open: false }))}
          variant="filled"
          elevation={6}
          icon={
            snackbar.severity === 'success' ? <SuccessIcon /> :
              snackbar.severity === 'error' ? <ErrorIcon /> :
                snackbar.severity === 'warning' ? <WarningIcon /> :
                  <InfoIcon />
          }
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Box>
  );
};

export default ModelManagement;