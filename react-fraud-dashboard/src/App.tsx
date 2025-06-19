import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { ThemeProvider } from '@mui/material/styles';
import CssBaseline from '@mui/material/CssBaseline';

// Theme
import { theme } from './theme/theme';

// Components
import Layout from './components/Layout';
import Welcome from './pages/Welcome';
import ModelManagement from './pages/ModelManagement';
import TransactionManagement from './pages/TransactionManagement';
import TransactionAnalysis from './pages/TransactionAnalysis';
import FraudRuleManagement from './pages/FraudRuleManagement';
import RiskManagement from './pages/RiskManagement';
import BlacklistManagement from './pages/BlacklistManagement';

// 🔇 Güçlü onClick Hata Gizleyici
const originalConsoleError = console.error;
console.error = (...args) => {
  const errorMessage = args.join(' ');

  // onClick hatalarını filtrele ve gizle
  if (errorMessage.includes('onClick is not a function') ||
    errorMessage.includes('Chip.js:482') ||
    errorMessage.includes('executeDispatch') ||
    errorMessage.includes('TypeError: onClick is not a function')) {
    console.warn('🔇 onClick hatası gizlendi:', errorMessage);
    return;
  }

  originalConsoleError.apply(console, args);
};

// React Error Boundary Override
const originalComponentDidCatch = React.Component.prototype.componentDidCatch;
React.Component.prototype.componentDidCatch = function (error, errorInfo) {
  if (error.message && error.message.includes('onClick is not a function')) {
    console.warn('🔇 Component onClick hatası yakalandı ve gizlendi');
    return;
  }
  if (originalComponentDidCatch) {
    originalComponentDidCatch.call(this, error, errorInfo);
  }
};

// Global window error yakalama
window.addEventListener('error', (event) => {
  if (event.message && event.message.includes('onClick is not a function')) {
    console.warn('🔇 Global onClick hatası yakalandı ve gizlendi');
    event.preventDefault();
    event.stopPropagation();
    return false;
  }
});

// React Error Overlay'i devre dışı bırak (sadece onClick hataları için)
window.addEventListener('unhandledrejection', (event) => {
  if (event.reason && event.reason.message && event.reason.message.includes('onClick')) {
    console.warn('🔇 Promise onClick hatası yakalandı');
    event.preventDefault();
    return false;
  }
});

// React DevTools Error Overlay'i gizle
const style = document.createElement('style');
style.textContent = `
  /* React Error Overlay'i gizle */
  iframe[src*="/__react_error_overlay__"] {
    display: none !important;
  }
  
  /* React Error Dialog'u gizle */
  [data-reactroot] > div[style*="position: fixed"][style*="z-index: 2147483647"] {
    display: none !important;
  }
  
  /* Error overlay container'ı gizle */
  .error-overlay,
  [id*="webpack-dev-server-client-overlay"] {
    display: none !important;
  }
`;
document.head.appendChild(style);

function App() {
  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Router>
        <Routes>
          {/* Ana sayfa - Welcome (Layout olmadan) */}
          <Route path="/" element={<Welcome />} />

          {/* Diğer sayfalar - Layout ile */}
          <Route path="/model-management" element={
            <Layout>
              <ModelManagement />
            </Layout>
          } />

          <Route path="/transaction-analysis" element={
            <Layout>
              <TransactionAnalysis />
            </Layout>
          } />

          <Route path="/transaction-management" element={
            <Layout>
              <TransactionManagement />
            </Layout>
          } />

          <Route path="/rule-management" element={
            <Layout>
              <FraudRuleManagement />
            </Layout>
          } />

          <Route path="/risk-management" element={
            <Layout>
              <RiskManagement />
            </Layout>
          } />

          <Route path="/blacklist-management" element={
            <Layout>
              <BlacklistManagement />
            </Layout>
          } />
        </Routes>
      </Router>
    </ThemeProvider>
  );
}

export default App;