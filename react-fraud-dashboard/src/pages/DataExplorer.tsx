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
  Pagination,
  Chip,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
} from '@mui/material';
import {
  Storage as DataIcon,
  Search as SearchIcon,
  Download as DownloadIcon,
  Visibility as ViewIcon,
  FilterList as FilterIcon,
  Refresh as RefreshIcon,
} from '@mui/icons-material';
import { DataGrid, GridColDef, GridToolbar } from '@mui/x-data-grid';
import FraudDetectionAPI, { Transaction } from '../services/api';

const DataExplorer: React.FC = () => {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState({
    dateFrom: '',
    dateTo: '',
    amountMin: '',
    amountMax: '',
    merchantCategory: '',
    riskLevel: '',
  });
  const [selectedTransaction, setSelectedTransaction] = useState<Transaction | null>(null);
  const [detailDialog, setDetailDialog] = useState(false);

  // Örnek transaction verileri
  const sampleTransactions: Transaction[] = [
    {
      id: 'TXN-2024-001234',
      transactionId: 'TXN-2024-001234',
      userId: 'USR-15234',
      amount: 450.75,
      merchantId: 'MER-OG-001',
      type: 'Purchase',
      status: 'completed',
      timestamp: '2024-01-15T02:30:15Z',
      ipAddress: '192.168.1.1',
      deviceId: 'DEV-15234',
      location: {
        country: 'Turkey',
        city: 'Istanbul'
      },
      deviceInfo: {
        deviceType: 'mobile',
        userAgent: 'Mozilla/5.0...'
      },
      merchantCategory: 'Online Oyun',
      hour: 2,
      dayOfWeek: 3,
      isWeekend: false,
      userAge: 19,
      userGender: 'M'
    },
    {
      id: 'TXN-2024-005678',
      transactionId: 'TXN-2024-005678',
      userId: 'USR-28456',
      amount: 1250.00,
      merchantId: 'MER-MAR-002',
      type: 'Purchase',
      status: 'completed',
      timestamp: '2024-01-15T14:15:30Z',
      ipAddress: '192.168.1.2',
      deviceId: 'DEV-28456',
      location: {
        country: 'Turkey',
        city: 'Ankara'
      },
      deviceInfo: {
        deviceType: 'desktop',
        userAgent: 'Mozilla/5.0...'
      },
      merchantCategory: 'Market',
      hour: 14,
      dayOfWeek: 1,
      isWeekend: false,
      userAge: 34,
      userGender: 'F'
    },
    {
      id: 'TXN-2024-009012',
      transactionId: 'TXN-2024-009012',
      userId: 'USR-39871',
      amount: 89.99,
      merchantId: 'MER-TEK-003',
      type: 'Purchase',
      status: 'completed',
      timestamp: '2024-01-13T10:45:22Z',
      ipAddress: '192.168.1.3',
      deviceId: 'DEV-39871',
      location: {
        country: 'Turkey',
        city: 'Izmir'
      },
      deviceInfo: {
        deviceType: 'tablet',
        userAgent: 'Mozilla/5.0...'
      },
      merchantCategory: 'Teknoloji',
      hour: 10,
      dayOfWeek: 6,
      isWeekend: true,
      userAge: 28,
      userGender: 'M'
    },
    {
      id: 'TXN-2024-003456',
      transactionId: 'TXN-2024-003456',
      userId: 'USR-41205',
      amount: 2750.50,
      merchantId: 'MER-TAK-004',
      type: 'Purchase',
      status: 'completed',
      timestamp: '2024-01-12T23:20:45Z',
      ipAddress: '192.168.1.4',
      deviceId: 'DEV-41205',
      location: {
        country: 'Turkey',
        city: 'Bursa'
      },
      deviceInfo: {
        deviceType: 'mobile',
        userAgent: 'Mozilla/5.0...'
      },
      merchantCategory: 'Taksi',
      hour: 23,
      dayOfWeek: 5,
      isWeekend: false,
      userAge: 45,
      userGender: 'F'
    },
    {
      id: 'TXN-2024-007890',
      transactionId: 'TXN-2024-007890',
      userId: 'USR-52913',
      amount: 156.25,
      merchantId: 'MER-RES-005',
      type: 'Purchase',
      status: 'completed',
      timestamp: '2024-01-11T19:30:10Z',
      ipAddress: '192.168.1.5',
      deviceId: 'DEV-52913',
      location: {
        country: 'Turkey',
        city: 'Antalya'
      },
      deviceInfo: {
        deviceType: 'desktop',
        userAgent: 'Mozilla/5.0...'
      },
      merchantCategory: 'Restoran',
      hour: 19,
      dayOfWeek: 4,
      isWeekend: false,
      userAge: 31,
      userGender: 'M'
    },
  ];

  // DataGrid sütun tanımları
  const columns: GridColDef[] = [
    {
      field: 'transactionId',
      headerName: 'İşlem ID',
      width: 150,
      renderCell: (params) => (
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
            {params.value}
          </Typography>
          <IconButton size="small" onClick={() => handleViewDetails(params.row)}>
            <ViewIcon fontSize="small" />
          </IconButton>
        </Box>
      ),
    },
    {
      field: 'amount',
      headerName: 'Tutar',
      width: 120,
      type: 'number',
      renderCell: (params) => (
        <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
          ₺{params.value.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
        </Typography>
      ),
    },
    {
      field: 'merchantCategory',
      headerName: 'Kategori',
      width: 130,
      renderCell: (params) => (
        <Chip 
          label={params.value} 
          size="small" 
          variant="outlined"
          color="primary"
        />
      ),
    },
    {
      field: 'timestamp',
      headerName: 'Tarih/Saat',
      width: 160,
      renderCell: (params) => (
        <Typography variant="body2">
          {new Date(params.value).toLocaleString('tr-TR')}
        </Typography>
      ),
    },
    {
      field: 'hour',
      headerName: 'Saat',
      width: 80,
      renderCell: (params) => (
        <Chip 
          label={`${params.value}:00`}
          size="small"
          color={params.value >= 22 || params.value <= 6 ? 'warning' : 'default'}
        />
      ),
    },
    {
      field: 'isWeekend',
      headerName: 'Hafta Sonu',
      width: 100,
      renderCell: (params) => (
        <Chip 
          label={params.value ? 'Evet' : 'Hayır'}
          size="small"
          color={params.value ? 'secondary' : 'default'}
        />
      ),
    },
    {
      field: 'userAge',
      headerName: 'Kullanıcı Yaşı',
      width: 110,
      type: 'number',
    },
    {
      field: 'userGender',
      headerName: 'Cinsiyet',
      width: 90,
      renderCell: (params) => (
        <Chip 
          label={params.value === 'M' ? 'Erkek' : 'Kadın'}
          size="small"
          color={params.value === 'M' ? 'info' : 'success'}
        />
      ),
    },
  ];

  useEffect(() => {
    const loadTransactions = async () => {
      try {
        setLoading(true);
        // API çağrısı (şimdilik mock data)
        setTimeout(() => {
          setTransactions(sampleTransactions);
          setLoading(false);
        }, 1000);
        
        // Gerçek API çağrısı:
        // const transactionData = await FraudDetectionAPI.getTransactions(100, 0);
        // setTransactions(transactionData);
        
      } catch (err) {
        console.error('İşlemler yüklenirken hata oluştu:', err);
      } finally {
        setLoading(false);
      }
    };

    loadTransactions();
  }, []);

  const handleViewDetails = (transaction: Transaction) => {
    setSelectedTransaction(transaction);
    setDetailDialog(true);
  };

  const handleApplyFilters = async () => {
    // Filtreleme logigi burada olacak
    console.log('Filtreler uygulanıyor:', filters);
  };

  const handleExportData = () => {
    // CSV export logigi
    const csvContent = transactions.map(t => 
      `${t.transactionId},${t.amount},${t.merchantCategory},${t.timestamp}`
    ).join('\n');
    
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.setAttribute('hidden', '');
    a.setAttribute('href', url);
    a.setAttribute('download', 'transactions.csv');
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  };

  const getRiskLevel = (transaction: Transaction) => {
    // Basit risk seviyesi hesaplama
    let riskScore = 0;
    
    if (transaction.amount > 1000) riskScore += 2;
    if ((transaction.hour || 0) >= 22 || (transaction.hour || 0) <= 6) riskScore += 2;
    if ((transaction.userAge || 0) < 25) riskScore += 1;
    if (transaction.merchantCategory === 'Online Oyun') riskScore += 2;
    
    if (riskScore >= 5) return { level: 'Yüksek', color: 'error' as const };
    if (riskScore >= 3) return { level: 'Orta', color: 'warning' as const };
    return { level: 'Düşük', color: 'success' as const };
  };

  return (
    <Box>
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <DataIcon sx={{ fontSize: 40, color: '#1976d2' }} />
          <Typography variant="h4" sx={{ fontWeight: 'bold' }}>
            Veri Gezgini
          </Typography>
        </Box>
        <Box sx={{ display: 'flex', gap: 2 }}>
          <Button
            variant="outlined"
            startIcon={<RefreshIcon />}
            onClick={() => window.location.reload()}
          >
            Yenile
          </Button>
          <Button
            variant="contained"
            startIcon={<DownloadIcon />}
            onClick={handleExportData}
          >
            Dışa Aktar
          </Button>
        </Box>
      </Box>

      <Typography variant="body1" color="textSecondary" sx={{ mb: 4 }}>
        Ham işlem verilerini keşfedin, filtreleyin ve analiz edin.
      </Typography>

      {/* Filtreler */}
      <Card sx={{ mb: 4 }}>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 3 }}>
            <FilterIcon />
            <Typography variant="h6">Filtreler</Typography>
          </Box>
          
          <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'end' }}>
            <TextField
              label="Başlangıç Tarihi"
              type="date"
              value={filters.dateFrom}
              onChange={(e) => setFilters(prev => ({ ...prev, dateFrom: e.target.value }))}
              InputLabelProps={{ shrink: true }}
              sx={{ minWidth: 150 }}
            />
            
            <TextField
              label="Bitiş Tarihi"
              type="date"
              value={filters.dateTo}
              onChange={(e) => setFilters(prev => ({ ...prev, dateTo: e.target.value }))}
              InputLabelProps={{ shrink: true }}
              sx={{ minWidth: 150 }}
            />
            
            <TextField
              label="Min Tutar"
              type="number"
              value={filters.amountMin}
              onChange={(e) => setFilters(prev => ({ ...prev, amountMin: e.target.value }))}
              sx={{ minWidth: 120 }}
            />
            
            <TextField
              label="Max Tutar"
              type="number"
              value={filters.amountMax}
              onChange={(e) => setFilters(prev => ({ ...prev, amountMax: e.target.value }))}
              sx={{ minWidth: 120 }}
            />
            
            <FormControl sx={{ minWidth: 150 }}>
              <InputLabel>Kategori</InputLabel>
              <Select
                value={filters.merchantCategory}
                onChange={(e) => setFilters(prev => ({ ...prev, merchantCategory: e.target.value }))}
                label="Kategori"
              >
                <MenuItem value="">Tümü</MenuItem>
                <MenuItem value="Market">Market</MenuItem>
                <MenuItem value="Restoran">Restoran</MenuItem>
                <MenuItem value="Teknoloji">Teknoloji</MenuItem>
                <MenuItem value="Online Oyun">Online Oyun</MenuItem>
                <MenuItem value="Taksi">Taksi</MenuItem>
              </Select>
            </FormControl>
            
            <Button
              variant="contained"
              onClick={handleApplyFilters}
              startIcon={<SearchIcon />}
              sx={{ height: 56 }}
            >
              Filtrele
            </Button>
          </Box>
        </CardContent>
      </Card>

      {/* Data Grid */}
      <Card>
        <CardContent>
          <Typography variant="h6" gutterBottom>
            İşlem Verileri ({transactions.length} kayıt)
          </Typography>
          
          <Box sx={{ height: 600, width: '100%' }}>
            <DataGrid
              rows={transactions}
              columns={columns}
              loading={loading}
              getRowId={(row) => row.transactionId}
              pageSizeOptions={[25, 50, 100]}
              initialState={{
                pagination: {
                  paginationModel: { page: 0, pageSize: 25 },
                },
              }}
              slots={{ toolbar: GridToolbar }}
              slotProps={{
                toolbar: {
                  showQuickFilter: true,
                  quickFilterProps: { debounceMs: 500 },
                },
              }}
              disableRowSelectionOnClick
              sx={{
                '& .MuiDataGrid-row:hover': {
                  backgroundColor: '#f5f5f5',
                },
              }}
            />
          </Box>
        </CardContent>
      </Card>

      {/* İşlem Detay Dialog */}
      <Dialog 
        open={detailDialog} 
        onClose={() => setDetailDialog(false)} 
        maxWidth="md" 
        fullWidth
      >
        <DialogTitle>
          İşlem Detayları
        </DialogTitle>
        <DialogContent>
          {selectedTransaction && (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3, mt: 2 }}>
              {/* Temel Bilgiler */}
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="h6" gutterBottom color="primary">
                    Temel Bilgiler
                  </Typography>
                  
                  <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 2 }}>
                    <Box>
                      <Typography variant="body2" color="textSecondary">
                        İşlem ID
                      </Typography>
                      <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
                        {selectedTransaction.transactionId}
                      </Typography>
                    </Box>
                    
                    <Box>
                      <Typography variant="body2" color="textSecondary">
                        Tutar
                      </Typography>
                      <Typography variant="h6" color="primary">
                        ₺{selectedTransaction.amount.toLocaleString('tr-TR', { minimumFractionDigits: 2 })}
                      </Typography>
                    </Box>
                    
                    <Box>
                      <Typography variant="body2" color="textSecondary">
                        Kategori
                      </Typography>
                      <Chip label={selectedTransaction.merchantCategory} color="primary" />
                    </Box>
                    
                    <Box>
                      <Typography variant="body2" color="textSecondary">
                        Tarih/Saat
                      </Typography>
                      <Typography variant="body1">
                        {new Date(selectedTransaction.timestamp).toLocaleString('tr-TR')}
                      </Typography>
                    </Box>
                  </Box>
                </CardContent>
              </Card>

              {/* Kullanıcı Bilgileri */}
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="h6" gutterBottom color="primary">
                    Kullanıcı Bilgileri
                  </Typography>
                  
                  <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 2 }}>
                    <Box>
                      <Typography variant="body2" color="textSecondary">
                        Kullanıcı ID
                      </Typography>
                      <Typography variant="body1">
                        {selectedTransaction.userId}
                      </Typography>
                    </Box>
                    
                    <Box>
                      <Typography variant="body2" color="textSecondary">
                        Yaş
                      </Typography>
                      <Typography variant="body1">
                        {selectedTransaction.userAge}
                      </Typography>
                    </Box>
                    
                    <Box>
                      <Typography variant="body2" color="textSecondary">
                        Cinsiyet
                      </Typography>
                      <Chip 
                        label={selectedTransaction.userGender === 'M' ? 'Erkek' : 'Kadın'}
                        color={selectedTransaction.userGender === 'M' ? 'info' : 'success'}
                      />
                    </Box>
                  </Box>
                </CardContent>
              </Card>

              {/* Risk Analizi */}
              <Card variant="outlined">
                <CardContent>
                  <Typography variant="h6" gutterBottom color="primary">
                    Risk Analizi
                  </Typography>
                  
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 3 }}>
                    <Box>
                      <Typography variant="body2" color="textSecondary">
                        Risk Seviyesi
                      </Typography>
                      <Chip 
                        label={getRiskLevel(selectedTransaction).level}
                        color={getRiskLevel(selectedTransaction).color}
                        size="medium"
                      />
                    </Box>
                    
                    <Box>
                      <Typography variant="body2" color="textSecondary">
                        Hafta Sonu İşlemi
                      </Typography>
                      <Chip 
                        label={selectedTransaction.isWeekend ? 'Evet' : 'Hayır'}
                        color={selectedTransaction.isWeekend ? 'warning' : 'default'}
                      />
                    </Box>
                    
                    <Box>
                      <Typography variant="body2" color="textSecondary">
                        İşlem Saati
                      </Typography>
                      <Chip 
                        label={`${selectedTransaction.hour || 0}:00`}
                        color={(selectedTransaction.hour || 0) >= 22 || (selectedTransaction.hour || 0) <= 6 ? 'warning' : 'default'}
                      />
                    </Box>
                  </Box>
                </CardContent>
              </Card>
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDetailDialog(false)}>Kapat</Button>
          <Button variant="contained">SHAP Analizi Yap</Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
};

export default DataExplorer; 