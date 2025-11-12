import axios from 'axios';
import {
  ApiResponse,
  CostRecord,
  CostSummary,
  CostAlert,
  CostThreshold,
  DashboardOverview,
  TrendData,
  HealthStatus,
  CostFilters
} from '../types';

// Configure base API URL (adjust according to your API deployment)
const API_BASE_URL = process.env.REACT_APP_API_BASE_URL || 'https://localhost:7001/api';

// Create axios instance with default configuration
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 30000, // 30 seconds timeout
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor for logging
apiClient.interceptors.request.use((config) => {
  console.log(`🔄 API Request: ${config.method?.toUpperCase()} ${config.url}`);
  return config;
});

// Response interceptor for logging and error handling
apiClient.interceptors.response.use(
  (response) => {
    console.log(`✅ API Response: ${response.status} ${response.config.url}`);
    return response;
  },
  (error) => {
    console.error(`❌ API Error: ${error.message}`, error.response?.data);
    return Promise.reject(error);
  }
);

// Costs API
export const costsApi = {
  // Get current costs
  getCurrentCosts: async (): Promise<ApiResponse<CostRecord[]>> => {
    const response = await apiClient.get<ApiResponse<CostRecord[]>>('/costs/current');
    return response.data;
  },

  // Get cost history with filters
  getCostHistory: async (filters?: CostFilters): Promise<ApiResponse<CostRecord[]>> => {
    const params = new URLSearchParams();
    if (filters?.startDate) params.append('startDate', filters.startDate);
    if (filters?.endDate) params.append('endDate', filters.endDate);
    if (filters?.serviceType) params.append('serviceType', filters.serviceType);
    if (filters?.resourceGroup) params.append('resourceGroup', filters.resourceGroup);
    
    const response = await apiClient.get<ApiResponse<CostRecord[]>>(`/costs/history?${params.toString()}`);
    return response.data;
  },

  // Get cost summary
  getCostSummary: async (days: number = 30): Promise<ApiResponse<CostSummary>> => {
    const response = await apiClient.get<ApiResponse<CostSummary>>(`/costs/summary?days=${days}`);
    return response.data;
  },
};

// Alerts API
export const alertsApi = {
  // Get active thresholds
  getActiveThresholds: async (): Promise<ApiResponse<CostThreshold[]>> => {
    const response = await apiClient.get<ApiResponse<CostThreshold[]>>('/alerts/thresholds');
    return response.data;
  },

  // Evaluate alerts
  evaluateAlerts: async (days: number = 7): Promise<ApiResponse<CostAlert[]>> => {
    const response = await apiClient.post<ApiResponse<CostAlert[]>>(`/alerts/evaluate?days=${days}`);
    return response.data;
  },

  // Get alert statistics
  getAlertStatistics: async (): Promise<ApiResponse<any>> => {
    const response = await apiClient.get<ApiResponse<any>>('/alerts/statistics');
    return response.data;
  },
};

// Dashboard API
export const dashboardApi = {
  // Get dashboard overview
  getOverview: async (days: number = 30): Promise<ApiResponse<DashboardOverview>> => {
    const response = await apiClient.get<ApiResponse<DashboardOverview>>(`/dashboard/overview?days=${days}`);
    return response.data;
  },

  // Get trend data
  getTrends: async (days: number = 14, groupBy: string = 'daily'): Promise<ApiResponse<TrendData>> => {
    const response = await apiClient.get<ApiResponse<TrendData>>(`/dashboard/trends?days=${days}&groupBy=${groupBy}`);
    return response.data;
  },

  // Get system metrics
  getMetrics: async (): Promise<ApiResponse<any>> => {
    const response = await apiClient.get<ApiResponse<any>>('/dashboard/metrics');
    return response.data;
  },
};

// Power BI API
export const powerBiApi = {
  // Get Power BI cost data
  getCostData: async (startDate?: string, endDate?: string): Promise<ApiResponse<CostRecord[]>> => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    
    const response = await apiClient.get<ApiResponse<CostRecord[]>>(`/powerbi/cost-data?${params.toString()}`);
    return response.data;
  },

  // Get Power BI cost trends
  getCostTrends: async (startDate?: string, endDate?: string, granularity: string = 'daily'): Promise<ApiResponse<any>> => {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);
    params.append('granularity', granularity);
    
    const response = await apiClient.get<ApiResponse<any>>(`/powerbi/cost-trends?${params.toString()}`);
    return response.data;
  },

  // Get Power BI embed configuration
  getEmbedConfig: async (reportId: string, workspaceId?: string): Promise<any> => {
    const params = new URLSearchParams();
    params.append('reportId', reportId);
    if (workspaceId) params.append('workspaceId', workspaceId);
    
    const response = await apiClient.get(`/powerbi/embed-config?${params.toString()}`);
    return response.data;
  },

  // Refresh Power BI dataset
  refreshDataset: async (): Promise<ApiResponse<any>> => {
    const response = await apiClient.post<ApiResponse<any>>('/powerbi/refresh-dataset');
    return response.data;
  },

  // Get available Power BI templates
  getTemplates: async (): Promise<ApiResponse<any[]>> => {
    const response = await apiClient.get<ApiResponse<any[]>>('/powerbi/templates');
    return response.data;
  },
};

// Health API
export const healthApi = {
  // Basic health check
  getHealth: async (): Promise<any> => {
    const response = await apiClient.get('/health');
    return response.data;
  },

  // Detailed health check
  getDetailedHealth: async (): Promise<HealthStatus> => {
    const response = await apiClient.get<HealthStatus>('/health/detailed');
    return response.data;
  },

  // Readiness check
  getReadiness: async (): Promise<any> => {
    const response = await apiClient.get('/health/ready');
    return response.data;
  },

  // Liveness check
  getLiveness: async (): Promise<any> => {
    const response = await apiClient.get('/health/live');
    return response.data;
  },
};

// Utility functions
export const apiUtils = {
  // Format currency
  formatCurrency: (amount: number, currency: string = 'USD'): string => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
      minimumFractionDigits: 2,
    }).format(amount);
  },

  // Format date for API
  formatDateForApi: (date: Date): string => {
    return date.toISOString().split('T')[0];
  },

  // Parse date from API
  parseDateFromApi: (dateString: string): Date => {
    return new Date(dateString);
  },

  // Get date range for common periods
  getDateRange: (period: 'week' | 'month' | 'quarter' | '3months' | '6months' | 'year') => {
    const endDate = new Date();
    const startDate = new Date();
    
    switch (period) {
      case 'week':
        startDate.setDate(endDate.getDate() - 7);
        break;
      case 'month':
        startDate.setMonth(endDate.getMonth() - 1);
        break;
      case 'quarter':
        startDate.setMonth(endDate.getMonth() - 3);
        break;
      case '3months':
        startDate.setMonth(endDate.getMonth() - 3);
        break;
      case '6months':
        startDate.setMonth(endDate.getMonth() - 6);
        break;
      case 'year':
        startDate.setFullYear(endDate.getFullYear() - 1);
        break;
    }
    
    return {
      startDate: apiUtils.formatDateForApi(startDate),
      endDate: apiUtils.formatDateForApi(endDate),
    };
  },
};

export default {
  costs: costsApi,
  alerts: alertsApi,
  dashboard: dashboardApi,
  powerbi: powerBiApi,
  health: healthApi,
  utils: apiUtils,
};