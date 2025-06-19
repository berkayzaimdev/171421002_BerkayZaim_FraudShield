// API Base URL
const API_BASE_URL = 'http://localhost:5000/api';

// Request/Response tipleri
export interface TransactionRequest {
  userId: string;
  amount: number;
  merchantId: string;
  type: 'Transfer' | 'Payment' | 'Withdrawal' | 'Deposit';
  location: {
    country: string;
    city: string;
    latitude?: number;
    longitude?: number;
  };
  deviceInfo: {
    deviceId: string;
    deviceType: string;
    ipAddress: string;
    userAgent: string;
  };
  additionalDataRequest: {
    cardType: string;
    cardBin: string;
    cardLast4: string;
    bankName: string;
    bankCountry: string;
    vFactors: Record<string, number>;
  };
}

export interface TransactionCreateResponse {
  transactionId: string;
  analysisId: string;
  fraudProbability: number;
  riskScore: string;
  decision: string;
  anomalyScore: number;
  isSuccessful: boolean;
  riskFactorCount: number;
  analyzedAt: string;
  message: string;
}

export interface TransactionListRequest {
  page: number;
  pageSize: number;
  userId?: string;
  startDate?: string;
  endDate?: string;
}

export interface TransactionListResponse {
  transactions: TransactionListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface TransactionListItem {
  transactionId: string;
  userId: string;
  amount: number;
  merchantId: string;
  type: string;
  createdAt: string;
  analysisResult?: {
    analysisId: string;
    fraudProbability: number;
    riskScore: string;
    decision: string;
    analyzedAt: string;
    riskFactorCount: number;
  };
}

export interface TransactionDetailResponse {
  transaction: {
    transactionId: string;
    userId: string;
    amount: number;
    merchantId: string;
    type: string;
    createdAt: string;
    location: {
      country: string;
      city: string;
      latitude?: number;
      longitude?: number;
    };
    deviceInfo: {
      deviceId: string;
      deviceType: string;
      ipAddress: string;
      userAgent: string;
    };
  };
  analysisResult: {
    analysisId: string;
    transactionId: string;
    fraudProbability: number;
    anomalyScore: number;
    riskScore: string;
    decision: string;
    status: string;
    analyzedAt: string;
    error?: string;
    totalRuleCount: number;
    triggeredRuleCount: number;
    appliedActions: string[];
  };
  riskFactors: Array<{
    riskFactorId: string;
    type: string;
    description: string;
    severity: string;
    confidence: number;
    source?: string;
    createdAt: string;
    ruleId?: string;
    actionTaken?: string;
  }>;
  riskEvaluations: Array<{
    riskEvaluationId: string;
    fraudProbability: number;
    anomalyScore: number;
    riskLevel: string;
    modelVersion?: string;
    confidence: number;
    evaluatedAt: string;
    processingTimeMs?: number;
    modelInfo?: Record<string, any>;
  }>;
}

// Context analiz request/response tipleri
export interface FraudDetectionResponse {
  riskScore: number;
  riskLevel: string;
  decision: string;
  actions: string[];
  triggeredRules: Array<{
    ruleId: string;
    ruleName: string;
    ruleCode: string;
    isTriggered: boolean;
    confidence: number;
    actions: string[];
  }>;
  resultMessage: string;
}

export interface TransactionCheckRequest {
  transactionId: string;
  userId: string;
  amount: number;
  currency: string;
  transactionType: string;
  merchantCategory?: string;
  recipientAccountNumber: string;
  recipientCountry: string;
  isRecurring: boolean;
  deviceId?: string;
  ipAddress?: string;
  additionalData: Record<string, any>;
}

export interface AccountAccessCheckRequest {
  userId: string;
  username: string;
  ipAddress: string;
  countryCode: string;
  city: string;
  deviceId: string;
  isNewDevice: boolean;
  lastSuccessfulLogin?: string;
  failedLoginCount: number;
  typicalAccessHours: number[];
  typicalAccessDays: string[];
  typicalCountries: string[];
  additionalData: Record<string, any>;
}

export interface IpCheckRequest {
  ipAddress: string;
  countryCode: string;
  city: string;
  ispAsn: string;
  isVpn: boolean;
  isTor: boolean;
  isBlacklisted: boolean;
  blacklistNotes: string;
  networkType: string;
  additionalData: Record<string, any>;
}

export interface DeviceCheckRequest {
  deviceId: string;
  deviceType: string;
  operatingSystem: string;
  browser: string;
  ipAddress: string;
  countryCode: string;
  isNewDevice: boolean;
  lastSeenDate?: string;
  associatedUserCount: number;
  additionalData: Record<string, any>;
}

export interface SessionCheckRequest {
  sessionId: string;
  userId: string;
  ipAddress: string;
  deviceId: string;
  userAgent: string;
  sessionDurationMinutes: number;
  pageViewCount: number;
  additionalData: Record<string, any>;
}

// API Service Class
export class FraudDetectionAPI {
  // 1. ADIM: İşlem Oluşturma + Fraud Analizi
  static async createTransaction(request: TransactionRequest): Promise<TransactionCreateResponse> {
    const response = await fetch(`${API_BASE_URL}/transaction/create`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.text();
      throw new Error(`Transaction oluşturma hatası: ${error}`);
    }

    return response.json();
  }

  // 2. ADIM: Transaction Listeleme
  static async getTransactionList(request: TransactionListRequest): Promise<TransactionListResponse> {
    const params = new URLSearchParams({
      page: request.page.toString(),
      pageSize: request.pageSize.toString(),
    });

    if (request.userId) params.append('userId', request.userId);
    if (request.startDate) params.append('startDate', request.startDate);
    if (request.endDate) params.append('endDate', request.endDate);

    const response = await fetch(`${API_BASE_URL}/transaction/list?${params}`);

    if (!response.ok) {
      throw new Error('Transaction listesi alınamadı');
    }

    return response.json();
  }

  // 3. ADIM: Transaction Detayı
  static async getTransactionDetail(transactionId: string): Promise<TransactionDetailResponse> {
    const response = await fetch(`${API_BASE_URL}/transaction/${transactionId}/detail`);

    if (!response.ok) {
      throw new Error('Transaction detayları alınamadı');
    }

    return response.json();
  }

  // 4. ADIM: Context Analizleri

  // Transaction Context
  static async analyzeTransactionContext(transactionId: string, request: TransactionCheckRequest): Promise<FraudDetectionResponse> {
    const response = await fetch(`${API_BASE_URL}/transaction/${transactionId}/analyze-context/transaction`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Transaction context analizi başarısız');
    }

    return response.json();
  }

  // Account Context
  static async analyzeAccountContext(transactionId: string, request: AccountAccessCheckRequest): Promise<FraudDetectionResponse> {
    const response = await fetch(`${API_BASE_URL}/transaction/${transactionId}/analyze-context/account`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Account context analizi başarısız');
    }

    return response.json();
  }

  // IP Context
  static async analyzeIpContext(transactionId: string, request: IpCheckRequest): Promise<FraudDetectionResponse> {
    const response = await fetch(`${API_BASE_URL}/transaction/${transactionId}/analyze-context/ip`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('IP context analizi başarısız');
    }

    return response.json();
  }

  // Device Context
  static async analyzeDeviceContext(transactionId: string, request: DeviceCheckRequest): Promise<FraudDetectionResponse> {
    const response = await fetch(`${API_BASE_URL}/transaction/${transactionId}/analyze-context/device`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Device context analizi başarısız');
    }

    return response.json();
  }

  // Session Context
  static async analyzeSessionContext(transactionId: string, request: SessionCheckRequest): Promise<FraudDetectionResponse> {
    const response = await fetch(`${API_BASE_URL}/transaction/${transactionId}/analyze-context/session`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      throw new Error('Session context analizi başarısız');
    }

    return response.json();
  }
} 