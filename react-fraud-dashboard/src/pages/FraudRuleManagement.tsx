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
    Tabs,
    Tab,
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
    Snackbar
} from '@mui/material';
import {
    Search,
    Refresh,
    Visibility,
    Warning,
    CheckCircle,
    Error,
    Schedule,
    Info,
    Close
} from '@mui/icons-material';
import FraudDetectionAPI, {
    FraudRuleResponse,
    FraudEventResponse,
    RuleCategory,
    RuleStatus,
    ImpactLevel,
    RuleType,
    RuleAction
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
            id={`rule-tabpanel-${index}`}
            aria-labelledby={`rule-tab-${index}`}
            {...other}
        >
            {value === index && <Box sx={{ p: 3 }}>{children}</Box>}
        </div>
    );
}

const FraudRuleManagement: React.FC = () => {
    const [activeTab, setActiveTab] = useState(0);
    const [rules, setRules] = useState<FraudRuleResponse[]>([]);
    const [events, setEvents] = useState<FraudEventResponse[]>([]);
    const [loading, setLoading] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');
    const [selectedRule, setSelectedRule] = useState<FraudRuleResponse | null>(null);
    const [selectedEvent, setSelectedEvent] = useState<FraudEventResponse | null>(null);
    const [ruleDetailOpen, setRuleDetailOpen] = useState(false);
    const [eventDetailOpen, setEventDetailOpen] = useState(false);
    const [snackbarMessage, setSnackbarMessage] = useState<string | null>(null);

    useEffect(() => {
        loadData();
    }, []);

    const loadData = async () => {
        setLoading(true);
        try {
            await Promise.all([loadRules(), loadEvents()]);
        } catch (error) {
            setSnackbarMessage('Veri yükleme hatası');
        } finally {
            setLoading(false);
        }
    };

    const loadRules = async () => {
        try {
            const response = await FraudDetectionAPI.getFraudRules();
            if (response.success && response.data) {
                setRules(response.data);
            }
        } catch (error) {
            console.error('Kurallar yüklenirken hata:', error);
        }
    };

    const loadEvents = async () => {
        try {
            const response = await FraudDetectionAPI.getFraudEvents();
            if (response.success && response.data) {
                setEvents(response.data);
            }
        } catch (error) {
            console.error('Olaylar yüklenirken hata:', error);
        }
    };

    const getStatusColor = (status: RuleStatus) => {
        switch (status) {
            case RuleStatus.Active: return 'success';
            case RuleStatus.Inactive: return 'default';
            case RuleStatus.TestMode: return 'warning';
            case RuleStatus.Draft: return 'info';
            default: return 'default';
        }
    };

    const getStatusIcon = (status: RuleStatus) => {
        switch (status) {
            case RuleStatus.Active: return <CheckCircle />;
            case RuleStatus.Inactive: return <Error />;
            case RuleStatus.TestMode: return <Warning />;
            case RuleStatus.Draft: return <Schedule />;
            default: return <Info />;
        }
    };

    const getImpactColor = (level: ImpactLevel) => {
        switch (level) {
            case ImpactLevel.Critical: return 'error';
            case ImpactLevel.High: return 'warning';
            case ImpactLevel.Medium: return 'info';
            case ImpactLevel.Low: return 'success';
            default: return 'default';
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

    const filteredRules = rules.filter(rule =>
        rule.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        rule.description.toLowerCase().includes(searchTerm.toLowerCase()) ||
        rule.ruleCode.toLowerCase().includes(searchTerm.toLowerCase())
    );

    const filteredEvents = events.filter(event =>
        event.ruleName.toLowerCase().includes(searchTerm.toLowerCase()) ||
        event.ruleCode.toLowerCase().includes(searchTerm.toLowerCase()) ||
        (event.accountId && event.accountId.toString().includes(searchTerm)) ||
        event.ipAddress.includes(searchTerm)
    );

    const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
        setActiveTab(newValue);
        setSearchTerm('');
    };

    const handleViewRuleDetail = (rule: FraudRuleResponse) => {
        setSelectedRule(rule);
        setRuleDetailOpen(true);
    };

    const handleViewEventDetail = (event: FraudEventResponse) => {
        setSelectedEvent(event);
        setEventDetailOpen(true);
    };

    const activeRulesCount = rules.filter(r => r.status === RuleStatus.Active).length;
    const unresolvedEventsCount = events.filter(e => !e.resolvedDate).length;

    return (
        <Container maxWidth="xl" sx={{ py: 4 }}>
            {/* Header */}
            <Box sx={{ mb: 4 }}>

                <Typography variant="body1" color="text.secondary">
                    Fraud tespit kuralları ve tetiklenen olayları yönetin
                </Typography>
            </Box>

            {/* Summary Cards */}
            <Box sx={{ display: 'flex', gap: 3, mb: 4 }}>
                <Card sx={{ flex: 1 }}>
                    <CardContent>
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                            <Box>
                                <Typography variant="h4" sx={{ fontWeight: 'bold', color: '#4caf50' }}>
                                    {rules.length}
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Toplam Kural
                                </Typography>
                            </Box>
                            <Badge badgeContent={activeRulesCount} color="success">
                                <CheckCircle sx={{ fontSize: 40, color: '#4caf50' }} />
                            </Badge>
                        </Box>
                    </CardContent>
                </Card>

                <Card sx={{ flex: 1 }}>
                    <CardContent>
                        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                            <Box>
                                <Typography variant="h4" sx={{ fontWeight: 'bold', color: '#ff9800' }}>
                                    {events.length}
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Toplam Olay
                                </Typography>
                            </Box>
                            <Badge badgeContent={unresolvedEventsCount} color="warning">
                                <Warning sx={{ fontSize: 40, color: '#ff9800' }} />
                            </Badge>
                        </Box>
                    </CardContent>
                </Card>
            </Box>

            {/* Tabs and Content */}
            <Paper sx={{ borderRadius: 3, overflow: 'hidden' }}>
                <Box sx={{ borderBottom: 1, borderColor: 'divider', px: 3, pt: 2 }}>
                    <Tabs value={activeTab} onChange={handleTabChange}>
                        <Tab
                            label={
                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                    <CheckCircle />
                                    Fraud Kuralları ({rules.length})
                                </Box>
                            }
                        />
                        <Tab
                            label={
                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                    <Warning />
                                    Tetiklenen Olaylar ({events.length})
                                </Box>
                            }
                        />
                    </Tabs>
                </Box>

                {/* Search and Actions */}
                <Box sx={{ p: 3, display: 'flex', alignItems: 'center', gap: 2, borderBottom: 1, borderColor: 'divider' }}>
                    <TextField
                        placeholder={activeTab === 0 ? "Kural ara..." : "Olay ara..."}
                        value={searchTerm}
                        onChange={(e) => setSearchTerm(e.target.value)}
                        sx={{ flexGrow: 1 }}
                        InputProps={{
                            startAdornment: (
                                <InputAdornment position="start">
                                    <Search />
                                </InputAdornment>
                            ),
                        }}
                    />
                    <Button
                        variant="outlined"
                        startIcon={<Refresh />}
                        onClick={loadData}
                        disabled={loading}
                    >
                        Yenile
                    </Button>
                </Box>

                {/* Rules Tab */}
                <TabPanel value={activeTab} index={0}>
                    {loading ? (
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                            <CircularProgress />
                        </Box>
                    ) : (
                        <TableContainer>
                            <Table>
                                <TableHead>
                                    <TableRow>
                                        <TableCell><strong>Kural Kodu</strong></TableCell>
                                        <TableCell><strong>Kural Adı</strong></TableCell>
                                        <TableCell><strong>Kategori</strong></TableCell>
                                        <TableCell><strong>Durum</strong></TableCell>
                                        <TableCell><strong>Etki Seviyesi</strong></TableCell>
                                        <TableCell><strong>Oluşturma Tarihi</strong></TableCell>
                                        <TableCell align="center"><strong>İşlemler</strong></TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {filteredRules.map((rule) => (
                                        <TableRow key={rule.id} hover>
                                            <TableCell>
                                                <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                                    {rule.ruleCode}
                                                </Typography>
                                            </TableCell>
                                            <TableCell>
                                                <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
                                                    {rule.name}
                                                </Typography>
                                                <Typography variant="caption" color="text.secondary">
                                                    {rule.description.length > 50
                                                        ? `${rule.description.substring(0, 50)}...`
                                                        : rule.description}
                                                </Typography>
                                            </TableCell>
                                            <TableCell>
                                                <Chip
                                                    label={FraudDetectionAPI.getRuleCategoryLabel(rule.category)}
                                                    size="small"
                                                    variant="outlined"
                                                />
                                            </TableCell>
                                            <TableCell>
                                                <Chip
                                                    icon={getStatusIcon(rule.status)}
                                                    label={FraudDetectionAPI.getRuleStatusLabel(rule.status)}
                                                    color={getStatusColor(rule.status)}
                                                    size="small"
                                                />
                                            </TableCell>
                                            <TableCell>
                                                <Chip
                                                    label={FraudDetectionAPI.getImpactLevelLabel(rule.impactLevel)}
                                                    color={getImpactColor(rule.impactLevel)}
                                                    size="small"
                                                />
                                            </TableCell>
                                            <TableCell>
                                                <Typography variant="body2">
                                                    {formatDate(rule.createdDate)}
                                                </Typography>
                                            </TableCell>
                                            <TableCell align="center">
                                                <Tooltip title="Detayları Görüntüle">
                                                    <IconButton
                                                        onClick={() => handleViewRuleDetail(rule)}
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
                </TabPanel>

                {/* Events Tab */}
                <TabPanel value={activeTab} index={1}>
                    {loading ? (
                        <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
                            <CircularProgress />
                        </Box>
                    ) : (
                        <TableContainer>
                            <Table>
                                <TableHead>
                                    <TableRow>
                                        <TableCell><strong>Kural Kodu</strong></TableCell>
                                        <TableCell><strong>Kural Adı</strong></TableCell>
                                        <TableCell><strong>Hesap ID</strong></TableCell>
                                        <TableCell><strong>IP Adresi</strong></TableCell>
                                        <TableCell><strong>Oluşturma Tarihi</strong></TableCell>
                                        <TableCell><strong>Durum</strong></TableCell>
                                        <TableCell align="center"><strong>İşlemler</strong></TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {filteredEvents.map((event) => (
                                        <TableRow key={event.id} hover>
                                            <TableCell>
                                                <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                                    {event.ruleCode}
                                                </Typography>
                                            </TableCell>
                                            <TableCell>
                                                <Typography variant="body2" sx={{ fontWeight: 'medium' }}>
                                                    {event.ruleName}
                                                </Typography>
                                            </TableCell>
                                            <TableCell>
                                                <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                                    {event.accountId || '-'}
                                                </Typography>
                                            </TableCell>
                                            <TableCell>
                                                <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                                    {event.ipAddress}
                                                </Typography>
                                            </TableCell>
                                            <TableCell>
                                                <Typography variant="body2">
                                                    {formatDate(event.createdDate)}
                                                </Typography>
                                            </TableCell>
                                            <TableCell>
                                                <Chip
                                                    label={event.resolvedDate ? 'Çözüldü' : 'Açık'}
                                                    color={event.resolvedDate ? 'success' : 'warning'}
                                                    size="small"
                                                />
                                            </TableCell>
                                            <TableCell align="center">
                                                <Tooltip title="Detayları Görüntüle">
                                                    <IconButton
                                                        onClick={() => handleViewEventDetail(event)}
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
                </TabPanel>
            </Paper>

            {/* Rule Detail Modal */}
            <Dialog
                open={ruleDetailOpen}
                onClose={() => setRuleDetailOpen(false)}
                maxWidth="md"
                fullWidth
            >
                <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="h6">Kural Detayları</Typography>
                    <IconButton onClick={() => setRuleDetailOpen(false)}>
                        <Close />
                    </IconButton>
                </DialogTitle>
                <DialogContent dividers>
                    {selectedRule && (
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                            <Box>
                                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                    Kural Kodu
                                </Typography>
                                <Typography variant="body1" sx={{ fontFamily: 'monospace', bgcolor: 'grey.100', p: 1, borderRadius: 1 }}>
                                    {selectedRule.ruleCode}
                                </Typography>
                            </Box>

                            <Box>
                                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                    Kural Adı
                                </Typography>
                                <Typography variant="body1">{selectedRule.name}</Typography>
                            </Box>

                            <Box>
                                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                    Açıklama
                                </Typography>
                                <Typography variant="body1">{selectedRule.description}</Typography>
                            </Box>

                            <Box sx={{ display: 'flex', gap: 3 }}>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Kategori
                                    </Typography>
                                    <Chip label={FraudDetectionAPI.getRuleCategoryLabel(selectedRule.category)} />
                                </Box>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Durum
                                    </Typography>
                                    <Chip
                                        icon={getStatusIcon(selectedRule.status)}
                                        label={FraudDetectionAPI.getRuleStatusLabel(selectedRule.status)}
                                        color={getStatusColor(selectedRule.status)}
                                    />
                                </Box>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Etki Seviyesi
                                    </Typography>
                                    <Chip
                                        label={FraudDetectionAPI.getImpactLevelLabel(selectedRule.impactLevel)}
                                        color={getImpactColor(selectedRule.impactLevel)}
                                    />
                                </Box>
                            </Box>

                            <Box>
                                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                    Aksiyonlar
                                </Typography>
                                <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                                    {selectedRule.actions.map((action, index) => (
                                        <Chip
                                            key={index}
                                            label={FraudDetectionAPI.getRuleActionLabel(action)}
                                            variant="outlined"
                                            size="small"
                                        />
                                    ))}
                                </Box>
                            </Box>

                            <Box>
                                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                    Koşul
                                </Typography>
                                <Typography
                                    variant="body2"
                                    sx={{
                                        fontFamily: 'monospace',
                                        bgcolor: 'grey.50',
                                        p: 2,
                                        borderRadius: 1,
                                        border: '1px solid',
                                        borderColor: 'grey.300',
                                        whiteSpace: 'pre-wrap'
                                    }}
                                >
                                    {selectedRule.condition}
                                </Typography>
                            </Box>

                            <Box sx={{ display: 'flex', gap: 3 }}>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Oluşturma Tarihi
                                    </Typography>
                                    <Typography variant="body2">{formatDate(selectedRule.createdDate)}</Typography>
                                </Box>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Son Değişiklik
                                    </Typography>
                                    <Typography variant="body2">{formatDate(selectedRule.lastModified)}</Typography>
                                </Box>
                            </Box>
                        </Box>
                    )}
                </DialogContent>
            </Dialog>

            {/* Event Detail Modal */}
            <Dialog
                open={eventDetailOpen}
                onClose={() => setEventDetailOpen(false)}
                maxWidth="md"
                fullWidth
            >
                <DialogTitle sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <Typography variant="h6">Olay Detayları</Typography>
                    <IconButton onClick={() => setEventDetailOpen(false)}>
                        <Close />
                    </IconButton>
                </DialogTitle>
                <DialogContent dividers>
                    {selectedEvent && (
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                            <Box sx={{ display: 'flex', gap: 3 }}>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Kural Kodu
                                    </Typography>
                                    <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
                                        {selectedEvent.ruleCode}
                                    </Typography>
                                </Box>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Kural Adı
                                    </Typography>
                                    <Typography variant="body1">{selectedEvent.ruleName}</Typography>
                                </Box>
                            </Box>

                            <Box sx={{ display: 'flex', gap: 3 }}>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Hesap ID
                                    </Typography>
                                    <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
                                        {selectedEvent.accountId || 'Belirtilmemiş'}
                                    </Typography>
                                </Box>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        IP Adresi
                                    </Typography>
                                    <Typography variant="body1" sx={{ fontFamily: 'monospace' }}>
                                        {selectedEvent.ipAddress}
                                    </Typography>
                                </Box>
                            </Box>

                            <Box>
                                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                    Cihaz Bilgisi
                                </Typography>
                                <Typography variant="body2" sx={{ fontFamily: 'monospace', bgcolor: 'grey.100', p: 1, borderRadius: 1 }}>
                                    {selectedEvent.deviceInfo}
                                </Typography>
                            </Box>

                            <Box>
                                <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                    Aksiyonlar
                                </Typography>
                                <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                                    {selectedEvent.actions.map((action, index) => (
                                        <Chip
                                            key={index}
                                            label={FraudDetectionAPI.getRuleActionLabel(action)}
                                            variant="outlined"
                                            size="small"
                                            color="warning"
                                        />
                                    ))}
                                </Box>
                            </Box>

                            <Box sx={{ display: 'flex', gap: 3 }}>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Oluşturma Tarihi
                                    </Typography>
                                    <Typography variant="body2">{formatDate(selectedEvent.createdDate)}</Typography>
                                </Box>
                                <Box sx={{ flex: 1 }}>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Durum
                                    </Typography>
                                    <Chip
                                        label={selectedEvent.resolvedDate ? 'Çözüldü' : 'Açık'}
                                        color={selectedEvent.resolvedDate ? 'success' : 'warning'}
                                        size="small"
                                    />
                                </Box>
                            </Box>

                            {selectedEvent.resolvedDate && (
                                <Box sx={{ display: 'flex', gap: 3 }}>
                                    <Box sx={{ flex: 1 }}>
                                        <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                            Çözüm Tarihi
                                        </Typography>
                                        <Typography variant="body2">{formatDate(selectedEvent.resolvedDate)}</Typography>
                                    </Box>
                                    <Box sx={{ flex: 1 }}>
                                        <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                            Çözen Kişi
                                        </Typography>
                                        <Typography variant="body2">{selectedEvent.resolvedBy || 'Belirtilmemiş'}</Typography>
                                    </Box>
                                </Box>
                            )}

                            {selectedEvent.resolutionNotes && (
                                <Box>
                                    <Typography variant="subtitle2" color="text.secondary" gutterBottom>
                                        Çözüm Notları
                                    </Typography>
                                    <Typography variant="body2" sx={{ bgcolor: 'grey.100', p: 2, borderRadius: 1 }}>
                                        {selectedEvent.resolutionNotes}
                                    </Typography>
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

export default FraudRuleManagement; 