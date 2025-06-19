import React, { useState, useEffect } from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  TextField,
  Button,
  CircularProgress,
  Alert,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Chip,
  Divider,
} from '@mui/material';
import {
  Psychology as PsychologyIcon,
  Search as SearchIcon,
  ModelTraining as ModelIcon,
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
} from 'recharts';
import FraudDetectionAPI, { ShapExplanation } from '../services/api';

const ShapAnalysis: React.FC = () => {
  const [transactionId, setTransactionId] = useState('');
  const [modelType, setModelType] = useState('Ensemble');
  const [shapData, setShapData] = useState<ShapExplanation | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Örnek SHAP verileri
  const sampleShapData = {
    featureImportance: {
      'amount': 0.45,
      'hour': 0.23,
      'merchant_category': 0.18,
      'user_age': 0.12,
      'day_of_week': 0.08,
      'is_weekend': -0.15,
      'user_location': -0.22,
      'payment_method': -0.09,
    },
    businessExplanation: `Bu işlem için ana risk faktörleri:
    • Yüksek işlem tutarı (450 TL) normal limitin üzerinde
    • Geç saatlerde (02:30) yapılan işlem şüpheli
    • Merchant kategorisi (online oyun) riskli kategori
    • Kullanıcı yaşı (19) genç kullanıcı profili`,
  };

  const handleAnalyze = async () => {
    if (!transactionId.trim()) {
      setError('Lütfen bir işlem ID\'si girin');
      return;
    }

    try {
      setLoading(true);
      setError(null);
      
      // API çağrısı (şimdilik mock data)
      setTimeout(() => {
        setShapData({
          transactionId,
          shapValues: {},
          featureImportance: sampleShapData.featureImportance,
          plotUrls: [],
          businessExplanation: sampleShapData.businessExplanation,
          baseValue: 0.5,
          prediction: 0.85,
        });
        setLoading(false);
      }, 2000);

      // Gerçek API çağrısı:
      // const result = await FraudDetectionAPI.getShapExplanation(transactionId, modelType);
      // setShapData(result);
      
    } catch (err) {
      setError('SHAP analizi sırasında hata oluştu');
      console.error('SHAP analiz hatası:', err);
    } finally {
      setLoading(false);
    }
  };

  const renderShapChart = () => {
    if (!shapData?.featureImportance) return null;

    const chartData = Object.entries(shapData.featureImportance)
      .map(([feature, value]) => ({
        feature: feature.replace('_', ' ').toUpperCase(),
        value: value,
        color: value > 0 ? '#f44336' : '#4caf50',
      }))
      .sort((a, b) => Math.abs(b.value) - Math.abs(a.value));

    return (
      <ResponsiveContainer width="100%" height={400}>
        <BarChart data={chartData} layout="horizontal">
          <CartesianGrid strokeDasharray="3 3" />
          <XAxis type="number" domain={[-1, 1]} />
          <YAxis type="category" dataKey="feature" width={150} />
          <Tooltip 
            formatter={(value: number) => [
              `${value > 0 ? 'Risk Artırıcı' : 'Risk Azaltıcı'}: ${Math.abs(value).toFixed(3)}`,
              'SHAP Değeri'
            ]}
          />
          <Bar 
            dataKey="value" 
            fill="#1976d2"
            radius={[0, 4, 4, 0]}
          />
        </BarChart>
      </ResponsiveContainer>
    );
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
        <PsychologyIcon sx={{ fontSize: 40, color: '#9c27b0' }} />
        <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
          SHAP Explainability Analizi
        </Typography>
      </Box>

      <Typography variant="body1" color="textSecondary" sx={{ mb: 4 }}>
        Model kararlarının açıklanması için SHAP (SHapley Additive exPlanations) analizi yapın.
        Her özelliğin model tahminindeki katkısını görselleştirin.
      </Typography>

      {/* Analiz Formu */}
      <Card sx={{ mb: 4 }}>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            İşlem Analizi
          </Typography>
          
          <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'end' }}>
            <TextField
              label="İşlem ID"
              value={transactionId}
              onChange={(e) => setTransactionId(e.target.value)}
              placeholder="Örn: TXN-2024-001234"
              sx={{ minWidth: 250 }}
              helperText="Analiz edilecek işlemin ID'sini girin"
            />
            
            <FormControl sx={{ minWidth: 150 }}>
              <InputLabel>Model Tipi</InputLabel>
              <Select
                value={modelType}
                onChange={(e) => setModelType(e.target.value)}
                label="Model Tipi"
              >
                <MenuItem value="Ensemble">Ensemble</MenuItem>
                <MenuItem value="LightGBM">LightGBM</MenuItem>
                <MenuItem value="PCA">PCA</MenuItem>
              </Select>
            </FormControl>
            
            <Button
              variant="contained"
              onClick={handleAnalyze}
              disabled={loading}
              startIcon={loading ? <CircularProgress size={20} /> : <SearchIcon />}
              sx={{ height: 56 }}
            >
              {loading ? 'Analiz Ediliyor...' : 'Analiz Et'}
            </Button>
          </Box>
          
          {error && (
            <Alert severity="error" sx={{ mt: 2 }}>
              {error}
            </Alert>
          )}
        </CardContent>
      </Card>

      {/* SHAP Sonuçları */}
      {shapData && (
        <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
          {/* SHAP Bar Chart */}
          <Card sx={{ flex: 2, minWidth: 400 }}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <TrendingUpIcon color="primary" />
                <Typography variant="h6">
                  Feature Importance (SHAP Values)
                </Typography>
              </Box>
              
              <Typography variant="body2" color="textSecondary" sx={{ mb: 3 }}>
                İşlem ID: <strong>{shapData.transactionId}</strong> - Model: <strong>{modelType}</strong>
              </Typography>
              
              {renderShapChart()}
              
              <Box sx={{ mt: 2, display: 'flex', gap: 2, justifyContent: 'center' }}>
                <Chip 
                  label="Risk Artırıcı" 
                  sx={{ bgcolor: '#ffebee', color: '#f44336' }}
                  size="small"
                />
                <Chip 
                  label="Risk Azaltıcı" 
                  sx={{ bgcolor: '#e8f5e8', color: '#4caf50' }}
                  size="small"
                />
              </Box>
            </CardContent>
          </Card>

          {/* Business Explanation */}
          <Card sx={{ flex: 1, minWidth: 300 }}>
            <CardContent>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                <ModelIcon color="primary" />
                <Typography variant="h6">
                  İş Açıklaması
                </Typography>
              </Box>
              
              <Box sx={{ 
                bgcolor: '#f5f5f5', 
                p: 2, 
                borderRadius: 1,
                border: '1px solid #e0e0e0' 
              }}>
                <Typography variant="body2" sx={{ whiteSpace: 'pre-line' }}>
                  {shapData.businessExplanation}
                </Typography>
              </Box>

              <Divider sx={{ my: 2 }} />

              <Typography variant="subtitle2" gutterBottom>
                Ana Risk Faktörleri:
              </Typography>
              
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                {Object.entries(shapData.featureImportance)
                  .filter(([_, value]) => Math.abs(value) > 0.1)
                  .sort(([,a], [,b]) => Math.abs(b) - Math.abs(a))
                  .slice(0, 5)
                  .map(([feature, value]) => (
                    <Box 
                      key={feature}
                      sx={{ 
                        display: 'flex', 
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        p: 1,
                        bgcolor: value > 0 ? '#ffebee' : '#e8f5e8',
                        borderRadius: 1,
                      }}
                    >
                      <Typography variant="body2" sx={{ textTransform: 'capitalize' }}>
                        {feature.replace('_', ' ')}
                      </Typography>
                      <Typography 
                        variant="body2" 
                        sx={{ 
                          fontWeight: 'bold',
                          color: value > 0 ? '#f44336' : '#4caf50'
                        }}
                      >
                        {value > 0 ? '+' : ''}{value.toFixed(3)}
                      </Typography>
                    </Box>
                  ))}
              </Box>
            </CardContent>
          </Card>
        </Box>
      )}

      {/* Örnek İşlemler */}
      {!shapData && (
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Örnek İşlem ID'leri
            </Typography>
            <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
              Test için kullanabileceğiniz örnek işlem ID'leri:
            </Typography>
            
            <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
              {[
                'TXN-2024-001234',
                'TXN-2024-005678',
                'TXN-2024-009012',
                'TXN-2024-003456',
                'TXN-2024-007890',
              ].map((id) => (
                <Chip
                  key={id}
                  label={id}
                  onClick={() => setTransactionId(id)}
                  variant="outlined"
                  clickable
                />
              ))}
            </Box>
          </CardContent>
        </Card>
      )}
    </Box>
  );
};

export default ShapAnalysis; 