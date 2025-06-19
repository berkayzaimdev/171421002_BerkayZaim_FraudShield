import React, { useState, useEffect } from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  Button,
  CircularProgress,
  Alert,
  Chip,
  LinearProgress,
  Tabs,
  Tab,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
} from '@mui/material';
import {
  ModelTraining as ModelIcon,
  Timeline as TimelineIcon,
  Assessment as AssessmentIcon,
  CompareArrows as CompareIcon,
  TrendingUp as TrendingUpIcon,
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
  RadarChart,
  PolarGrid,
  PolarAngleAxis,
  PolarRadiusAxis,
  Radar,
} from 'recharts';
import { FraudDetectionAPI } from '../services/api';

// ==================== INTERFACES ====================
interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

interface ModelAnalysisData {
  modelName: string;
  accuracy: number;
  precision: number;
  recall: number;
  f1Score: number;
  auc: number;
  trainingDate: string;
}

// ==================== HELPER COMPONENT ====================
function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`simple-tabpanel-${index}`}
      aria-labelledby={`simple-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
    </div>
  );
}

const ModelAnalysis: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [models, setModels] = useState<ModelAnalysisData[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Örnek model verileri
  const sampleModels: ModelAnalysisData[] = [
    {
      modelName: 'Ensemble',
      accuracy: 94.2,
      precision: 92.5,
      recall: 89.3,
      f1Score: 90.8,
      auc: 95.1,
      trainingDate: '2024-01-15',
    },
    {
      modelName: 'LightGBM',
      accuracy: 91.8,
      precision: 88.7,
      recall: 87.2,
      f1Score: 87.9,
      auc: 92.3,
      trainingDate: '2024-01-14',
    },
    {
      modelName: 'PCA',
      accuracy: 87.5,
      precision: 85.1,
      recall: 82.4,
      f1Score: 83.7,
      auc: 89.6,
      trainingDate: '2024-01-13',
    },
  ];

  // Feature importance örnek verisi
  const featureImportanceData = [
    { feature: 'Amount', importance: 0.35 },
    { feature: 'Hour', importance: 0.22 },
    { feature: 'Merchant Category', importance: 0.18 },
    { feature: 'User Age', importance: 0.12 },
    { feature: 'Day of Week', importance: 0.08 },
    { feature: 'Payment Method', importance: 0.05 },
  ];

  // Model performans trendi
  const performanceTrend = [
    { date: '2024-01-10', accuracy: 89.2, precision: 87.1, recall: 85.3 },
    { date: '2024-01-11', accuracy: 90.5, precision: 88.4, recall: 86.8 },
    { date: '2024-01-12', accuracy: 91.8, precision: 89.7, recall: 87.5 },
    { date: '2024-01-13', accuracy: 92.3, precision: 90.2, recall: 88.1 },
    { date: '2024-01-14', accuracy: 93.6, precision: 91.5, recall: 89.2 },
    { date: '2024-01-15', accuracy: 94.2, precision: 92.5, recall: 89.3 },
  ];

  // Radar chart için model karşılaştırma verisi
  const radarData = [
    { metric: 'Accuracy', Ensemble: 94.2, LightGBM: 91.8, PCA: 87.5 },
    { metric: 'Precision', Ensemble: 92.5, LightGBM: 88.7, PCA: 85.1 },
    { metric: 'Recall', Ensemble: 89.3, LightGBM: 87.2, PCA: 82.4 },
    { metric: 'F1-Score', Ensemble: 90.8, LightGBM: 87.9, PCA: 83.7 },
    { metric: 'AUC', Ensemble: 95.1, LightGBM: 92.3, PCA: 89.6 },
  ];

  useEffect(() => {
    const loadModels = async () => {
      try {
        setLoading(true);
        // API çağrısı (şimdilik mock data)
        setTimeout(() => {
          setModels(sampleModels);
          setLoading(false);
        }, 1000);

        // Gerçek API çağrısı:
        // const modelList = await FraudDetectionAPI.getAllModels();
        // const modelMetrics = await Promise.all(
        //   modelList.map(name => FraudDetectionAPI.getModelMetrics(name))
        // );
        // setModels(modelMetrics);

      } catch (err) {
        setError('Model verileri yüklenirken hata oluştu');
        console.error('Model yükleme hatası:', err);
      } finally {
        setLoading(false);
      }
    };

    loadModels();
  }, []);

  const getStatusColor = (value: number) => {
    if (value >= 90) return '#4caf50';
    if (value >= 80) return '#ff9800';
    return '#f44336';
  };

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  if (loading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '400px' }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return <Alert severity="error">{error}</Alert>;
  }

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <ModelIcon sx={{ fontSize: 40, color: '#1976d2' }} />
        <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
          Model Performans Analizi
        </Typography>
      </Box>

      <Typography variant="body1" color="textSecondary" sx={{ mb: 4 }}>
        Fraud detection modellerinin performans metrikleri, karşılaştırması ve trend analizi.
      </Typography>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={activeTab} onChange={handleTabChange}>
          <Tab
            label="Model Karşılaştırma"
            icon={<CompareIcon />}
            iconPosition="start"
          />
          <Tab
            label="Performans Trendi"
            icon={<TimelineIcon />}
            iconPosition="start"
          />
          <Tab
            label="Feature Importance"
            icon={<AssessmentIcon />}
            iconPosition="start"
          />
        </Tabs>
      </Box>

      {/* Model Karşılaştırma Tab */}
      <TabPanel value={activeTab} index={0}>
        <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
          {/* Model Kartları */}
          <Box sx={{ flex: 1, minWidth: 300 }}>
            <Typography variant="h6" gutterBottom>
              Aktif Modeller
            </Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              {models.map((model) => (
                <Card key={model.modelName} sx={{ border: model.modelName === 'Ensemble' ? '2px solid #1976d2' : '1px solid #e0e0e0' }}>
                  <CardContent>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                      <Typography variant="h6">
                        {model.modelName}
                      </Typography>
                      {model.modelName === 'Ensemble' && (
                        <Chip label="Aktif Model" color="primary" size="small" />
                      )}
                    </Box>

                    <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 2 }}>
                      <Box>
                        <Typography variant="body2" color="textSecondary">Accuracy</Typography>
                        <Typography variant="h6" sx={{ color: getStatusColor(model.accuracy) }}>
                          %{model.accuracy}
                        </Typography>
                      </Box>
                      <Box>
                        <Typography variant="body2" color="textSecondary">Precision</Typography>
                        <Typography variant="h6" sx={{ color: getStatusColor(model.precision) }}>
                          %{model.precision}
                        </Typography>
                      </Box>
                      <Box>
                        <Typography variant="body2" color="textSecondary">Recall</Typography>
                        <Typography variant="h6" sx={{ color: getStatusColor(model.recall) }}>
                          %{model.recall}
                        </Typography>
                      </Box>
                      <Box>
                        <Typography variant="body2" color="textSecondary">F1-Score</Typography>
                        <Typography variant="h6" sx={{ color: getStatusColor(model.f1Score) }}>
                          %{model.f1Score}
                        </Typography>
                      </Box>
                    </Box>

                    <Box sx={{ mt: 2 }}>
                      <Typography variant="body2" color="textSecondary" gutterBottom>
                        AUC: %{model.auc}
                      </Typography>
                      <LinearProgress
                        variant="determinate"
                        value={model.auc}
                        sx={{ height: 8, borderRadius: 4 }}
                      />
                    </Box>

                    <Typography variant="caption" color="textSecondary" sx={{ mt: 1, display: 'block' }}>
                      Son Eğitim: {model.trainingDate ? new Date(model.trainingDate).toLocaleDateString('tr-TR') : 'Bilinmiyor'}
                    </Typography>
                  </CardContent>
                </Card>
              ))}
            </Box>
          </Box>

          {/* Radar Chart */}
          <Card sx={{ flex: 1, minWidth: 400 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Model Karşılaştırma Radarı
              </Typography>
              <ResponsiveContainer width="100%" height={400}>
                <RadarChart data={radarData}>
                  <PolarGrid />
                  <PolarAngleAxis dataKey="metric" />
                  <PolarRadiusAxis angle={30} domain={[70, 100]} />
                  <Radar name="Ensemble" dataKey="Ensemble" stroke="#1976d2" fill="#1976d2" fillOpacity={0.3} />
                  <Radar name="LightGBM" dataKey="LightGBM" stroke="#ff9800" fill="#ff9800" fillOpacity={0.3} />
                  <Radar name="PCA" dataKey="PCA" stroke="#f44336" fill="#f44336" fillOpacity={0.3} />
                  <Tooltip />
                </RadarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </Box>
      </TabPanel>

      {/* Performans Trendi Tab */}
      <TabPanel value={activeTab} index={1}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Model Performans Trendi (Son 7 Gün)
            </Typography>
            <ResponsiveContainer width="100%" height={400}>
              <LineChart data={performanceTrend}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" />
                <YAxis domain={[80, 100]} />
                <Tooltip />
                <Line type="monotone" dataKey="accuracy" stroke="#1976d2" name="Accuracy" strokeWidth={2} />
                <Line type="monotone" dataKey="precision" stroke="#ff9800" name="Precision" strokeWidth={2} />
                <Line type="monotone" dataKey="recall" stroke="#4caf50" name="Recall" strokeWidth={2} />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </TabPanel>

      {/* Feature Importance Tab */}
      <TabPanel value={activeTab} index={2}>
        <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
          {/* Feature Importance Chart */}
          <Card sx={{ flex: 2, minWidth: 400 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Feature Importance (Ensemble Model)
              </Typography>
              <ResponsiveContainer width="100%" height={400}>
                <BarChart data={featureImportanceData} layout="horizontal">
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis type="number" domain={[0, 0.4]} />
                  <YAxis type="category" dataKey="feature" width={120} />
                  <Tooltip formatter={(value) => [`${(Number(value) * 100).toFixed(1)}%`, 'Önem Derecesi']} />
                  <Bar dataKey="importance" fill="#1976d2" radius={[0, 4, 4, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          {/* Feature Açıklamaları */}
          <Card sx={{ flex: 1, minWidth: 300 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Feature Açıklamaları
              </Typography>
              <TableContainer component={Paper} elevation={0}>
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Feature</TableCell>
                      <TableCell align="right">Önem (%)</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {featureImportanceData.map((row) => (
                      <TableRow key={row.feature}>
                        <TableCell>{row.feature}</TableCell>
                        <TableCell align="right">
                          <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', gap: 1 }}>
                            <LinearProgress
                              variant="determinate"
                              value={row.importance * 100}
                              sx={{ width: 60, height: 6, borderRadius: 3 }}
                            />
                            <Typography variant="body2">
                              {(row.importance * 100).toFixed(1)}%
                            </Typography>
                          </Box>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            </CardContent>
          </Card>
        </Box>
      </TabPanel>
    </Box>
  );
};

export default ModelAnalysis; 