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
  List,
  ListItem,
  ListItemText,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  TrendingUp as TrendingUpIcon,
  Warning as WarningIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Assessment as PowerBiIcon,
  Timeline as TimelineIcon,
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
import { DashboardMetrics } from './DashboardMetrics';
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
  const [powerBiEnabled, setPowerBiEnabled] = useState(true); // ✅ Habilitado para produção
  const queryClient = useQueryClient();

  // Fetch dashboard overview
  const {
    data: overview,
    isLoading: overviewLoading,
    error: overviewError,
    refetch: refetchOverview,
  } = useQuery({
    queryKey: ['dashboard-overview', selectedPeriod],
    queryFn: async () => {
      // Real API call to Azure Function
      const response = await api.dashboard.getOverview();
      console.log('📊 Dashboard API Full Response:', response);
      console.log('📊 Dashboard Data:', response.data);
      // API returns {success: true, data: {...}}
      // We want to access response.data which contains the actual dashboard data
      return response;
    },
    refetchInterval: 60000, // Refresh every 60 seconds (cache is 1h)
    staleTime: 30000, // Consider fresh for 30 seconds
  });

  // Fetch trend data
  const {
    data: trends,
    isLoading: trendsLoading,
    error: trendsError,
  } = useQuery({
    queryKey: ['dashboard-trends', selectedPeriod],
    queryFn: async () => {
      // Real API call to get costs
      const response = await api.costs.getCurrentCosts();
      return response.data;
    },
    refetchInterval: 60000,
    staleTime: 10000,
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

  // Log para debug
  console.log('📊 Dashboard Overview Data:', overview);

  // Transform data for charts - usar dados reais da API
  // Se tiver dados, eles vêm em overview.data
  const dashboardData = overview?.data || overview;

  // Preparar dados para os gráficos a partir da API real
  const timeSeriesData: TimeSeriesData[] = overview?.data?.DailyTrend?.map((item: any) => ({
    date: item.Date || item.date,
    value: item.Cost || item.value || 0,
  })) || [];

  const serviceChartData: ChartData[] = overview?.data?.CostByService?.map((item: any) => ({
    name: item.ServiceName || item.name,
    value: item.Cost || item.value || 0,
  })) || [];

  const resourceGroupChartData: ChartData[] = overview?.data?.TopResources?.slice(0, 10).map((item: any) => ({
    name: item.ResourceName || item.name,
    value: item.Cost || item.value || 0,
  })) || [];

  if (overviewError || trendsError) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="error" sx={{ mb: 2 }}>
          Failed to load dashboard data. Please check your API connection.
          <br />
          Error: {overviewError?.message || trendsError?.message}
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
          {overview?.data?.Summary && (
            <Chip
              label={`R$ ${overview.data.Summary.Total?.toFixed(2) || '0.00'}`}
              color="primary"
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
      {(overviewLoading || trendsLoading) && (
        <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {/* Dashboard Metrics Component - Adaptar dados da API */}
      {overview?.data && !overviewLoading && (
        <Box sx={{ mb: 3 }}>
          <Paper elevation={2} sx={{ p: 3 }}>
            <Typography variant="h5" gutterBottom>
              💰 Resumo de Custos
            </Typography>
            
            <Grid container spacing={3} sx={{ mt: 2 }}>
              {/* Total Cost */}
              <Grid item xs={12} md={3}>
                <Card sx={{ bgcolor: 'primary.light', color: 'white' }}>
                  <CardContent>
                    <Typography variant="h6">Custo Total</Typography>
                    <Typography variant="h4">
                      R$ {overview.data.Summary?.Total?.toFixed(2) || overview.data.TotalCost?.toFixed(2) || '0.00'}
                    </Typography>
                    <Typography variant="caption">
                      {overview.data.Period || 'Últimos 30 dias'} • {overview.data.TopResources?.length || overview.data.Resources?.length || 0} recursos
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>

              {/* Top Resource */}
              <Grid item xs={12} md={3}>
                <Card>
                  <CardContent>
                    <Typography variant="h6">Recurso com Maior Custo</Typography>
                    <Typography variant="h5">
                      R$ {overview.data.TopResources?.[0]?.Cost?.toFixed(2) || overview.data.Resources?.[0]?.Cost?.toFixed(2) || '0.00'}
                    </Typography>
                    <Typography variant="caption" noWrap>
                      {overview.data.TopResources?.[0]?.ResourceName || overview.data.Resources?.[0]?.ResourceName || 'N/A'}
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>

              {/* Resource Count */}
              <Grid item xs={12} md={3}>
                <Card>
                  <CardContent>
                    <Typography variant="h6">Total de Recursos</Typography>
                    <Typography variant="h4">
                      {overview.data.TopResources?.length || overview.data.Resources?.length || 0}
                    </Typography>
                    <Typography variant="caption">
                      Recursos monitorados
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>

              {/* Cache Info */}
              <Grid item xs={12} md={3}>
                <Card>
                  <CardContent>
                    <Typography variant="h6">Previsão Mensal</Typography>
                    <Typography variant="h4">
                      R$ {overview.data.Summary?.Forecast?.toFixed(2) || '0.00'}
                    </Typography>
                    <Typography variant="caption">
                      Estimativa fim do mês
                    </Typography>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>

            {/* Top Resources List */}
            <Typography variant="h6" sx={{ mt: 4, mb: 2 }}>
              📊 Top 10 Recursos por Custo
            </Typography>
            <List>
              {(overview.data.TopResources || overview.data.Resources)?.slice(0, 10).map((resource: any, index: number) => (
                <ListItem key={resource.ResourceId || index} sx={{ bgcolor: index % 2 === 0 ? 'grey.50' : 'white' }}>
                  <ListItemText
                    primary={`${index + 1}. ${resource.ResourceName}`}
                    secondary={resource.ResourceType?.split('/').pop() || 'Unknown Type'}
                  />
                  <Chip 
                    label={`R$ ${resource.Cost?.toFixed(2) || '0.00'}`} 
                    color={index < 3 ? 'error' : 'default'}
                  />
                </ListItem>
              ))}
            </List>

            {/* Recommendations */}
            {overview.data.Recommendations && overview.data.Recommendations.length > 0 && (
              <>
                <Typography variant="h6" sx={{ mt: 4, mb: 2 }}>
                  💡 Recomendações
                </Typography>
                {overview.data.Recommendations.map((rec: string, index: number) => (
                  <Alert key={index} severity="info" sx={{ mb: 1 }}>
                    {rec}
                  </Alert>
                ))}
              </>
            )}
          </Paper>
        </Box>
      )}

      {/* Key Metrics - OLD (manter como fallback) */}
      {overview?.data?.summary && !overviewLoading && (
        <Grid container spacing={3} sx={{ mb: 3, mt: 2 }}>
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
              title="Alerts Configurados"
              value={overview.data.Alerts?.length || 0}
              subtitle={`Custo: R$ ${overview.data.Summary?.Total?.toFixed(2) || '0.00'}`}
              color="secondary"
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <MetricCard
              title="Serviços Azure"
              value={overview.data.CostByService?.length || 0}
              subtitle={`${overview.data.TopResources?.length || 0} recursos`}
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
            {/* Daily Cost Trend - só mostra se tiver dados */}
            {timeSeriesData && timeSeriesData.length > 0 ? (
              <Grid item xs={12}>
                <Paper sx={{ p: 3 }}>
                  <CostAreaChart
                    title="📈 Daily Cost Trend"
                    data={timeSeriesData}
                    height={300}
                  />
                </Paper>
              </Grid>
            ) : (
              <Grid item xs={12}>
                <Alert severity="info" icon={<TimelineIcon />}>
                  <strong>Tendência Diária em Desenvolvimento</strong>
                  <br />
                  Os dados de custo diário serão disponibilizados em breve. 
                  Por enquanto, utilize os gráficos de Custo por Serviço e Recursos abaixo.
                </Alert>
              </Grid>
            )}

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
                {overview?.data && (
                  <Box>
                    <Typography variant="body1" sx={{ mb: 2 }}>
                      <strong>Período:</strong> {overview.data.Period || 'Últimos 30 dias'}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
                      Subscription: {overview.data.SubscriptionId}
                    </Typography>
                    
                    <Grid container spacing={2} sx={{ mb: 3 }}>
                      <Grid item xs={6} md={3}>
                        <Typography variant="h6" color="primary">
                          R$ {overview.data.Summary?.Total?.toFixed(2) || '0.00'}
                        </Typography>
                        <Typography variant="body2">Custo Total</Typography>
                      </Grid>
                      <Grid item xs={6} md={3}>
                        <Typography variant="h6" color="secondary">
                          R$ {overview.data.Summary?.Forecast?.toFixed(2) || '0.00'}
                        </Typography>
                        <Typography variant="body2">Previsão Mensal</Typography>
                      </Grid>
                      <Grid item xs={6} md={3}>
                        <Typography variant="h6" color={overview.data.Summary?.ChangePercent > 0 ? "error" : "success"}>
                          {overview.data.Summary?.ChangePercent?.toFixed(1) || '0.0'}%
                        </Typography>
                        <Typography variant="body2">Variação</Typography>
                      </Grid>
                      <Grid item xs={6} md={3}>
                        <Typography variant="h6" color="info">
                          {overview.data.TopResources?.length || 0}
                        </Typography>
                        <Typography variant="body2">Recursos</Typography>
                      </Grid>
                    </Grid>

                    {/* Gráfico de tendência - só mostra se tiver dados */}
                    {overview.data.DailyTrend && overview.data.DailyTrend.length > 0 ? (
                      <TrendLineChart
                        data={overview.data.DailyTrend}
                        height={400}
                      />
                    ) : (
                      <Alert severity="info" sx={{ mt: 2 }}>
                        📊 Dados de tendência diária serão disponibilizados em breve. 
                        Use a aba Overview para visualizar custos por serviço e recursos.
                      </Alert>
                    )}
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
                {overview?.data && (
                  <Box>
                    <Grid container spacing={3} sx={{ mb: 3 }}>
                      <Grid item xs={12} md={6}>
                        <Card variant="outlined">
                          <CardContent>
                            <Typography variant="h6" gutterBottom>
                              Budget Overview
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Custo Atual: R$ {overview.data.Summary?.Total?.toFixed(2) || '0.00'}
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Previsão: R$ {overview.data.Summary?.Forecast?.toFixed(2) || '0.00'}
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Variação: {overview.data.Summary?.ChangePercent?.toFixed(1) || 0}%
                            </Typography>
                          </CardContent>
                        </Card>
                      </Grid>
                      <Grid item xs={12} md={6}>
                        <Card variant="outlined">
                          <CardContent>
                            <Typography variant="h6" gutterBottom>
                              Alerts Configurados
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Total: {overview.data.Alerts?.length || 0}
                            </Typography>
                            <Typography variant="body2" gutterBottom color="success.main">
                              Status: {overview.data.Alerts?.length === 0 ? 'Nenhum alerta ativo' : `${overview.data.Alerts.length} alerta(s)`}
                            </Typography>
                          </CardContent>
                        </Card>
                      </Grid>
                    </Grid>
                    <Alert severity="success" sx={{ mb: 2 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <CheckCircleIcon />
                        <Box>
                          <Typography variant="body2" fontWeight="bold">
                            System Health: Good
                          </Typography>
                          <Typography variant="caption">
                            Última atualização: {new Date().toLocaleString('pt-BR')}
                          </Typography>
                        </Box>
                      </Box>
                    </Alert>
                    {overview.data.Alerts && overview.data.Alerts.length > 0 && (
                      <Box>
                        <Typography variant="h6" gutterBottom>Alertas Ativos</Typography>
                        {overview.data.Alerts.map((alert: any, index: number) => (
                          <Alert key={index} severity="warning" sx={{ mb: 1 }}>
                            {JSON.stringify(alert)}
                          </Alert>
                        ))}
                      </Box>
                    )}
                  </Box>
                )}
                {!overview?.data && !overviewLoading && (
                  <Alert severity="info">
                    Nenhum dado de alertas disponível no momento.
                  </Alert>
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
                  🎯 Dashboard Details
                </Typography>
                {overview?.data && (
                  <Box>
                    <Grid container spacing={3}>
                      <Grid item xs={12} md={6}>
                        <Card variant="outlined">
                          <CardContent>
                            <Typography variant="h6" gutterBottom>
                              📊 Subscription Info
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Subscription ID: {overview.data.SubscriptionId || 'N/A'}
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Período: {overview.data.Period || 'N/A'}
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Moeda: {overview.data.Summary?.Currency || 'BRL'}
                            </Typography>
                            <Typography variant="body2">
                              Total de Recursos: {overview.data.TopResources?.length || 0}
                            </Typography>
                          </CardContent>
                        </Card>
                      </Grid>
                      <Grid item xs={12} md={6}>
                        <Card variant="outlined">
                          <CardContent>
                            <Typography variant="h6" gutterBottom>
                              💰 Cost Summary
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Custo Atual: R$ {overview.data.Summary?.Total?.toFixed(2) || '0.00'}
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Previsão Mensal: R$ {overview.data.Summary?.Forecast?.toFixed(2) || '0.00'}
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Período: {overview.data.Period || 'Últimos 30 dias'}
                            </Typography>
                            <Typography variant="body2" gutterBottom>
                              Variação: {overview.data.Summary?.ChangePercent?.toFixed(1) || '0.0'}%
                            </Typography>
                            <Typography variant="body2">
                              Serviços: {overview.data.CostByService?.length || 0}
                            </Typography>
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
            <Grid item xs={12}>
              <ExecutiveDashboard
                height="500px"
                className="mb-6"
                onError={(error) => console.error('Executive dashboard error:', error)}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <DetailedCostAnalysis
                height="400px"
                showToolbar={true}
                onError={(error) => console.error('Detailed analysis error:', error)}
              />
            </Grid>
            <Grid item xs={12} md={6}>
              <CostOptimization
                height="400px"
                showToolbar={true}
                onError={(error) => console.error('Cost optimization error:', error)}
              />
            </Grid>
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

export default Dashboard;