import React, { useState, useEffect, useCallback } from 'react';
import {
  Box,
  Card,
  CardContent,
  Typography,
  Chip,
  Modal,
  Button,
  TextField,
  Alert,
  CircularProgress,
  Accordion,
  AccordionSummary,
  AccordionDetails,
  List,
  ListItem,
  ListItemText,
  ListItemIcon,
  Divider,
  LinearProgress,
  Stack,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Fab,
  Tooltip,
  TablePagination,
  IconButton,
  Grid,
  Stepper,
  Step,
  StepLabel,
  StepContent,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  FormHelperText,
  StepIcon,
  Avatar,
  Collapse
} from '@mui/material';
import {
  Add as AddIcon,
  PlayArrow as PlayIcon,
  Refresh as RefreshIcon,
  Assessment as AssessmentIcon,
  Security as SecurityIcon,
  Computer as ComputerIcon,
  LocationOn as LocationIcon,
  AccountCircle as AccountIcon,
  Timeline as TimelineIcon,
  SmartToy as SmartToyIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Warning as WarningIcon,
  ExpandMore as ExpandMoreIcon,
  Visibility as VisibilityIcon,
  Search as SearchIcon,
  FilterList as FilterIcon,
  Speed as SpeedIcon,
  Analytics as AnalyticsIcon,
  NetworkCheck as NetworkCheckIcon,
  DeviceHub as DeviceHubIcon,
  People as PeopleIcon,
  Psychology as PsychologyIcon,
  Shield as ShieldIcon,
  Assignment as AssignmentIcon,
  AutoFixHigh as AutoFixHighIcon,
  Science as ScienceIcon,
  DataUsage as DataUsageIcon
} from '@mui/icons-material';
import { FraudDetectionAPI, ApiResponse, TransactionType, SessionCheckRequest, ModelEvaluationRequest, ComprehensiveFraudCheckRequest, TransactionCheckRequest, AccountAccessCheckRequest, IpCheckRequest, DeviceCheckRequest, FraudDetectionResponse, AnalysisResult, ModelEvaluationResponse, ComprehensiveFraudCheckResponse } from '../services/api';

// ==================== TYPES ====================
interface Transaction {
  id: string;
  transactionId: string;
  userId: string;
  amount: number;
  merchantId: string;
  type: 'Purchase' | 'Withdrawal' | 'Transfer' | 'Deposit' | 'CreditCard';
  status: 'pending' | 'analyzing' | 'completed' | 'failed';
  transactionTime: string;
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
    additionalInfo?: any;
  };
  // Backend response fields
  riskScore?: number;
  riskLevel?: string;
  triggeredRuleCount?: number;
  fraudProbability?: number;
  anomalyScore?: number;
  decision?: string;
  analyzedAt?: string;
  analysisStatus?: string;
  riskFactors?: RiskFactor[];
}

interface FraudAnalysisStep {
  id: string;
  name: string;
  description: string;
  icon: React.ReactNode;
  color: string;
  status: 'waiting' | 'running' | 'completed' | 'failed' | 'skipped';
  result?: any;
  error?: string;
  duration?: number;
}

interface ContextAnalysis {
  type: string;
  status: 'waiting' | 'running' | 'completed' | 'failed';
  riskScore: number;
  riskLevel: 'Low' | 'Medium' | 'High' | 'Critical';
  result?: any;
  error?: string;
  triggeredRules: string[];
  recommendations: string[];
}

interface RiskEvaluation {
  overallRiskScore: number;
  overallRiskLevel: 'Low' | 'Medium' | 'High' | 'Critical';
  fraudProbability: number;
  anomalyScore: number;
  decision: 'Approved' | 'Blocked' | 'Review';
  riskFactors: RiskFactor[];
  mlAnalysis: MLAnalysis | null;
  appliedActions: string[];
  totalTriggeredRules: number;
  processingTime: number;
}

interface RiskFactor {
  id: string;
  code: string;
  description: string;
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  confidence: number;
  source: string;
  ruleId: string;
  detectedAt: string;
  metadata: Record<string, any>;
  actionTaken?: string;
  type?: string;
}

interface MLAnalysis {
  primaryModel: string;
  modelVersion: string;
  features: Record<string, number>;
  featureImportance: Record<string, number>;
  confidence: number;
  ensembleUsed: boolean;
  processingTime: number;
  modelMetrics: {
    accuracy: number;
    precision: number;
    recall: number;
    f1Score: number;
  };
}

interface TransactionForm {
  userId: string;
  amount: number;
  merchantId: string;
  type: Transaction['type'];
  location: {
    country: string;
    city: string;
    latitude?: number;
    longitude?: number;
  };
  deviceInfo: {
    deviceId: string;
    deviceType: string;
    ipAddress: string;
    userAgent: string;
    additionalInfo?: any;
  };
  additionalDataRequest?: any;
}

// ==================== CONSTANTS ====================
const FRAUD_ANALYSIS_STEPS: FraudAnalysisStep[] = [
  {
    id: 'initial-analysis',
    name: 'İşlem Analizi',
    description: 'Temel işlem analizi ve risk değerlendirmesi',
    icon: <AssessmentIcon />,
    color: '#1976d2',
    status: 'waiting'
  },
  {
    id: 'account-context',
    name: 'Hesap Kontrolü',
    description: 'Hesap güvenliği ve erişim kontrolü',
    icon: <AccountIcon />,
    color: '#388e3c',
    status: 'waiting'
  },
  {
    id: 'ip-context',
    name: 'IP Kontrolü',
    description: 'IP adresi güvenlik analizi',
    icon: <NetworkCheckIcon />,
    color: '#f57c00',
    status: 'waiting'
  },
  {
    id: 'device-context',
    name: 'Cihaz Kontrolü',
    description: 'Cihaz güvenliği ve fingerprint analizi',
    icon: <DeviceHubIcon />,
    color: '#d32f2f',
    status: 'waiting'
  },
  {
    id: 'session-context',
    name: 'Oturum Kontrolü',
    description: 'Kullanıcı davranış kalıpları analizi',
    icon: <TimelineIcon />,
    color: '#0288d1',
    status: 'waiting'
  }
];

const COUNTRIES = [
  { code: 'TR', name: 'Türkiye', cities: ['Istanbul', 'Ankara', 'Izmir', 'Bursa', 'Antalya'] },
  { code: 'US', name: 'ABD', cities: ['New York', 'Los Angeles', 'Chicago', 'Houston', 'Phoenix'] },
  { code: 'GB', name: 'İngiltere', cities: ['London', 'Manchester', 'Birmingham', 'Liverpool', 'Leeds'] },
  { code: 'DE', name: 'Almanya', cities: ['Berlin', 'Munich', 'Hamburg', 'Cologne', 'Frankfurt'] },
  { code: 'FR', name: 'Fransa', cities: ['Paris', 'Lyon', 'Marseille', 'Toulouse', 'Nice'] }
];

const MERCHANTS = [
  'MERCHANT_001', 'MERCHANT_AMAZON', 'MERCHANT_TRENDYOL', 'MERCHANT_HEPSIBURADA',
  'MERCHANT_GITTIGIDIYOR', 'MERCHANT_N11', 'MERCHANT_VATAN', 'MERCHANT_MEDIAMARKT'
];

const DEVICE_TYPES = ['Mobile', 'Desktop', 'Tablet', 'Unknown'];

const USER_AGENTS = [
  'Mozilla/5.0 (iPhone; CPU iPhone OS 14_7_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.2 Mobile/15E148 Safari/604.1',
  'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
  'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
  'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36',
  'Mozilla/5.0 (iPad; CPU OS 14_7_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.2 Mobile/15E148 Safari/604.1'
];

// ==================== HELPER FUNCTIONS ====================
const generateRandomIP = (): string => {
  const getRandomByte = () => Math.floor(Math.random() * 255) + 1;
  return `${getRandomByte()}.${getRandomByte()}.${getRandomByte()}.${getRandomByte()}`;
};

const generateDeviceId = (): string => {
  return `device_${Math.random().toString(36).substring(2, 10)}`;
};

const generateUserId = (): string => {
  // UUID format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
  const uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
  return uuid;
};

const generateSessionId = (): string => {
  // UUID format: xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
  const uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
  return uuid;
};

const generateMerchantId = (): string => {
  // MERCHANT_xxxxxxxx format
  const randomId = Math.random().toString(36).substring(2, 10);
  return `MERCHANT_${randomId}`;
};

const generateVFactors = (): Record<string, number> => {
  const factors: Record<string, number> = {};
  const vNumbers = [1, 2, 3, 4, 5, 10, 14, 16, 17, 20, 21, 22, 23, 24, 25, 26, 27, 28];

  vNumbers.forEach(num => {
    factors[`V${num}`] = (Math.random() - 0.5) * 6; // -3 ile 3 arası random değer
  });

  return factors;
};

// Risk score objesini string'e çeviren helper fonksiyon
const getRiskScoreValue = (riskScore: any): number => {
  if (typeof riskScore === 'number') {
    return riskScore;
  }
  if (typeof riskScore === 'object' && riskScore !== null) {
    // Yeni RiskScore objesi yapısı
    if (riskScore.value !== undefined) {
      return riskScore.value;
    }
    // Eski yapı için fallback
    return riskScore.score || riskScore.value || 0;
  }
  return 0;
};

// Risk level'ı string'e çeviren helper fonksiyon
const getRiskLevelValue = (riskScore: any): string => {
  let level = '';
  if (typeof riskScore === 'string') {
    level = riskScore;
  }
  else if (typeof riskScore === 'object' && riskScore !== null) {
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
};

const getRiskColor = (level: string) => {
  switch (level?.toLowerCase()) {
    case 'critical':
    case 'kritik': return '#d32f2f';
    case 'high':
    case 'yüksek': return '#f57c00';
    case 'medium':
    case 'orta': return '#fbc02d';
    case 'low':
    case 'düşük': return '#388e3c';
    default: return '#757575';
  }
};

const getStatusColor = (status: string) => {
  switch (status?.toLowerCase()) {
    case 'completed': return '#4caf50';
    case 'running': return '#2196f3';
    case 'failed': return '#f44336';
    case 'waiting': return '#ff9800';
    case 'skipped': return '#9e9e9e';
    default: return '#757575';
  }
};

// ==================== MAIN COMPONENT ====================
const TransactionManagement: React.FC = () => {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [currentTransaction, setCurrentTransaction] = useState<Transaction | null>(null);
  const [analysisSteps, setAnalysisSteps] = useState<FraudAnalysisStep[]>(FRAUD_ANALYSIS_STEPS);
  const [currentStepIndex, setCurrentStepIndex] = useState(0);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [analysisResults, setAnalysisResults] = useState<Record<string, any>>({});
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showAnalysisModal, setShowAnalysisModal] = useState(false);

  // Pagination state
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  // Search and filter state
  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const [filterType, setFilterType] = useState<string>('all');
  const [filterAmount, setFilterAmount] = useState<string>('all');
  const [sortBy, setSortBy] = useState<string>('transactionTime');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');

  // Detail modal state
  const [detailData, setDetailData] = useState<{
    transaction?: any;
    analysisResult?: any;
    riskFactors?: any[];
    riskEvaluations?: any[];
  }>({});
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);

  // Detail data değiştiğinde console.log
  useEffect(() => {
    if (Object.keys(detailData).length > 0) {
      console.log('🔍 Detail Data in Modal:', detailData);
      console.log('🔍 AnalysisResult in Modal:', detailData.analysisResult);
      console.log('🔍 RiskFactors in Modal:', detailData.riskFactors);
    }
  }, [detailData]);

  // Transaction form state with smart defaults
  const [formData, setFormData] = useState<TransactionForm>({
    userId: generateUserId(),
    amount: 4400.00,
    merchantId: generateMerchantId(),
    type: 'Purchase',
    location: {
      country: 'TR',
      city: 'Istanbul',
      latitude: 41.0082,
      longitude: 28.9784
    },
    deviceInfo: {
      deviceId: generateDeviceId(),
      deviceType: 'Mobile',
      ipAddress: generateRandomIP(),
      userAgent: USER_AGENTS[0],
      additionalInfo: {
        OS: 'iOS 14.7.1',
        Model: 'iPhone 12'
      }
    }
  });

  // ==================== EFFECTS ====================
  useEffect(() => {
    loadTransactions();
  }, [page, rowsPerPage]);

  // Auto-generate smart defaults
  useEffect(() => {
    const timer = setTimeout(() => {
      setFormData(prev => ({
        ...prev,
        userId: generateUserId(),
        merchantId: generateMerchantId(),
        deviceInfo: {
          ...prev.deviceInfo,
          deviceId: generateDeviceId(),
          ipAddress: generateRandomIP()
        }
      }));
    }, 2000);

    return () => clearTimeout(timer);
  }, []);

  // ==================== TEST METHODS ====================
  const testEndpoint = async (endpoint: string, data: any) => {
    try {
      console.log(`🧪 Testing ${endpoint} with data:`, data);
      const response = await FraudDetectionAPI.testEndpoint(endpoint, data);
      console.log(`✅ ${endpoint} success:`, response);
      return response;
    } catch (error) {
      console.error(`❌ ${endpoint} failed:`, error);
      return null;
    }
  };

  const testAllEndpoints = async () => {
    console.log('🧪 Testing working endpoints with minimal data...');

    // Test check-account-access - UUID format accountId kullan
    const accountId = generateUserId();
    await testEndpoint('/api/FraudDetection/check-account-access', {
      accountId: accountId,
      username: `user_${Math.random().toString(36).substring(2, 8)}`,
      accessDate: new Date().toISOString(),
      ipAddress: '192.168.1.1',
      countryCode: 'TR',
      city: 'Istanbul',
      deviceId: `device_${Math.random().toString(36).substring(2, 8)}`,
      isTrustedDevice: true,
      uniqueIpCount24h: 2,
      uniqueCountryCount24h: 1,
      isSuccessful: true,
      failedLoginAttempts: 0,
      typicalAccessHours: [9, 10, 11, 12, 13, 14, 15, 16, 17, 18],
      typicalAccessDays: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
      typicalCountries: ['TR'],
      additionalData: {
        Browser: 'Chrome',
        OperatingSystem: 'Windows 10'
      }
    });

    // Test check-ip - IP kontrolü
    await testEndpoint('/api/FraudDetection/check-ip', {
      ipAddress: '192.168.1.1',
      countryCode: 'TR',
      city: 'Istanbul',
      ispAsn: 'AS12345 ISP Provider',
      reputationScore: 80,
      isBlacklisted: false,
      blacklistNotes: '',
      isDatacenterOrProxy: false,
      networkType: 'Residential',
      uniqueAccountCount10m: 1,
      uniqueAccountCount1h: 2,
      uniqueAccountCount24h: 10,
      failedLoginCount10m: 0,
      additionalData: {
        LastSeenDate: new Date().toISOString()
      }
    });

    // Test check-device - Cihaz kontrolü
    await testEndpoint('/api/FraudDetection/check-device', {
      deviceId: `device_${Math.random().toString(36).substring(2, 8)}`,
      deviceType: 'Desktop',
      operatingSystem: 'Windows 10',
      browser: 'Chrome',
      ipAddress: '192.168.1.1',
      countryCode: 'TR',
      isEmulator: false,
      isJailbroken: false,
      isRooted: false,
      firstSeenDate: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
      lastSeenDate: new Date().toISOString(),
      uniqueAccountCount24h: 1,
      uniqueIpCount24h: 2,
      additionalData: {
        DeviceModel: 'Desktop',
        AppVersion: '1.0.1'
      }
    });

    // Test check-session - UUID format sessionId kullan
    const sessionId = generateSessionId();
    await testEndpoint('/api/FraudDetection/check-session', {
      sessionId: sessionId,
      accountId: generateUserId(),
      startTime: new Date(Date.now() - 30 * 60 * 1000).toISOString(), // 30 dakika önce
      lastActivityTime: new Date().toISOString(),
      durationMinutes: 30,
      ipAddress: '192.168.1.1',
      deviceId: `device_${Math.random().toString(36).substring(2, 8)}`,
      userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36',
      rapidNavigationCount: 5,
      additionalData: {
        PageViews: 10,
        LastUrl: "/account/summary"
      }
    });
  };

  // ==================== HANDLERS ====================
  const loadTransactions = async () => {
    try {
      setIsLoading(true);
      setError(null);

      const response = await FraudDetectionAPI.getTransactions(rowsPerPage, page * rowsPerPage);

      if (response.success && response.data) {
        const responseData = response.data as any;

        if (responseData && typeof responseData === 'object' && 'data' in responseData && Array.isArray(responseData.data)) {
          const mappedTransactions = responseData.data.map(mapBackendTransaction);
          setTransactions(mappedTransactions);
          setTotalCount(responseData.total || 0);
        } else if (Array.isArray(responseData)) {
          setTransactions(responseData.map(mapBackendTransaction));
          setTotalCount(responseData.length);
        } else {
          setTransactions([]);
          setTotalCount(0);
        }
      } else {
        loadMockTransactions();
      }
    } catch (err) {
      console.error('❌ İşlem yükleme hatası:', err);
      setError('İşlemler yüklenirken hata oluştu');
      loadMockTransactions();
    } finally {
      setIsLoading(false);
    }
  };

  const mapBackendTransaction = (backendTransaction: any): Transaction => {
    return {
      id: backendTransaction.id || backendTransaction.transactionId || Math.random().toString(36).substring(2, 10),
      transactionId: backendTransaction.transactionId || backendTransaction.id || Math.random().toString(36).substring(2, 10),
      userId: backendTransaction.userId || backendTransaction.accountId || Math.random().toString(36).substring(2, 10),
      amount: backendTransaction.amount || 0,
      merchantId: backendTransaction.merchantId || 'Unknown',
      type: mapBackendTransactionType(backendTransaction.type),
      status: mapBackendStatus(backendTransaction.status),
      transactionTime: backendTransaction.transactionTime || backendTransaction.timestamp || new Date().toISOString(),
      ipAddress: backendTransaction.ipAddress || backendTransaction.location?.ipAddress || '0.0.0.0',
      deviceId: backendTransaction.deviceId || backendTransaction.deviceInfo?.deviceId || Math.random().toString(36).substring(2, 10),
      location: {
        country: backendTransaction.location?.country || backendTransaction.countryCode || 'Unknown',
        city: backendTransaction.location?.city || 'Unknown',
        latitude: backendTransaction.location?.latitude,
        longitude: backendTransaction.location?.longitude
      },
      deviceInfo: {
        deviceType: backendTransaction.deviceInfo?.deviceType || backendTransaction.deviceType || 'Unknown',
        userAgent: backendTransaction.deviceInfo?.userAgent || 'Unknown',
        additionalInfo: backendTransaction.deviceInfo?.additionalInfo
      },
      // Risk score parsing - yeni RiskScore objesi yapısı
      riskScore: backendTransaction.riskScore ?
        (typeof backendTransaction.riskScore === 'object' ?
          backendTransaction.riskScore.score : backendTransaction.riskScore) : undefined,
      riskLevel: backendTransaction.riskScore ?
        (typeof backendTransaction.riskScore === 'object' ?
          backendTransaction.riskScore.level : backendTransaction.riskLevel) : undefined,
      triggeredRuleCount: backendTransaction.triggeredRuleCount || backendTransaction.totalRuleCount || 0,
      fraudProbability: backendTransaction.fraudProbability,
      anomalyScore: backendTransaction.anomalyScore,
      decision: backendTransaction.decision,
      analyzedAt: backendTransaction.analyzedAt,
      analysisStatus: backendTransaction.status,
      riskFactors: backendTransaction.riskFactors?.map((rf: any) => ({
        id: rf.id || Math.random().toString(36).substring(2, 10),
        code: rf.code || rf.type || 'Unknown',
        description: rf.description || '',
        severity: rf.severity || 'Medium',
        confidence: rf.confidence || 0,
        source: rf.source || 'Unknown',
        ruleId: rf.ruleId || rf.relatedRuleId || '',
        detectedAt: rf.detectedAt || new Date().toISOString(),
        metadata: rf.metadata || {}
      })) || []
    };
  };

  const mapBackendTransactionType = (type: string): Transaction['type'] => {
    switch (type?.toLowerCase()) {
      case 'purchase': return 'Purchase';
      case 'withdrawal': return 'Withdrawal';
      case 'transfer': return 'Transfer';
      case 'deposit': return 'Deposit';
      case 'creditcard': return 'CreditCard';
      default: return 'Purchase';
    }
  };

  const mapBackendStatus = (status: string): Transaction['status'] => {
    switch (status?.toLowerCase()) {
      case 'pending': return 'pending';
      case 'analyzing': return 'analyzing';
      case 'completed': return 'completed';
      case 'failed': return 'failed';
      case 'approved': return 'completed';
      case 'requiresreview': return 'analyzing';
      case 'blocked': return 'failed';
      default: return 'pending';
    }
  };

  const loadMockTransactions = () => {
    const mockTransactions: Transaction[] = [
      {
        id: '1',
        transactionId: 'TXN_001',
        userId: 'USER_001',
        amount: 1500.00,
        merchantId: 'MERCHANT_001',
        type: 'Purchase',
        status: 'completed',
        transactionTime: new Date().toISOString(),
        ipAddress: '192.168.1.100',
        deviceId: 'DEVICE_001',
        location: { country: 'TR', city: 'Istanbul' },
        deviceInfo: { deviceType: 'Mobile', userAgent: 'Mozilla/5.0' }
      }
    ];
    setTransactions(mockTransactions);
  };

  const handleCreateTransaction = () => {
    // Smart defaults with auto-generation
    setFormData({
      userId: generateUserId(),
      amount: Math.floor(Math.random() * 5000) + 1000, // 1000-6000 arası
      merchantId: generateMerchantId(),
      type: 'Purchase',
      location: {
        country: 'TR',
        city: 'Istanbul',
        latitude: 41.0082,
        longitude: 28.9784
      },
      deviceInfo: {
        deviceId: generateDeviceId(),
        deviceType: DEVICE_TYPES[Math.floor(Math.random() * DEVICE_TYPES.length)],
        ipAddress: generateRandomIP(),
        userAgent: USER_AGENTS[Math.floor(Math.random() * USER_AGENTS.length)],
        additionalInfo: {
          OS: 'iOS 14.7.1',
          Model: 'iPhone 12'
        }
      }
    });
    setShowCreateModal(true);
  };

  const handleGenerateField = (field: string) => {
    setFormData(prev => {
      const newData = { ...prev };

      switch (field) {
        case 'userId':
          newData.userId = generateUserId();
          break;
        case 'deviceId':
          newData.deviceInfo.deviceId = generateDeviceId();
          break;
        case 'ipAddress':
          newData.deviceInfo.ipAddress = generateRandomIP();
          break;
        case 'userAgent':
          newData.deviceInfo.userAgent = USER_AGENTS[Math.floor(Math.random() * USER_AGENTS.length)];
          break;
        case 'merchantId':
          newData.merchantId = generateMerchantId();
          break;
        case 'amount':
          newData.amount = Math.floor(Math.random() * 5000) + 1000;
          break;
      }

      return newData;
    });
  };

  const handleChangePage = (event: unknown, newPage: number) => {
    setPage(newPage);
  };

  const handleChangeRowsPerPage = (event: React.ChangeEvent<HTMLInputElement>) => {
    setRowsPerPage(parseInt(event.target.value, 10));
    setPage(0);
  };

  const getStatusColor = (status: string) => {
    switch (status?.toLowerCase()) {
      case 'completed': return 'success';
      case 'analyzing': return 'warning';
      case 'failed': return 'error';
      case 'pending': return 'default';
      case 'approved': return 'success';
      case 'requiresreview': return 'warning';
      case 'blocked': return 'error';
      default: return 'default';
    }
  };

  const getStatusText = (status: string) => {
    switch (status?.toLowerCase()) {
      case 'completed': return 'Tamamlandı';
      case 'analyzing': return 'Analiz Ediliyor';
      case 'failed': return 'Başarısız';
      case 'pending': return 'Beklemede';
      case 'approved': return 'Onaylandı';
      case 'requiresreview': return 'İnceleme Gerekli';
      case 'blocked': return 'Engellendi';
      default: return 'Bilinmiyor';
    }
  };

  const getTypeText = (type: string) => {
    switch (type?.toLowerCase()) {
      case 'purchase': return 'Satın Alma';
      case 'withdrawal': return 'Para Çekme';
      case 'transfer': return 'Transfer';
      case 'deposit': return 'Para Yatırma';
      case 'creditcard': return 'Kredi Kartı';
      default: return 'Bilinmiyor';
    }
  };

  const getTypeIcon = (type: string) => {
    switch (type?.toLowerCase()) {
      case 'purchase': return '💳';
      case 'withdrawal': return '💸';
      case 'transfer': return '🔄';
      case 'deposit': return '💰';
      case 'creditcard': return '💳';
      default: return '❓';
    }
  };

  const getAmountRange = (amount: number) => {
    if (amount < 100) return '0-100₺';
    if (amount < 500) return '100-500₺';
    if (amount < 1000) return '500-1000₺';
    if (amount < 5000) return '1000-5000₺';
    if (amount < 10000) return '5000-10000₺';
    return '10000₺+';
  };

  const handleStartAnalysis = async (transaction: Transaction) => {
    setCurrentTransaction(transaction);
    setAnalysisSteps(FRAUD_ANALYSIS_STEPS.map(step => ({ ...step, status: 'waiting' as const })));
    setCurrentStepIndex(0);
    setAnalysisResults({});
    setIsAnalyzing(true);
    setShowAnalysisModal(true);

    // İlk analizi başlat
    await runAnalysisStep(0, transaction);
  };

  const runAnalysisStep = async (stepIndex: number, transaction: Transaction) => {
    if (stepIndex >= analysisSteps.length) return;

    const step = analysisSteps[stepIndex];

    // Step'i running yap
    setAnalysisSteps(prev => prev.map((s, i) =>
      i === stepIndex ? { ...s, status: 'running' } : s
    ));

    try {
      const startTime = Date.now();
      let response: ApiResponse<any>;

      switch (step.id) {
        case 'initial-analysis':
          // İşlem analizi - doğru endpoint: /api/FraudDetection/analyze
          response = await FraudDetectionAPI.analyzeTransaction({
            userId: transaction.userId,
            amount: transaction.amount,
            merchantId: transaction.merchantId,
            type: transaction.type === 'Purchase' ? 1 :
              transaction.type === 'Withdrawal' ? 2 :
                transaction.type === 'Transfer' ? 3 :
                  transaction.type === 'Deposit' ? 4 : 1,
            location: {
              latitude: transaction.location.latitude || 41.0082,
              longitude: transaction.location.longitude || 28.9784,
              country: transaction.location.country,
              city: transaction.location.city
            },
            deviceInfo: {
              deviceId: transaction.deviceId,
              deviceType: transaction.deviceInfo.deviceType,
              ipAddress: transaction.ipAddress,
              userAgent: transaction.deviceInfo.userAgent,
              additionalInfo: transaction.deviceInfo.additionalInfo || {
                OS: transaction.deviceInfo.userAgent.includes('iPhone') ? 'iOS 14.7.1' :
                  transaction.deviceInfo.userAgent.includes('Android') ? 'Android 11' :
                    transaction.deviceInfo.userAgent.includes('Windows') ? 'Windows 10' : 'Unknown',
                Model: transaction.deviceInfo.deviceType === 'Mobile' ? 'iPhone 12' : 'Desktop'
              }
            },
            additionalDataRequest: {
              cardType: "VISA",
              cardBin: "411111",
              cardLast4: "1234",
              cardExpiryMonth: 12,
              cardExpiryYear: 2025,
              bankName: "XYZ Bank",
              bankCountry: transaction.location.country,
              vFactors: generateVFactors(),
              daysSinceFirstTransaction: Math.floor(Math.random() * 365) + 30,
              transactionVelocity24h: Math.floor(Math.random() * 20) + 1,
              averageTransactionAmount: transaction.amount * (Math.random() * 0.5 + 0.7),
              isNewPaymentMethod: Math.random() > 0.7,
              isInternational: transaction.location.country !== 'TR',
              customValues: {
                Time: Math.floor(Math.random() * 86400).toString(),
                RecurringPayment: Math.random() > 0.8 ? "true" : "false",
                HasRecentRefund: Math.random() > 0.9 ? "true" : "false"
              }
            }
          });
          break;

        case 'account-context':
          // Hesap bağlam kontrolü - UUID format accountId kullan
          const accountId = generateUserId();
          const accountRequest: AccountAccessCheckRequest = {
            accountId: accountId,
            username: `user_${Math.random().toString(36).substring(2, 8)}`,
            accessDate: new Date().toISOString(),
            ipAddress: transaction.ipAddress,
            countryCode: transaction.location.country,
            city: transaction.location.city,
            deviceId: transaction.deviceId,
            isTrustedDevice: Math.random() > 0.3,
            uniqueIpCount24h: Math.floor(Math.random() * 5) + 1,
            uniqueCountryCount24h: Math.random() > 0.9 ? 2 : 1,
            isSuccessful: true,
            failedLoginAttempts: Math.floor(Math.random() * 3),
            typicalAccessHours: [9, 10, 11, 12, 13, 14, 15, 16, 17, 18],
            typicalAccessDays: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"],
            typicalCountries: [transaction.location.country],
            additionalData: {
              Browser: transaction.deviceInfo.userAgent.includes('Chrome') ? 'Chrome' :
                transaction.deviceInfo.userAgent.includes('Safari') ? 'Safari' :
                  transaction.deviceInfo.userAgent.includes('Firefox') ? 'Firefox' : 'Unknown',
              OperatingSystem: transaction.deviceInfo.userAgent.includes('Windows') ? 'Windows' :
                transaction.deviceInfo.userAgent.includes('Mac') ? 'macOS' :
                  transaction.deviceInfo.userAgent.includes('Linux') ? 'Linux' :
                    transaction.deviceInfo.userAgent.includes('iPhone') ? 'iOS' :
                      transaction.deviceInfo.userAgent.includes('Android') ? 'Android' : 'Unknown'
            }
          };
          response = await FraudDetectionAPI.checkAccountContext(accountRequest);
          break;

        case 'ip-context':
          // IP bağlam kontrolü
          const ipRequest: IpCheckRequest = {
            ipAddress: transaction.ipAddress,
            countryCode: transaction.location.country,
            city: transaction.location.city,
            ispAsn: `AS${Math.floor(Math.random() * 99999) + 1000} ISP Provider`,
            reputationScore: Math.floor(Math.random() * 41) + 60, // 60-100 arası
            isBlacklisted: Math.random() < 0.05, // %5 şans
            blacklistNotes: "",
            isDatacenterOrProxy: Math.random() < 0.1, // %10 şans
            networkType: Math.random() > 0.8 ? "Business" : "Residential",
            uniqueAccountCount10m: Math.floor(Math.random() * 3) + 1,
            uniqueAccountCount1h: Math.floor(Math.random() * 10) + 1,
            uniqueAccountCount24h: Math.floor(Math.random() * 50) + 5,
            failedLoginCount10m: Math.floor(Math.random() * 2),
            additionalData: {
              LastSeenDate: new Date(Date.now() - Math.random() * 24 * 60 * 60 * 1000).toISOString()
            }
          };
          response = await FraudDetectionAPI.checkIpContext(ipRequest);
          break;

        case 'device-context':
          // Cihaz bağlam kontrolü
          const deviceRequest: DeviceCheckRequest = {
            deviceId: transaction.deviceId,
            deviceType: transaction.deviceInfo.deviceType,
            operatingSystem: transaction.deviceInfo.userAgent.includes('iPhone') ? 'iOS 14.7.1' :
              transaction.deviceInfo.userAgent.includes('Android') ? 'Android 11' :
                transaction.deviceInfo.userAgent.includes('Windows') ? 'Windows 10' :
                  transaction.deviceInfo.userAgent.includes('Mac') ? 'macOS 12' : 'Unknown',
            browser: transaction.deviceInfo.userAgent.includes('Chrome') ? 'Chrome' :
              transaction.deviceInfo.userAgent.includes('Safari') ? 'Safari' :
                transaction.deviceInfo.userAgent.includes('Firefox') ? 'Firefox' : 'Unknown',
            ipAddress: transaction.ipAddress,
            countryCode: transaction.location.country,
            isEmulator: Math.random() < 0.02, // %2 şans
            isJailbroken: Math.random() < 0.05, // %5 şans
            isRooted: Math.random() < 0.03, // %3 şans
            firstSeenDate: new Date(Date.now() - Math.random() * 90 * 24 * 60 * 60 * 1000).toISOString(),
            lastSeenDate: new Date().toISOString(),
            uniqueAccountCount24h: Math.floor(Math.random() * 3) + 1,
            uniqueIpCount24h: Math.floor(Math.random() * 5) + 1,
            additionalData: {
              DeviceModel: transaction.deviceInfo.additionalInfo?.Model ||
                (transaction.deviceInfo.deviceType === 'Mobile' ? 'iPhone 12' : 'Desktop'),
              AppVersion: `1.0.${Math.floor(Math.random() * 10) + 1}`
            }
          };
          response = await FraudDetectionAPI.checkDeviceContext(deviceRequest);
          break;

        case 'session-context':
          // Oturum bağlam kontrolü - UUID format sessionId kullan
          const sessionId = generateSessionId();
          const sessionRequest: SessionCheckRequest = {
            sessionId: sessionId,
            accountId: generateUserId(),
            startTime: new Date(Date.now() - 30 * 60 * 1000).toISOString(), // 30 dakika önce
            lastActivityTime: new Date().toISOString(),
            durationMinutes: 30,
            ipAddress: transaction.ipAddress,
            deviceId: transaction.deviceId,
            userAgent: transaction.deviceInfo.userAgent,
            rapidNavigationCount: 5,
            additionalData: {
              PageViews: 10,
              LastUrl: "/account/summary"
            }
          };
          response = await FraudDetectionAPI.checkSessionContext(sessionRequest);
          break;

        default:
          throw new Error(`Bilinmeyen analiz adımı: ${step.id}`);
      }

      const duration = Date.now() - startTime;

      // Response'u parse et ve normalize et
      let parsedResult = response.data;

      // Eğer response.data içinde data varsa (nested response)
      if (response.data && typeof response.data === 'object' && 'data' in response.data) {
        parsedResult = response.data.data;
      }

      // Response'u normalize et
      const normalizedResult = normalizeAnalysisResponse(parsedResult, step.id);

      // Step'i completed yap
      setAnalysisSteps(prev => prev.map((s, i) =>
        i === stepIndex ? {
          ...s,
          status: 'completed',
          result: normalizedResult,
          duration
        } : s
      ));

      // Sonucu kaydet
      setAnalysisResults(prev => ({
        ...prev,
        [step.id]: normalizedResult
      }));

      // Bir sonraki step'e geç
      if (stepIndex < analysisSteps.length - 1) {
        setCurrentStepIndex(stepIndex + 1);
        // Kısa bekleme süresi
        setTimeout(() => {
          runAnalysisStep(stepIndex + 1, transaction);
        }, 1000);
      } else {
        // Tüm analizler tamamlandı
        setIsAnalyzing(false);
      }

    } catch (error) {
      console.error(`❌ ${step.name} hatası:`, error);

      // Detaylı hata bilgisi
      let errorMessage = 'Bilinmeyen hata';
      if (error instanceof Error) {
        errorMessage = error.message;
      }

      // Axios hatası ise response detayları
      if ((error as any)?.response) {
        const axiosError = error as any;
        console.log(`🔍 ${step.name} - Detaylı Hata:`, {
          status: axiosError.response?.status,
          statusText: axiosError.response?.statusText,
          data: axiosError.response?.data,
          config: {
            url: axiosError.config?.url,
            method: axiosError.config?.method,
            data: axiosError.config?.data
          }
        });

        if (axiosError.response?.data?.title) {
          errorMessage = `${axiosError.response.status}: ${axiosError.response.data.title}`;
        } else if (axiosError.response?.data?.message) {
          errorMessage = axiosError.response.data.message;
        } else {
          errorMessage = `HTTP ${axiosError.response?.status}: ${axiosError.response?.statusText}`;
        }

        // Validation errors varsa göster
        if (axiosError.response?.data?.errors) {
          console.log(`📋 ${step.name} - Validation Errors:`, axiosError.response.data.errors);
          errorMessage += ' - Validation hatasi (console bakin)';
        }
      }

      // Step'i failed yap
      setAnalysisSteps(prev => prev.map((s, i) =>
        i === stepIndex ? {
          ...s,
          status: 'failed',
          error: errorMessage
        } : s
      ));

      // Sonraki step'e geç (hata olsa bile devam et)
      if (stepIndex < analysisSteps.length - 1) {
        setCurrentStepIndex(stepIndex + 1);
        setTimeout(() => {
          runAnalysisStep(stepIndex + 1, transaction);
        }, 1000);
      } else {
        setIsAnalyzing(false);
      }
    }
  };

  // Response'u normalize eden helper fonksiyon
  const normalizeAnalysisResponse = (response: any, stepId: string) => {
    if (!response) return null;

    console.log(`🔍 Normalizing response for ${stepId}:`, response);

    // Temel alanları normalize et
    const normalized = {
      // Risk bilgileri - farklı response formatlarını handle et
      riskScore: 0,
      riskLevel: 'Unknown',
      fraudProbability: 0,
      anomalyScore: 0,

      // Karar bilgileri - farklı decision formatlarını handle et
      decision: 'Unknown',
      resultType: 'Unknown',

      // Kural bilgileri
      triggeredRuleCount: 0,
      triggeredRules: [],

      // Aksiyon bilgileri
      appliedActions: [],

      // Risk faktörleri
      riskFactors: [],

      // Mesaj bilgileri
      resultMessage: '',

      // Durum bilgileri
      status: 'Unknown',
      isSuccess: false,

      // Zaman bilgileri
      processedAt: new Date().toISOString(),
      processingTime: 0,

      // Context-specific bilgiler
      context: stepId,

      // Raw response (debug için)
      rawResponse: response
    };

    // Risk score parsing - yeni RiskScore objesi yapısı
    if (response.riskScore) {
      if (typeof response.riskScore === 'object' && response.riskScore !== null) {
        // Yeni RiskScore objesi yapısı
        normalized.riskScore = response.riskScore.score || 0;
        normalized.riskLevel = response.riskScore.level || 'Bilinmiyor';
      } else if (typeof response.riskScore === 'number') {
        normalized.riskScore = response.riskScore;
        // Risk level'ı score'a göre hesapla
        if (normalized.riskScore >= 0.9) {
          normalized.riskLevel = 'Kritik';
        } else if (normalized.riskScore >= 0.75) {
          normalized.riskLevel = 'Yüksek';
        } else if (normalized.riskScore >= 0.5) {
          normalized.riskLevel = 'Orta';
        } else {
          normalized.riskLevel = 'Düşük';
        }
      }
    }

    // Fraud probability parsing
    if (response.fraudProbability !== undefined) {
      normalized.fraudProbability = typeof response.fraudProbability === 'number' ?
        response.fraudProbability : 0;
    }

    // Anomaly score parsing
    if (response.anomalyScore !== undefined) {
      normalized.anomalyScore = typeof response.anomalyScore === 'number' ?
        response.anomalyScore : 0;
    }

    // Decision parsing
    if (response.decision) {
      normalized.decision = response.decision;
    } else if (response.resultType) {
      normalized.decision = response.resultType;
    }

    // Result type parsing
    if (response.resultType) {
      normalized.resultType = response.resultType;
    }

    // Status parsing
    if (response.status) {
      normalized.status = response.status;
    }

    // Success parsing
    normalized.isSuccess = response.isSuccess !== undefined ? response.isSuccess :
      response.decision === 'Approve' || response.decision === 'Approved' ||
      response.resultType === 'Approved' || response.result === 'Success' ||
      response.status === 'Success' || response.outcome === 'Success' ||
      response.decision === 'Allow';

    // Triggered rules parsing
    if (response.triggeredRules && Array.isArray(response.triggeredRules)) {
      normalized.triggeredRules = response.triggeredRules.map((rule: any) => ({
        ruleId: rule.ruleId || rule.id || rule.ruleCode || 'Unknown',
        ruleName: rule.ruleName || rule.name || rule.description || 'Unknown',
        ruleCode: rule.ruleCode || rule.code || rule.type || 'Unknown',
        isTriggered: rule.isTriggered !== undefined ? rule.isTriggered : true,
        confidence: rule.confidence || rule.score || 0,
        actions: rule.actions || rule.recommendedActions || [],
        context: rule.context || stepId
      }));
    }

    // Applied actions parsing
    if (response.appliedActions && Array.isArray(response.appliedActions)) {
      normalized.appliedActions = response.appliedActions.map((action: any) =>
        typeof action === 'string' ? action : action.action || action.name || 'Unknown'
      );
    } else if (response.actions && Array.isArray(response.actions)) {
      normalized.appliedActions = response.actions.map((action: any) =>
        typeof action === 'string' ? action : action.action || action.name || 'Unknown'
      );
    }

    // Result message parsing
    if (response.resultMessage) {
      normalized.resultMessage = response.resultMessage;
    } else if (response.message) {
      normalized.resultMessage = response.message;
    }

    // Processing time parsing
    if (response.processingTime !== undefined) {
      normalized.processingTime = response.processingTime;
    } else if (response.duration !== undefined) {
      normalized.processingTime = response.duration;
    }

    // Processed at parsing
    if (response.processedAt) {
      normalized.processedAt = response.processedAt;
    } else if (response.analyzedAt) {
      normalized.processedAt = response.analyzedAt;
    } else if (response.timestamp) {
      normalized.processedAt = response.timestamp;
    }

    // Triggered rule count parsing
    if (response.triggeredRuleCount !== undefined) {
      normalized.triggeredRuleCount = response.triggeredRuleCount;
    } else if (response.triggeredRules && Array.isArray(response.triggeredRules)) {
      normalized.triggeredRuleCount = response.triggeredRules.length;
    }

    // Risk factors parsing - ana analiz sonucu için
    if (response.riskFactors && Array.isArray(response.riskFactors)) {
      normalized.riskFactors = response.riskFactors.map((factor: any) => ({
        id: factor.id || Math.random().toString(36).substring(2, 10),
        code: factor.code || factor.type || 'Unknown',
        description: factor.description || '',
        severity: factor.severity || 'Medium',
        confidence: factor.confidence || 0,
        source: factor.source || 'Unknown',
        ruleId: factor.ruleId || factor.relatedRuleId || '',
        detectedAt: factor.detectedAt || new Date().toISOString(),
        metadata: factor.metadata || {}
      }));
    }

    console.log(`✅ Normalized response for ${stepId}:`, normalized);

    return normalized;
  };

  const handleSubmitTransaction = async () => {
    try {
      setIsLoading(true);

      // Yeni işlem oluştur
      const newTransaction: Transaction = {
        id: Math.random().toString(36).substring(2, 10),
        transactionId: `TXN_${Date.now()}`,
        userId: formData.userId,
        amount: formData.amount,
        merchantId: formData.merchantId,
        type: formData.type,
        status: 'pending',
        transactionTime: new Date().toISOString(),
        ipAddress: formData.deviceInfo.ipAddress,
        deviceId: formData.deviceInfo.deviceId,
        location: formData.location,
        deviceInfo: formData.deviceInfo
      };

      // Transaction listesine ekle
      setTransactions(prev => [newTransaction, ...prev]);
      setShowCreateModal(false);

      // Otomatik analiz başlat
      await handleStartAnalysis(newTransaction);

    } catch (error) {
      console.error('❌ İşlem oluşturma hatası:', error);
      setError('İşlem oluşturulurken hata oluştu');
    } finally {
      setIsLoading(false);
    }
  };

  const filteredTransactions = transactions.filter(transaction => {
    const matchesSearch =
      transaction.transactionId.toLowerCase().includes(searchTerm.toLowerCase()) ||
      transaction.merchantId.toLowerCase().includes(searchTerm.toLowerCase()) ||
      transaction.ipAddress.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesStatusFilter = filterStatus === 'all' ||
      transaction.status?.toLowerCase() === filterStatus.toLowerCase();

    const matchesTypeFilter = filterType === 'all' ||
      transaction.type?.toLowerCase() === filterType.toLowerCase();

    const matchesAmountFilter = filterAmount === 'all' ||
      getAmountRange(transaction.amount) === filterAmount;

    return matchesSearch && matchesStatusFilter && matchesTypeFilter && matchesAmountFilter;
  });

  // Sorting
  const sortedTransactions = [...filteredTransactions].sort((a, b) => {
    let aValue: any, bValue: any;

    switch (sortBy) {
      case 'transactionTime':
        aValue = new Date(a.transactionTime).getTime();
        bValue = new Date(b.transactionTime).getTime();
        break;
      case 'amount':
        aValue = a.amount;
        bValue = b.amount;
        break;
      case 'riskScore':
        aValue = getRiskScoreValue(a.riskScore);
        bValue = getRiskScoreValue(b.riskScore);
        break;
      case 'transactionId':
        aValue = a.transactionId;
        bValue = b.transactionId;
        break;
      default:
        aValue = a[sortBy as keyof Transaction];
        bValue = b[sortBy as keyof Transaction];
    }

    if (sortOrder === 'asc') {
      return aValue > bValue ? 1 : -1;
    } else {
      return aValue < bValue ? 1 : -1;
    }
  });

  const handleSort = (field: string) => {
    if (sortBy === field) {
      setSortOrder(sortOrder === 'asc' ? 'desc' : 'asc');
    } else {
      setSortBy(field);
      setSortOrder('desc');
    }
  };

  const handleLoadTransactionDetail = async (transaction: Transaction) => {
    try {
      setIsLoadingDetail(true);
      setDetailData({});

      // Tek endpoint ile 3 tablodan veri çek
      const response = await FraudDetectionAPI.getTransactionCompleteDetails(transaction.transactionId);

      console.log('🔍 Backend Response:', response);
      console.log('🔍 Response Success:', response.success);
      console.log('🔍 Response Data:', response.data);

      if (response.success && response.data) {
        console.log('📊 Detail Data:', response.data);
        console.log('📊 Transaction:', response.data.transaction);
        console.log('📊 AnalysisResult:', response.data.analysisResult);
        console.log('📊 RiskFactors:', response.data.riskFactors);

        // Backend response yapısını kontrol et
        const actualData = (response.data as any).data || response.data;

        setDetailData({
          transaction: actualData.transaction || transaction,
          analysisResult: actualData.analysisResult || null,
          riskFactors: actualData.riskFactors || []
        });
      } else {
        // Fallback: Eski yöntemle ayrı ayrı çek
        console.log('⚠️ Yeni endpoint başarısız, eski yöntemle devam ediliyor...');
        const [transactionDetail, analysisResult, riskFactors] = await Promise.all([
          FraudDetectionAPI.getTransactionDetails(transaction.transactionId),
          FraudDetectionAPI.getAnalysisResultByTransaction(transaction.transactionId),
          FraudDetectionAPI.getRiskFactorsByTransaction(transaction.transactionId)
        ]);

        setDetailData({
          transaction: transactionDetail.success ? transactionDetail.data : transaction,
          analysisResult: analysisResult.success ? analysisResult.data : null,
          riskFactors: riskFactors.success ? riskFactors.data : []
        });
      }

      setCurrentTransaction(transaction);
      setShowAnalysisModal(true);

    } catch (error) {
      console.error('❌ Detay yükleme hatası:', error);
      setError('İşlem detayları yüklenirken hata oluştu');
    } finally {
      setIsLoadingDetail(false);
    }
  };

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
          İşlem Yönetimi
        </Typography>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={loadTransactions}
            disabled={isLoading}
          >
            Yenile
          </Button>
          <Button
            variant="outlined"
            color="secondary"
            onClick={testAllEndpoints}
            disabled={isLoading}
          >
            🧪 Test Endpoints
          </Button>
          <Button
            variant="contained"
            startIcon={<AddIcon />}
            onClick={() => setShowCreateModal(true)}
          >
            Yeni İşlem
          </Button>
        </Box>
      </Box>

      {/* Error Alert */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      {/* Search and Filter Bar */}
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {/* Search Bar */}
            <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
              <TextField
                size="small"
                placeholder="İşlem ID, Merchant veya IP ara..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                InputProps={{
                  startAdornment: <SearchIcon sx={{ mr: 1, color: 'text.secondary' }} />
                }}
                sx={{ flex: 1 }}
              />
              <Button
                variant="outlined"
                onClick={() => {
                  setSearchTerm('');
                  setFilterStatus('all');
                  setFilterType('all');
                  setFilterAmount('all');
                }}
                startIcon={<RefreshIcon />}
              >
                Temizle
              </Button>
            </Box>

            {/* Filter Row */}
            <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
              <FormControl size="small" sx={{ minWidth: 150 }}>
                <InputLabel>Durum</InputLabel>
                <Select
                  value={filterStatus}
                  onChange={(e) => setFilterStatus(e.target.value)}
                  label="Durum"
                >
                  <MenuItem value="all">Tümü</MenuItem>
                  <MenuItem value="pending">Beklemede</MenuItem>
                  <MenuItem value="analyzing">Analiz Ediliyor</MenuItem>
                  <MenuItem value="completed">Tamamlandı</MenuItem>
                  <MenuItem value="failed">Başarısız</MenuItem>
                  <MenuItem value="approved">Onaylandı</MenuItem>
                  <MenuItem value="requiresreview">İnceleme Gerekli</MenuItem>
                  <MenuItem value="blocked">Engellendi</MenuItem>
                </Select>
              </FormControl>

              <FormControl size="small" sx={{ minWidth: 150 }}>
                <InputLabel>İşlem Türü</InputLabel>
                <Select
                  value={filterType}
                  onChange={(e) => setFilterType(e.target.value)}
                  label="İşlem Türü"
                >
                  <MenuItem value="all">Tümü</MenuItem>
                  <MenuItem value="purchase">Satın Alma</MenuItem>
                  <MenuItem value="withdrawal">Para Çekme</MenuItem>
                  <MenuItem value="transfer">Transfer</MenuItem>
                  <MenuItem value="deposit">Para Yatırma</MenuItem>
                  <MenuItem value="creditcard">Kredi Kartı</MenuItem>
                </Select>
              </FormControl>

              <FormControl size="small" sx={{ minWidth: 150 }}>
                <InputLabel>Tutar Aralığı</InputLabel>
                <Select
                  value={filterAmount}
                  onChange={(e) => setFilterAmount(e.target.value)}
                  label="Tutar Aralığı"
                >
                  <MenuItem value="all">Tümü</MenuItem>
                  <MenuItem value="0-100₺">0-100₺</MenuItem>
                  <MenuItem value="100-500₺">100-500₺</MenuItem>
                  <MenuItem value="500-1000₺">500-1000₺</MenuItem>
                  <MenuItem value="1000-5000₺">1000-5000₺</MenuItem>
                  <MenuItem value="5000-10000₺">5000-10000₺</MenuItem>
                  <MenuItem value="10000₺+">10000₺+</MenuItem>
                </Select>
              </FormControl>
            </Box>

            {/* Results Summary */}
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Typography variant="body2" color="text.secondary">
                {sortedTransactions.length} işlem bulundu
              </Typography>
              <Box sx={{ display: 'flex', gap: 1 }}>
                <Typography variant="body2" color="text.secondary">
                  Sıralama:
                </Typography>
                <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                  {sortBy === 'transactionTime' ? 'Tarih' :
                    sortBy === 'amount' ? 'Tutar' :
                      sortBy === 'riskScore' ? 'Risk Skoru' :
                        sortBy === 'transactionId' ? 'İşlem ID' : sortBy}
                  {' '}
                  {sortOrder === 'asc' ? '↑' : '↓'}
                </Typography>
              </Box>
            </Box>
          </Box>
        </CardContent>
      </Card>

      {/* Transactions Table */}
      <Card sx={{ boxShadow: 3, borderRadius: 2 }}>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow sx={{ bgcolor: 'grey.100' }}>
                <TableCell>
                  <Button
                    size="small"
                    onClick={() => handleSort('transactionId')}
                    sx={{
                      fontWeight: 'bold',
                      color: sortBy === 'transactionId' ? 'primary.main' : 'inherit',
                      textTransform: 'none',
                      letterSpacing: 0.5
                    }}
                    endIcon={sortBy === 'transactionId' ?
                      (sortOrder === 'asc' ? '↑' : '↓') : null}
                  >
                    İşlem ID
                  </Button>
                </TableCell>
                <TableCell>
                  <Button
                    size="small"
                    onClick={() => handleSort('amount')}
                    sx={{
                      fontWeight: 'bold',
                      color: sortBy === 'amount' ? 'primary.main' : 'inherit',
                      textTransform: 'none',
                      letterSpacing: 0.5
                    }}
                    endIcon={sortBy === 'amount' ?
                      (sortOrder === 'asc' ? '↑' : '↓') : null}
                  >
                    Tutar (₺)
                  </Button>
                </TableCell>
                <TableCell>
                  <Button
                    size="small"
                    onClick={() => handleSort('type')}
                    sx={{
                      fontWeight: 'bold',
                      color: sortBy === 'type' ? 'primary.main' : 'inherit',
                      textTransform: 'none',
                      letterSpacing: 0.5
                    }}
                    endIcon={sortBy === 'type' ?
                      (sortOrder === 'asc' ? '↑' : '↓') : null}
                  >
                    İşlem Türü
                  </Button>
                </TableCell>
                <TableCell>
                  <Button
                    size="small"
                    onClick={() => handleSort('status')}
                    sx={{
                      fontWeight: 'bold',
                      color: sortBy === 'status' ? 'primary.main' : 'inherit',
                      textTransform: 'none',
                      letterSpacing: 0.5
                    }}
                    endIcon={sortBy === 'status' ?
                      (sortOrder === 'asc' ? '↑' : '↓') : null}
                  >
                    Durum
                  </Button>
                </TableCell>
                <TableCell>
                  <Button
                    size="small"
                    onClick={() => handleSort('transactionTime')}
                    sx={{
                      fontWeight: 'bold',
                      color: sortBy === 'transactionTime' ? 'primary.main' : 'inherit',
                      textTransform: 'none',
                      letterSpacing: 0.5
                    }}
                    endIcon={sortBy === 'transactionTime' ?
                      (sortOrder === 'asc' ? '↑' : '↓') : null}
                  >
                    Tarih
                  </Button>
                </TableCell>
                <TableCell><strong>İşlemler</strong></TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={6} align="center">
                    <CircularProgress />
                  </TableCell>
                </TableRow>
              ) : transactions.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6} align="center">
                    <Box sx={{ py: 4, textAlign: 'center' }}>
                      <SearchIcon sx={{ fontSize: 48, color: 'text.secondary', mb: 2 }} />
                      <Typography variant="h6" color="text.secondary" gutterBottom>
                        İşlem Bulunamadı
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Arama kriterlerinizi değiştirmeyi deneyin
                      </Typography>
                    </Box>
                  </TableCell>
                </TableRow>
              ) : (
                transactions.map((transaction) => (
                  <TableRow key={transaction.id} hover sx={{ transition: 'background 0.2s', '&:hover': { bgcolor: 'grey.100' } }}>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 'bold' }}>
                        {transaction.transactionId}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 'bold', fontSize: '1.1rem' }}>
                        ₺{Number(transaction.amount).toLocaleString()}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {getAmountRange(transaction.amount)}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="body2" sx={{ fontSize: '1.2rem' }}>
                          {getTypeIcon(transaction.type)}
                        </Typography>
                        <Typography variant="body2">
                          {getTypeText(transaction.type)}
                        </Typography>
                      </Box>
                    </TableCell>
                    <TableCell>
                      <Chip
                        label={getStatusText(transaction.status)}
                        color={getStatusColor(transaction.status) as any}
                        size="small"
                        sx={{ fontWeight: 'bold', borderRadius: 1 }}
                      />
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2">
                        {new Date(transaction.transactionTime).toLocaleDateString('tr-TR')}
                      </Typography>
                      <Typography variant="caption" color="text.secondary">
                        {new Date(transaction.transactionTime).toLocaleTimeString('tr-TR', {
                          hour: '2-digit',
                          minute: '2-digit'
                        })}
                      </Typography>
                    </TableCell>
                    <TableCell>
                      <IconButton
                        size="small"
                        onClick={() => handleLoadTransactionDetail(transaction)}
                        color="primary"
                        disabled={isLoadingDetail}
                        sx={{
                          bgcolor: 'primary.main',
                          color: 'white',
                          '&:hover': {
                            bgcolor: 'primary.dark'
                          }
                        }}
                      >
                        {isLoadingDetail ? <CircularProgress size={20} color="inherit" /> : <VisibilityIcon />}
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>

        {/* Pagination */}
        <TablePagination
          rowsPerPageOptions={[10, 25, 50, 100]}
          component="div"
          count={sortedTransactions.length}
          rowsPerPage={rowsPerPage}
          page={page}
          onPageChange={handleChangePage}
          onRowsPerPageChange={handleChangeRowsPerPage}
          labelRowsPerPage="Sayfa başına:"
          labelDisplayedRows={({ from, to, count }) =>
            `${from}-${to} / ${count !== -1 ? count : `more than ${to}`}`
          }
        />
      </Card>

      {/* İşlem Oluştur Modal */}
      <Modal open={showCreateModal} onClose={() => setShowCreateModal(false)}>
        <Box sx={{
          position: 'absolute',
          top: '50%',
          left: '50%',
          transform: 'translate(-50%, -50%)',
          width: { xs: '95%', sm: '90%', md: 800 },
          maxHeight: '90vh',
          bgcolor: 'background.paper',
          borderRadius: 2,
          boxShadow: 24,
          overflow: 'auto'
        }}>
          <Card>
            <CardContent sx={{ p: 3 }}>
              <Typography variant="h5" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <AddIcon color="primary" />
                <span>🔧 Yeni İşlem Oluştur</span>
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                Fraud analizi için test işlemi oluşturun. Alanlar otomatik olarak akıllı varsayılan değerlerle doldurulur.
              </Typography>

              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                {/* Sol Kolon - İşlem Bilgileri */}
                <Box>
                  <Typography variant="h6" gutterBottom color="primary">
                    💰 İşlem Bilgileri
                  </Typography>

                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                    <Box sx={{ display: 'flex', gap: 1 }}>
                      <TextField
                        fullWidth
                        label="Kullanıcı ID"
                        value={formData.userId}
                        onChange={(e) => setFormData(prev => ({ ...prev, userId: e.target.value }))}
                        size="small"
                      />
                      <Button
                        variant="outlined"
                        onClick={() => handleGenerateField('userId')}
                        sx={{ minWidth: '100px' }}
                        startIcon={<AutoFixHighIcon />}
                      >
                        Yenile
                      </Button>
                    </Box>

                    <Box sx={{ display: 'flex', gap: 1 }}>
                      <TextField
                        fullWidth
                        label="Tutar (₺)"
                        type="number"
                        value={formData.amount}
                        onChange={(e) => setFormData(prev => ({ ...prev, amount: Number(e.target.value) }))}
                        size="small"
                        InputProps={{
                          startAdornment: <Typography sx={{ mr: 1 }}>₺</Typography>
                        }}
                      />
                      <Button
                        variant="outlined"
                        onClick={() => handleGenerateField('amount')}
                        sx={{ minWidth: '100px' }}
                        startIcon={<AutoFixHighIcon />}
                      >
                        Random
                      </Button>
                    </Box>

                    <Box sx={{ display: 'flex', gap: 1 }}>
                      <TextField
                        fullWidth
                        label="Merchant ID"
                        value={formData.merchantId}
                        onChange={(e) => setFormData(prev => ({ ...prev, merchantId: e.target.value }))}
                        size="small"
                      />
                      <Button
                        variant="outlined"
                        onClick={() => handleGenerateField('merchantId')}
                        sx={{ minWidth: '100px' }}
                        startIcon={<AutoFixHighIcon />}
                      >
                        Generate
                      </Button>
                    </Box>

                    <FormControl fullWidth size="small">
                      <InputLabel>İşlem Tipi</InputLabel>
                      <Select
                        value={formData.type}
                        onChange={(e) => setFormData(prev => ({ ...prev, type: e.target.value as Transaction['type'] }))}
                        label="İşlem Tipi"
                      >
                        <MenuItem value="Purchase">💳 Satın Alma</MenuItem>
                        <MenuItem value="Withdrawal">💸 Para Çekme</MenuItem>
                        <MenuItem value="Transfer">🔄 Transfer</MenuItem>
                        <MenuItem value="Deposit">💰 Para Yatırma</MenuItem>
                        <MenuItem value="CreditCard">💳 Kredi Kartı</MenuItem>
                      </Select>
                    </FormControl>
                  </Box>
                </Box>

                {/* Sağ Kolon - Lokasyon & Cihaz */}
                <Box>
                  <Typography variant="h6" gutterBottom color="primary">
                    🌍 Lokasyon & Cihaz
                  </Typography>

                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                    <Box sx={{ display: 'flex', gap: 1 }}>
                      <FormControl fullWidth size="small">
                        <InputLabel>Ülke</InputLabel>
                        <Select
                          value={formData.location.country}
                          onChange={(e) => {
                            const country = COUNTRIES.find(c => c.code === e.target.value);
                            setFormData(prev => ({
                              ...prev,
                              location: {
                                ...prev.location,
                                country: e.target.value,
                                city: country?.cities[0] || 'Unknown'
                              }
                            }));
                          }}
                          label="Ülke"
                        >
                          {COUNTRIES.map(country => (
                            <MenuItem key={country.code} value={country.code}>
                              {country.name}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    </Box>

                    <FormControl fullWidth size="small">
                      <InputLabel>Şehir</InputLabel>
                      <Select
                        value={formData.location.city}
                        onChange={(e) => setFormData(prev => ({
                          ...prev,
                          location: { ...prev.location, city: e.target.value }
                        }))}
                        label="Şehir"
                      >
                        {COUNTRIES.find(c => c.code === formData.location.country)?.cities.map(city => (
                          <MenuItem key={city} value={city}>
                            {city}
                          </MenuItem>
                        )) || []}
                      </Select>
                    </FormControl>

                    <Box sx={{ display: 'flex', gap: 1 }}>
                      <TextField
                        fullWidth
                        label="IP Adresi"
                        value={formData.deviceInfo.ipAddress}
                        onChange={(e) => setFormData(prev => ({
                          ...prev,
                          deviceInfo: { ...prev.deviceInfo, ipAddress: e.target.value }
                        }))}
                        size="small"
                      />
                      <Button
                        variant="outlined"
                        onClick={() => handleGenerateField('ipAddress')}
                        sx={{ minWidth: '100px' }}
                        startIcon={<NetworkCheckIcon />}
                      >
                        Generate
                      </Button>
                    </Box>

                    <Box sx={{ display: 'flex', gap: 1 }}>
                      <TextField
                        fullWidth
                        label="Cihaz ID"
                        value={formData.deviceInfo.deviceId}
                        onChange={(e) => setFormData(prev => ({
                          ...prev,
                          deviceInfo: { ...prev.deviceInfo, deviceId: e.target.value }
                        }))}
                        size="small"
                      />
                      <Button
                        variant="outlined"
                        onClick={() => handleGenerateField('deviceId')}
                        sx={{ minWidth: '100px' }}
                        startIcon={<DeviceHubIcon />}
                      >
                        Generate
                      </Button>
                    </Box>

                    <FormControl fullWidth size="small">
                      <InputLabel>Cihaz Tipi</InputLabel>
                      <Select
                        value={formData.deviceInfo.deviceType}
                        onChange={(e) => setFormData(prev => ({
                          ...prev,
                          deviceInfo: { ...prev.deviceInfo, deviceType: e.target.value }
                        }))}
                        label="Cihaz Tipi"
                      >
                        {DEVICE_TYPES.map(type => (
                          <MenuItem key={type} value={type}>
                            {type === 'Mobile' ? '📱 Mobile' :
                              type === 'Desktop' ? '🖥️ Desktop' :
                                type === 'Tablet' ? '📱 Tablet' : '❓ Unknown'}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </Box>
                </Box>

                {/* Alt Kısım - User Agent */}
                <Box>
                  <Typography variant="h6" gutterBottom color="primary">
                    🌐 Browser & OS
                  </Typography>

                  <Box sx={{ display: 'flex', gap: 1 }}>
                    <TextField
                      fullWidth
                      label="User Agent"
                      value={formData.deviceInfo.userAgent}
                      onChange={(e) => setFormData(prev => ({
                        ...prev,
                        deviceInfo: { ...prev.deviceInfo, userAgent: e.target.value }
                      }))}
                      size="small"
                      multiline
                      rows={2}
                    />
                    <Button
                      variant="outlined"
                      onClick={() => handleGenerateField('userAgent')}
                      sx={{ minWidth: '100px', alignSelf: 'flex-start' }}
                      startIcon={<AutoFixHighIcon />}
                    >
                      Random
                    </Button>
                  </Box>

                  <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                    💡 User Agent string'i browser ve işletim sistemi bilgilerini içerir
                  </Typography>
                </Box>

                {/* Preview Card */}
                <Box>
                  <Card variant="outlined" sx={{ bgcolor: 'grey.50' }}>
                    <CardContent sx={{ p: 2 }}>
                      <Typography variant="subtitle2" gutterBottom>
                        📋 İşlem Önizlemesi
                      </Typography>
                      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2 }}>
                        <Box sx={{ flex: '1 1 25%', minWidth: '120px' }}>
                          <Typography variant="caption" color="text.secondary">Kullanıcı</Typography>
                          <Typography variant="body2">{formData.userId}</Typography>
                        </Box>
                        <Box sx={{ flex: '1 1 25%', minWidth: '120px' }}>
                          <Typography variant="caption" color="text.secondary">Tutar</Typography>
                          <Typography variant="body2">₺{formData.amount.toLocaleString()}</Typography>
                        </Box>
                        <Box sx={{ flex: '1 1 25%', minWidth: '120px' }}>
                          <Typography variant="caption" color="text.secondary">Lokasyon</Typography>
                          <Typography variant="body2">{formData.location.city}, {formData.location.country}</Typography>
                        </Box>
                        <Box sx={{ flex: '1 1 25%', minWidth: '120px' }}>
                          <Typography variant="caption" color="text.secondary">IP</Typography>
                          <Typography variant="body2">{formData.deviceInfo.ipAddress}</Typography>
                        </Box>
                      </Box>
                    </CardContent>
                  </Card>
                </Box>
              </Box>

              {/* Butonlar */}
              <Box sx={{ mt: 3, display: 'flex', gap: 2, justifyContent: 'flex-end' }}>
                <Button variant="outlined" onClick={() => setShowCreateModal(false)}>
                  İptal
                </Button>
                <Button
                  variant="contained"
                  onClick={handleSubmitTransaction}
                  disabled={isLoading}
                  startIcon={isLoading ? <CircularProgress size={20} /> : <PlayIcon />}
                  sx={{ minWidth: '200px' }}
                >
                  {isLoading ? 'İşlem Oluşturuluyor...' : 'İşlem Oluştur ve Analiz Et'}
                </Button>
              </Box>
            </CardContent>
          </Card>
        </Box>
      </Modal>

      {/* Detail Analysis Modal */}
      <Modal open={showAnalysisModal} onClose={() => setShowAnalysisModal(false)}>
        <Box sx={{
          position: 'absolute',
          top: '50%',
          left: '50%',
          transform: 'translate(-50%, -50%)',
          width: '95%',
          maxWidth: 1400,
          bgcolor: 'background.paper',
          boxShadow: 24,
          borderRadius: 2,
          maxHeight: '95vh',
          overflow: 'auto'
        }}>
          {currentTransaction && (
            <>
              {/* Header */}
              <Box sx={{ p: 3, bgcolor: 'grey.100', borderBottom: 1, borderColor: 'divider' }}>
                <Typography variant="h5" gutterBottom>
                  🔍 İşlem Detayları: {currentTransaction.transactionId}
                </Typography>
                <Box sx={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
                  <Box sx={{ flex: '1 1 200px' }}>
                    <Typography variant="caption">Kullanıcı</Typography>
                    <Typography variant="body2">{currentTransaction.userId}</Typography>
                  </Box>
                  <Box sx={{ flex: '1 1 200px' }}>
                    <Typography variant="caption">Tutar</Typography>
                    <Typography variant="body2">₺{Number(currentTransaction.amount).toLocaleString()}</Typography>
                  </Box>
                  <Box sx={{ flex: '1 1 200px' }}>
                    <Typography variant="caption">IP</Typography>
                    <Typography variant="body2">{currentTransaction.ipAddress}</Typography>
                  </Box>
                  <Box sx={{ flex: '1 1 200px' }}>
                    <Typography variant="caption">Konum</Typography>
                    <Typography variant="body2">{currentTransaction.location.city}, {currentTransaction.location.country}</Typography>
                  </Box>
                  <Box sx={{ flex: '1 1 200px' }}>
                    <Typography variant="caption">Merchant</Typography>
                    <Typography variant="body2">{currentTransaction.merchantId}</Typography>
                  </Box>
                  <Box sx={{ flex: '1 1 200px' }}>
                    <Typography variant="caption">Durum</Typography>
                    <Chip
                      label={getStatusText(currentTransaction.status)}
                      color={getStatusColor(currentTransaction.status) as any}
                      size="small"
                    />
                  </Box>
                </Box>
              </Box>

              <Box sx={{ p: 3 }}>
                {isLoadingDetail ? (
                  <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
                    <CircularProgress size={60} />
                    <Typography variant="h6" sx={{ ml: 2, alignSelf: 'center' }}>
                      İşlem detayları yükleniyor...
                    </Typography>
                  </Box>
                ) : (
                  <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', lg: 'repeat(2, 1fr)' }, gap: 3 }}>

                    {/* Transaction Details */}
                    <Card>
                      <CardContent>
                        <Typography variant="h6" gutterBottom>
                          💰 İşlem Bilgileri
                        </Typography>
                        {detailData.transaction && (
                          <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 2 }}>
                            <Box>
                              <Typography variant="caption" color="text.secondary">İşlem ID</Typography>
                              <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 'bold' }}>
                                {detailData.transaction.transactionId || currentTransaction.transactionId}
                              </Typography>
                            </Box>
                            <Box>
                              <Typography variant="caption" color="text.secondary">Kullanıcı ID</Typography>
                              <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                {detailData.transaction.userId || currentTransaction.userId}
                              </Typography>
                            </Box>
                            <Box>
                              <Typography variant="caption" color="text.secondary">Tutar</Typography>
                              <Typography variant="body2" sx={{ fontWeight: 'bold', fontSize: '1.1rem' }}>
                                ₺{Number(detailData.transaction.amount || currentTransaction.amount).toLocaleString()}
                              </Typography>
                            </Box>
                            <Box>
                              <Typography variant="caption" color="text.secondary">İşlem Türü</Typography>
                              <Typography variant="body2">
                                {getTypeIcon(detailData.transaction.type || currentTransaction.type)} {getTypeText(detailData.transaction.type || currentTransaction.type)}
                              </Typography>
                            </Box>
                            <Box>
                              <Typography variant="caption" color="text.secondary">Merchant</Typography>
                              <Typography variant="body2">
                                {detailData.transaction.merchantId || currentTransaction.merchantId}
                              </Typography>
                            </Box>
                            <Box>
                              <Typography variant="caption" color="text.secondary">Durum</Typography>
                              <Chip
                                label={getStatusText(detailData.transaction.status || currentTransaction.status)}
                                color={getStatusColor(detailData.transaction.status || currentTransaction.status) as any}
                                size="small"
                              />
                            </Box>
                            <Box>
                              <Typography variant="caption" color="text.secondary">IP Adresi</Typography>
                              <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                {detailData.transaction.ipAddress || currentTransaction.ipAddress}
                              </Typography>
                            </Box>
                            <Box>
                              <Typography variant="caption" color="text.secondary">Cihaz ID</Typography>
                              <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                {detailData.transaction.deviceId || currentTransaction.deviceId}
                              </Typography>
                            </Box>
                            <Box>
                              <Typography variant="caption" color="text.secondary">Konum</Typography>
                              <Typography variant="body2">
                                {detailData.transaction.location?.city || currentTransaction.location.city}, {detailData.transaction.location?.country || currentTransaction.location.country}
                              </Typography>
                            </Box>
                            <Box>
                              <Typography variant="caption" color="text.secondary">Tarih</Typography>
                              <Typography variant="body2">
                                {new Date(detailData.transaction.transactionTime || currentTransaction.transactionTime).toLocaleString('tr-TR')}
                              </Typography>
                            </Box>
                          </Box>
                        )}
                      </CardContent>
                    </Card>

                    {/* Analysis Result */}
                    <Card>
                      <CardContent>
                        <Typography variant="h6" gutterBottom>
                          🔍 Analiz Sonucu
                        </Typography>
                        {detailData.analysisResult ? (
                          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                            <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 2 }}>
                              <Box>
                                <Typography variant="caption" color="text.secondary">Analiz ID</Typography>
                                <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                  {detailData.analysisResult.id}
                                </Typography>
                              </Box>
                              <Box>
                                <Typography variant="caption" color="text.secondary">Durum</Typography>
                                <Chip
                                  label={detailData.analysisResult.status}
                                  color={detailData.analysisResult.status === 'Completed' ? 'success' : 'warning'}
                                  size="small"
                                />
                              </Box>
                              <Box>
                                <Typography variant="caption" color="text.secondary">Dolandırıcılık Olasılığı</Typography>
                                <Typography variant="body2" sx={{ fontWeight: 'bold', color: 'error.main' }}>
                                  {(detailData.analysisResult.fraudProbability * 100).toFixed(1)}%
                                </Typography>
                              </Box>
                              <Box>
                                <Typography variant="caption" color="text.secondary">Anomali Skoru</Typography>
                                <Typography variant="body2" sx={{ fontWeight: 'bold', color: 'warning.main' }}>
                                  {detailData.analysisResult.anomalyScore?.toFixed(2) || '0.00'}
                                </Typography>
                              </Box>
                              <Box>
                                <Typography variant="caption" color="text.secondary">Risk Skoru</Typography>
                                <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                                  {(detailData.analysisResult.riskScore?.score * 100 || 0).toFixed(1)}%
                                </Typography>
                              </Box>
                              <Box>
                                <Typography variant="caption" color="text.secondary">Risk Seviyesi</Typography>
                                <Chip
                                  label={detailData.analysisResult.riskLevel || getRiskLevelValue(detailData.analysisResult.riskScore?.level)}
                                  sx={{ bgcolor: getRiskColor(detailData.analysisResult.riskLevel || getRiskLevelValue(detailData.analysisResult.riskScore?.level)), color: 'white' }}
                                  size="small"
                                />
                              </Box>
                              <Box>
                                <Typography variant="caption" color="text.secondary">Karar</Typography>
                                <Chip
                                  label={detailData.analysisResult.decision === 'Approve' ? 'Onaylandı' :
                                    detailData.analysisResult.decision === 'Deny' ? 'Reddedildi' :
                                      detailData.analysisResult.decision}
                                  color={detailData.analysisResult.decision === 'Approve' ? 'success' :
                                    detailData.analysisResult.decision === 'Deny' ? 'error' : 'warning'}
                                  size="small"
                                />
                              </Box>
                              <Box>
                                <Typography variant="caption" color="text.secondary">Analiz Tarihi</Typography>
                                <Typography variant="body2">
                                  {new Date(detailData.analysisResult.analyzedAt).toLocaleString('tr-TR')}
                                </Typography>
                              </Box>
                            </Box>

                            {detailData.analysisResult.appliedActions && detailData.analysisResult.appliedActions.length > 0 && (
                              <Box>
                                <Typography variant="caption" color="primary.main" sx={{ fontWeight: 'bold' }}>
                                  Uygulanan Aksiyonlar:
                                </Typography>
                                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mt: 1 }}>
                                  {detailData.analysisResult.appliedActions.map((action: string, index: number) => (
                                    <Chip key={index} label={action} size="small" color="primary" variant="outlined" />
                                  ))}
                                </Box>
                              </Box>
                            )}
                          </Box>
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            Analiz sonucu bulunamadı
                          </Typography>
                        )}
                      </CardContent>
                    </Card>

                    {/* Risk Factors */}
                    <Card>
                      <CardContent>
                        <Typography variant="h6" gutterBottom>
                          ⚠️ Risk Faktörleri ({detailData.riskFactors?.length || 0})
                        </Typography>
                        {detailData.riskFactors && detailData.riskFactors.length > 0 ? (
                          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                            {detailData.riskFactors.map((factor: any, index: number) => (
                              <Card key={index} variant="outlined" sx={{
                                borderColor: factor.severity === 'Critical' ? 'error.main' :
                                  factor.severity === 'High' ? 'warning.main' :
                                    factor.severity === 'Medium' ? 'info.main' : 'success.main',
                                borderWidth: 2
                              }}>
                                <CardContent sx={{ p: 2 }}>
                                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                                    <Box>
                                      <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
                                        {factor.code}
                                      </Typography>
                                      <Typography variant="caption" color="text.secondary">
                                        {factor.source}
                                      </Typography>
                                    </Box>
                                    <Chip
                                      label={factor.severity}
                                      size="small"
                                      color={factor.severity === 'Critical' ? 'error' :
                                        factor.severity === 'High' ? 'warning' :
                                          factor.severity === 'Medium' ? 'info' : 'success'}
                                      sx={{ fontWeight: 'bold' }}
                                    />
                                  </Box>

                                  <Typography variant="body2" sx={{ mb: 1 }}>
                                    {factor.description}
                                  </Typography>

                                  <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 1 }}>
                                    <Box>
                                      <Typography variant="caption" color="text.secondary">Güven:</Typography>
                                      <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                                        {(factor.confidence * 100).toFixed(1)}%
                                      </Typography>
                                    </Box>
                                    <Box>
                                      <Typography variant="caption" color="text.secondary">Tespit:</Typography>
                                      <Typography variant="body2" sx={{ fontSize: '0.8rem' }}>
                                        {new Date(factor.detectedAt).toLocaleString('tr-TR')}
                                      </Typography>
                                    </Box>
                                  </Box>

                                  {factor.ruleId && (
                                    <Box sx={{ mt: 1 }}>
                                      <Typography variant="caption" color="text.secondary">Kural ID:</Typography>
                                      <Typography variant="body2" sx={{ fontFamily: 'monospace', fontSize: '0.8rem' }}>
                                        {factor.ruleId}
                                      </Typography>
                                    </Box>
                                  )}

                                  {factor.actionTaken && (
                                    <Box sx={{ mt: 1 }}>
                                      <Typography variant="caption" color="primary.main" sx={{ fontWeight: 'bold' }}>
                                        Alınan Aksiyon:
                                      </Typography>
                                      <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                                        {factor.actionTaken === 'RequireAdditionalVerification' ? 'Ek Doğrulama Gerekli' :
                                          factor.actionTaken === 'Block' ? 'Engelle' :
                                            factor.actionTaken === 'Flag' ? 'İşaretle' :
                                              factor.actionTaken}
                                      </Typography>
                                    </Box>
                                  )}

                                  {factor.type && (
                                    <Box sx={{ mt: 1 }}>
                                      <Typography variant="caption" color="text.secondary">Tip:</Typography>
                                      <Typography variant="body2" sx={{ fontWeight: 'bold' }}>
                                        {factor.type}
                                      </Typography>
                                    </Box>
                                  )}
                                </CardContent>
                              </Card>
                            ))}
                          </Box>
                        ) : (
                          <Typography variant="body2" color="text.secondary">
                            Risk faktörü bulunamadı
                          </Typography>
                        )}
                      </CardContent>
                    </Card>
                  </Box>
                )}
              </Box>

              {/* Footer */}
              <Box sx={{ p: 3, bgcolor: 'grey.100', borderTop: 1, borderColor: 'divider' }}>
                <Button variant="contained" onClick={() => setShowAnalysisModal(false)} fullWidth>
                  Kapat
                </Button>
              </Box>
            </>
          )}
        </Box>
      </Modal>
    </Box>
  );
};

export default TransactionManagement; 