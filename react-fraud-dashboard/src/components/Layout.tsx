import React from 'react';
import { Box, Container, Fade } from '@mui/material';
import { useLocation } from 'react-router-dom';
import Header from './Header';
import Sidebar from './Sidebar';
import { alpha } from '@mui/material/styles';
import { getPageColor, COLORS } from '../theme/theme';

interface LayoutProps {
    children: React.ReactNode;
}

const Layout: React.FC<LayoutProps> = ({ children }) => {
    const location = useLocation();
    const pageColor = getPageColor(location.pathname);

    return (
        <Box
            sx={{
                display: 'flex',
                minHeight: '100vh',
                bgcolor: 'background.default'
            }}
        >
            <Sidebar />
            <Box
                sx={{
                    flexGrow: 1,
                    display: 'flex',
                    flexDirection: 'column',
                    minHeight: '100vh'
                }}
            >
                <Header />

                {/* Ana İçerik Alanı */}
                <Box
                    component="main"
                    sx={{
                        flexGrow: 1,
                        position: 'relative',
                        background: `linear-gradient(135deg, ${alpha(pageColor, 0.02)} 0%, ${alpha(pageColor, 0.01)} 100%)`,
                        minHeight: 'calc(100vh - 120px)',
                    }}
                >
                    {/* Dekoratif Background Pattern */}
                    <Box
                        sx={{
                            position: 'absolute',
                            top: 0,
                            left: 0,
                            right: 0,
                            height: '200px',
                            background: `linear-gradient(135deg, ${alpha(pageColor, 0.05)} 0%, transparent 70%)`,
                            zIndex: 0,
                            '&::before': {
                                content: '""',
                                position: 'absolute',
                                top: 0,
                                left: 0,
                                right: 0,
                                bottom: 0,
                                backgroundImage: `radial-gradient(circle at 20% 50%, ${alpha(pageColor, 0.1)} 0%, transparent 50%), 
                                  radial-gradient(circle at 80% 20%, ${alpha(pageColor, 0.08)} 0%, transparent 50%), 
                                  radial-gradient(circle at 40% 80%, ${alpha(pageColor, 0.06)} 0%, transparent 50%)`,
                            }
                        }}
                    />

                    {/* İçerik Container */}
                    <Container
                        maxWidth="xl"
                        sx={{
                            position: 'relative',
                            zIndex: 1,
                            py: 3,
                            px: { xs: 2, sm: 3 }
                        }}
                    >
                        <Fade in timeout={500}>
                            <Box>
                                {children}
                            </Box>
                        </Fade>
                    </Container>
                </Box>

                {/* Footer */}
                <Box
                    component="footer"
                    sx={{
                        mt: 'auto',
                        py: 2,
                        px: 3,
                        borderTop: `1px solid ${COLORS.grey[200]}`,
                        bgcolor: 'background.paper',
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center',
                        fontSize: '0.875rem',
                        color: 'text.secondary'
                    }}
                >
                    <Box>
                        © 2024 FraudShield Analytics Platform - Advanced ML Fraud Detection
                    </Box>
                    <Box sx={{ display: 'flex', gap: 2 }}>
                        <Box
                            sx={{
                                width: 8,
                                height: 8,
                                borderRadius: '50%',
                                bgcolor: COLORS.success,
                                animation: 'pulse 2s infinite',
                                '@keyframes pulse': {
                                    '0%': {
                                        transform: 'scale(0.95)',
                                        boxShadow: `0 0 0 0 ${alpha(COLORS.success, 0.7)}`,
                                    },
                                    '70%': {
                                        transform: 'scale(1)',
                                        boxShadow: `0 0 0 10px ${alpha(COLORS.success, 0)}`,
                                    },
                                    '100%': {
                                        transform: 'scale(0.95)',
                                        boxShadow: `0 0 0 0 ${alpha(COLORS.success, 0)}`,
                                    },
                                }
                            }}
                        />
                        <span>Sistem Aktif</span>
                    </Box>
                </Box>
            </Box>
        </Box>
    );
};

export default Layout; 