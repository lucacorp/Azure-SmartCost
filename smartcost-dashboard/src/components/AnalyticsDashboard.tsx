import React, { useState } from 'react';
import {
  Container,
  Grid,
  Paper,
  Typography,
  Box,
  Alert,
  CircularProgress,
  Card,
  CardContent,
  Tab,
  Tabs,
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import {
  CostPieChart,
  CostBarChart,
  CostAreaChart,
  MetricCard,
} from './Charts';
import api from '../services/api';

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
      id={`analytics-tabpanel-${index}`}
      aria-labelledby={`analytics-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
    </div>
  );
}

export const AnalyticsDashboard: React.FC = () => {
  const [selectedTab, setSelectedTab] = useState(0);
  
  // Get subscription ID from environment or use default
  const subscriptionId = process.env.REACT_APP_SUBSCRIPTION_ID || 'default-subscription-id';

  // Fetch cost analytics
  const {
    data: costAnalytics,
    isLoading: analyticsLoading,
    error: analyticsError,
  } = useQuery({
    queryKey: ['cost-analytics', subscriptionId],
    queryFn: () => api.analytics.getCostAnalytics(subscriptionId),
    refetchInterval: 60000,
  });

  // Fetch service breakdown
  const {
    data: serviceBreakdown,
    isLoading: servicesLoading,
  } = useQuery({
    queryKey: ['service-breakdown', subscriptionId],
    queryFn: () => api.analytics.getServiceBreakdown(subscriptionId),
    refetchInterval: 60000,
  });

  // Fetch daily trend
  const {
    data: dailyTrend,
    isLoading: trendLoading,
  } = useQuery({
    queryKey: ['daily-trend', subscriptionId],
    queryFn: () => api.analytics.getDailyCostTrend(subscriptionId),
    refetchInterval: 60000,
  });

  // Fetch top resources
  const {
    data: topResources,
    isLoading: resourcesLoading,
  } = useQuery({
    queryKey: ['top-resources', subscriptionId],
    queryFn: () => api.analytics.getTopCostResources(subscriptionId),
    refetchInterval: 60000,
  });

  const handleTabChange = (event: React.SyntheticEvent, newValue: number) => {
    setSelectedTab(newValue);
  };

  if (analyticsError) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
        <Alert severity="error">
          Falha ao carregar dados de analytics. Verifique a conexão com a API.
          <br />
          Error: {analyticsError.message}
        </Alert>
      </Container>
    );
  }

  const isLoading = analyticsLoading || servicesLoading || trendLoading || resourcesLoading;

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      {/* Header */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="h4" component="h1" fontWeight="bold">
          📊 Analytics Dashboard
        </Typography>
        <Typography variant="body2" color="text.secondary">
          Análise completa de custos Azure - Solução nativa sem Power BI
        </Typography>
      </Box>

      {/* Loading State */}
      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', my: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {/* Key Metrics */}
      {costAnalytics && !isLoading && (
        <Grid container spacing={3} sx={{ mb: 3 }}>
          <Grid item xs={12} sm={6} md={3}>
            <MetricCard
              title="Custo Total"
              value={costAnalytics.totalCost}
              subtitle={`${costAnalytics.currency}`}
              color="primary"
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <MetricCard
              title="Média Diária"
              value={costAnalytics.dailyAverage}
              subtitle={`${costAnalytics.currency}/dia`}
              color="secondary"
            />
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography variant="h6">Tendência</Typography>
                <Typography variant="h4" color={costAnalytics.trendPercentage > 0 ? 'error' : 'success'}>
                  {costAnalytics.trendPercentage > 0 ? '+' : ''}{costAnalytics.trendPercentage.toFixed(1)}%
                </Typography>
                <Typography variant="caption">
                  {costAnalytics.trendPercentage > 0 ? '📈 Aumento' : '📉 Redução'}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} sm={6} md={3}>
            <Card>
              <CardContent>
                <Typography variant="h6">Top Serviço</Typography>
                <Typography variant="body1" noWrap>
                  {costAnalytics.topService}
                </Typography>
                <Typography variant="caption">
                  {costAnalytics.recordCount} registros
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {/* Tabs */}
      <Paper sx={{ mb: 3 }}>
        <Tabs
          value={selectedTab}
          onChange={handleTabChange}
          aria-label="analytics tabs"
          variant="fullWidth"
        >
          <Tab label="📊 Visão Geral" />
          <Tab label="💼 Por Serviço" />
          <Tab label="📈 Tendência" />
          <Tab label="🏆 Top Recursos" />
        </Tabs>

        {/* Visão Geral Tab */}
        <TabPanel value={selectedTab} index={0}>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  📊 Resumo de Custos
                </Typography>
                {costAnalytics && (
                  <Box>
                    <Typography variant="body1" sx={{ mb: 2 }}>
                      <strong>Período:</strong> {new Date(costAnalytics.startDate).toLocaleDateString()} - {new Date(costAnalytics.endDate).toLocaleDateString()}
                    </Typography>
                    <Grid container spacing={2}>
                      <Grid item xs={6} md={3}>
                        <Typography variant="h6" color="primary">
                          {costAnalytics.currency} {costAnalytics.totalCost.toFixed(2)}
                        </Typography>
                        <Typography variant="body2">Custo Total</Typography>
                      </Grid>
                      <Grid item xs={6} md={3}>
                        <Typography variant="h6" color="secondary">
                          {costAnalytics.currency} {costAnalytics.dailyAverage.toFixed(2)}
                        </Typography>
                        <Typography variant="body2">Média Diária</Typography>
                      </Grid>
                      <Grid item xs={6} md={3}>
                        <Typography variant="h6" color={costAnalytics.trendPercentage > 0 ? "error" : "success"}>
                          {costAnalytics.trendPercentage > 0 ? '+' : ''}{costAnalytics.trendPercentage.toFixed(1)}%
                        </Typography>
                        <Typography variant="body2">Variação</Typography>
                      </Grid>
                      <Grid item xs={6} md={3}>
                        <Typography variant="h6" color="info">
                          {costAnalytics.recordCount}
                        </Typography>
                        <Typography variant="body2">Registros</Typography>
                      </Grid>
                    </Grid>
                  </Box>
                )}
              </Paper>
            </Grid>

            {/* Charts Grid */}
            {dailyTrend && dailyTrend.length > 0 && (
              <Grid item xs={12}>
                <Paper sx={{ p: 3 }}>
                  <CostAreaChart
                    title="📈 Tendência de Custos (Últimos 30 dias)"
                    data={dailyTrend.map((d: any) => ({
                      date: new Date(d.date).toLocaleDateString(),
                      value: d.totalCost,
                    }))}
                    height={300}
                  />
                </Paper>
              </Grid>
            )}

            {serviceBreakdown && serviceBreakdown.length > 0 && (
              <Grid item xs={12} md={6}>
                <Paper sx={{ p: 3 }}>
                  <CostPieChart
                    title="💼 Custo por Serviço"
                    data={serviceBreakdown.map((s: any) => ({
                      name: s.serviceName,
                      value: s.totalCost,
                    }))}
                    height={350}
                  />
                </Paper>
              </Grid>
            )}

            {topResources && topResources.length > 0 && (
              <Grid item xs={12} md={6}>
                <Paper sx={{ p: 3 }}>
                  <CostBarChart
                    title="🏆 Top 10 Recursos"
                    data={topResources.map((r: any) => ({
                      name: r.resourceName,
                      value: r.totalCost,
                    }))}
                    height={350}
                  />
                </Paper>
              </Grid>
            )}
          </Grid>
        </TabPanel>

        {/* Por Serviço Tab */}
        <TabPanel value={selectedTab} index={1}>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  💼 Breakdown por Serviço Azure
                </Typography>
                {serviceBreakdown && serviceBreakdown.length > 0 ? (
                  <Box>
                    {serviceBreakdown.map((service: any, index: number) => (
                      <Box key={index} sx={{ mb: 2, p: 2, bgcolor: 'grey.50', borderRadius: 1 }}>
                        <Grid container spacing={2}>
                          <Grid item xs={12} md={4}>
                            <Typography variant="subtitle1" fontWeight="bold">
                              {service.serviceName}
                            </Typography>
                          </Grid>
                          <Grid item xs={6} md={2}>
                            <Typography variant="body2" color="text.secondary">
                              Custo Total
                            </Typography>
                            <Typography variant="h6" color="primary">
                              {service.currency} {service.totalCost.toFixed(2)}
                            </Typography>
                          </Grid>
                          <Grid item xs={6} md={2}>
                            <Typography variant="body2" color="text.secondary">
                              Recursos
                            </Typography>
                            <Typography variant="h6">
                              {service.resourceCount}
                            </Typography>
                          </Grid>
                          <Grid item xs={12} md={4}>
                            <Typography variant="body2" color="text.secondary">
                              Média Diária
                            </Typography>
                            <Typography variant="h6" color="secondary">
                              {service.currency} {service.averageDailyCost.toFixed(2)}
                            </Typography>
                          </Grid>
                        </Grid>
                      </Box>
                    ))}
                  </Box>
                ) : (
                  <Alert severity="info">
                    Nenhum dado de serviços disponível no momento.
                  </Alert>
                )}
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        {/* Tendência Tab */}
        <TabPanel value={selectedTab} index={2}>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  📈 Tendência Diária de Custos
                </Typography>
                {dailyTrend && dailyTrend.length > 0 ? (
                  <CostAreaChart
                    title=""
                    data={dailyTrend.map((d: any) => ({
                      date: new Date(d.date).toLocaleDateString(),
                      value: d.totalCost,
                    }))}
                    height={400}
                  />
                ) : (
                  <Alert severity="info">
                    Nenhum dado de tendência disponível no momento.
                  </Alert>
                )}
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>

        {/* Top Recursos Tab */}
        <TabPanel value={selectedTab} index={3}>
          <Grid container spacing={3}>
            <Grid item xs={12}>
              <Paper sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                  🏆 Top 10 Recursos por Custo
                </Typography>
                {topResources && topResources.length > 0 ? (
                  <Box>
                    {topResources.map((resource: any, index: number) => (
                      <Box 
                        key={index} 
                        sx={{ 
                          mb: 2, 
                          p: 2, 
                          bgcolor: index < 3 ? 'error.light' : 'grey.50', 
                          borderRadius: 1,
                          color: index < 3 ? 'white' : 'inherit'
                        }}
                      >
                        <Grid container spacing={2} alignItems="center">
                          <Grid item xs={1}>
                            <Typography variant="h6" fontWeight="bold">
                              #{index + 1}
                            </Typography>
                          </Grid>
                          <Grid item xs={12} md={5}>
                            <Typography variant="subtitle1" fontWeight="bold">
                              {resource.resourceName}
                            </Typography>
                            <Typography variant="caption">
                              {resource.serviceName}
                            </Typography>
                          </Grid>
                          <Grid item xs={6} md={3}>
                            <Typography variant="body2" color={index < 3 ? 'inherit' : 'text.secondary'}>
                              Custo Total
                            </Typography>
                            <Typography variant="h6" color={index < 3 ? 'inherit' : 'primary'}>
                              {resource.currency} {resource.totalCost.toFixed(2)}
                            </Typography>
                          </Grid>
                          <Grid item xs={6} md={3}>
                            <Typography variant="caption" noWrap>
                              {resource.resourceId.split('/').slice(-2).join('/')}
                            </Typography>
                          </Grid>
                        </Grid>
                      </Box>
                    ))}
                  </Box>
                ) : (
                  <Alert severity="info">
                    Nenhum dado de recursos disponível no momento.
                  </Alert>
                )}
              </Paper>
            </Grid>
          </Grid>
        </TabPanel>
      </Paper>
    </Container>
  );
};

export default AnalyticsDashboard;
