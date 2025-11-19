import React from 'react';
import {
  Box,
  Grid,
  Paper,
  Typography,
  Card,
  CardContent,
  Chip,
  List,
  ListItem,
  ListItemText,
  LinearProgress,
  Alert,
} from '@mui/material';
import {
  TrendingUp,
  TrendingDown,
  TrendingFlat,
  CloudQueue,
  Storage,
  Assessment,
} from '@mui/icons-material';

interface DashboardMetricsProps {
  data: any;
}

export const DashboardMetrics: React.FC<DashboardMetricsProps> = ({ data }) => {
  if (!data) return null;

  const { Summary, TopResources, CostByService, Recommendations } = data;

  const getTrendIcon = () => {
    if (Summary.ChangePercent > 5) return <TrendingUp color="error" />;
    if (Summary.ChangePercent < -5) return <TrendingDown color="success" />;
    return <TrendingFlat color="action" />;
  };

  const getTrendColor = () => {
    if (Summary.ChangePercent > 5) return 'error';
    if (Summary.ChangePercent < -5) return 'success';
    return 'info';
  };

  return (
    <Box>
      {/* Summary Cards */}
      <Grid container spacing={3} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <Card elevation={2}>
            <CardContent>
              <Typography color="textSecondary" gutterBottom variant="body2">
                Custo Total (30 dias)
              </Typography>
              <Typography variant="h4" component="div" sx={{ mb: 1 }}>
                R$ {Summary.Total.toFixed(2)}
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                {getTrendIcon()}
                <Typography variant="body2" color={getTrendColor()}>
                  {Summary.ChangePercent > 0 ? '+' : ''}
                  {Summary.ChangePercent.toFixed(1)}% vs anterior
                </Typography>
              </Box>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card elevation={2}>
            <CardContent>
              <Typography color="textSecondary" gutterBottom variant="body2">
                Período Anterior
              </Typography>
              <Typography variant="h4" component="div" sx={{ mb: 1 }}>
                R$ {Summary.Previous.toFixed(2)}
              </Typography>
              <Typography variant="body2" color="textSecondary">
                30 dias atrás
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card elevation={2}>
            <CardContent>
              <Typography color="textSecondary" gutterBottom variant="body2">
                Forecast (próximo mês)
              </Typography>
              <Typography variant="h4" component="div" sx={{ mb: 1 }}>
                R$ {Summary.Forecast.toFixed(2)}
              </Typography>
              <Typography variant="body2" color="warning.main">
                Estimativa +10%
              </Typography>
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} sm={6} md={3}>
          <Card elevation={2}>
            <CardContent>
              <Typography color="textSecondary" gutterBottom variant="body2">
                Variação
              </Typography>
              <Typography variant="h4" component="div" sx={{ mb: 1 }}>
                R$ {Math.abs(Summary.Change).toFixed(2)}
              </Typography>
              <Chip
                label={Summary.Change > 0 ? 'Aumento' : Summary.Change < 0 ? 'Redução' : 'Estável'}
                color={Summary.Change > 0 ? 'error' : Summary.Change < 0 ? 'success' : 'default'}
                size="small"
              />
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Grid container spacing={3}>
        {/* Top Resources */}
        <Grid item xs={12} md={6}>
          <Paper elevation={2} sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <CloudQueue color="primary" />
              Top 10 Recursos
            </Typography>
            <List dense>
              {TopResources.slice(0, 10).map((resource: any, index: number) => (
                <ListItem key={index}>
                  <ListItemText
                    primary={resource.ResourceName}
                    secondary={
                      <Box>
                        <Typography variant="caption" color="textSecondary">
                          {resource.ResourceType}
                        </Typography>
                        <LinearProgress
                          variant="determinate"
                          value={(resource.Cost / TopResources[0].Cost) * 100}
                          sx={{ mt: 0.5, mb: 0.5 }}
                        />
                      </Box>
                    }
                  />
                  <Typography variant="body2" fontWeight="bold">
                    R$ {resource.Cost.toFixed(2)}
                  </Typography>
                </ListItem>
              ))}
            </List>
          </Paper>
        </Grid>

        {/* Cost By Service */}
        <Grid item xs={12} md={6}>
          <Paper elevation={2} sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Storage color="primary" />
              Breakdown por Serviço
            </Typography>
            <List dense>
              {CostByService.map((service: any, index: number) => (
                <ListItem key={index}>
                  <ListItemText
                    primary={service.Service}
                    secondary={`${service.ResourceCount} recurso(s)`}
                  />
                  <Box sx={{ textAlign: 'right' }}>
                    <Typography variant="body2" fontWeight="bold">
                      R$ {service.Cost.toFixed(2)}
                    </Typography>
                    <Typography variant="caption" color="textSecondary">
                      {service.Percentage.toFixed(1)}%
                    </Typography>
                  </Box>
                </ListItem>
              ))}
            </List>
          </Paper>
        </Grid>

        {/* Recommendations */}
        <Grid item xs={12}>
          <Paper elevation={2} sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <Assessment color="primary" />
              Recomendações
            </Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              {Recommendations.map((rec: string, index: number) => (
                <Alert key={index} severity="info" variant="outlined">
                  {rec}
                </Alert>
              ))}
            </Box>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};
