import { useNavigate } from 'react-router-dom';

// Ortak aksiyon tipleri
export interface ActionHandler {
  type: 'navigate' | 'function' | 'external' | 'download' | 'modal';
  target?: string;
  handler?: () => void;
  data?: any;
}

// Güvenli aksiyon handler'ı oluşturucu
export const createSafeActionHandler = (action: ActionHandler) => {
  return () => {
    try {
      switch (action.type) {
        case 'navigate':
          if (action.target) {
            // Navigation logic burada implement edilecek
            window.location.href = action.target;
          }
          break;

        case 'function':
          if (typeof action.handler === 'function') {
            action.handler();
          }
          break;

        case 'external':
          if (action.target) {
            window.open(action.target, '_blank', 'noopener,noreferrer');
          }
          break;

        case 'download':
          if (action.target) {
            const link = document.createElement('a');
            link.href = action.target;
            link.download = action.data?.filename || 'download';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
          }
          break;

        case 'modal':
          console.log('Modal action:', action.data);
          // Modal logic burada implement edilecek
          break;

        default:
          console.warn('Unknown action type:', action.type);
      }
    } catch (error) {
      console.error('Action handler error:', error);
    }
  };
};

// React Navigation için hook
export const useActionHandlers = () => {
  const navigate = useNavigate();

  const createNavigationHandler = (path: string) => {
    return createSafeActionHandler({
      type: 'function',
      handler: () => navigate(path)
    });
  };

  const createFunctionHandler = (fn: () => void) => {
    return createSafeActionHandler({
      type: 'function',
      handler: fn
    });
  };

  const createExternalHandler = (url: string) => {
    return createSafeActionHandler({
      type: 'external',
      target: url
    });
  };

  const createDownloadHandler = (url: string, filename?: string) => {
    return createSafeActionHandler({
      type: 'download',
      target: url,
      data: { filename }
    });
  };

  return {
    createNavigationHandler,
    createFunctionHandler,
    createExternalHandler,
    createDownloadHandler,
  };
};

// Önceden tanımlanmış ortak handler'lar
export const commonHandlers = {
  noop: () => { },
  goBack: () => window.history.back(),
  refresh: () => window.location.reload(),
  print: () => window.print(),
  scrollToTop: () => window.scrollTo({ top: 0, behavior: 'smooth' }),
};

// Güvenli event handler wrapper
export const withSafeHandler = <T extends (...args: any[]) => any>(
  handler: T | undefined,
  fallback?: () => void
): ((...args: Parameters<T>) => void) => {
  return (...args: Parameters<T>) => {
    try {
      if (typeof handler === 'function') {
        handler(...args);
      } else if (typeof fallback === 'function') {
        fallback();
      }
    } catch (error) {
      console.error('Safe handler error:', error);
    }
  };
};

export default {
  createSafeActionHandler,
  useActionHandlers,
  commonHandlers,
  withSafeHandler,
}; 