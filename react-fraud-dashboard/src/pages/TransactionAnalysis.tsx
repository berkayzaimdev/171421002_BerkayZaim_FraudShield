import React, { useState, useEffect } from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  Button,
  TextField,
  MenuItem,
  FormControl,
  InputLabel,
  Select,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Alert,
  Tabs,
  Tab,
  LinearProgress,
} from '@mui/material';
import {
  AccountBalance as TransactionIcon,
  TrendingUp as TrendingUpIcon,
  Warning as WarningIcon,
  CheckCircle as CheckCircleIcon,
  Search as SearchIcon,
  Security as SecurityIcon,
  Assessment as AssessmentIcon,
  Timeline as TimelineIcon,
} from '@mui/icons-material';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  LineChart,
  Line,
  PieChart,
  Pie,
  Cell,
  ScatterChart,
  Scatter,
} from 'recharts';
import FraudDetectionAPI, { Transaction, ModelPrediction } from '../services/api';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

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

const TransactionAnalysis: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [singleTransactionId, setSingleTransactionId] = useState('');
  const [prediction, setPrediction] = useState<ModelPrediction | null>(null);
  const [loading, setLoading] = useState(false);

  // Risk dağılımı verileri
  const riskDistributionData = [
    { name: 'Düşük Risk', value: 12450, color: '#4caf50' },
    { name: 'Orta Risk', value: 3420, color: '#ff9800' },
    { name: 'Yüksek Risk', value: 1230, color: '#f44336' },
  ];

  // Saatlik işlem dağılımı
  const hourlyDistributionData = [
    { hour: '00-02', normal: 234, fraud: 45 },
    { hour: '02-04', normal: 123, fraud: 67 },
    { hour: '04-06', normal: 156, fraud: 23 },
    { hour: '06-08', normal: 567, fraud: 12 },
    { hour: '08-10', normal: 1234, fraud: 34 },
    { hour: '10-12', normal: 1567, fraud: 45 },
    { hour: '12-14', normal: 1789, fraud: 56 },
    { hour: '14-16', normal: 1456, fraud: 67 },
    { hour: '16-18', normal: 1345, fraud: 78 },
    { hour: '18-20', normal: 1123, fraud: 89 },
    { hour: '20-22', normal: 897, fraud: 95 },
    { hour: '22-24', normal: 456, fraud: 87 },
  ];

  // Kategori bazlı risk analizi
  const categoryRiskData = [
    { category: 'Market', total: 5670, fraud: 45, rate: 0.8 },
    { category: 'Restoran', total: 3420, fraud: 67, rate: 2.0 },
    { category: 'Teknoloji', total: 2340, fraud: 89, rate: 3.8 },
    { category: 'Online Oyun', total: 1560, fraud: 156, rate: 10.0 },
    { category: 'Taksi', total: 2100, fraud: 78, rate: 3.7 },
    { category: 'Benzin', total: 4500, fraud: 23, rate: 0.5 },
  ];

  // Tutar-risk scatter plot
  const amountRiskScatterData = [
    { amount: 50, risk: 0.1, category: 'Market' },
    { amount: 150, risk: 0.3, category: 'Restoran' },
    { amount: 450, risk: 0.8, category: 'Online Oyun' },
    { amount: 750, risk: 0.4, category: 'Teknoloji' },
    { amount: 1200, risk: 0.6, category: 'Taksi' },
    { amount: 2500, risk: 0.9, category: 'Taksi' },
    { amount: 3500, risk: 0.95, category: 'Online Oyun' },
    { amount: 89, risk: 0.2, category: 'Benzin' },
    { amount: 340, risk: 0.4, category: 'Market' },
    { amount: 670, risk: 0.7, category: 'Teknoloji' },
  ];

  const handleAnalyzeSingleTransaction = async () => {
    if (!singleTransactionId.trim()) {
      return;
    }

    try {
      setLoading(true);
      // Mock tahmin
      const mockPrediction: ModelPrediction = {
        transactionId: singleTransactionId,
        isFraudulent: 'Yes',
        probability: 0.85,
        score: 8.5,
        anomalyScore: 0.75,
        riskLevel: 'High',
        modelType: 'Ensemble',
      };

      setTimeout(() => {
        setPrediction(mockPrediction);
        setLoading(false);
      }, 2000);

      // Gerçek API çağrısı:
      // const mockTransaction: Transaction = { ... };
      // const result = await FraudDetectionAPI.predictTransaction(mockTransaction);
      // setPrediction(result);

    } catch (err) {
      console.error('Tahmin hatası:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  const getRiskLevelColor = (level: string | undefined) => {
    if (!level) return '#666';
    switch (level) {
      case 'High': return '#f44336';
      case 'Medium': return '#ff9800';
      case 'Low': return '#4caf50';
      default: return '#666';
    }
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <TransactionIcon sx={{ fontSize: 40, color: '#1976d2' }} />
        <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
          İşlem Risk Analizi
        </Typography>
      </Box>

      <Typography variant="body1" color="textSecondary" sx={{ mb: 4 }}>
        İşlem bazlı risk değerlendirme, tahmin ve analiz araçları.
      </Typography>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={activeTab} onChange={handleTabChange}>
          <Tab 
            label="Tekil Analiz" 
            icon={<SearchIcon />} 
            iconPosition="start"
          />
          <Tab 
            label="Risk Dağılımları" 
            icon={<AssessmentIcon />} 
            iconPosition="start"
          />
          <Tab 
            label="Trend Analizleri" 
            icon={<TimelineIcon />} 
            iconPosition="start"
          />
        </Tabs>
      </Box>

      {/* Tekil Analiz Tab */}
      <TabPanel value={activeTab} index={0}>
        <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
          {/* Tekil İşlem Analizi */}
          <Box sx={{ flex: 1, minWidth: 400 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Tekil İşlem Risk Değerlendirmesi
                </Typography>
                
                <Box sx={{ display: 'flex', gap: 2, mb: 3, alignItems: 'end' }}>
                  <TextField
                    label="İşlem ID"
                    value={singleTransactionId}
                    onChange={(e) => setSingleTransactionId(e.target.value)}
                    placeholder="TXN-2024-001234"
                    fullWidth
                  />
                  <Button
                    variant="contained"
                    onClick={handleAnalyzeSingleTransaction}
                    disabled={loading}
                    startIcon={loading ? null : <SearchIcon />}
                    sx={{ height: 56, minWidth: 120 }}
                  >
                    {loading ? 'Analiz...' : 'Analiz Et'}
                  </Button>
                </Box>

                {loading && <LinearProgress sx={{ mb: 2 }} />}

                {prediction && (
                  <Box sx={{ mt: 3 }}>
                    <Alert severity={prediction.isFraudulent === 'Yes' ? 'error' : 'success'} sx={{ mb: 3 }}>
                      <Typography variant="h6">
                        {prediction.isFraudulent === 'Yes' ? 'FRAUD RİSKİ TESPİT EDİLDİ!' : 'Normal İşlem'}
                      </Typography>
                      <Typography>
                        Risk Skoru: {prediction.score}/10
                      </Typography>
                    </Alert>

                    <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 2 }}>
                      <Box>
                        <Typography variant="body2" color="textSecondary">
                          Fraud Olasılığı
                        </Typography>
                        <Typography variant="h6" sx={{ color: getRiskLevelColor(prediction.riskLevel) }}>
                          %{(prediction.probability * 100).toFixed(1)}
                        </Typography>
                      </Box>
                      
                      <Box sx={{ textAlign: 'center' }}>
                        <Typography variant="caption" color="textSecondary">
                          Anomali Skoru
                        </Typography>
                        <Typography variant="h6" sx={{ color: getRiskLevelColor(prediction.riskLevel) }}>
                          %{((prediction.anomalyScore || 0) * 100).toFixed(1)}
                        </Typography>
                      </Box>
                      
                      <Box>
                        <Typography variant="body2" color="textSecondary">
                          Risk Seviyesi
                        </Typography>
                        <Chip 
                          label={prediction.riskLevel}
                          color={prediction.riskLevel === 'High' ? 'error' : 
                                prediction.riskLevel === 'Medium' ? 'warning' : 'success'}
                        />
                      </Box>
                      
                      <Box>
                        <Typography variant="body2" color="textSecondary">
                          İşlem ID
                        </Typography>
                        <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
                          {prediction.transactionId}
                        </Typography>
                      </Box>
                    </Box>

                    <Button 
                      variant="outlined" 
                      fullWidth 
                      sx={{ mt: 2 }}
                      href={`/shap?transactionId=${prediction.transactionId}`}
                    >
                      SHAP Analizi Yap
                    </Button>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Box>

          {/* Risk İstatistikleri */}
          <Box sx={{ flex: 1, minWidth: 400 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Günlük Risk İstatistikleri
                </Typography>
                
                <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                    <CheckCircleIcon sx={{ color: '#4caf50' }} />
                    <Box sx={{ flexGrow: 1 }}>
                      <Typography variant="body2" color="textSecondary">
                        Normal İşlemler
                      </Typography>
                      <Typography variant="h6">
                        24,156
                      </Typography>
                    </Box>
                    <Typography variant="h4" color="success.main">
                      96.8%
                    </Typography>
                  </Box>

                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                    <WarningIcon sx={{ color: '#ff9800' }} />
                    <Box sx={{ flexGrow: 1 }}>
                      <Typography variant="body2" color="textSecondary">
                        Şüpheli İşlemler
                      </Typography>
                      <Typography variant="h6">
                        567
                      </Typography>
                    </Box>
                    <Typography variant="h4" color="warning.main">
                      2.3%
                    </Typography>
                  </Box>

                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                    <SecurityIcon sx={{ color: '#f44336' }} />
                    <Box sx={{ flexGrow: 1 }}>
                      <Typography variant="body2" color="textSecondary">
                        Fraud İşlemler
                      </Typography>
                      <Typography variant="h6">
                        234
                      </Typography>
                    </Box>
                    <Typography variant="h4" color="error.main">
                      0.9%
                    </Typography>
                  </Box>
                </Box>

                <ResponsiveContainer width="100%" height={200} style={{ marginTop: '20px' }}>
                  <PieChart>
                    <Pie
                      data={riskDistributionData}
                      cx="50%"
                      cy="50%"
                      outerRadius={60}
                      dataKey="value"
                      label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(1)}%`}
                    >
                      {riskDistributionData.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={entry.color} />
                      ))}
                    </Pie>
                    <Tooltip />
                  </PieChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Box>
        </Box>
      </TabPanel>

      {/* Risk Dağılımları Tab */}
      <TabPanel value={activeTab} index={1}>
        <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
          {/* Kategori Bazlı Risk */}
          <Box sx={{ flex: 1, minWidth: 500 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Kategori Bazlı Fraud Oranları
                </Typography>
                <ResponsiveContainer width="100%" height={400}>
                  <BarChart data={categoryRiskData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="category" />
                    <YAxis />
                    <Tooltip 
                      formatter={(value, name) => [
                        name === 'rate' ? `%${value}` : value,
                        name === 'rate' ? 'Fraud Oranı' : 'İşlem Sayısı'
                      ]}
                    />
                    <Bar dataKey="total" fill="#1976d2" name="Toplam İşlem" />
                    <Bar dataKey="fraud" fill="#f44336" name="Fraud İşlem" />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Box>

          {/* Tutar-Risk Scatter */}
          <Box sx={{ flex: 1, minWidth: 500 }}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Tutar vs Risk Skoru Dağılımı
                </Typography>
                <ResponsiveContainer width="100%" height={400}>
                  <ScatterChart data={amountRiskScatterData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis type="number" dataKey="amount" name="Tutar" unit="₺" />
                    <YAxis type="number" dataKey="risk" name="Risk" domain={[0, 1]} />
                    <Tooltip 
                      formatter={(value, name) => [
                        name === 'risk' ? (Number(value) * 100).toFixed(1) + '%' : '₺' + value,
                        name === 'risk' ? 'Risk Skoru' : 'Tutar'
                      ]}
                    />
                    <Scatter dataKey="risk" fill="#ff6b6b" />
                  </ScatterChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Box>
        </Box>
      </TabPanel>

      {/* Trend Analizleri Tab */}
      <TabPanel value={activeTab} index={2}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Saatlik İşlem ve Fraud Dağılımı
            </Typography>
            <ResponsiveContainer width="100%" height={400}>
              <LineChart data={hourlyDistributionData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="hour" />
                <YAxis />
                <Tooltip />
                <Line 
                  type="monotone" 
                  dataKey="normal" 
                  stroke="#4caf50" 
                  name="Normal İşlemler" 
                  strokeWidth={3}
                />
                <Line 
                  type="monotone" 
                  dataKey="fraud" 
                  stroke="#f44336" 
                  name="Fraud İşlemler" 
                  strokeWidth={3}
                />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </TabPanel>
    </Box>
  );
};

export default TransactionAnalysis; 