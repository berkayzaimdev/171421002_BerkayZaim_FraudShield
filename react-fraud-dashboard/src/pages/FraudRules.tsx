import React, { useState, useEffect } from 'react';
import {
  Card,
  CardContent,
  Typography,
  Box,
  Button,
  Switch,
  Chip,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  FormControlLabel,
  Alert,
  LinearProgress,
  Tabs,
  Tab,
} from '@mui/material';
import {
  Rule as RuleIcon,
  Edit as EditIcon,
  Delete as DeleteIcon,
  Add as AddIcon,
  Timeline as TimelineIcon,
  Assessment as AssessmentIcon,
  CheckCircle as CheckCircleIcon,
  Cancel as CancelIcon,
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
} from 'recharts';
import FraudDetectionAPI, { FraudRule } from '../services/api';

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

const FraudRules: React.FC = () => {
  const [activeTab, setActiveTab] = useState(0);
  const [rules, setRules] = useState<FraudRule[]>([]);
  const [loading, setLoading] = useState(true);
  const [editDialog, setEditDialog] = useState(false);
  const [selectedRule, setSelectedRule] = useState<FraudRule | null>(null);
  const [newRule, setNewRule] = useState({
    name: '',
    description: '',
    isActive: true,
  });

  // Örnek kural verileri
  const sampleRules: FraudRule[] = [
    {
      id: '1',
      name: 'Yüksek Tutar Kontrolü',
      description: 'Günlük 5000 TL üzeri işlemleri kontrol et',
      isActive: true,
      condition: 'amount > 5000',
      action: 'BLOCK',
      severity: 'High',
      triggerCount: 245,
      createdAt: '2024-01-10T10:00:00Z',
      successRate: 87
    },
    {
      id: '2',
      name: 'Gece Saati İşlemleri',
      description: '02:00-06:00 arası yapılan işlemleri kontrol et',
      isActive: true,
      condition: 'hour >= 2 && hour <= 6',
      action: 'REVIEW',
      severity: 'Medium',
      triggerCount: 156,
      createdAt: '2024-01-10T10:00:00Z',
      successRate: 73
    },
    {
      id: '3',
      name: 'Çoklu Cihaz Kullanımı',
      description: 'Aynı kullanıcının 1 saat içinde 3+ farklı cihazdan işlem yapması',
      isActive: false,
      condition: 'uniqueDeviceCount > 2',
      action: 'ALERT',
      severity: 'Medium',
      triggerCount: 89,
      createdAt: '2024-01-10T10:00:00Z',
      successRate: 65
    },
    {
      id: '4',
      name: 'Şüpheli IP Kontrolü',
      description: 'Kara listede olan IP adreslerinden gelen işlemler',
      isActive: true,
      condition: 'isBlacklistedIP = true',
      action: 'BLOCK',
      severity: 'Critical',
      triggerCount: 312,
      createdAt: '2024-01-10T10:00:00Z',
      successRate: 94
    },
    {
      id: '5',
      name: 'Velocity Check',
      description: '5 dakika içinde 3+ ardışık işlem kontrolü',
      isActive: true,
      condition: 'transactionCount5Min >= 3',
      action: 'REVIEW',
      severity: 'High',
      triggerCount: 178,
      createdAt: '2024-01-10T10:00:00Z',
      successRate: 81
    }
  ];

  // Kural performans verileri
  const rulePerformanceData = [
    { name: 'Yüksek Tutar', triggers: 156, success: 133, rate: 85.2 },
    { name: 'Gece Saati', triggers: 89, success: 82, rate: 92.1 },
    { name: 'Şüpheli IP', triggers: 67, success: 63, rate: 94.3 },
    { name: 'Velocity', triggers: 134, success: 106, rate: 78.9 },
    { name: 'Çoklu Cihaz', triggers: 23, success: 18, rate: 76.8 },
  ];

  // Zaman bazlı kural tetiklenme verileri
  const ruleTimelineData = [
    { time: '08:00', highAmount: 12, nightTime: 2, suspiciousIP: 5, velocity: 8 },
    { time: '12:00', highAmount: 18, nightTime: 0, suspiciousIP: 7, velocity: 15 },
    { time: '16:00', highAmount: 25, nightTime: 0, suspiciousIP: 9, velocity: 20 },
    { time: '20:00', highAmount: 15, nightTime: 3, suspiciousIP: 6, velocity: 12 },
    { time: '00:00', highAmount: 8, nightTime: 25, suspiciousIP: 3, velocity: 5 },
    { time: '04:00', highAmount: 3, nightTime: 15, suspiciousIP: 1, velocity: 2 },
  ];

  // Kural durumu dağılımı
  const ruleStatusData = [
    { name: 'Aktif', value: 4, color: '#4caf50' },
    { name: 'Pasif', value: 1, color: '#f44336' },
  ];

  useEffect(() => {
    const loadRules = async () => {
      try {
        setLoading(true);
        // API çağrısı (şimdilik mock data)
        setTimeout(() => {
          setRules(sampleRules);
          setLoading(false);
        }, 1000);
        
        // Gerçek API çağrısı:
        // const rulesData = await FraudDetectionAPI.getFraudRules();
        // setRules(rulesData);
        
      } catch (err) {
        console.error('Kurallar yüklenirken hata oluştu:', err);
      } finally {
        setLoading(false);
      }
    };

    loadRules();
  }, []);

  const handleToggleRule = async (ruleId: string, isActive: boolean) => {
    try {
      setLoading(true);
      
      setRules(prev => prev.map(rule => 
        rule.id === ruleId ? { ...rule, isActive } : rule
      ));
    } catch (err) {
      console.error('Kural güncellenirken hata oluştu:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleEditRule = (rule: FraudRule) => {
    setSelectedRule(rule);
    setNewRule({
      name: rule.name,
      description: rule.description,
      isActive: rule.isActive,
    });
    setEditDialog(true);
  };

  const handleSaveRule = async () => {
    try {
      if (selectedRule) {
        // API çağrısı - güncelleme
        // await FraudDetectionAPI.updateFraudRule(selectedRule.id, newRule);
        
        setRules(prev => prev.map(rule => 
          rule.id === selectedRule.id 
            ? { ...rule, ...newRule }
            : rule
        ));
      }
      setEditDialog(false);
      setSelectedRule(null);
      setNewRule({ name: '', description: '', isActive: true });
    } catch (err) {
      console.error('Kural kaydedilirken hata oluştu:', err);
    }
  };

  const getStatusChip = (isActive: boolean) => (
    <Chip
      icon={isActive ? <CheckCircleIcon /> : <CancelIcon />}
      label={isActive ? 'Aktif' : 'Pasif'}
      color={isActive ? 'success' : 'error'}
      size="small"
    />
  );

  const handleTabChange = (_event: React.SyntheticEvent, newValue: number) => {
    setActiveTab(newValue);
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <RuleIcon sx={{ fontSize: 40, color: '#1976d2' }} />
          <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
            Fraud Kural Yönetimi
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => setEditDialog(true)}
        >
          Yeni Kural
        </Button>
      </Box>

      <Typography variant="body1" color="textSecondary" sx={{ mb: 4 }}>
        Fraud detection kurallarının yönetimi, performans analizi ve yapılandırması.
      </Typography>

      {/* Tabs */}
      <Box sx={{ borderBottom: 1, borderColor: 'divider', mb: 3 }}>
        <Tabs value={activeTab} onChange={handleTabChange}>
          <Tab 
            label="Kural Listesi" 
            icon={<RuleIcon />} 
            iconPosition="start"
          />
          <Tab 
            label="Performans Analizi" 
            icon={<AssessmentIcon />} 
            iconPosition="start"
          />
          <Tab 
            label="Zaman Analizi" 
            icon={<TimelineIcon />} 
            iconPosition="start"
          />
        </Tabs>
      </Box>

      {/* Kural Listesi Tab */}
      <TabPanel value={activeTab} index={0}>
        <Box sx={{ display: 'flex', gap: 3, flexWrap: 'wrap' }}>
          {/* Kural Tablosu */}
          <Card sx={{ flex: 2, minWidth: 600 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Aktif Kurallar
              </Typography>
              
              {loading ? (
                <LinearProgress />
              ) : (
                <TableContainer>
                  <Table>
                    <TableHead>
                      <TableRow>
                        <TableCell>Kural Adı</TableCell>
                        <TableCell>Açıklama</TableCell>
                        <TableCell align="center">Durum</TableCell>
                        <TableCell align="center">Tetiklenme</TableCell>
                        <TableCell align="center">Başarı Oranı</TableCell>
                        <TableCell align="center">İşlemler</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {rules.map((rule) => (
                        <TableRow key={rule.id}>
                          <TableCell>
                            <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
                              {rule.name}
                            </Typography>
                          </TableCell>
                          <TableCell>
                            <Typography variant="body2" color="textSecondary">
                              {rule.description}
                            </Typography>
                          </TableCell>
                          <TableCell align="center">
                            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 1 }}>
                              {getStatusChip(rule.isActive)}
                              <Switch
                                checked={rule.isActive}
                                onChange={(e) => handleToggleRule(rule.id, e.target.checked)}
                                size="small"
                              />
                            </Box>
                          </TableCell>
                          <TableCell align="center">
                            <Typography variant="body2">
                              {rule.triggerCount}
                            </Typography>
                          </TableCell>
                          <TableCell align="center">
                            <Typography 
                              variant="body2" 
                              sx={{ 
                                color: (rule.successRate || 0) > 80 ? '#4caf50' : 
                                       (rule.successRate || 0) > 60 ? '#ff9800' : '#f44336'
                              }}
                            >
                              %{rule.successRate || 0}
                            </Typography>
                          </TableCell>
                          <TableCell align="center">
                            <IconButton 
                              size="small" 
                              onClick={() => handleEditRule(rule)}
                            >
                              <EditIcon />
                            </IconButton>
                            <IconButton size="small" color="error">
                              <DeleteIcon />
                            </IconButton>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
            </CardContent>
          </Card>

          {/* Kural İstatistikleri */}
          <Card sx={{ flex: 1, minWidth: 300 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Kural İstatistikleri
              </Typography>
              
              <Box sx={{ mb: 3 }}>
                <Typography variant="body2" color="textSecondary" gutterBottom>
                  Toplam Kural Sayısı
                </Typography>
                <Typography variant="h4" color="primary">
                  {rules.length}
                </Typography>
              </Box>

              <Box sx={{ mb: 3 }}>
                <Typography variant="body2" color="textSecondary" gutterBottom>
                  Aktif Kurallar
                </Typography>
                <Typography variant="h4" color="success.main">
                  {rules.filter(r => r.isActive).length}
                </Typography>
              </Box>

              <Box sx={{ mb: 3 }}>
                <Typography variant="body2" color="textSecondary" gutterBottom>
                  Ortalama Başarı Oranı
                </Typography>
                <Typography variant="h4" color="warning.main">
                  %{(rules.reduce((acc, rule) => acc + (rule.successRate || 0), 0) / rules.length).toFixed(1)}
                </Typography>
              </Box>

              {/* Kural Durumu Pie Chart */}
              <Typography variant="body2" color="textSecondary" gutterBottom sx={{ mt: 3 }}>
                Kural Durumu Dağılımı
              </Typography>
              <ResponsiveContainer width="100%" height={200}>
                <PieChart>
                  <Pie
                    data={ruleStatusData}
                    cx="50%"
                    cy="50%"
                    outerRadius={60}
                    dataKey="value"
                    label={({ name, value }) => `${name}: ${value}`}
                  >
                    {ruleStatusData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </Box>
      </TabPanel>

      {/* Performans Analizi Tab */}
      <TabPanel value={activeTab} index={1}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Kural Performans Karşılaştırması
            </Typography>
            <ResponsiveContainer width="100%" height={400}>
              <BarChart data={rulePerformanceData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="name" />
                <YAxis />
                <Tooltip />
                <Bar dataKey="triggers" fill="#1976d2" name="Tetiklenme Sayısı" />
                <Bar dataKey="success" fill="#4caf50" name="Başarılı Tespit" />
              </BarChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </TabPanel>

      {/* Zaman Analizi Tab */}
      <TabPanel value={activeTab} index={2}>
        <Card>
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Saatlik Kural Tetiklenme Dağılımı
            </Typography>
            <ResponsiveContainer width="100%" height={400}>
              <LineChart data={ruleTimelineData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="time" />
                <YAxis />
                <Tooltip />
                <Line type="monotone" dataKey="highAmount" stroke="#1976d2" name="Yüksek Tutar" strokeWidth={2} />
                <Line type="monotone" dataKey="nightTime" stroke="#f44336" name="Gece Saati" strokeWidth={2} />
                <Line type="monotone" dataKey="suspiciousIP" stroke="#ff9800" name="Şüpheli IP" strokeWidth={2} />
                <Line type="monotone" dataKey="velocity" stroke="#4caf50" name="Velocity" strokeWidth={2} />
              </LineChart>
            </ResponsiveContainer>
          </CardContent>
        </Card>
      </TabPanel>

      {/* Kural Düzenleme Dialog */}
      <Dialog open={editDialog} onClose={() => setEditDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>
          {selectedRule ? 'Kuralı Düzenle' : 'Yeni Kural Oluştur'}
        </DialogTitle>
        <DialogContent>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3, mt: 2 }}>
            <TextField
              label="Kural Adı"
              value={newRule.name}
              onChange={(e) => setNewRule(prev => ({ ...prev, name: e.target.value }))}
              fullWidth
            />
            <TextField
              label="Açıklama"
              value={newRule.description}
              onChange={(e) => setNewRule(prev => ({ ...prev, description: e.target.value }))}
              multiline
              rows={3}
              fullWidth
            />
            <FormControlLabel
              control={
                <Switch
                  checked={newRule.isActive}
                  onChange={(e) => setNewRule(prev => ({ ...prev, isActive: e.target.checked }))}
                />
              }
              label="Kural Aktif"
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditDialog(false)}>İptal</Button>
          <Button variant="contained" onClick={handleSaveRule}>
            Kaydet
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default FraudRules; 