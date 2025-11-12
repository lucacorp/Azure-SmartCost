// API Response Types
export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message: string;
  errors: string[];
  timestamp: string;
  requestId: string;
}

// Cost Data Types
export interface CostRecord {
  id: string;
  partitionKey: string;
  subscriptionId: string;
  date: string;
  totalCost: number;
  currency: string;
  resourceGroup: string;
  serviceName: string;
  createdAt: string;
  timestamp: number;
}

export interface CostSummary {
  overview: {
    totalCost: number;
    averageDailyCost: number;
    daysAnalyzed: number;
    period: {
      startDate: string;
      endDate: string;
    };
  };
  byService: Array<{
    service: string;
    totalCost: number;
    percentage: number;
  }>;
  byResourceGroup: Array<{
    resourceGroup: string;
    totalCost: number;
    percentage: number;
  }>;
  costByDay: Array<{
    date: string;
    totalCost: number;
  }>;
}

// Alert Types
export interface CostAlert {
  id: string;
  type: AlertType;
  level: AlertLevel;
  message: string;
  createdAt: string;
  thresholdId?: string;
  actualValue: number;
  thresholdValue: number;
  resourceGroup?: string;
  serviceName?: string;
}

export interface CostThreshold {
  id: string;
  name: string;
  alertType: AlertType;
  alertLevel: AlertLevel;
  thresholdValue: number;
  comparisonOperator: ComparisonOperator;
  isEnabled: boolean;
  createdAt: string;
  resourceGroup?: string;
  serviceName?: string;
  description?: string;
}

export enum AlertType {
  DailyCost = 0,
  MonthlyCost = 1,
  WeeklyTrend = 2,
  Anomaly = 3
}

export enum AlertLevel {
  Info = 0,
  Warning = 1,
  Critical = 2
}

export enum ComparisonOperator {
  GreaterThan = 0,
  LessThan = 1,
  EqualTo = 2
}

// Dashboard Types
export interface DashboardOverview {
  summary: {
    currentCost: number;
    totalPeriodCost: number;
    dailyCosts: Array<{
      date: string;
      cost: number;
    }>;
    trend: string;
    period: {
      startDate: string;
      endDate: string;
      daysAnalyzed: number;
    };
  };
  byService: Array<{
    serviceName: string;
    totalCost: number;
    percentage: number;
  }>;
  byResourceGroup: Array<{
    resourceGroup: string;
    totalCost: number;
    percentage: number;
  }>;
  alertsOverview: {
    activeThresholds: number;
    currentAlerts: number;
    criticalAlerts: number;
    warningAlerts: number;
    infoAlerts: number;
    healthStatus: string;
  };
  dataQuality: {
    recordsCount: number;
    dateRange: {
      first: string;
      last: string;
    } | null;
    uniqueServices: number;
    uniqueResourceGroups: number;
    lastUpdated: string;
  };
}

export interface TrendData {
  trendData: Array<{
    date?: string;
    week?: string;
    month?: string;
    cost: number;
    recordCount?: number;
    topService?: string;
  }>;
  analysis: {
    period: {
      startDate: string;
      endDate: string;
      groupBy: string;
    };
    statistics: {
      totalCost: number;
      averageDailyCost: number;
      highestDailyCost: number;
      lowestDailyCost: number;
      trend: string;
    };
  };
}

// Chart Data Types
export interface ChartData {
  name: string;
  value: number;
  label?: string;
  color?: string;
}

export interface TimeSeriesData {
  date: string;
  cost: number;
  previousCost?: number;
}

// Filter Types
export interface CostFilters {
  startDate?: string;
  endDate?: string;
  serviceType?: string;
  resourceGroup?: string;
}

// Health Check Types
export interface HealthStatus {
  overallStatus: string;
  timestamp: string;
  version: string;
  operationId: string;
  checks: Array<{
    service: string;
    status: string;
    details: string | object;
  }>;
  summary: {
    totalChecks: number;
    healthyChecks: number;
    responseTime: string;
  };
}