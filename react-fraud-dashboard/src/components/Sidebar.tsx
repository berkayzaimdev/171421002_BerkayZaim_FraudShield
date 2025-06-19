import React from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  Drawer,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Box,
  Typography,
  Divider,
} from '@mui/material';
import {
  ModelTraining as ModelIcon,
  ManageAccounts as TransactionIcon,
  Rule as RuleIcon,
  TrendingUp as TrendingUpIcon,
  Shield as ShieldIcon,
  Block as BlockIcon,
  Analytics as AnalyticsIcon,
} from '@mui/icons-material';

const drawerWidth = 260;

interface MenuItem {
  text: string;
  icon: React.ReactElement;
  path: string;
  description: string;
}

const menuItems: MenuItem[] = [
  {
    text: 'Model Yönetimi',
    icon: <ModelIcon />,
    path: '/model-management',
    description: 'ML Model Eğitimi ve Yönetimi',
  },
  {
    text: 'İşlem Yönetimi',
    icon: <TransactionIcon />,
    path: '/transaction-management',
    description: 'İşlem İzleme ve Yönetimi',
  },
  {
    text: 'Kural Yönetimi',
    icon: <RuleIcon />,
    path: '/rule-management',
    description: 'Fraud Kuralları ve Olayları',
  },
  {
    text: 'Risk Yönetimi',
    icon: <ShieldIcon />,
    path: '/risk-management',
    description: 'Risk Faktörleri ve Analizi',
  },
  {
    text: 'Kara Liste',
    icon: <BlockIcon />,
    path: '/blacklist-management',
    description: 'IP, Hesap ve Cihaz Blokları',
  },
];

const Sidebar: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();

  const handleNavigation = (path: string) => {
    navigate(path);
  };

  return (
    <Drawer
      variant="permanent"
      sx={{
        width: drawerWidth,
        flexShrink: 0,
        '& .MuiDrawer-paper': {
          width: drawerWidth,
          boxSizing: 'border-box',
          background: 'linear-gradient(180deg, #f8f9fa 0%, #e9ecef 100%)',
          borderRight: '1px solid #dee2e6',
        },
      }}
    >
      <Box sx={{ p: 3, textAlign: 'center' }}>
        <TrendingUpIcon sx={{ fontSize: 48, color: '#1976d2', mb: 2 }} />
        <Typography variant="h5" sx={{ fontWeight: 'bold', color: '#1976d2', mb: 1 }}>
          Fraud Shield
        </Typography>
        <Typography variant="body2" color="textSecondary">
          v2.0 Advanced Analytics
        </Typography>
      </Box>

      <Divider sx={{ mx: 2 }} />

      <List sx={{ px: 2, py: 3 }}>
        {menuItems.map((item) => (
          <ListItem key={item.text} disablePadding sx={{ mb: 2 }}>
            <ListItemButton
              onClick={() => handleNavigation(item.path)}
              selected={location.pathname === item.path}
              sx={{
                borderRadius: 3,
                py: 2,
                px: 2,
                '&.Mui-selected': {
                  backgroundColor: '#1976d2',
                  color: 'white',
                  '&:hover': {
                    backgroundColor: '#1565c0',
                  },
                  '& .MuiListItemIcon-root': {
                    color: 'white',
                  },
                },
                '&:hover': {
                  backgroundColor: '#f5f5f5',
                },
                transition: 'all 0.2s ease-in-out',
              }}
            >
              <ListItemIcon sx={{ minWidth: 48 }}>
                {item.icon}
              </ListItemIcon>
              <Box>
                <ListItemText
                  primary={item.text}
                  secondary={item.description}
                  primaryTypographyProps={{
                    fontWeight: 'medium',
                    fontSize: '1rem',
                    mb: 0.5
                  }}
                  secondaryTypographyProps={{
                    fontSize: '0.875rem',
                    color: 'inherit',
                    sx: { opacity: 0.7 }
                  }}
                />
              </Box>
            </ListItemButton>
          </ListItem>
        ))}
      </List>
    </Drawer>
  );
};

export default Sidebar; 