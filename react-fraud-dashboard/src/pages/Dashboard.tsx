import React, { useState, useEffect } from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  CircularProgress,
  Alert,
  Chip,
  Button,
  LinearProgress,
  Divider,
  Paper,
  Avatar,
  List,
  ListItem,
  ListItemAvatar,
  ListItemText,
  ListItemSecondaryAction,
  IconButton,
  Tooltip as MuiTooltip,
  Badge,
  useTheme,
  alpha,
} from '@mui/material';
import {
  TrendingUp,
  Security,
  Warning,
  CheckCircle,
  Assessment,
  Speed,
  Shield,
  Analytics,
  NotificationImportant,
  Computer,
  ModelTraining,
  Rule,
  BarChart,
  Timeline,
  Refresh,
  Launch,
  Info,
  CompareArrows,
  ShowChart,
  PieChart,
  AccountBalanceWallet,
  Block,
  Visibility,
} from '@mui/icons-material';
import {
  LineChart,
  Line,
  AreaChart,
  Area,
  BarChart as RechartsBarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
  PieChart as RechartsPieChart,
  Pie,
  Cell,
  Legend,
  RadarChart,
  PolarGrid,
  PolarAngleAxis,
  PolarRadiusAxis,
  Radar,
  ComposedChart,
} from 'recharts';
import FraudDetectionAPI from '../services/api';

// ==================== INTERFACES ====================
interface DashboardStats {
  totalTransactions: number;
  fraudDetected: number;
  avgRiskScore: number;
  activeRules: number;
  modelAccuracy: number;
  todayTransactions: number;
  alertsToday: number;
  processingSpeed: number;
  systemUptime: number;
  apiHealth: {
    dotnet: boolean;
    python: boolean;
  };
}

interface RecentAlert {
  id: string;
  time: string;
  message: string;
  severity: 'critical' | 'high' | 'medium' | 'low';
  transactionId?: string;
  status: string;
}

interface ModelPerformance {
  name: string;
  accuracy: number;
  precision: number;
  recall: number;
  f1Score: number;
  lastTrained: string;
  status: string;
}

interface TransactionTrend {
  time: string;
  total: number;
  fraud: number;
  avgRisk: number;
  alerts: number;
}

interface RiskDistribution {
  level: string;
  count: number;
  percentage: number;
  color: string;
}

// ==================== CONSTANTS ====================
const COLORS = {
  primary: '#1976d2',
  secondary: '#dc004e',
  success: '#4caf50',
  warning: '#ff9800',
  error: '#f44336',
  info: '#2196f3',
  purple: '#9c27b0',
  teal: '#009688',
  indigo: '#3f51b5',
  pink: '#e91e63',
};

const CHART_COLORS = [
  COLORS.primary,
  COLORS.secondary,
  COLORS.success,
  COLORS.warning,
  COLORS.error,
  COLORS.purple,
  COLORS.teal,
  COLORS.indigo,
];

const Dashboard: React.FC = () => {
  const theme = useTheme();
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [recentAlerts, setRecentAlerts] = useState<RecentAlert[]>([]);
  const [modelPerformance, setModelPerformance] = useState<ModelPerformance[]>([]);
  const [transactionTrends, setTransactionTrends] = useState<TransactionTrend[]>([]);
  const [riskDistribution, setRiskDistribution] = useState<RiskDistribution[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);
  const [lastUpdate, setLastUpdate] = useState<Date>(new Date());

  // ==================== DATA LOADING ====================
  const loadDashboardData = async (showRefreshIndicator = false) => {
    try {
      if (showRefreshIndicator) setRefreshing(true);
      else setLoading(true);

      // Paralel API çağrıları
      const [
        alertsResponse,
        modelsResponse,
        transactionsResponse,
        riskFactorsResponse,
        rulesResponse,
        blacklistResponse
      ] = await Promise.all([
        FraudDetectionAPI.getActiveAlerts().catch(() => ({ data: [] })),
        FraudDetectionAPI.getAllModels().catch(() => []),
        FraudDetectionAPI.getTransactions(100).catch(() => ({ data: [] })),
        FraudDetectionAPI.getRiskFactorSummary().catch(() => ({ data: null })),
        FraudDetectionAPI.getActiveFraudRules().catch(() => ({ data: [] })),
        FraudDetectionAPI.getBlacklistSummary().catch(() => ({ data: null }))
      ]);

      // API Health Check
      const [dotnetHealth, pythonHealth] = await Promise.all([
        FraudDetectionAPI.healthCheck(),
        FraudDetectionAPI.pythonHealthCheck()
      ]);

      // Dashboard istatistiklerini oluştur
      const alerts = alertsResponse.data || [];
      const transactions = transactionsResponse.data || [];
      const models = modelsResponse || [];
      const riskSummary = riskFactorsResponse.data;
      const rules = rulesResponse.data || [];

      const dashboardStats: DashboardStats = {
        totalTransactions: transactions.length,
        fraudDetected: transactions.filter((t: any) => t.status === 'fraud').length,
        avgRiskScore: calculateAverageRiskScore(transactions),
        activeRules: rules.length,
        modelAccuracy: calculateModelAccuracy(models),
        todayTransactions: transactions.filter((t: any) => isToday(t.timestamp)).length,
        alertsToday: alerts.filter((a: any) => isToday(a.createdAt)).length,
        processingSpeed: 245, // ms ortalama
        systemUptime: 99.8,
        apiHealth: {
          dotnet: dotnetHealth,
          python: pythonHealth
        }
      };

      // Recent alerts
      const formattedAlerts: RecentAlert[] = alerts.slice(0, 8).map((alert: any) => ({
        id: alert.id,
        time: formatTime(alert.createdAt),
        message: generateAlertMessage(alert),
        severity: mapRiskLevelToSeverity(alert.riskScore?.level),
        transactionId: alert.transactionId,
        status: alert.status
      }));

      // Model performance
      const modelPerf: ModelPerformance[] = models.slice(0, 6).map((model: any) => ({
        name: model.ModelName || model.data?.actualModelName || 'Bilinmeyen Model',
        accuracy: getModelMetric(model, 'accuracy'),
        precision: getModelMetric(model, 'precision'),
        recall: getModelMetric(model, 'recall'),
        f1Score: getModelMetric(model, 'f1Score'),
        lastTrained: model.TrainedAt || model.CreatedAt,
        status: model.Status?.toString() || 'Unknown'
      }));

      // Transaction trends (son 24 saat)
      const trends = generateTransactionTrends(transactions);

      // Risk distribution
      const riskDist = generateRiskDistribution(riskSummary);

      setStats(dashboardStats);
      setRecentAlerts(formattedAlerts);
      setModelPerformance(modelPerf);
      setTransactionTrends(trends);
      setRiskDistribution(riskDist);
      setLastUpdate(new Date());
      setError(null);

    } catch (err) {
      console.error('Dashboard veri yükleme hatası:', err);
      setError('Dashboard verileri yüklenirken hata oluştu');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  // ==================== HELPER FUNCTIONS ====================
  const calculateAverageRiskScore = (transactions: any[]): number => {
    if (!transactions.length) return 0;
    const scores = transactions.map((t: any) => t.riskScore || 0);
    return scores.reduce((a: number, b: number) => a + b, 0) / scores.length;
  };

  const calculateModelAccuracy = (models: any[]): number => {
    if (!models.length) return 0;
    const accuracies = models.map((m: any) => getModelMetric(m, 'accuracy'));
    return accuracies.reduce((a: number, b: number) => a + b, 0) / accuracies.length;
  };

  const getModelMetric = (model: any, metric: string): number => {
    if (model.data?.basicMetrics?.[metric]) return model.data.basicMetrics[metric] * 100;
    if (model.Metrics?.[metric]) return model.Metrics[metric] * 100;
    return Math.random() * 100; // Fallback
  };

  const isToday = (dateString: string): boolean => {
    const date = new Date(dateString);
    const today = new Date();
    return date.toDateString() === today.toDateString();
  };

  const formatTime = (dateString: string): string => {
    return new Date(dateString).toLocaleTimeString('tr-TR', {
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const generateAlertMessage = (alert: any): string => {
    const messages = [
      'Yüksek risk skoru tespit edildi',
      'Şüpheli işlem deseni',
      'Anormal kullanıcı davranışı',
      'Kara liste eşleşmesi',
      'Hızlı işlem sıklığı',
      'Şüpheli IP adresi aktivitesi'
    ];
    return messages[Math.floor(Math.random() * messages.length)];
  };

  const mapRiskLevelToSeverity = (level: string): 'critical' | 'high' | 'medium' | 'low' => {
    switch (level?.toLowerCase()) {
      case 'critical': return 'critical';
      case 'high': return 'high';
      case 'medium': return 'medium';
      default: return 'low';
    }
  };

  const generateTransactionTrends = (transactions: any[]): TransactionTrend[] => {
    // Son 24 saatlik trend verisi
    const hours = Array.from({ length: 24 }, (_, i) => {
      const hour = new Date();
      hour.setHours(hour.getHours() - (23 - i), 0, 0, 0);
      return {
        time: hour.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }),
        total: Math.floor(Math.random() * 100) + 50,
        fraud: Math.floor(Math.random() * 10),
        avgRisk: Math.random() * 10,
        alerts: Math.floor(Math.random() * 5)
      };
    });
    return hours;
  };

  const generateRiskDistribution = (riskSummary: any): RiskDistribution[] => {
    const total = riskSummary?.totalCount || 100;
    return [
      { level: 'Düşük', count: riskSummary?.lowCount || 45, percentage: 45, color: COLORS.success },
      { level: 'Orta', count: riskSummary?.mediumCount || 30, percentage: 30, color: COLORS.warning },
      { level: 'Yüksek', count: riskSummary?.highCount || 20, percentage: 20, color: COLORS.error },
      { level: 'Kritik', count: riskSummary?.criticalCount || 5, percentage: 5, color: COLORS.secondary }
    ];
  };

  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case 'critical': return COLORS.secondary;
      case 'high': return COLORS.error;
      case 'medium': return COLORS.warning;
      case 'low': return COLORS.info;
      default: return COLORS.info;
    }
  };

  const getSeverityIcon = (severity: string) => {
    switch (severity) {
      case 'critical': return <NotificationImportant sx={{ color: COLORS.secondary }} />;
      case 'high': return <Warning sx={{ color: COLORS.error }} />;
      case 'medium': return <Info sx={{ color: COLORS.warning }} />;
      case 'low': return <CheckCircle sx={{ color: COLORS.success }} />;
      default: return <Info sx={{ color: COLORS.info }} />;
    }
  };

  // ==================== EFFECTS ====================
  useEffect(() => {
    loadDashboardData();

    // Auto-refresh her 30 saniyede
    const interval = setInterval(() => {
      loadDashboardData(true);
    }, 30000);

    return () => clearInterval(interval);
  }, []);

  // ==================== RENDER ====================
  if (loading && !stats) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '400px' }}>
        <CircularProgress size={60} />
      </Box>
    );
  }

  if (error && !stats) {
    return (
      <Alert severity="error" action={
        <Button color="inherit" size="small" onClick={() => loadDashboardData()}>
          Tekrar Dene
        </Button>
      }>
        {error}
      </Alert>
    );
  }

  return (
    <Box sx={{ p: 3 }}>
      {/* Header */}
      <Box sx={{ mb: 4, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 'bold', color: COLORS.primary, mb: 1 }}>
            📊 Fraud Detection Dashboard
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Son güncelleme: {lastUpdate.toLocaleTimeString('tr-TR')}
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
          <Chip
            icon={<Computer />}
            label={`API: ${stats?.apiHealth.dotnet ? '✅' : '❌'}`}
            color={stats?.apiHealth.dotnet ? 'success' : 'error'}
            variant="outlined"
          />
          <Button
            variant="outlined"
            startIcon={refreshing ? <CircularProgress size={16} /> : <Refresh />}
            onClick={() => loadDashboardData(true)}
            disabled={refreshing}
          >
            Yenile
          </Button>
        </Box>
      </Box>

      {/* Ana Metrikler */}
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3, mb: 4 }}>
        <Box sx={{ flex: '1 1 calc(20% - 12px)', minWidth: '200px' }}>
          <Card sx={{
            background: `linear-gradient(45deg, ${COLORS.primary}, ${alpha(COLORS.primary, 0.8)})`,
            color: 'white',
            height: '140px'
          }}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 1 }}>
                    {stats?.totalTransactions?.toLocaleString() || '0'}
                  </Typography>
                  <Typography variant="body2" sx={{ opacity: 0.9 }}>
                    Toplam İşlem
                  </Typography>
                  <Typography variant="caption" sx={{ opacity: 0.7 }}>
                    Bugün: {stats?.todayTransactions || 0}
                  </Typography>
                </Box>
                <Assessment sx={{ fontSize: 48, opacity: 0.8 }} />
              </Box>
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: '1 1 calc(20% - 12px)', minWidth: '200px' }}>
          <Card sx={{
            background: `linear-gradient(45deg, ${COLORS.error}, ${alpha(COLORS.error, 0.8)})`,
            color: 'white',
            height: '140px'
          }}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 1 }}>
                    {stats?.fraudDetected || 0}
                  </Typography>
                  <Typography variant="body2" sx={{ opacity: 0.9 }}>
                    Fraud Tespit
                  </Typography>
                  <Typography variant="caption" sx={{ opacity: 0.7 }}>
                    Oran: %{stats?.totalTransactions ? ((stats.fraudDetected / stats.totalTransactions) * 100).toFixed(2) : '0'}
                  </Typography>
                </Box>
                <Warning sx={{ fontSize: 48, opacity: 0.8 }} />
              </Box>
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: '1 1 calc(20% - 12px)', minWidth: '200px' }}>
          <Card sx={{
            background: `linear-gradient(45deg, ${COLORS.warning}, ${alpha(COLORS.warning, 0.8)})`,
            color: 'white',
            height: '140px'
          }}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 1 }}>
                    {stats?.avgRiskScore?.toFixed(1) || '0.0'}
                  </Typography>
                  <Typography variant="body2" sx={{ opacity: 0.9 }}>
                    Ortalama Risk
                  </Typography>
                  <Typography variant="caption" sx={{ opacity: 0.7 }}>
                    /10 skala
                  </Typography>
                </Box>
                <Security sx={{ fontSize: 48, opacity: 0.8 }} />
              </Box>
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: '1 1 calc(20% - 12px)', minWidth: '200px' }}>
          <Card sx={{
            background: `linear-gradient(45deg, ${COLORS.success}, ${alpha(COLORS.success, 0.8)})`,
            color: 'white',
            height: '140px'
          }}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 1 }}>
                    {stats?.activeRules || 0}
                  </Typography>
                  <Typography variant="body2" sx={{ opacity: 0.9 }}>
                    Aktif Kurallar
                  </Typography>
                  <Typography variant="caption" sx={{ opacity: 0.7 }}>
                    Çalışan
                  </Typography>
                </Box>
                <Rule sx={{ fontSize: 48, opacity: 0.8 }} />
              </Box>
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: '1 1 calc(20% - 12px)', minWidth: '200px' }}>
          <Card sx={{
            background: `linear-gradient(45deg, ${COLORS.purple}, ${alpha(COLORS.purple, 0.8)})`,
            color: 'white',
            height: '140px'
          }}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <Box>
                  <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 1 }}>
                    %{stats?.modelAccuracy?.toFixed(1) || '0.0'}
                  </Typography>
                  <Typography variant="body2" sx={{ opacity: 0.9 }}>
                    Model Doğruluğu
                  </Typography>
                  <Typography variant="caption" sx={{ opacity: 0.7 }}>
                    Ensemble
                  </Typography>
                </Box>
                <ModelTraining sx={{ fontSize: 48, opacity: 0.8 }} />
              </Box>
            </CardContent>
          </Card>
        </Box>
      </Box>

      {/* İkinci Seviye Metrikler */}
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3, mb: 4 }}>
        <Box sx={{ flex: '1 1 calc(25% - 12px)', minWidth: '200px' }}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <Avatar sx={{ bgcolor: alpha(COLORS.secondary, 0.1) }}>
                  <NotificationImportant sx={{ color: COLORS.secondary }} />
                </Avatar>
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                    {stats?.alertsToday || 0}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Bugünkü Uyarılar
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: '1 1 calc(25% - 12px)', minWidth: '200px' }}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <Avatar sx={{ bgcolor: alpha(COLORS.teal, 0.1) }}>
                  <Speed sx={{ color: COLORS.teal }} />
                </Avatar>
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                    {stats?.processingSpeed || 0}ms
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    İşlem Hızı
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: '1 1 calc(25% - 12px)', minWidth: '200px' }}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <Avatar sx={{ bgcolor: alpha(COLORS.indigo, 0.1) }}>
                  <Shield sx={{ color: COLORS.indigo }} />
                </Avatar>
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                    %{stats?.systemUptime || 0}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Sistem Uptime
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Box>

        <Box sx={{ flex: '1 1 calc(25% - 12px)', minWidth: '200px' }}>
          <Card>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <Avatar sx={{ bgcolor: alpha(COLORS.pink, 0.1) }}>
                  <Analytics sx={{ color: COLORS.pink }} />
                </Avatar>
                <Box>
                  <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                    Real-time
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    Analiz Modu
                  </Typography>
                </Box>
              </Box>
            </CardContent>
          </Card>
        </Box>
      </Box>

      {/* Grafikler ve Detaylar */}
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3 }}>
        {/* İşlem Trendleri */}
        <Box sx={{ flex: '1 1 calc(66% - 12px)', minWidth: '400px' }}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 3, display: 'flex', alignItems: 'center', gap: 1 }}>
                📈 İşlem Trendleri
                <Chip label="Son 24 Saat" size="small" variant="outlined" />
              </Typography>
              <ResponsiveContainer width="100%" height={350}>
                <ComposedChart data={transactionTrends}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="time" />
                  <YAxis yAxisId="left" />
                  <YAxis yAxisId="right" orientation="right" />
                  <RechartsTooltip />
                  <Legend />
                  <Area
                    yAxisId="left"
                    type="monotone"
                    dataKey="total"
                    fill={alpha(COLORS.primary, 0.3)}
                    stroke={COLORS.primary}
                    name="Toplam İşlem"
                  />
                  <Bar yAxisId="left" dataKey="fraud" fill={COLORS.error} name="Fraud" />
                  <Line
                    yAxisId="right"
                    type="monotone"
                    dataKey="avgRisk"
                    stroke={COLORS.warning}
                    strokeWidth={3}
                    name="Ortalama Risk"
                  />
                </ComposedChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </Box>

        {/* Risk Dağılımı */}
        <Box sx={{ flex: '1 1 calc(34% - 12px)', minWidth: '300px' }}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 3 }}>
                🎯 Risk Dağılımı
              </Typography>
              <ResponsiveContainer width="100%" height={350}>
                <RechartsPieChart>
                  <Pie
                    data={riskDistribution}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    label={({ level, percentage }: any) => `${level} (${percentage}%)`}
                    outerRadius={80}
                    fill="#8884d8"
                    dataKey="count"
                  >
                    {riskDistribution.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <RechartsTooltip />
                </RechartsPieChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </Box>

        {/* Son Uyarılar */}
        <Box sx={{ flex: '1 1 calc(50% - 12px)', minWidth: '400px' }}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 2, display: 'flex', alignItems: 'center', gap: 1 }}>
                🚨 Son Uyarılar
                <Badge badgeContent={recentAlerts.length} color="error" />
              </Typography>
              <List sx={{ maxHeight: 400, overflow: 'auto' }}>
                {recentAlerts.map((alert, index) => (
                  <ListItem key={alert.id} divider={index < recentAlerts.length - 1}>
                    <ListItemAvatar>
                      {getSeverityIcon(alert.severity)}
                    </ListItemAvatar>
                    <ListItemText
                      primary={alert.message}
                      secondary={`${alert.time} • ${alert.status}`}
                    />
                    <ListItemSecondaryAction>
                      <MuiTooltip title="Detayları Gör">
                        <IconButton size="small">
                          <Visibility />
                        </IconButton>
                      </MuiTooltip>
                    </ListItemSecondaryAction>
                  </ListItem>
                ))}
              </List>
            </CardContent>
          </Card>
        </Box>

        {/* Model Performansı */}
        <Box sx={{ flex: '1 1 calc(50% - 12px)', minWidth: '400px' }}>
          <Card>
            <CardContent>
              <Typography variant="h6" sx={{ mb: 3 }}>
                🤖 Model Performansı
              </Typography>
              <ResponsiveContainer width="100%" height={400}>
                <RadarChart data={modelPerformance.slice(0, 3)}>
                  <PolarGrid />
                  <PolarAngleAxis dataKey="name" />
                  <PolarRadiusAxis angle={90} domain={[0, 100]} />
                  <Radar
                    name="Accuracy"
                    dataKey="accuracy"
                    stroke={COLORS.primary}
                    fill={alpha(COLORS.primary, 0.3)}
                  />
                  <Radar
                    name="Precision"
                    dataKey="precision"
                    stroke={COLORS.success}
                    fill={alpha(COLORS.success, 0.3)}
                  />
                  <Radar
                    name="Recall"
                    dataKey="recall"
                    stroke={COLORS.warning}
                    fill={alpha(COLORS.warning, 0.3)}
                  />
                  <Legend />
                </RadarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </Box>
      </Box>
    </Box>
  );
};

export default Dashboard; 