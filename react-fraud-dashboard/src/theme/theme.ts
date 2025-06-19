import { createTheme, ThemeOptions } from '@mui/material/styles';
import { alpha } from '@mui/material/styles';

// FraudShield Platform Renk Paleti
export const COLORS = {
    primary: '#1976d2',
    secondary: '#dc004e',
    success: '#4caf50',
    warning: '#ff9800',
    error: '#f44336',
    info: '#2196f3',
    purple: '#9c27b0',
    teal: '#009688',
    indigo: '#3f51b5',
    pink: '#e91e63',
    grey: {
        50: '#fafafa',
        100: '#f5f5f5',
        200: '#eeeeee',
        300: '#e0e0e0',
        400: '#bdbdbd',
        500: '#9e9e9e',
        600: '#757575',
        700: '#616161',
        800: '#424242',
        900: '#212121',
    }
};

// Gradient sistemleri
export const GRADIENTS = {
    primary: `linear-gradient(135deg, ${COLORS.primary} 0%, ${alpha(COLORS.primary, 0.8)} 100%)`,
    secondary: `linear-gradient(135deg, ${COLORS.secondary} 0%, ${alpha(COLORS.secondary, 0.8)} 100%)`,
    purple: `linear-gradient(135deg, ${COLORS.purple} 0%, ${alpha(COLORS.purple, 0.8)} 100%)`,
    teal: `linear-gradient(135deg, ${COLORS.teal} 0%, ${alpha(COLORS.teal, 0.8)} 100%)`,
    indigo: `linear-gradient(135deg, ${COLORS.indigo} 0%, ${alpha(COLORS.indigo, 0.8)} 100%)`,
    warning: `linear-gradient(135deg, ${COLORS.warning} 0%, ${alpha(COLORS.warning, 0.8)} 100%)`,
    error: `linear-gradient(135deg, ${COLORS.error} 0%, ${alpha(COLORS.error, 0.8)} 100%)`,
    success: `linear-gradient(135deg, ${COLORS.success} 0%, ${alpha(COLORS.success, 0.8)} 100%)`,
    rainbow: `linear-gradient(135deg, ${COLORS.primary} 0%, ${COLORS.purple} 25%, ${COLORS.teal} 50%, ${COLORS.warning} 75%, ${COLORS.secondary} 100%)`,
};

// Gölge sistemi
export const SHADOWS = {
    card: `0 4px 20px ${alpha('#000', 0.1)}`,
    cardHover: `0 8px 30px ${alpha('#000', 0.15)}`,
    button: `0 2px 8px ${alpha('#000', 0.15)}`,
    modal: `0 16px 40px ${alpha('#000', 0.2)}`,
};

// Border radius sistemi
export const RADIUS = {
    xs: 4,
    sm: 8,
    md: 12,
    lg: 16,
    xl: 20,
    full: 9999,
};

// Spacing sistemi
export const SPACING = {
    xs: 4,
    sm: 8,
    md: 16,
    lg: 24,
    xl: 32,
    xxl: 40,
};

// Ana tema konfigürasyonu
const themeOptions: ThemeOptions = {
    palette: {
        mode: 'light',
        primary: {
            main: COLORS.primary,
            light: alpha(COLORS.primary, 0.7),
            dark: alpha(COLORS.primary, 0.9),
            contrastText: '#ffffff',
        },
        secondary: {
            main: COLORS.secondary,
            light: alpha(COLORS.secondary, 0.7),
            dark: alpha(COLORS.secondary, 0.9),
            contrastText: '#ffffff',
        },
        success: {
            main: COLORS.success,
            light: alpha(COLORS.success, 0.7),
            dark: alpha(COLORS.success, 0.9),
        },
        warning: {
            main: COLORS.warning,
            light: alpha(COLORS.warning, 0.7),
            dark: alpha(COLORS.warning, 0.9),
        },
        error: {
            main: COLORS.error,
            light: alpha(COLORS.error, 0.7),
            dark: alpha(COLORS.error, 0.9),
        },
        info: {
            main: COLORS.info,
            light: alpha(COLORS.info, 0.7),
            dark: alpha(COLORS.info, 0.9),
        },
        background: {
            default: '#fafafa',
            paper: '#ffffff',
        },
        text: {
            primary: COLORS.grey[800],
            secondary: COLORS.grey[600],
        },
        divider: COLORS.grey[200],
    },
    typography: {
        fontFamily: '"Inter", "Roboto", "Arial", sans-serif',
        h1: {
            fontSize: '2.5rem',
            fontWeight: 700,
            lineHeight: 1.2,
            letterSpacing: '-0.02em',
        },
        h2: {
            fontSize: '2rem',
            fontWeight: 700,
            lineHeight: 1.3,
            letterSpacing: '-0.01em',
        },
        h3: {
            fontSize: '1.75rem',
            fontWeight: 600,
            lineHeight: 1.3,
            letterSpacing: '-0.01em',
        },
        h4: {
            fontSize: '1.5rem',
            fontWeight: 600,
            lineHeight: 1.4,
        },
        h5: {
            fontSize: '1.25rem',
            fontWeight: 600,
            lineHeight: 1.4,
        },
        h6: {
            fontSize: '1.125rem',
            fontWeight: 600,
            lineHeight: 1.4,
        },
        body1: {
            fontSize: '1rem',
            lineHeight: 1.6,
        },
        body2: {
            fontSize: '0.875rem',
            lineHeight: 1.6,
        },
        button: {
            fontWeight: 600,
            textTransform: 'none' as const,
            fontSize: '0.875rem',
        },
    },
    shape: {
        borderRadius: RADIUS.sm,
    },
    components: {
        // Card bileşeni
        MuiCard: {
            styleOverrides: {
                root: {
                    boxShadow: SHADOWS.card,
                    borderRadius: RADIUS.md,
                    border: `1px solid ${COLORS.grey[200]}`,
                    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
                    '&:hover': {
                        boxShadow: SHADOWS.cardHover,
                        transform: 'translateY(-2px)',
                    },
                },
            },
        },
        // Button bileşeni
        MuiButton: {
            styleOverrides: {
                root: {
                    borderRadius: RADIUS.sm,
                    padding: '10px 20px',
                    fontSize: '0.875rem',
                    fontWeight: 600,
                    boxShadow: 'none',
                    transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
                    '&:hover': {
                        boxShadow: SHADOWS.button,
                        transform: 'translateY(-1px)',
                    },
                },
                contained: {
                    '&:hover': {
                        boxShadow: SHADOWS.button,
                    },
                },
                outlined: {
                    borderWidth: '2px',
                    '&:hover': {
                        borderWidth: '2px',
                    },
                },
            },
        },
        // Chip bileşeni
        MuiChip: {
            styleOverrides: {
                root: {
                    borderRadius: RADIUS.sm,
                    fontWeight: 500,
                },
                outlined: {
                    borderWidth: '2px',
                },
            },
        },
        // Paper bileşeni
        MuiPaper: {
            styleOverrides: {
                root: {
                    borderRadius: RADIUS.md,
                    border: `1px solid ${COLORS.grey[200]}`,
                },
                elevation1: {
                    boxShadow: SHADOWS.card,
                },
            },
        },
        // Dialog bileşeni
        MuiDialog: {
            styleOverrides: {
                paper: {
                    borderRadius: RADIUS.lg,
                    boxShadow: SHADOWS.modal,
                },
            },
        },
        // TextField bileşeni
        MuiTextField: {
            styleOverrides: {
                root: {
                    '& .MuiOutlinedInput-root': {
                        borderRadius: RADIUS.sm,
                        '&:hover .MuiOutlinedInput-notchedOutline': {
                            borderWidth: '2px',
                        },
                        '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
                            borderWidth: '2px',
                        },
                    },
                },
            },
        },
        // Table bileşeni
        MuiTableContainer: {
            styleOverrides: {
                root: {
                    borderRadius: RADIUS.md,
                    border: `1px solid ${COLORS.grey[200]}`,
                },
            },
        },
        MuiTableHead: {
            styleOverrides: {
                root: {
                    '& .MuiTableCell-head': {
                        backgroundColor: COLORS.grey[50],
                        fontWeight: 600,
                        borderBottom: `2px solid ${COLORS.grey[200]}`,
                    },
                },
            },
        },
        MuiTableRow: {
            styleOverrides: {
                root: {
                    transition: 'background-color 0.2s ease',
                    '&:hover': {
                        backgroundColor: alpha(COLORS.primary, 0.04),
                    },
                },
            },
        },
        // AppBar bileşeni
        MuiAppBar: {
            styleOverrides: {
                root: {
                    boxShadow: 'none',
                    borderBottom: `1px solid ${COLORS.grey[200]}`,
                },
            },
        },
        // Drawer bileşeni
        MuiDrawer: {
            styleOverrides: {
                paper: {
                    borderRight: `1px solid ${COLORS.grey[200]}`,
                },
            },
        },
    },
};

export const theme = createTheme(themeOptions);

// Yardımcı fonksiyonlar
export const getPageColor = (pathname: string): string => {
    switch (pathname) {
        case '/model-management':
            return COLORS.purple;
        case '/transaction-analysis':
            return COLORS.teal;
        case '/transaction-management':
            return COLORS.indigo;
        case '/rule-management':
            return COLORS.warning;
        case '/risk-management':
            return COLORS.error;
        case '/blacklist-management':
            return COLORS.error;
        default:
            return COLORS.primary;
    }
};

export const getPageGradient = (pathname: string): string => {
    switch (pathname) {
        case '/model-management':
            return GRADIENTS.purple;
        case '/transaction-analysis':
            return GRADIENTS.teal;
        case '/transaction-management':
            return GRADIENTS.indigo;
        case '/rule-management':
            return GRADIENTS.warning;
        case '/risk-management':
            return GRADIENTS.error;
        case '/blacklist-management':
            return GRADIENTS.error;
        default:
            return GRADIENTS.primary;
    }
};

export default theme; 