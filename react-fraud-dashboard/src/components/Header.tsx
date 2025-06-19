import React from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  AppBar,
  Toolbar,
  Typography,
  Box,
  Button,
  alpha,
} from '@mui/material';
import {
  Home as HomeIcon,
  ModelTraining as ModelIcon,
  Analytics as AnalyticsIcon,
  Security as SecurityIcon,
  Shield as ShieldIcon,
  Assessment as AssessmentIcon,
  Block as BlockIcon,
} from '@mui/icons-material';
import { COLORS } from '../theme/theme';

const Header: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();

  const getPageInfo = () => {
    switch (location.pathname) {
      case '/model-management':
        return {
          title: 'Model Yönetimi',
          subtitle: 'ML Model Eğitimi ve Optimizasyon',
          icon: <ModelIcon sx={{ fontSize: 32 }} />,
          color: COLORS.purple
        };
      case '/transaction-analysis':
        return {
          title: 'İşlem Analizi',
          subtitle: 'Comprehensive Fraud Detection Analizi',
          icon: <AnalyticsIcon sx={{ fontSize: 32 }} />,
          color: COLORS.teal
        };
      case '/transaction-management':
        return {
          title: 'İşlem Yönetimi',
          subtitle: 'İşlem İzleme ve Toplu Yönetim',
          icon: <AssessmentIcon sx={{ fontSize: 32 }} />,
          color: COLORS.indigo
        };
      case '/rule-management':
        return {
          title: 'Kural Yönetimi',
          subtitle: 'Fraud Detection Kuralları ve Event Yönetimi',
          icon: <SecurityIcon sx={{ fontSize: 32 }} />,
          color: COLORS.warning
        };
      case '/risk-management':
        return {
          title: 'Risk Yönetimi',
          subtitle: 'Risk Faktör Analizi ve Blacklist Yönetimi',
          icon: <ShieldIcon sx={{ fontSize: 32 }} />,
          color: COLORS.error
        };
      case '/blacklist-management':
        return {
          title: 'Kara Liste Yönetimi',
          subtitle: 'IP, Hesap ve Cihaz Blokları',
          icon: <BlockIcon sx={{ fontSize: 32 }} />,
          color: COLORS.error
        };
      default:
        return {
          title: 'FraudShield',
          subtitle: 'Advanced Analytics Platform',
          icon: <HomeIcon sx={{ fontSize: 32 }} />,
          color: COLORS.primary
        };
    }
  };

  const pageInfo = getPageInfo();

  return (
    <AppBar
      position="static"
      elevation={0}
      sx={{
        background: `linear-gradient(135deg, ${alpha(pageInfo.color, 0.1)} 0%, ${alpha(pageInfo.color, 0.05)} 100%)`,
        borderBottom: `3px solid ${alpha(pageInfo.color, 0.3)}`,
        color: 'text.primary'
      }}
    >
      <Toolbar sx={{ py: 2, px: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', flexGrow: 1 }}>
          <Box
            sx={{
              bgcolor: alpha(pageInfo.color, 0.1),
              color: pageInfo.color,
              borderRadius: 2,
              p: 1,
              mr: 3,
              display: 'flex',
              alignItems: 'center',
              border: `2px solid ${alpha(pageInfo.color, 0.3)}`
            }}
          >
            {pageInfo.icon}
          </Box>

          <Box>
            <Typography
              variant="h4"
              sx={{
                fontWeight: 'bold',
                color: pageInfo.color,
                mb: 0.5
              }}
            >
              {pageInfo.title}
            </Typography>
            <Typography
              variant="body1"
              sx={{
                color: 'text.secondary',
                fontWeight: 500
              }}
            >
              {pageInfo.subtitle}
            </Typography>
          </Box>
        </Box>

        <Button
          startIcon={<HomeIcon />}
          onClick={() => navigate('/')}
          sx={{
            bgcolor: alpha(pageInfo.color, 0.1),
            color: pageInfo.color,
            border: `2px solid ${alpha(pageInfo.color, 0.3)}`,
            borderRadius: 2,
            px: 2,
            py: 1,
            fontWeight: 'bold',
            '&:hover': {
              bgcolor: alpha(pageInfo.color, 0.2),
              borderColor: pageInfo.color,
            },
            transition: 'all 0.3s ease'
          }}
        >
          Ana Sayfa
        </Button>
      </Toolbar>
    </AppBar>
  );
};

export default Header; 