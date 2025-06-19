import React, { createContext, useContext, useReducer, useCallback, useEffect } from 'react';
import { FraudDetectionAPI, ModelMetadata, ModelStatus, ModelType } from '../services/api';

// ==================== TYPES ====================
interface ModelState {
  models: ModelMetadata[];
  selectedModel: ModelMetadata | null;
  isLoading: boolean;
  error: string | null;
  filters: {
    search: string;
    status: string;
    type: string;
    sortBy: string;
    sortDirection: 'asc' | 'desc';
  };
  pagination: {
    page: number;
    rowsPerPage: number;
  };
}

type ModelAction =
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_MODELS'; payload: ModelMetadata[] }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'SELECT_MODEL'; payload: ModelMetadata | null }
  | { type: 'UPDATE_MODEL'; payload: { id: string; updates: Partial<ModelMetadata> } }
  | { type: 'ADD_MODEL'; payload: ModelMetadata }
  | { type: 'DELETE_MODEL'; payload: string }
  | { type: 'SET_FILTERS'; payload: Partial<ModelState['filters']> }
  | { type: 'SET_PAGINATION'; payload: Partial<ModelState['pagination']> }
  | { type: 'RESET_FILTERS' };

interface ModelContextType {
  state: ModelState;
  actions: {
    loadModels: () => Promise<void>;
    selectModel: (model: ModelMetadata | null) => void;
    updateModel: (id: string, updates: Partial<ModelMetadata>) => Promise<void>;
    addModel: (model: ModelMetadata) => void;
    deleteModel: (id: string) => Promise<void>;
    setFilters: (filters: Partial<ModelState['filters']>) => void;
    setPagination: (pagination: Partial<ModelState['pagination']>) => void;
    resetFilters: () => void;
    getFilteredModels: () => ModelMetadata[];
  };
}

// ==================== INITIAL STATE ====================
const initialState: ModelState = {
  models: [],
  selectedModel: null,
  isLoading: false,
  error: null,
  filters: {
    search: '',
    status: '',
    type: '',
    sortBy: 'date',
    sortDirection: 'desc',
  },
  pagination: {
    page: 0,
    rowsPerPage: 10,
  },
};

// ==================== REDUCER ====================
function modelReducer(state: ModelState, action: ModelAction): ModelState {
  switch (action.type) {
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };
    
    case 'SET_MODELS':
      return { ...state, models: action.payload, error: null };
    
    case 'SET_ERROR':
      return { ...state, error: action.payload, isLoading: false };
    
    case 'SELECT_MODEL':
      return { ...state, selectedModel: action.payload };
    
    case 'UPDATE_MODEL':
      return {
        ...state,
        models: state.models.map(model =>
          model.id === action.payload.id
            ? { ...model, ...action.payload.updates }
            : model
        ),
      };
    
    case 'ADD_MODEL':
      return {
        ...state,
        models: [action.payload, ...state.models],
      };
    
    case 'DELETE_MODEL':
      return {
        ...state,
        models: state.models.filter(model => model.id !== action.payload),
        selectedModel: state.selectedModel?.id === action.payload ? null : state.selectedModel,
      };
    
    case 'SET_FILTERS':
      return {
        ...state,
        filters: { ...state.filters, ...action.payload },
        pagination: { ...state.pagination, page: 0 }, // Reset page when filters change
      };
    
    case 'SET_PAGINATION':
      return {
        ...state,
        pagination: { ...state.pagination, ...action.payload },
      };
    
    case 'RESET_FILTERS':
      return {
        ...state,
        filters: initialState.filters,
        pagination: { ...state.pagination, page: 0 },
      };
    
    default:
      return state;
  }
}

// ==================== CONTEXT ====================
const ModelContext = createContext<ModelContextType | null>(null);

// ==================== PROVIDER ====================
export const ModelProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [state, dispatch] = useReducer(modelReducer, initialState);

  // ========== ACTIONS ==========
  const loadModels = useCallback(async () => {
    dispatch({ type: 'SET_LOADING', payload: true });
    try {
      console.log('🔄 Model verileri yükleniyor...');
      const models = await FraudDetectionAPI.getAllModels();
      
      if (models && models.length > 0) {
        dispatch({ type: 'SET_MODELS', payload: models });
        console.log(`✅ ${models.length} model başarıyla yüklendi`);
      } else {
        // Fallback data
        const fallbackModels: ModelMetadata[] = [
          {
            id: "model-1",
            ModelName: "Ensemble Model",
            Version: "1.0.0",
            Type: ModelType.Ensemble,
            Status: ModelStatus.Active,
            MetricsJson: JSON.stringify({
              auc: 0.96,
              recall: 0.78,
              accuracy: 0.94,
              f1_score: 0.85,
              precision: 0.90
            }),
            Metrics: {
              auc: 0.96,
              recall: 0.78,
              accuracy: 0.94,
              f1_score: 0.85,
              precision: 0.90
            },
            TrainedAt: new Date().toISOString(),
            CreatedAt: new Date().toISOString(),
            CreatedBy: "System",
            LastModifiedBy: "System",
            Configuration: JSON.stringify({ ensemble: true })
          },
        ];
        dispatch({ type: 'SET_MODELS', payload: fallbackModels });
        console.log('⚠️ Demo veriler yüklendi');
      }
    } catch (error: any) {
      console.error('❌ Model yükleme hatası:', error);
      dispatch({ type: 'SET_ERROR', payload: error.message });
    } finally {
      dispatch({ type: 'SET_LOADING', payload: false });
    }
  }, []);

  const selectModel = useCallback((model: ModelMetadata | null) => {
    dispatch({ type: 'SELECT_MODEL', payload: model });
  }, []);

  const updateModel = useCallback(async (id: string, updates: Partial<ModelMetadata>) => {
    try {
      // API call to update model
      await FraudDetectionAPI.updateModel(id, updates);
      dispatch({ type: 'UPDATE_MODEL', payload: { id, updates } });
      console.log(`✅ Model ${id} güncellendi`);
    } catch (error: any) {
      console.error(`❌ Model güncelleme hatası:`, error);
      dispatch({ type: 'SET_ERROR', payload: error.message });
    }
  }, []);

  const addModel = useCallback((model: ModelMetadata) => {
    dispatch({ type: 'ADD_MODEL', payload: model });
  }, []);

  const deleteModel = useCallback(async (id: string) => {
    try {
      await FraudDetectionAPI.deleteModel(id);
      dispatch({ type: 'DELETE_MODEL', payload: id });
      console.log(`✅ Model ${id} silindi`);
    } catch (error: any) {
      console.error(`❌ Model silme hatası:`, error);
      dispatch({ type: 'SET_ERROR', payload: error.message });
    }
  }, []);

  const setFilters = useCallback((filters: Partial<ModelState['filters']>) => {
    dispatch({ type: 'SET_FILTERS', payload: filters });
  }, []);

  const setPagination = useCallback((pagination: Partial<ModelState['pagination']>) => {
    dispatch({ type: 'SET_PAGINATION', payload: pagination });
  }, []);

  const resetFilters = useCallback(() => {
    dispatch({ type: 'RESET_FILTERS' });
  }, []);

  const getFilteredModels = useCallback(() => {
    let filtered = [...state.models];

    // Search filter
    if (state.filters.search) {
      const searchTerm = state.filters.search.toLowerCase();
      filtered = filtered.filter(model =>
        model.ModelName.toLowerCase().includes(searchTerm) ||
        model.Type.toLowerCase().includes(searchTerm)
      );
    }

    // Status filter
    if (state.filters.status) {
      filtered = filtered.filter(model => model.Status === state.filters.status);
    }

    // Type filter
    if (state.filters.type) {
      filtered = filtered.filter(model => model.Type === state.filters.type);
    }

    // Sort
    filtered.sort((a, b) => {
      let aValue, bValue;
      
      switch (state.filters.sortBy) {
        case 'name':
          aValue = a.ModelName;
          bValue = b.ModelName;
          break;
        case 'type':
          aValue = a.Type;
          bValue = b.Type;
          break;
        case 'status':
          aValue = a.Status;
          bValue = b.Status;
          break;
        case 'accuracy':
          aValue = a.Metrics?.accuracy || 0;
          bValue = b.Metrics?.accuracy || 0;
          break;
        case 'date':
        default:
          aValue = new Date(a.TrainedAt).getTime();
          bValue = new Date(b.TrainedAt).getTime();
          break;
      }

      if (state.filters.sortDirection === 'asc') {
        return aValue > bValue ? 1 : -1;
      } else {
        return aValue < bValue ? 1 : -1;
      }
    });

    return filtered;
  }, [state.models, state.filters]);

  // Auto-load models on mount
  useEffect(() => {
    loadModels();
  }, [loadModels]);

  const contextValue: ModelContextType = {
    state,
    actions: {
      loadModels,
      selectModel,
      updateModel,
      addModel,
      deleteModel,
      setFilters,
      setPagination,
      resetFilters,
      getFilteredModels,
    },
  };

  return (
    <ModelContext.Provider value={contextValue}>
      {children}
    </ModelContext.Provider>
  );
};

// ==================== HOOK ====================
export const useModels = () => {
  const context = useContext(ModelContext);
  if (!context) {
    throw new Error('useModels must be used within a ModelProvider');
  }
  return context;
}; 