import React, { useState, useEffect } from 'react';
import {
    Container,
    Typography,
    Paper,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Chip,
    IconButton,
    Box,
    TextField,
    InputAdornment,
    Button,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Alert,
    Tooltip,
    CircularProgress,
    Card,
    CardContent,
    Badge,
    Snackbar,
    MenuItem,
    Select,
    FormControl,
    InputLabel,
    LinearProgress
} from '@mui/material';
import {
    Search,
    Refresh,
    Visibility,
    Warning,
    Shield,
    TrendingUp,
    Info,
    Close,
    FilterList
} from '@mui/icons-material';
import FraudDetectionAPI, {
    RiskFactorResponse,
    RiskFactorSummary
} from '../services/api';

const RiskManagement: React.FC = () => {
    const [riskFactors, setRiskFactors] = useState<RiskFactorResponse[]>([]);
    const [summary, setSummary] = useState<RiskFactorSummary | null>(null);
    const [loading, setLoading] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');
    const [typeFilter, setTypeFilter] = useState<string>('all');
    const [severityFilter, setSeverityFilter] = useState<string>('all');
    const [sourceFilter, setSourceFilter] = useState<string>('all');
    const [selectedRiskFactor, setSelectedRiskFactor] = useState<RiskFactorResponse | null>(null);
    const [detailOpen, setDetailOpen] = useState(false);
    const [snackbarMessage, setSnackbarMessage] = useState<string | null>(null);

    useEffect(() => {
        loadData();
    }, []);

    const loadData = async () => {
        setLoading(true);
        try {
            await Promise.all([loadRiskFactors(), loadSummary()]);
        } catch (error) {
            setSnackbarMessage('Veri yükleme hatası');
        } finally {
            setLoading(false);
        }
    };

    const loadRiskFactors = async () => {
        try {
            const response = await FraudDetectionAPI.getRiskFactors(1000, 0);
            if (response.success && response.data) {
                setRiskFactors(response.data);
            }
        } catch (error) {
            console.error('Risk faktörleri yüklenirken hata:', error);
        }
    };

    const loadSummary = async () => {
        try {
            const response = await FraudDetectionAPI.getRiskFactorSummary();
            if (response.success && response.data) {
                setSummary(response.data);
            }
        } catch (error) {
            console.error('Özet yüklenirken hata:', error);
        }
    };

    const formatDate = (dateString: string) => {
        return new Date(dateString).toLocaleDateString('tr-TR', {
            year: 'numeric',
            month: 'short',
            day: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    const filteredRiskFactors = riskFactors.filter(rf => {
        const matchesSearch =
            rf.description.toLowerCase().includes(searchTerm.toLowerCase()) ||
            rf.code.toLowerCase().includes(searchTerm.toLowerCase()) ||
            rf.transactionId.includes(searchTerm);

        const matchesType = typeFilter === 'all' || rf.type === typeFilter;
        const matchesSeverity = severityFilter === 'all' || rf.severity === severityFilter;
        const matchesSource = sourceFilter === 'all' || rf.source === sourceFilter;

        return matchesSearch && matchesType && matchesSeverity && matchesSource;
    });

    const handleViewDetail = (riskFactor: RiskFactorResponse) => {
        setSelectedRiskFactor(riskFactor);
        setDetailOpen(true);
    };

    const resetFilters = () => {
        setSearchTerm('');
        setTypeFilter('all');
        setSeverityFilter('all');
        setSourceFilter('all');
    };

    // Unique values for filters
    const uniqueTypes = Array.from(new Set(riskFactors.map(rf => rf.type)));
    const uniqueSources = Array.from(new Set(riskFactors.map(rf => rf.source)));

    return (
        <Container maxWidth="xl" sx={{ py: 4 }}>
            {/* Header */}
            <Box sx={{ mb: 4 }}>

                <Typography variant="body1" color="text.secondary">
                    Risk faktörlerini analiz edin ve yönetin
                </Typography>
            </Box>

            {/* Summary Cards */}
            <Box sx={{ display: 'flex', gap: 3, mb: 4, flexWrap: 'wrap' }}>
                <Card sx={{ flex: 1, minWidth: 200 }}>
                    <CardContent>
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                            <Box>
                                <Typography variant="h4" sx={{ fontWeight: 'bold', color: '#1976d2' }}>
                                    {summary?.totalCount || 0}
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Toplam Risk Faktörü
                                </Typography>
                            </Box>
                            <Shield sx={{ fontSize: 40, color: '#1976d2' }} />
                        </Box>
                    </CardContent>
                </Card>

                <Card sx={{ flex: 1, minWidth: 200 }}>
                    <CardContent>
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                            <Box>
                                <Typography variant="h4" sx={{ fontWeight: 'bold', color: '#f44336' }}>
                                    {summary?.criticalCount || 0}
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Kritik Risk
                                </Typography>
                            </Box>
                            <Badge badgeContent={summary?.criticalCount || 0} color="error">
                                <Warning sx={{ fontSize: 40, color: '#f44336' }} />
                            </Badge>
                        </Box>
                    </CardContent>
                </Card>

                <Card sx={{ flex: 1, minWidth: 200 }}>
                    <CardContent>
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                            <Box>
                                <Typography variant="h4" sx={{ fontWeight: 'bold', color: '#ff9800' }}>
                                    {summary?.highCount || 0}
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Yüksek Risk
                                </Typography>
                            </Box>
                            <Badge badgeContent={summary?.highCount || 0} color="warning">
                                <TrendingUp sx={{ fontSize: 40, color: '#ff9800' }} />
                            </Badge>
                        </Box>
                    </CardContent>
                </Card>

                <Card sx={{ flex: 1, minWidth: 200 }}>
                    <CardContent>
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                            <Box>
                                <Typography variant="h4" sx={{ fontWeight: 'bold', color: '#4caf50' }}>
                                    {summary?.averageConfidence ? (summary.averageConfidence * 100).toFixed(1) : 0}%
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Ortalama Güven
                                </Typography>
                            </Box>
                            <Info sx={{ fontSize: 40, color: '#4caf50' }} />
                        </Box>
                    </CardContent>
                </Card>
            </Box>

            {/* Filters and Search */}
            <Paper sx={{ borderRadius: 3, overflow: 'hidden', mb: 3 }}>
                <Box sx={{ p: 3, display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'center' }}>
                    <TextField
                        placeholder="Risk faktörü ara..."
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        sx={{ minWidth: 300 }}
                        InputProps={{
                            startAdornment: (
                                <InputAdornment position="start">
                                    <Search />
                                </InputAdornment>
                            ),
                        }}
                    />

                    <FormControl sx={{ minWidth: 150 }}>
                        <InputLabel>Tip</InputLabel>
                        <Select
                            value={typeFilter}
                            label="Tip"
                            onChange={(e) => setTypeFilter(e.target.value)}
                        >
                            <MenuItem value="all">Tümü</MenuItem>
                            {uniqueTypes.map(type => (
                                <MenuItem key={type} value={type}>
                                    {FraudDetectionAPI.getRiskFactorTypeLabel(type)}
                                </MenuItem>
                            ))}
                        </Select>
                    </FormControl>

                    <FormControl sx={{ minWidth: 150 }}>
                        <InputLabel>Risk Seviyesi</InputLabel>
                        <Select
                            value={severityFilter}
                            label="Risk Seviyesi"
                            onChange={(e) => setSeverityFilter(e.target.value)}
                        >
                            <MenuItem value="all">Tümü</MenuItem>
                            <MenuItem value="Low">Düşük</MenuItem>
                            <MenuItem value="Medium">Orta</MenuItem>
                            <MenuItem value="High">Yüksek</MenuItem>
                            <MenuItem value="Critical">Kritik</MenuItem>
                        </Select>
                    </FormControl>

                    <FormControl sx={{ minWidth: 150 }}>
                        <InputLabel>Kaynak</InputLabel>
                        <Select
                            value={sourceFilter}
                            label="Kaynak"
                            onChange={(e) => setSourceFilter(e.target.value)}
                        >
                            <MenuItem value="all">Tümü</MenuItem>
                            {uniqueSources.map(source => (
                                <MenuItem key={source} value={source}>
                                    {source}
                                </MenuItem>
                            ))}
                        </Select>
                    </FormControl>

                    <Button
                        variant="outlined"
                        startIcon={<FilterList />}
                        onClick={resetFilters}
                    >
                        Temizle
                    </Button>

                    <Button
                        variant="outlined"
                        startIcon={<Refresh />}
                        onClick={loadData}
                        disabled={loading}
                    >
                        Yenile
                    </Button>
                </Box>
            </Paper>

            {/* Risk Factors Table */}
            <Paper sx={{ borderRadius: 3, overflow: 'hidden' }}>
                {loading ? (
                    <Box sx={{ p: 3 }}>
                        <LinearProgress />
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                            <CircularProgress />
                        </Box>
                    </Box>
                ) : (
                    <TableContainer>
                        <Table>
                            <TableHead>
                                <TableRow>

                                    <TableCell><strong>Kod</strong></TableCell>
                                    <TableCell><strong>Tip</strong></TableCell>
                                    <TableCell><strong>Açıklama</strong></TableCell>
                                    <TableCell><strong>Güven Oranı</strong></TableCell>
                                    <TableCell><strong>Risk Seviyesi</strong></TableCell>
                                    <TableCell><strong>Kaynak</strong></TableCell>
                                    <TableCell><strong>Tespit Tarihi</strong></TableCell>
                                    <TableCell align="center"><strong>İşlemler</strong></TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {filteredRiskFactors.map((riskFactor) => (
                                    <TableRow key={riskFactor.id} hover>

                                        <TableCell>
                                            <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                                {riskFactor.code}
                                            </Typography>
                                        </TableCell>
                                        <TableCell>
                                            <Chip
                                                label={FraudDetectionAPI.getRiskFactorTypeLabel(riskFactor.type)}
                                                size="small"
                                                variant="outlined"
                                            />
                                        </TableCell>
                                        <TableCell>
                                            <Typography variant="body2">
                                                {riskFactor.description.length > 60
                                                    ? `${riskFactor.description.substring(0, 60)}...`
                                                    : riskFactor.description}
                                            </Typography>
                                        </TableCell>
                                        <TableCell>
                                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                                <LinearProgress
                                                    variant="determinate"
                                                    value={riskFactor.confidence * 100}
                                                    sx={{ flexGrow: 1, height: 6, borderRadius: 3 }}
                                                    color={riskFactor.confidence > 0.7 ? 'error' : riskFactor.confidence > 0.5 ? 'warning' : 'success'}
                                                />
                                                <Typography variant="caption">
                                                    {(riskFactor.confidence * 100).toFixed(0)}%
                                                </Typography>
                                            </Box>
                                        </TableCell>
                                        <TableCell>
                                            <Chip
                                                label={FraudDetectionAPI.getRiskLevelLabel(riskFactor.severity)}
                                                color={FraudDetectionAPI.getRiskLevelColor(riskFactor.severity)}
                                                size="small"
                                            />
                                        </TableCell>
                                        <TableCell>
                                            <Chip
                                                label={riskFactor.source}
                                                size="small"
                                                variant="outlined"
                                                color="primary"
                                            />
                                        </TableCell>
                                        <TableCell>
                                            <Typography variant="body2">
                                                {formatDate(riskFactor.detectedAt)}
                                            </Typography>
                                        </TableCell>
                                        <TableCell align="center">
                                            <Tooltip title="Detayları Görüntüle">
                                                <IconButton
                                                    onClick={() => handleViewDetail(riskFactor)}
                                                    color="primary"
                                                    size="small"
                                                >
                                                    <Visibility />
                                                </IconButton>
                                            </Tooltip>
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </TableContainer>
                )}
            </Paper>

            {/* Detail Modal */}
            <Dialog
                open={detailOpen}
                onClose={() => setDetailOpen(false)}
                maxWidth="md"
                fullWidth
            >
                <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="h6">Risk Faktörü Detayları</Typography>
                    <IconButton onClick={() => setDetailOpen(false)}>
                        <Close />
                    </IconButton>
                </DialogTitle>
                <DialogContent dividers>
                    {selectedRiskFactor && (
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                            <Box sx={{ display: 'flex', gap: 3 }}>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        İşlem ID
                                    </Typography>
                                    <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
                                        {selectedRiskFactor.transactionId}
                                    </Typography>
                                </Box>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Kod
                                    </Typography>
                                    <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
                                        {selectedRiskFactor.code}
                                    </Typography>
                                </Box>
                            </Box>

                            <Box>
                                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                    Açıklama
                                </Typography>
                                <Typography variant="body1">{selectedRiskFactor.description}</Typography>
                            </Box>

                            <Box sx={{ display: 'flex', gap: 3 }}>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Tip
                                    </Typography>
                                    <Chip label={FraudDetectionAPI.getRiskFactorTypeLabel(selectedRiskFactor.type)} />
                                </Box>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Risk Seviyesi
                                    </Typography>
                                    <Chip
                                        label={FraudDetectionAPI.getRiskLevelLabel(selectedRiskFactor.severity)}
                                        color={FraudDetectionAPI.getRiskLevelColor(selectedRiskFactor.severity)}
                                    />
                                </Box>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Kaynak
                                    </Typography>
                                    <Chip label={selectedRiskFactor.source} color="primary" />
                                </Box>
                            </Box>

                            <Box>
                                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                    Güven Oranı
                                </Typography>
                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                                    <LinearProgress
                                        variant="determinate"
                                        value={selectedRiskFactor.confidence * 100}
                                        sx={{ flexGrow: 1, height: 8, borderRadius: 4 }}
                                        color={selectedRiskFactor.confidence > 0.7 ? 'error' : selectedRiskFactor.confidence > 0.5 ? 'warning' : 'success'}
                                    />
                                    <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                                        {(selectedRiskFactor.confidence * 100).toFixed(1)}%
                                    </Typography>
                                </Box>
                            </Box>

                            <Box sx={{ display: 'flex', gap: 3 }}>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Analiz Sonucu ID
                                    </Typography>
                                    <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                        {selectedRiskFactor.analysisResultId}
                                    </Typography>
                                </Box>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Tespit Tarihi
                                    </Typography>
                                    <Typography variant="body2">{formatDate(selectedRiskFactor.detectedAt)}</Typography>
                                </Box>
                            </Box>

                            {selectedRiskFactor.ruleId && (
                                <Box>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        İlgili Kural ID
                                    </Typography>
                                    <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                        {selectedRiskFactor.ruleId}
                                    </Typography>
                                </Box>
                            )}

                            {selectedRiskFactor.actionTaken && (
                                <Box>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Alınan Aksiyon
                                    </Typography>
                                    <Typography variant="body2">{selectedRiskFactor.actionTaken}</Typography>
                                </Box>
                            )}
                        </Box>
                    )}
                </DialogContent>
            </Dialog>

            {/* Snackbar for notifications */}
            <Snackbar
                open={!!snackbarMessage}
                autoHideDuration={3000}
                onClose={() => setSnackbarMessage(null)}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
            >
                <Alert severity="info" onClose={() => setSnackbarMessage(null)}>
                    {snackbarMessage}
                </Alert>
            </Snackbar>
        </Container>
    );
};

export default RiskManagement; 