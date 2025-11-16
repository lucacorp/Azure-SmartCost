// Mock data for demo purposes
// This will be replaced with real data from API once users connect their Azure subscriptions

export const mockTenant = {
    id: 'tenant-demo-001',
    name: 'Contoso Corporation',
    domain: 'contoso.com',
    subscriptionTier: 'Premium',
    isActive: true,
};

export const mockSubscriptions = [
    {
        id: 'sub-prod-001',
        subscriptionId: 'aaaaaaaa-1111-2222-3333-444444444444',
        name: 'Contoso-Production',
        environment: 'Production',
        monthlyBudget: 15000,
        currentSpend: 12543.67,
        forecastedSpend: 14200,
        isActive: true,
    },
    {
        id: 'sub-dev-001',
        subscriptionId: 'bbbbbbbb-2222-3333-4444-555555555555',
        name: 'Contoso-Development',
        environment: 'Development',
        monthlyBudget: 5000,
        currentSpend: 3876.45,
        forecastedSpend: 4100,
        isActive: true,
    },
    {
        id: 'sub-staging-001',
        subscriptionId: 'cccccccc-3333-4444-5555-666666666666',
        name: 'Contoso-Staging',
        environment: 'Staging',
        monthlyBudget: 8000,
        currentSpend: 5234.12,
        forecastedSpend: 6800,
        isActive: true,
    },
];

export const mockCostData = {
    currentMonth: {
        total: 21654.24,
        breakdown: {
            Compute: 11200.00,
            Storage: 4300.00,
            Networking: 2800.00,
            Database: 2354.24,
            Other: 1000.00,
        },
        trend: '+8.5%',
        comparedToLastMonth: 1702.32,
    },
    last6Months: [
        { month: 'Jun', total: 18234.56, budget: 28000 },
        { month: 'Jul', total: 19876.43, budget: 28000 },
        { month: 'Aug', total: 18543.21, budget: 28000 },
        { month: 'Sep', total: 19321.78, budget: 28000 },
        { month: 'Oct', total: 19951.92, budget: 28000 },
        { month: 'Nov', total: 21654.24, budget: 28000 },
    ],
    topResourceGroups: [
        { name: 'rg-production-app', cost: 8543.21, percentage: 39.5 },
        { name: 'rg-database-cluster', cost: 5234.67, percentage: 24.2 },
        { name: 'rg-analytics-pipeline', cost: 3876.45, percentage: 17.9 },
        { name: 'rg-development', cost: 2100.32, percentage: 9.7 },
        { name: 'rg-staging-env', cost: 1899.59, percentage: 8.7 },
    ],
    costByService: [
        { name: 'Virtual Machines', cost: 7800.00, percentage: 36.0 },
        { name: 'Azure SQL Database', cost: 4500.00, percentage: 20.8 },
        { name: 'Storage Accounts', cost: 4300.00, percentage: 19.9 },
        { name: 'App Services', cost: 2400.00, percentage: 11.1 },
        { name: 'Load Balancer', cost: 1400.00, percentage: 6.5 },
        { name: 'Other Services', cost: 1254.24, percentage: 5.7 },
    ],
};

export const mockAlerts = [
    {
        id: 'alert-001',
        type: 'budget',
        severity: 'warning',
        title: 'Production subscription approaching budget limit',
        message: 'Current spend is 83.6% of monthly budget ($12,543 of $15,000)',
        timestamp: new Date('2024-11-14T15:30:00Z'),
        isRead: false,
        subscription: 'Contoso-Production',
    },
    {
        id: 'alert-002',
        type: 'anomaly',
        severity: 'info',
        title: 'Unusual spending pattern detected',
        message: 'Storage costs increased 45% compared to last week',
        timestamp: new Date('2024-11-13T10:15:00Z'),
        isRead: false,
        subscription: 'Contoso-Production',
    },
    {
        id: 'alert-003',
        type: 'recommendation',
        severity: 'success',
        title: 'Cost optimization opportunity',
        message: 'You can save $450/month by rightsizing 3 underutilized VMs',
        timestamp: new Date('2024-11-12T08:00:00Z'),
        isRead: true,
        subscription: 'All',
    },
];

export const mockRecommendations = [
    {
        id: 'rec-001',
        type: 'rightsize',
        title: 'Rightsize underutilized Virtual Machines',
        description: '3 VMs are running at less than 10% CPU utilization',
        potentialSavings: 450,
        impact: 'Medium',
        effort: 'Low',
        resources: ['vm-prod-web-01', 'vm-prod-web-02', 'vm-staging-app-01'],
    },
    {
        id: 'rec-002',
        type: 'reserved',
        title: 'Purchase Reserved Instances',
        description: 'Save up to 72% on consistent workloads with 3-year commitment',
        potentialSavings: 2800,
        impact: 'High',
        effort: 'Low',
        resources: ['SQL Database Standard tier', 'App Service Premium v3'],
    },
    {
        id: 'rec-003',
        type: 'orphaned',
        title: 'Delete orphaned resources',
        description: 'Found 8 unattached disks and 2 unused public IPs',
        potentialSavings: 120,
        impact: 'Low',
        effort: 'Low',
        resources: ['disk-old-backup-*', 'pip-unused-*'],
    },
    {
        id: 'rec-004',
        type: 'storage',
        title: 'Optimize storage tier',
        description: 'Move infrequently accessed data to Cool or Archive tier',
        potentialSavings: 340,
        impact: 'Medium',
        effort: 'Medium',
        resources: ['storageprod001', 'storagebackup002'],
    },
];

export const mockBudgets = [
    {
        id: 'budget-001',
        name: 'Production Monthly Budget',
        amount: 15000,
        spent: 12543.67,
        remaining: 2456.33,
        percentage: 83.6,
        period: 'Monthly',
        status: 'warning',
    },
    {
        id: 'budget-002',
        name: 'Development Monthly Budget',
        amount: 5000,
        spent: 3876.45,
        remaining: 1123.55,
        percentage: 77.5,
        period: 'Monthly',
        status: 'ok',
    },
    {
        id: 'budget-003',
        name: 'Q4 2024 Total Budget',
        amount: 75000,
        spent: 58234.56,
        remaining: 16765.44,
        percentage: 77.6,
        period: 'Quarterly',
        status: 'ok',
    },
];

export const mockDashboardMetrics = {
    totalSpend: 21654.24,
    monthlyBudget: 28000,
    budgetUtilization: 77.3,
    forecastedSpend: 25100,
    potentialSavings: 3710,
    activeSubscriptions: 3,
    activeAlerts: 2,
    recommendations: 4,
};

// Helper to simulate API delay
export const simulateApiDelay = (ms: number = 500) => 
    new Promise(resolve => setTimeout(resolve, ms));

// Mock API functions
export const mockApi = {
    getDashboardMetrics: async () => {
        await simulateApiDelay();
        return {
            totalSpend: mockDashboardMetrics.totalSpend,
            monthlyBudget: mockDashboardMetrics.monthlyBudget,
            budgetUtilization: mockDashboardMetrics.budgetUtilization,
            forecastedSpend: mockDashboardMetrics.forecastedSpend,
            potentialSavings: mockDashboardMetrics.potentialSavings,
            activeSubscriptions: mockDashboardMetrics.activeSubscriptions,
            activeAlerts: mockDashboardMetrics.activeAlerts,
            recommendations: mockDashboardMetrics.recommendations,
            data: {
                summary: {
                    totalPeriodCost: mockCostData.currentMonth.total,
                    currentCost: mockCostData.currentMonth.total,
                    period: {
                        startDate: '2024-11-01',
                        endDate: '2024-11-30',
                        daysAnalyzed: 30,
                    },
                    dailyCosts: mockCostData.last6Months.map(m => ({
                        date: m.month,
                        cost: m.total,
                    })),
                },
                byService: mockCostData.costByService.map(s => ({
                    serviceName: s.name,
                    totalCost: s.cost,
                })),
                byResourceGroup: mockCostData.topResourceGroups.map(rg => ({
                    resourceGroup: rg.name,
                    totalCost: rg.cost,
                })),
                alertsOverview: {
                    healthStatus: 'Good',
                    currentAlerts: mockAlerts.length,
                    criticalAlerts: mockAlerts.filter(a => a.severity === 'warning' || a.severity === 'error').length,
                },
                dataQuality: {
                    recordsCount: 15234,
                    dateRange: {
                        first: '2024-06-01',
                        last: '2024-11-30',
                    },
                    uniqueServices: mockCostData.costByService.length,
                    uniqueResourceGroups: mockCostData.topResourceGroups.length,
                    lastUpdated: new Date().toISOString(),
                },
            },
        };
    },
    
    getSubscriptions: async () => {
        await simulateApiDelay();
        return mockSubscriptions;
    },
    
    getCostData: async () => {
        await simulateApiDelay();
        return {
            currentMonth: mockCostData.currentMonth,
            last6Months: mockCostData.last6Months,
            topResourceGroups: mockCostData.topResourceGroups,
            costByService: mockCostData.costByService,
            data: {
                analysis: {
                    period: {
                        startDate: '2024-11-01',
                        endDate: '2024-11-30',
                        daysAnalyzed: 30,
                    },
                    statistics: {
                        trend: mockCostData.currentMonth.trend,
                        totalCost: mockCostData.currentMonth.total,
                        averageDailyCost: mockCostData.currentMonth.total / 30,
                        peakDailyCost: mockCostData.currentMonth.total / 25,
                        highestDailyCost: mockCostData.currentMonth.total / 25,
                        lowestDailyCost: mockCostData.currentMonth.total / 35,
                    },
                    insights: [
                        'Compute costs increased by 12% this month',
                        'Storage optimization can save $450/month',
                        'Database costs are within expected range',
                    ],
                },
                trendData: mockCostData.last6Months.map(m => ({
                    date: m.month,
                    cost: m.total,
                    budget: m.budget,
                })),
                predictions: {
                    nextMonthForecast: mockCostData.currentMonth.total * 1.05,
                    confidenceLevel: 85,
                    factors: ['Historical trends', 'Resource scaling patterns', 'Seasonal variations'],
                },
            },
        };
    },
    
    getAlerts: async () => {
        await simulateApiDelay();
        return mockAlerts;
    },
    
    getRecommendations: async () => {
        await simulateApiDelay();
        return mockRecommendations;
    },
    
    getBudgets: async () => {
        await simulateApiDelay();
        return mockBudgets;
    },
};
