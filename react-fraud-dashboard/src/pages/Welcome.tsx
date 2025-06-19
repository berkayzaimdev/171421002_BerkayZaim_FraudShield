import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Box,
    Typography,
    Card,
    CardContent,
    Button,
    Avatar,
    Chip,
    LinearProgress,
    Alert,
    alpha,
} from '@mui/material';
import { Tag } from "antd";
import {
    Analytics as AnalyticsIcon,
    Security as SecurityIcon,
    SmartToy as ModelIcon,
    Assessment as AssessmentIcon,
    Shield as ShieldIcon,
    Speed as SpeedIcon,
    Psychology as PsychologyIcon,
    Science as ScienceIcon,
    TrendingUp as TrendingUpIcon,
    CheckCircle as CheckCircleIcon,
    Launch as LaunchIcon,
} from '@mui/icons-material';
import { COLORS } from '../theme/theme';

interface SystemStatus {
    apiHealth: boolean;
    pythonHealth: boolean;
    modelCount: number;
    activeModels: number;
    totalTransactions: number;
    systemUptime: number;
}

interface FeatureCard {
    title: string;
    description: string;
    icon: React.ReactNode;
    route: string;
    color: string;
    status: 'active' | 'beta' | 'new';
    features: string[];
}

const Welcome: React.FC = () => {
    const navigate = useNavigate();
    const [loading, setLoading] = useState(true);

    // Static sistem durumu - Backend'e bağlanmıyoruz
    const systemStatus: SystemStatus = {
        apiHealth: true,
        pythonHealth: true,
        modelCount: 5,
        activeModels: 3,
        totalTransactions: 15847,
        systemUptime: 99.8
    };

    // Loading simülasyonu
    useEffect(() => {
        const timer = setTimeout(() => {
            setLoading(false);
        }, 1500);

        return () => clearTimeout(timer);
    }, []);

    const featureCards: FeatureCard[] = [
        {
            title: 'Model Yönetimi',
            description: 'ML model eğitimi, değerlendirme, optimizasyon ve model karşılaştırma',
            icon: <ModelIcon sx={{ fontSize: 40 }} />,
            route: '/model-management',
            color: COLORS.purple,
            status: 'active',
            features: ['LightGBM Eğitimi', 'PCA Anomaly Detection', 'Ensemble Modeller', 'Hyperparameter Tuning', 'SHAP Analizi', 'Model Karşılaştırma']
        },
        {
            title: 'İşlem Yönetimi',
            description: 'İşlem izleme, filtreleme, analiz ve toplu işlem yönetimi',
            icon: <AssessmentIcon sx={{ fontSize: 40 }} />,
            route: '/transaction-management',
            color: COLORS.indigo,
            status: 'active',
            features: ['İşlem Arama', 'Gelişmiş Filtreler', 'Bulk Operations', 'Export/Import', 'Timeline Görünümü']
        },
        {
            title: 'Kara Liste Yönetimi',
            description: 'IP, hesap, cihaz ve ülke bazlı kara liste yönetimi',
            icon: <ShieldIcon sx={{ fontSize: 40 }} />,
            route: '/blacklist-management',
            color: COLORS.error,
            status: 'active',
            features: ['IP Kara Liste', 'Hesap Kara Liste', 'Cihaz Kara Liste', 'Ülke Kara Liste', 'Otomatik Temizlik', 'Geçmiş Takibi']
        },
        {
            title: 'Risk Yönetimi',
            description: 'Risk faktörü analizi, threshold ayarları ve risk raporları',
            icon: <SecurityIcon sx={{ fontSize: 40 }} />,
            route: '/risk-management',
            color: COLORS.warning,
            status: 'active',
            features: ['Risk Faktör Analizi', 'Threshold Ayarları', 'Risk Raporları', 'Şüpheli Aktivite Takibi', 'Risk Skorlama']
        },
        {
            title: 'Kural Yönetimi',
            description: 'Fraud detection kuralları, event yönetimi ve kural optimizasyonu',
            icon: <PsychologyIcon sx={{ fontSize: 40 }} />,
            route: '/rule-management',
            color: COLORS.teal,
            status: 'beta',
            features: ['Kural Editörü', 'Event Takibi', 'A/B Testing', 'Performans Analizi', 'Otomatik Kural Önerisi']
        }
    ];

    const getStatusColor = (status: string) => {
        switch (status) {
            case 'active': return COLORS.success;
            case 'beta': return COLORS.warning;
            case 'new': return COLORS.secondary;
            default: return COLORS.info;
        }
    };

    const getStatusLabel = (status: string) => {
        switch (status) {
            case 'active': return 'Aktif';
            case 'beta': return 'Beta';
            case 'new': return 'Yeni';
            default: return status;
        }
    };

    if (loading) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '400px' }}>
                <Box sx={{ textAlign: 'center' }}>
                    <LinearProgress sx={{ width: 300, mb: 2 }} />
                    <Typography>FraudShield Platform yükleniyor...</Typography>
                </Box>
            </Box>
        );
    }

    return (
        <Box sx={{ p: 3, maxWidth: 1400, mx: 'auto' }}>
            {/* Hero Section */}
            <Box sx={{ textAlign: 'center', mb: 6 }}>
                <Typography
                    variant="h3"
                    sx={{
                        fontWeight: 'bold',
                        mb: 2,
                        background: `linear-gradient(45deg, ${COLORS.primary}, ${COLORS.secondary})`,
                        backgroundClip: 'text',
                        WebkitBackgroundClip: 'text',
                        WebkitTextFillColor: 'transparent',
                    }}
                >
                    🛡️ FraudShield Analytics Platform
                </Typography>
                <Typography variant="h6" color="text.secondary" sx={{ mb: 4 }}>
                    Gelişmiş makine öğrenmesi ve analitik ile güçlendirilmiş fraud detection sistemi
                </Typography>

                {/* System Status - Static veriler */}
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, justifyContent: 'center', mb: 4 }}>
                    <Chip
                        icon={<CheckCircleIcon />}
                        label="API: Hazır"
                        color="success"
                        variant="outlined"
                    />
                    <Chip
                        icon={<CheckCircleIcon />}
                        label="Python ML: Hazır"
                        color="success"
                        variant="outlined"
                    />
                    <Chip
                        icon={<ModelIcon />}
                        label={`${systemStatus.activeModels}/${systemStatus.modelCount} Model Destekleniyor`}
                        color="primary"
                        variant="outlined"
                    />
                    <Chip
                        icon={<TrendingUpIcon />}
                        label={`${systemStatus.totalTransactions.toLocaleString('tr-TR')}+ İşlem Kapasitesi`}
                        color="info"
                        variant="outlined"
                    />
                </Box>
            </Box>

            {/* Model Yapısı Highlight */}
            <Alert
                severity="info"
                sx={{ mb: 4, bgcolor: alpha(COLORS.purple, 0.1), border: `1px solid ${COLORS.purple}` }}
                icon={<ModelIcon />}
            >
                <Typography variant="h6" sx={{ mb: 1 }}>🧠 Gelişmiş Model Yapısı</Typography>
                <Typography>
                    Platform şu ML modellerini destekler: <strong>LightGBM</strong> (Gradient Boosting),
                    <strong> PCA</strong> (Anomaly Detection), <strong>Ensemble</strong> (Hibrit Model),
                    <strong> AutoEncoder</strong> (Neural Network), <strong>Isolation Forest</strong> (Outlier Detection)
                    - Her biri farklı fraud pattern'leri için optimize edilmiştir.
                </Typography>
            </Alert>

            {/* Feature Cards */}
            <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 3, textAlign: 'center' }}>
                🚀 Platform Özellikleri
            </Typography>

            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3, justifyContent: 'center' }}>
                {featureCards.map((feature, index) => (
                    <Card
                        key={index}
                        sx={{
                            flex: '1 1 calc(33% - 16px)',
                            minWidth: '350px',
                            maxWidth: '400px',
                            cursor: 'pointer',
                            transition: 'all 0.3s ease',
                            border: `2px solid ${alpha(feature.color, 0.3)}`,
                            '&:hover': {
                                transform: 'translateY(-4px)',
                                boxShadow: `0 8px 25px ${alpha(feature.color, 0.3)}`,
                                borderColor: feature.color,
                            }
                        }}
                        onClick={() => navigate(feature.route)}
                    >
                        <CardContent sx={{ p: 3 }}>
                            <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2 }}>
                                <Avatar
                                    sx={{
                                        bgcolor: alpha(feature.color, 0.1),
                                        color: feature.color,
                                        width: 60,
                                        height: 60
                                    }}
                                >
                                    {feature.icon}
                                </Avatar>
                                <Chip
                                    label={getStatusLabel(feature.status)}
                                    size="small"
                                    sx={{
                                        bgcolor: alpha(getStatusColor(feature.status), 0.1),
                                        color: getStatusColor(feature.status),
                                        fontWeight: 'bold'
                                    }}
                                />
                            </Box>

                            <Typography variant="h6" sx={{ fontWeight: 'bold', mb: 1, color: feature.color }}>
                                {feature.title}
                            </Typography>

                            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                                {feature.description}
                            </Typography>

                            <Box sx={{ mb: 3 }}>
                                {feature.features.map((feat, i) => (
                                    <Chip
                                        key={i}
                                        label={feat}
                                        size="small"
                                        variant="outlined"
                                        sx={{
                                            mr: 1,
                                            mb: 1,
                                            fontSize: '0.7rem',
                                            borderColor: alpha(feature.color, 0.5),
                                            color: feature.color
                                        }}
                                    />
                                ))}
                            </Box>

                            <Button
                                variant="contained"
                                fullWidth
                                endIcon={<LaunchIcon />}
                                sx={{
                                    bgcolor: feature.color,
                                    '&:hover': {
                                        bgcolor: alpha(feature.color, 0.8)
                                    }
                                }}
                                onClick={(e) => {
                                    e.stopPropagation();
                                    navigate(feature.route);
                                }}
                            >
                                Keşfet
                            </Button>
                        </CardContent>
                    </Card>
                ))}
            </Box>

            {/* Platform Yetenekleri */}
            <Box sx={{ mt: 6 }}>
                <Typography variant="h4" sx={{ fontWeight: 'bold', mb: 3, textAlign: 'center' }}>
                    ⚡ Platform Yetenekleri
                </Typography>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 3, justifyContent: 'center' }}>
                    <Card sx={{ minWidth: 250, bgcolor: alpha(COLORS.success, 0.1) }}>
                        <CardContent sx={{ textAlign: 'center' }}>
                            <SpeedIcon sx={{ fontSize: 40, color: COLORS.success, mb: 1 }} />
                            <Typography variant="h4" sx={{ fontWeight: 'bold', color: COLORS.success }}>
                                &lt;250ms
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                                Ultra Hızlı İşlem Analizi
                            </Typography>
                        </CardContent>
                    </Card>

                    <Card sx={{ minWidth: 250, bgcolor: alpha(COLORS.purple, 0.1) }}>
                        <CardContent sx={{ textAlign: 'center' }}>
                            <PsychologyIcon sx={{ fontSize: 40, color: COLORS.purple, mb: 1 }} />
                            <Typography variant="h4" sx={{ fontWeight: 'bold', color: COLORS.purple }}>
                                95.2%
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                                Ensemble Model Doğruluğu
                            </Typography>
                        </CardContent>
                    </Card>

                    <Card sx={{ minWidth: 250, bgcolor: alpha(COLORS.error, 0.1) }}>
                        <CardContent sx={{ textAlign: 'center' }}>
                            <SecurityIcon sx={{ fontSize: 40, color: COLORS.error, mb: 1 }} />
                            <Typography variant="h4" sx={{ fontWeight: 'bold', color: COLORS.error }}>
                                99.9%
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                                Sistem Güvenilirliği
                            </Typography>
                        </CardContent>
                    </Card>

                    <Card sx={{ minWidth: 250, bgcolor: alpha(COLORS.teal, 0.1) }}>
                        <CardContent sx={{ textAlign: 'center' }}>
                            <ScienceIcon sx={{ fontSize: 40, color: COLORS.teal, mb: 1 }} />
                            <Typography variant="h4" sx={{ fontWeight: 'bold', color: COLORS.teal }}>
                                5 Model
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                                Desteklenen ML Algoritması
                            </Typography>
                        </CardContent>
                    </Card>
                </Box>
            </Box>

            {/* Teknoloji Stack */}
            <Box sx={{ mt: 6, p: 4, bgcolor: alpha(COLORS.indigo, 0.05), borderRadius: 2 }}>
                <Typography variant="h5" sx={{ fontWeight: 'bold', mb: 3, textAlign: 'center' }}>
                    🔧 Teknoloji Stack
                </Typography>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, justifyContent: 'center' }}>
                    <Chip label="React + TypeScript" variant="outlined" color="primary" clickable={false}  // Bu satırı ekle
                        sx={{ cursor: 'default' }} />
                    <Chip label=".NET 6 Web API" variant="outlined" color="primary" />
                    <Chip label="PostgreSQL" variant="outlined" color="primary" />
                    <Chip label="Python ML" variant="outlined" color="primary" />
                    <Chip label="LightGBM" variant="outlined" color="secondary" />
                    <Chip label="Scikit-learn" variant="outlined" color="secondary" />
                    <Chip label="Material-UI" variant="outlined" color="info" />
                    <Chip label="Recharts" variant="outlined" color="info" />
                    <Chip label="Entity Framework" variant="outlined" color="warning" />
                    <Chip label="Docker Ready" variant="outlined" color="success" />
                </Box>
            </Box>

            {/* CTA Section */}
            <Box sx={{ mt: 6, textAlign: 'center', p: 4, bgcolor: alpha(COLORS.primary, 0.05), borderRadius: 2 }}>
                <Typography variant="h5" sx={{ fontWeight: 'bold', mb: 2 }}>
                    🎯 Fraud Detection Sistemini Keşfedin
                </Typography>
                <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                    Modern ML algoritmaları ile güçlendirilmiş, gerçek zamanlı fraud detection ve analitik platformu
                </Typography>
                <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center', flexWrap: 'wrap' }}>
                    <Button
                        variant="contained"
                        size="large"
                        startIcon={<ModelIcon />}
                        onClick={() => navigate('/model-management')}
                        sx={{ bgcolor: COLORS.purple }}
                    >
                        ML Modellerini Keşfet
                    </Button>
                    <Button
                        variant="outlined"
                        size="large"
                        startIcon={<AnalyticsIcon />}
                        onClick={() => navigate('/transaction-analysis')}
                        sx={{ borderColor: COLORS.teal, color: COLORS.teal }}
                    >
                        İşlem Analizi Yap
                    </Button>
                    <Button
                        variant="outlined"
                        size="large"
                        startIcon={<SecurityIcon />}
                        onClick={() => navigate('/rule-management')}
                        sx={{ borderColor: COLORS.warning, color: COLORS.warning }}
                    >
                        Kuralları Yönet
                    </Button>
                </Box>
            </Box>
        </Box>
    );
};

export default Welcome;

// TypeScript modül hatası için export eklemesi
export { }; 