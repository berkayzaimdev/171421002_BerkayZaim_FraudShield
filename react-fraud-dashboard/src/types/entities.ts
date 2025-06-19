import { Transaction, ModelMetrics, FraudRule } from '../services/api';

// Core Entities
export interface User {
  id: string;
  username: string;
  email: string;
  role: 'admin' | 'analyst' | 'viewer';
  firstName: string;
  lastName: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  permissions: Permission[];
}

export interface Permission {
  id: number;
  name: string;
  resource: string;
  action: string; // create, read, update, delete
}

// Fraud Detection Entities
export interface FraudCase {
  id: string;
  transactionId: string;
  status: 'pending' | 'investigating' | 'confirmed' | 'false_positive';
  severity: 'low' | 'medium' | 'high' | 'critical';
  assignedTo?: string;
  investigatorNotes?: string;
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string;
  evidence: Evidence[];
  riskScore: number;
  autoDetectedBy: string[];
}

export interface Evidence {
  id: string;
  type: 'transaction_pattern' | 'user_behavior' | 'device_fingerprint' | 'location' | 'network';
  description: string;
  severity: number;
  confidence: number;
  source: string;
  metadata: Record<string, any>;
  createdAt: string;
}

// Transaction Entities
export interface TransactionDetails extends Transaction {
  cardNumber?: string;
  cardType?: string;
  merchantId: string;
  merchantName: string;
  merchantLocation: {
    country: string;
    city: string;
    coordinates?: {
      lat: number;
      lng: number;
    };
  };
  deviceInfo: {
    deviceType: string;
    userAgent: string;
  };
  networkInfo?: NetworkInfo;
  authenticationMethod: string;
  processingTime: number;
  fees: number;
  currency: string;
  exchangeRate?: number;
}

export interface DeviceInfo {
  deviceId: string;
  deviceType: string;
  userAgent: string;
  os: string;
  browser: string;
  ipAddress: string;
  fingerprint: string;
  isNewDevice: boolean;
  location?: {
    country: string;
    city: string;
    coordinates: {
      lat: number;
      lng: number;
    };
  };
}

export interface NetworkInfo {
  ipAddress: string;
  vpnDetected: boolean;
  proxyDetected: boolean;
  torDetected: boolean;
  reputation: 'good' | 'suspicious' | 'bad';
  asn: string;
  isp: string;
  geolocation: {
    country: string;
    city: string;
    region: string;
  };
}

// Model Management Entities
export interface ModelVersion {
  id: string;
  name: string;
  version: string;
  type: 'ensemble' | 'lightgbm' | 'xgboost' | 'neural_network' | 'isolation_forest';
  status: 'training' | 'testing' | 'deployed' | 'archived';
  createdAt: string;
  trainedAt?: string;
  deployedAt?: string;
  metrics: ModelMetrics;
  hyperparameters: Record<string, any>;
  featureSet: string[];
  trainingDataInfo: {
    startDate: string;
    endDate: string;
    sampleCount: number;
    fraudCount: number;
    balanceRatio: number;
  };
  performance: PerformanceMetrics[];
  author: string;
  description?: string;
  tags: string[];
}

export interface PerformanceMetrics {
  date: string;
  accuracy: number;
  precision: number;
  recall: number;
  f1Score: number;
  auc: number;
  falsePosRate: number;
  falseNegRate: number;
  truePositives: number;
  trueNegatives: number;
  falsePositives: number;
  falseNegatives: number;
  throughput: number; // predictions per second
  latency: number; // average response time in ms
}

// Rule Management Entities
export interface FraudRuleDetails extends FraudRule {
  conditions: RuleCondition[];
  actions: RuleAction[];
  schedule?: RuleSchedule;
  version: number;
  author: string;
  lastModifiedBy: string;
  lastModifiedAt: string;
  testResults?: RuleTestResult[];
  impact: {
    totalTriggered: number;
    truePositives: number;
    falsePositives: number;
    trueNegatives: number;
    falseNegatives: number;
    precision: number;
    recall: number;
  };
}

export interface RuleCondition {
  id: string;
  field: string;
  operator: 'equals' | 'not_equals' | 'greater_than' | 'less_than' | 'contains' | 'in' | 'between';
  value: any;
  dataType: 'string' | 'number' | 'boolean' | 'date' | 'array';
}

export interface RuleAction {
  id: string;
  type: 'block' | 'flag' | 'alert' | 'require_review' | 'increase_monitoring';
  parameters: Record<string, any>;
  priority: number;
}

export interface RuleSchedule {
  enabled: boolean;
  startTime?: string;
  endTime?: string;
  daysOfWeek: number[];
  timezone: string;
}

export interface RuleTestResult {
  id: string;
  testDate: string;
  testDataset: string;
  sampleSize: number;
  triggered: number;
  accuracy: number;
  precision: number;
  recall: number;
  executionTime: number;
  notes?: string;
}

// Alert Management
export interface Alert {
  id: string;
  type: 'fraud_detected' | 'system_error' | 'performance_degradation' | 'data_quality';
  severity: 'info' | 'warning' | 'error' | 'critical';
  title: string;
  message: string;
  source: string;
  entityId?: string;
  entityType?: 'transaction' | 'user' | 'model' | 'rule';
  status: 'new' | 'acknowledged' | 'investigating' | 'resolved' | 'dismissed';
  assignedTo?: string;
  createdAt: string;
  updatedAt: string;
  resolvedAt?: string;
  metadata: Record<string, any>;
  actions: AlertAction[];
}

export interface AlertAction {
  id: string;
  type: 'email' | 'sms' | 'webhook' | 'slack' | 'teams';
  recipient: string;
  template: string;
  triggered: boolean;
  triggeredAt?: string;
  error?: string;
}

// Analytics & Reporting
export interface Report {
  id: string;
  name: string;
  type: 'fraud_summary' | 'model_performance' | 'rule_effectiveness' | 'transaction_trends';
  format: 'pdf' | 'excel' | 'csv' | 'json';
  schedule?: ReportSchedule;
  parameters: Record<string, any>;
  lastGenerated?: string;
  status: 'active' | 'paused' | 'error';
  recipients: string[];
  createdBy: string;
  createdAt: string;
}

export interface ReportSchedule {
  frequency: 'daily' | 'weekly' | 'monthly' | 'quarterly';
  time: string;
  timezone: string;
  enabled: boolean;
}

// System Configuration
export interface SystemConfig {
  id: string;
  category: 'model' | 'alert' | 'security' | 'performance' | 'integration';
  key: string;
  value: any;
  dataType: 'string' | 'number' | 'boolean' | 'object' | 'array';
  description: string;
  isEditable: boolean;
  requiredRestart: boolean;
  lastModifiedBy: string;
  lastModifiedAt: string;
}

// Audit & Logging
export interface AuditLog {
  id: string;
  userId: string;
  userName: string;
  action: string;
  resource: string;
  resourceId?: string;
  oldValue?: any;
  newValue?: any;
  ipAddress: string;
  userAgent: string;
  timestamp: string;
  success: boolean;
  errorMessage?: string;
}

// Data Quality
export interface DataQualityMetric {
  id: string;
  table: string;
  column: string;
  metric: 'completeness' | 'accuracy' | 'consistency' | 'timeliness' | 'uniqueness';
  value: number;
  threshold: number;
  status: 'pass' | 'warning' | 'fail';
  measuredAt: string;
  details?: Record<string, any>;
} 