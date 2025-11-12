import React, { useState, useEffect } from 'react';
import {
  Container,
  Grid,
  Paper,
  Typography,
  Box,
  Alert,
  CircularProgress,
  Chip,
  Card,
  CardContent,
  Tab,
  Tabs,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  TrendingUp as TrendingUpIcon,
  Warning as WarningIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Assessment as PowerBiIcon,
} from '@mui/icons-material';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import {
  CostPieChart,
  CostBarChart,
  TrendLineChart,
  CostAreaChart,
  MetricCard,
} from './Charts';
import {
  ExecutiveDashboard,
  DetailedCostAnalysis,
  CostOptimization,
  BudgetAnalysis,
} from './PowerBiReport';
import api from '../services/api';
import { DashboardOverview, TrendData, ChartData, TimeSeriesData } from '../types';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`dashboard-tabpanel-${index}`}
      aria-labelledby={`dashboard-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
    </div>
  );
}

export const Dashboard: React.FC = () => {
  const [selectedTab, setSelectedTab] = useState(0);
  const [selectedPeriod, setSelectedPeriod] = useState(30);
  const [powerBiEnabled, setPowerBiEnabled] = useState(true);
  const queryClient = useQueryClient();

  // Fetch dashboard overview
  const {
    data: overview,
    isLoading: overviewLoading,
    error: overviewError,
    refetch: refetchOverview,
  } = useQuery({
    queryKey: ['dashboard-overview', selectedPeriod],
    queryFn: () => api.dashboard.getOverview(selectedPeriod),
    refetchInterval: 30000, // Refresh every 30 seconds
    staleTime: 10000, // Consider fresh for 10 seconds
  });

  // Fetch trend data
  const {
    data: trends,
    isLoading: trendsLoading,
    error: trendsError,
  } = useQuery({
    queryKey: ['dashboard-trends', selectedPeriod],
    queryFn: () => api.dashboard.getTrends(selectedPeriod, 'daily'),
    refetchInterval: 30000,
    staleTime: 10000,
  });

  // Fetch alerts statistics
  const {
    data: alertStats,
    isLoading: alertsLoading,
    error: alertsError,
  } = useQuery({
    queryKey: ['alert-statistics'],
    queryFn: () => api.alerts.getAlertStatistics(),
    refetchInterval: 60000, // Refresh every minute
    staleTime: 30000,
  });

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setSelectedTab(newValue);
  };

  const handleRefresh = () => {
    queryClient.invalidateQueries({ queryKey: ['dashboard-overview'] });
    queryClient.invalidateQueries({ queryKey: ['dashboard-trends'] });
    queryClient.invalidateQueries({ queryKey: ['alert-statistics'] });
  };

  const getHealthStatusColor = (status: string) => {
    if (status.includes('Critical')) return 'error';
    if (status.includes('Warning')) return 'warning';
    return 'success';
  };

  const getHealthStatusIcon = (status: string) => {
    if (status.includes('Critical')) return <ErrorIcon />;
    if (status.includes('Warning')) return <WarningIcon />;
    return <CheckCircleIcon />;
  };

  // Transform data for charts
  const serviceChartData: ChartData[] = overview?.data?.byService?.map(item => ({
    name: item.serviceName,
    value: item.totalCost,
    label: `${item.serviceName}: $${item.totalCost.toLocaleString()}`,
  })) || [];

  const resourceGroupChartData: ChartData[] = overview?.data?.byResourceGroup?.map(item => ({
    name: item.resourceGroup,
    value: item.totalCost,
    label: `${item.resourceGroup}: $${item.totalCost.toLocaleString()}`,
  })) || [];

  const timeSeriesData: TimeSeriesData[] = overview?.data?.summary?.dailyCosts?.map(item => ({
    date: item.date,
    cost: item.cost,
  })) || [];

  if (overviewError || trendsError || alertsError) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load dashboard data. Please check your API connection.
          <br />
          Error: {overviewError?.message || trendsError?.message || alertsError?.message}
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      {/* Header */}
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" component="h1" fontWeight="bold">
          🏢 Azure SmartCost Dashboard
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          {overview?.data?.alertsOverview && (
            <Chip
              icon={getHealthStatusIcon(overview.data.alertsOverview.healthStatus)}
              label={overview.data.alertsOverview.healthStatus}
              color={getHealthStatusColor(overview.data.alertsOverview.healthStatus)}
              variant="outlined"
            />
          )}
          <Tooltip title="Toggle Power BI Integration">
            <IconButton 
              onClick={() => setPowerBiEnabled(!powerBiEnabled)} 
              color={powerBiEnabled ? "primary" : "default"}
            >
              <PowerBiIcon />
            </IconButton>
          </Tooltip>
          <Tooltip title="Refresh Dashboard">
            <IconButton onClick={handleRefresh} color="primary">
              <RefreshIcon />
            </IconButton>
          </Tooltip>
        </Box>
      </Box>

      {/* Loading State */}
      {(overviewLoading || trendsLoading || alertsLoading) && (
        <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {/* Key Metrics */}
      {overview?.data && !overviewLoading && (
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} sm={6} md={3}>
            <MetricCard
              title="Current Period Cost"
              value={overview.data.summary.totalPeriodCost}
              subtitle={`${selectedPeriod} days`}
              color="primary"
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <MetricCard
              title="Current Daily Cost"
              value={overview.data.summary.currentCost}
              subtitle="Today"
              color="secondary"
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <MetricCard
              title="Active Alerts"
              value={overview.data.alertsOverview.currentAlerts}
              subtitle={`${overview.data.alertsOverview.criticalAlerts} critical`}
              color={overview.data.alertsOverview.criticalAlerts > 0 ? 'error' : 'success'}
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <MetricCard
              title="Services Monitored"
              value={overview.data.dataQuality.uniqueServices}
              subtitle={`${overview.data.dataQuality.uniqueResourceGroups} resource groups`}
              color="success"
            />
          </Grid>
        </Grid>
      )}

      {/* Tabs */}
      <Paper sx={{ mb: 3 }}>
        <Tabs
          value={selectedTab}
          onChange={handleTabChange}
          aria-label="dashboard tabs"
          variant="fullWidth"
        >
          <Tab label="📊 Overview" />
          <Tab label="📈 Trends" />
          <Tab label="🚨 Alerts" />
          <Tab label="🎯 Details" />
          {powerBiEnabled && <Tab label="📊 Power BI" />}
        </Tabs>

        {/* Overview Tab */}
        <TabPanel value={selectedTab} index={0}>
          <Grid container spacing={3}>
            {/* Daily Cost Trend */}
            <Grid item xs={12}>
              <Paper sx={{ p: 3 }}>
                <CostAreaChart
                  title="📈 Daily Cost Trend"
                  data={timeSeriesData}
                  height={300}
                />
              </Paper>
            </Grid>

            {/* Cost by Service */}
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 3 }}>
                <CostPieChart
                  title="💼 Cost by Service"
                  data={serviceChartData}
                  height={350}
                />
              </Paper>
            </Grid>

            {/* Cost by Resource Group */}
            <Grid item xs={12} md={6}>
              <Paper sx={{ p: 3 }}>
                <CostBarChart
                  title="📦 Cost by Resource Group"
                  data={resourceGroupChartData}
                  height={350}
                />
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        {/* Trends Tab */}
        <TabPanel value={selectedTab} index={1}>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  📈 Cost Analysis & Trends
                </Typography>
                {trends?.data && (
                  <Box>
                    <Typography variant="body1" sx={{ mb: 2 }}>
                      <strong>Trend:</strong> {trends.data.analysis.statistics.trend}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                      Analysis for {trends.data.analysis.period.startDate} to{' '}
                      {trends.data.analysis.period.endDate}
                    </Typography>
                    
                    <Grid container spacing={2} sx={{ mb: 3 }}>
                      <Grid item xs={6} md={3}>
                        <Typography variant="h6" color="primary">
                          ${trends.data.analysis.statistics.totalCost.toLocaleString()}
                        </Typography>
                        <Typography variant="body2">Total Cost</Typography>
                      </Grid>
                      <Grid item xs={6} md={3}>
                        <Typography variant="h6" color="secondary">
                          ${trends.data.analysis.statistics.averageDailyCost.toLocaleString()}
                        </Typography>
                        <Typography variant="body2">Avg Daily</Typography>
                      </Grid>
                      <Grid item xs={6} md={3}>
                        <Typography variant="h6" color="error">
                          ${trends.data.analysis.statistics.highestDailyCost.toLocaleString()}
                        </Typography>
                        <Typography variant="body2">Peak Day</Typography>
                      </Grid>
                      <Grid item xs={6} md={3}>
                        <Typography variant="h6" color="success">
                          ${trends.data.analysis.statistics.lowestDailyCost.toLocaleString()}
                        </Typography>
                        <Typography variant="body2">Lowest Day</Typography>
                      </Grid>
                    </Grid>

                    <TrendLineChart
                      data={trends.data.trendData as any[]}
                      height={400}
                    />
                  </Box>
                )}
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        {/* Alerts Tab */}
        <TabPanel value={selectedTab} index={2}>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  🚨 Alerts & Monitoring
                </Typography>
                {alertStats?.data && (
                  <Box>
                    <Grid container spacing={3} sx={{ mb: 3 }}>
                      <Grid item xs={12} md={6}>
                        <Card variant="outlined">
                          <CardContent>
                            <Typography variant="h6" gutterBottom>
                              Threshold Configuration
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Total Thresholds: {alertStats.data.thresholdConfiguration.totalThresholds}
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Active: {alertStats.data.thresholdConfiguration.activeThresholds}
                            </Typography>
                          </CardContent>
                        </Card>
                      </Grid>
                      <Grid item xs={12} md={6}>
                        <Card variant="outlined">
                          <CardContent>
                            <Typography variant="h6" gutterBottom>
                              Recent Activity (7 days)
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Total Alerts: {alertStats.data.recentActivity.last7Days.totalAlerts}
                            </Typography>
                            <Typography variant="body2" color="error" gutterBottom>
                              Critical: {alertStats.data.recentActivity.last7Days.criticalAlerts}
                            </Typography>
                            <Typography variant="body2" color="warning" gutterBottom>
                              Warning: {alertStats.data.recentActivity.last7Days.warningAlerts}
                            </Typography>
                            <Typography variant="body2" color="info">
                              Info: {alertStats.data.recentActivity.last7Days.infoAlerts}
                            </Typography>
                          </CardContent>
                        </Card>
                      </Grid>
                    </Grid>

                    <Alert
                      severity={
                        alertStats.data.healthStatus.status.includes('Critical') ? 'error' :
                        alertStats.data.healthStatus.status.includes('Warning') ? 'warning' : 'success'
                      }
                      icon={getHealthStatusIcon(alertStats.data.healthStatus.status)}
                    >
                      <strong>System Health:</strong> {alertStats.data.healthStatus.status}
                      <br />
                      <small>Last evaluated: {alertStats.data.healthStatus.lastEvaluated}</small>
                    </Alert>
                  </Box>
                )}
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        {/* Details Tab */}
        <TabPanel value={selectedTab} index={3}>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  🎯 Data Quality & System Details
                </Typography>
                {overview?.data && (
                  <Box>
                    <Grid container spacing={3}>
                      <Grid item xs={12} md={6}>
                        <Card variant="outlined">
                          <CardContent>
                            <Typography variant="h6" gutterBottom>
                              Data Quality
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Records: {overview.data.dataQuality.recordsCount.toLocaleString()}
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Date Range: {overview.data.dataQuality.dateRange?.first} to{' '}
                              {overview.data.dataQuality.dateRange?.last}
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Services: {overview.data.dataQuality.uniqueServices}
                            </Typography>
                            <Typography variant="body2">
                              Resource Groups: {overview.data.dataQuality.uniqueResourceGroups}
                            </Typography>
                          </CardContent>
                        </Card>
                      </Grid>
                      <Grid item xs={12} md={6}>
                        <Card variant="outlined">
                          <CardContent>
                            <Typography variant="h6" gutterBottom>
                              System Status
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Last Updated: {overview.data.dataQuality.lastUpdated}
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Period: {overview.data.summary.period.startDate} to{' '}
                              {overview.data.summary.period.endDate}
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Days Analyzed: {overview.data.summary.period.daysAnalyzed}
                            </Typography>
                            <Chip
                              label={overview.data.alertsOverview.healthStatus}
                              color={getHealthStatusColor(overview.data.alertsOverview.healthStatus)}
                              size="small"
                            />
                          </CardContent>
                        </Card>
                      </Grid>
                    </Grid>
                  </Box>
                )}
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        {/* Power BI Tab */}
        <TabPanel value={selectedTab} index={4}>
          <Grid container spacing={3}>
            {/* Executive Dashboard */}
            <Grid item xs={12}>
              <ExecutiveDashboard
                height="500px"
                className="mb-6"
                onError={(error) => console.error('Executive dashboard error:', error)}
              />
            </Grid>

            {/* Detailed Analysis */}
            <Grid item xs={12} md={6}>
              <DetailedCostAnalysis
                height="400px"
                showToolbar={true}
                onError={(error) => console.error('Detailed analysis error:', error)}
              />
            </Grid>

            {/* Cost Optimization */}
            <Grid item xs={12} md={6}>
              <CostOptimization
                height="400px"
                showToolbar={true}
                onError={(error) => console.error('Cost optimization error:', error)}
              />
            </Grid>

            {/* Budget Analysis */}
            <Grid item xs={12}>
              <BudgetAnalysis
                height="400px"
                showToolbar={true}
                onError={(error) => console.error('Budget analysis error:', error)}
              />
            </Grid>
          </Grid>
        </TabPanel>
      </Paper>
    </Container>
  );
};