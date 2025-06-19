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
    Tabs,
    Tab
} from '@mui/material';
import {
    Search,
    Refresh,
    Add,
    Block,
    Visibility,
    Delete,
    CleaningServices,
    Shield,
    Computer,
    Language,
    Person,
    Close,
    FilterList
} from '@mui/icons-material';
import FraudDetectionAPI, {
    BlacklistItemResponse,
    BlacklistSummary,
    BlacklistType,
    BlacklistItemCreateRequest
} from '../services/api';

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
            id={`simple-tabpanel-${index}`}
            aria-labelledby={`simple-tab-${index}`}
            {...other}
        >
            {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
        </div>
    );
}

const BlacklistManagement: React.FC = () => {
    const [blacklistItems, setBlacklistItems] = useState<BlacklistItemResponse[]>([]);
    const [summary, setSummary] = useState<BlacklistSummary | null>(null);
    const [loading, setLoading] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');
    const [typeFilter, setTypeFilter] = useState<string>('all');
    const [statusFilter, setStatusFilter] = useState<string>('all');
    const [selectedItem, setSelectedItem] = useState<BlacklistItemResponse | null>(null);
    const [detailOpen, setDetailOpen] = useState(false);
    const [addDialogOpen, setAddDialogOpen] = useState(false);
    const [snackbarMessage, setSnackbarMessage] = useState<string | null>(null);
    const [tabValue, setTabValue] = useState(0);

    // Add dialog form state
    const [newItem, setNewItem] = useState<BlacklistItemCreateRequest>({
        type: BlacklistType.IpAddress,
        value: '',
        reason: '',
        durationHours: null,
        addedBy: 'admin'
    });

    useEffect(() => {
        loadData();
    }, []);

    const loadData = async () => {
        setLoading(true);
        try {
            await Promise.all([loadBlacklistItems(), loadSummary()]);
        } catch (error) {
            setSnackbarMessage('Veri yükleme hatası');
        } finally {
            setLoading(false);
        }
    };

    const loadBlacklistItems = async () => {
        try {
            const response = await FraudDetectionAPI.getBlacklistItems(1000, 0);
            if (response.success && response.data) {
                setBlacklistItems(response.data);
            }
        } catch (error) {
            console.error('Kara liste öğeleri yüklenirken hata:', error);
        }
    };

    const loadSummary = async () => {
        try {
            const response = await FraudDetectionAPI.getBlacklistSummary();
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

    const filteredItems = blacklistItems.filter(item => {
        const matchesSearch =
            item.value.toLowerCase().includes(searchTerm.toLowerCase()) ||
            item.reason.toLowerCase().includes(searchTerm.toLowerCase()) ||
            item.addedBy.toLowerCase().includes(searchTerm.toLowerCase());

        const matchesType = typeFilter === 'all' || item.type === typeFilter;
        const matchesStatus = statusFilter === 'all' || item.status === statusFilter;

        return matchesSearch && matchesType && matchesStatus;
    });

    const handleViewDetail = (item: BlacklistItemResponse) => {
        setSelectedItem(item);
        setDetailOpen(true);
    };

    const handleAddItem = async () => {
        try {
            const response = await FraudDetectionAPI.addBlacklistItem(newItem);
            if (response.success) {
                setSnackbarMessage('Kara liste öğesi başarıyla eklendi');
                setAddDialogOpen(false);
                resetForm();
                await loadData();
            } else {
                setSnackbarMessage('Kara liste öğesi eklenirken hata oluştu');
            }
        } catch (error) {
            setSnackbarMessage('Kara liste öğesi eklenirken hata oluştu');
        }
    };

    const handleInvalidateItem = async (id: string) => {
        try {
            const response = await FraudDetectionAPI.invalidateBlacklistItem(id, { invalidatedBy: 'admin' });
            if (response.success) {
                setSnackbarMessage('Kara liste öğesi geçersiz kılındı');
                await loadData();
            } else {
                setSnackbarMessage('Geçersiz kılma işlemi başarısız');
            }
        } catch (error) {
            setSnackbarMessage('Geçersiz kılma işlemi başarısız');
        }
    };

    const handleDeleteItem = async (id: string) => {
        try {
            const response = await FraudDetectionAPI.deleteBlacklistItem(id);
            if (response.success) {
                setSnackbarMessage('Kara liste öğesi silindi');
                await loadData();
            } else {
                setSnackbarMessage('Silme işlemi başarısız');
            }
        } catch (error) {
            setSnackbarMessage('Silme işlemi başarısız');
        }
    };

    const handleCleanupExpired = async () => {
        try {
            const response = await FraudDetectionAPI.cleanupExpiredItems();
            if (response.success) {
                setSnackbarMessage(`${response.data} adet süresi dolmuş öğe temizlendi`);
                await loadData();
            } else {
                setSnackbarMessage('Temizlik işlemi başarısız');
            }
        } catch (error) {
            setSnackbarMessage('Temizlik işlemi başarısız');
        }
    };

    const resetForm = () => {
        setNewItem({
            type: BlacklistType.IpAddress,
            value: '',
            reason: '',
            durationHours: null,
            addedBy: 'admin'
        });
    };

    const resetFilters = () => {
        setSearchTerm('');
        setTypeFilter('all');
        setStatusFilter('all');
    };

    const getTypeIcon = (type: string) => {
        switch (type) {
            case 'IpAddress': return <Language />;
            case 'Account': return <Person />;
            case 'Device': return <Computer />;
            case 'Country': return <Language />;
            default: return <Shield />;
        }
    };

    return (
        <Container maxWidth="xl" sx={{ py: 4 }}>
            {/* Header */}
            <Box sx={{ mb: 4 }}>

                <Typography variant="body1" color="text.secondary">
                    IP adresleri, hesaplar, cihazlar ve ülkeleri yönetin
                </Typography>
            </Box>

            {/* Summary Cards */}
            <Box sx={{ display: 'flex', gap: 3, mb: 4, flexWrap: 'wrap' }}>
                <Card sx={{ flex: 1, minWidth: 200 }}>
                    <CardContent>
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                            <Box>
                                <Typography variant="h4" sx={{ fontWeight: 'bold', color: '#1976d2' }}>
                                    {summary?.activeIpCount || 0}
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Aktif IP Blokları
                                </Typography>
                            </Box>
                            <Badge badgeContent={summary?.totalIpCount || 0} color="primary">
                                <Language sx={{ fontSize: 40, color: '#1976d2' }} />
                            </Badge>
                        </Box>
                    </CardContent>
                </Card>

                <Card sx={{ flex: 1, minWidth: 200 }}>
                    <CardContent>
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                            <Box>
                                <Typography variant="h4" sx={{ fontWeight: 'bold', color: '#f44336' }}>
                                    {summary?.activeAccountCount || 0}
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Aktif Hesap Blokları
                                </Typography>
                            </Box>
                            <Badge badgeContent={summary?.totalAccountCount || 0} color="error">
                                <Person sx={{ fontSize: 40, color: '#f44336' }} />
                            </Badge>
                        </Box>
                    </CardContent>
                </Card>

                <Card sx={{ flex: 1, minWidth: 200 }}>
                    <CardContent>
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                            <Box>
                                <Typography variant="h4" sx={{ fontWeight: 'bold', color: '#ff9800' }}>
                                    {summary?.activeDeviceCount || 0}
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Aktif Cihaz Blokları
                                </Typography>
                            </Box>
                            <Badge badgeContent={summary?.totalDeviceCount || 0} color="warning">
                                <Computer sx={{ fontSize: 40, color: '#ff9800' }} />
                            </Badge>
                        </Box>
                    </CardContent>
                </Card>

                <Card sx={{ flex: 1, minWidth: 200 }}>
                    <CardContent>
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                            <Box>
                                <Typography variant="h4" sx={{ fontWeight: 'bold', color: '#9c27b0' }}>
                                    {summary?.activeCountryCount || 0}
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Aktif Ülke Blokları
                                </Typography>
                            </Box>
                            <Badge badgeContent={summary?.totalCountryCount || 0} color="secondary">
                                <Language sx={{ fontSize: 40, color: '#9c27b0' }} />
                            </Badge>
                        </Box>
                    </CardContent>
                </Card>
            </Box>

            {/* Filters and Actions */}
            <Paper sx={{ borderRadius: 3, overflow: 'hidden', mb: 3 }}>
                <Box sx={{ p: 3, display: 'flex', gap: 2, flexWrap: 'wrap', alignItems: 'center' }}>
                    <TextField
                        placeholder="Kara liste öğesi ara..."
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
                            <MenuItem value="IpAddress">IP Adresi</MenuItem>
                            <MenuItem value="Account">Hesap</MenuItem>
                            <MenuItem value="Device">Cihaz</MenuItem>
                            <MenuItem value="Country">Ülke</MenuItem>
                        </Select>
                    </FormControl>

                    <FormControl sx={{ minWidth: 150 }}>
                        <InputLabel>Durum</InputLabel>
                        <Select
                            value={statusFilter}
                            label="Durum"
                            onChange={(e) => setStatusFilter(e.target.value)}
                        >
                            <MenuItem value="all">Tümü</MenuItem>
                            <MenuItem value="Active">Aktif</MenuItem>
                            <MenuItem value="Invalidated">Geçersiz</MenuItem>
                            <MenuItem value="Expired">Süresi Dolmuş</MenuItem>
                        </Select>
                    </FormControl>

                    <Button
                        variant="outlined"
                        startIcon={<FilterList />}
                        onClick={resetFilters}
                    >
                        Temizle
                    </Button>

                    <Box sx={{ flexGrow: 1 }} />

                    <Button
                        variant="outlined"
                        startIcon={<CleaningServices />}
                        onClick={handleCleanupExpired}
                        color="warning"
                    >
                        Süresi Dolmuşları Temizle
                    </Button>

                    <Button
                        variant="outlined"
                        startIcon={<Refresh />}
                        onClick={loadData}
                        disabled={loading}
                    >
                        Yenile
                    </Button>

                    <Button
                        variant="contained"
                        startIcon={<Add />}
                        onClick={() => setAddDialogOpen(true)}
                    >
                        Yeni Ekle
                    </Button>
                </Box>
            </Paper>

            {/* Main Content */}
            <Paper sx={{ borderRadius: 3, overflow: 'hidden' }}>
                {loading ? (
                    <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                        <CircularProgress />
                    </Box>
                ) : (
                    <TableContainer>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell><strong>Tip</strong></TableCell>
                                    <TableCell><strong>Değer</strong></TableCell>
                                    <TableCell><strong>Neden</strong></TableCell>
                                    <TableCell><strong>Durum</strong></TableCell>
                                    <TableCell><strong>Ekleyen</strong></TableCell>
                                    <TableCell><strong>Ekleme Tarihi</strong></TableCell>
                                    <TableCell><strong>Bitiş Tarihi</strong></TableCell>
                                    <TableCell align="center"><strong>İşlemler</strong></TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {filteredItems.map((item) => (
                                    <TableRow key={item.id} hover>
                                        <TableCell>
                                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                                {getTypeIcon(item.type)}
                                                <Chip
                                                    label={FraudDetectionAPI.getBlacklistTypeLabel(item.type)}
                                                    size="small"
                                                    variant="outlined"
                                                />
                                            </Box>
                                        </TableCell>
                                        <TableCell>
                                            <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                                {item.value}
                                            </Typography>
                                        </TableCell>
                                        <TableCell>
                                            <Typography variant="body2">
                                                {item.reason.length > 50
                                                    ? `${item.reason.substring(0, 50)}...`
                                                    : item.reason}
                                            </Typography>
                                        </TableCell>
                                        <TableCell>
                                            <Chip
                                                label={FraudDetectionAPI.getBlacklistStatusLabel(item.status)}
                                                color={FraudDetectionAPI.getBlacklistStatusColor(item.status)}
                                                size="small"
                                            />
                                        </TableCell>
                                        <TableCell>
                                            <Typography variant="body2">{item.addedBy}</Typography>
                                        </TableCell>
                                        <TableCell>
                                            <Typography variant="body2">
                                                {formatDate(item.createdAt)}
                                            </Typography>
                                        </TableCell>
                                        <TableCell>
                                            <Typography variant="body2">
                                                {item.expiryDate ? formatDate(item.expiryDate) : 'Süresiz'}
                                            </Typography>
                                        </TableCell>
                                        <TableCell align="center">
                                            <Box sx={{ display: 'flex', gap: 1 }}>
                                                <Tooltip title="Detayları Görüntüle">
                                                    <IconButton
                                                        onClick={() => handleViewDetail(item)}
                                                        color="primary"
                                                        size="small"
                                                    >
                                                        <Visibility />
                                                    </IconButton>
                                                </Tooltip>
                                                {item.isActive && (
                                                    <Tooltip title="Geçersiz Kıl">
                                                        <IconButton
                                                            onClick={() => handleInvalidateItem(item.id)}
                                                            color="warning"
                                                            size="small"
                                                        >
                                                            <Block />
                                                        </IconButton>
                                                    </Tooltip>
                                                )}
                                                <Tooltip title="Sil">
                                                    <IconButton
                                                        onClick={() => handleDeleteItem(item.id)}
                                                        color="error"
                                                        size="small"
                                                    >
                                                        <Delete />
                                                    </IconButton>
                                                </Tooltip>
                                            </Box>
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </TableContainer>
                )}
            </Paper>

            {/* Add Dialog */}
            <Dialog open={addDialogOpen} onClose={() => setAddDialogOpen(false)} maxWidth="sm" fullWidth>
                <DialogTitle>Yeni Kara Liste Öğesi Ekle</DialogTitle>
                <DialogContent>
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3, pt: 2 }}>
                        <FormControl fullWidth>
                            <InputLabel>Tip</InputLabel>
                            <Select
                                value={newItem.type}
                                label="Tip"
                                onChange={(e) => setNewItem({ ...newItem, type: e.target.value as BlacklistType })}
                            >
                                <MenuItem value={BlacklistType.IpAddress}>IP Adresi</MenuItem>
                                <MenuItem value={BlacklistType.Account}>Hesap</MenuItem>
                                <MenuItem value={BlacklistType.Device}>Cihaz</MenuItem>
                                <MenuItem value={BlacklistType.Country}>Ülke</MenuItem>
                            </Select>
                        </FormControl>

                        <TextField
                            label="Değer"
                            value={newItem.value}
                            onChange={(e) => setNewItem({ ...newItem, value: e.target.value })}
                            fullWidth
                            placeholder={
                                newItem.type === BlacklistType.IpAddress ? '192.168.1.1' :
                                    newItem.type === BlacklistType.Account ? 'user@example.com' :
                                        newItem.type === BlacklistType.Device ? 'device-id-123' :
                                            'TR'
                            }
                        />

                        <TextField
                            label="Neden"
                            value={newItem.reason}
                            onChange={(e) => setNewItem({ ...newItem, reason: e.target.value })}
                            fullWidth
                            multiline
                            rows={3}
                            placeholder="Neden kara listeye eklendiğini açıklayın..."
                        />

                        <TextField
                            label="Süre (Saat)"
                            type="number"
                            value={newItem.durationHours || ''}
                            onChange={(e) => setNewItem({ ...newItem, durationHours: e.target.value ? parseFloat(e.target.value) : null })}
                            fullWidth
                            placeholder="Boş bırakırsanız süresiz olur"
                        />

                        <TextField
                            label="Ekleyen"
                            value={newItem.addedBy || ''}
                            onChange={(e) => setNewItem({ ...newItem, addedBy: e.target.value })}
                            fullWidth
                        />
                    </Box>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setAddDialogOpen(false)}>İptal</Button>
                    <Button
                        onClick={handleAddItem}
                        variant="contained"
                        disabled={!newItem.value || !newItem.reason}
                    >
                        Ekle
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Detail Dialog */}
            <Dialog
                open={detailOpen}
                onClose={() => setDetailOpen(false)}
                maxWidth="md"
                fullWidth
            >
                <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="h6">Kara Liste Öğesi Detayları</Typography>
                    <IconButton onClick={() => setDetailOpen(false)}>
                        <Close />
                    </IconButton>
                </DialogTitle>
                <DialogContent dividers>
                    {selectedItem && (
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                            <Box sx={{ display: 'flex', gap: 3 }}>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Tip
                                    </Typography>
                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                        {getTypeIcon(selectedItem.type)}
                                        <Chip label={FraudDetectionAPI.getBlacklistTypeLabel(selectedItem.type)} />
                                    </Box>
                                </Box>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Durum
                                    </Typography>
                                    <Chip
                                        label={FraudDetectionAPI.getBlacklistStatusLabel(selectedItem.status)}
                                        color={FraudDetectionAPI.getBlacklistStatusColor(selectedItem.status)}
                                    />
                                </Box>
                            </Box>

                            <Box>
                                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                    Değer
                                </Typography>
                                <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
                                    {selectedItem.value}
                                </Typography>
                            </Box>

                            <Box>
                                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                    Neden
                                </Typography>
                                <Typography variant="body1">{selectedItem.reason}</Typography>
                            </Box>

                            <Box sx={{ display: 'flex', gap: 3 }}>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Ekleyen
                                    </Typography>
                                    <Typography variant="body1">{selectedItem.addedBy}</Typography>
                                </Box>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Ekleme Tarihi
                                    </Typography>
                                    <Typography variant="body1">{formatDate(selectedItem.createdAt)}</Typography>
                                </Box>
                            </Box>

                            <Box sx={{ display: 'flex', gap: 3 }}>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Bitiş Tarihi
                                    </Typography>
                                    <Typography variant="body1">
                                        {selectedItem.expiryDate ? formatDate(selectedItem.expiryDate) : 'Süresiz'}
                                    </Typography>
                                </Box>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Aktif/Süresi Dolmuş
                                    </Typography>
                                    <Box sx={{ display: 'flex', gap: 1 }}>
                                        <Chip
                                            label={selectedItem.isActive ? 'Aktif' : 'Pasif'}
                                            color={selectedItem.isActive ? 'success' : 'default'}
                                            size="small"
                                        />
                                        <Chip
                                            label={selectedItem.isExpired ? 'Süresi Dolmuş' : 'Geçerli'}
                                            color={selectedItem.isExpired ? 'error' : 'success'}
                                            size="small"
                                        />
                                    </Box>
                                </Box>
                            </Box>

                            {selectedItem.invalidatedBy && (
                                <Box sx={{ display: 'flex', gap: 3 }}>
                                    <Box sx={{ flex: 1 }}>
                                        <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                            Geçersiz Kılan
                                        </Typography>
                                        <Typography variant="body1">{selectedItem.invalidatedBy}</Typography>
                                    </Box>
                                    <Box sx={{ flex: 1 }}>
                                        <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                            Geçersiz Kılma Tarihi
                                        </Typography>
                                        <Typography variant="body1">
                                            {selectedItem.invalidatedAt ? formatDate(selectedItem.invalidatedAt) : '-'}
                                        </Typography>
                                    </Box>
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

export default BlacklistManagement; 