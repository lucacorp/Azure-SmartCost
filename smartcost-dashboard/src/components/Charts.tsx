import React from 'react';
import {
  PieChart,
  Pie,
  Cell,
  ResponsiveContainer,
  Tooltip,
  Legend,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  LineChart,
  Line,
  Area,
  AreaChart,
} from 'recharts';
import { Box, Typography, Paper, useTheme } from '@mui/material';
import { ChartData, TimeSeriesData } from '../types';

interface ChartProps {
  title?: string;
  data: any[];
  height?: number;
  showLegend?: boolean;
}

// Custom colors for consistency
const CHART_COLORS = [
  '#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8',
  '#82CA9D', '#FFC658', '#FF7C7C', '#8DD1E1', '#D084D0'
];

// Custom Tooltip Component
const CustomTooltip = ({ active, payload, label }: any) => {
  const theme = useTheme();
  
  if (active && payload && payload.length) {
    return (
      <Paper
        elevation={3}
        sx={{
          p: 2,
          backgroundColor: theme.palette.background.paper,
          border: `1px solid ${theme.palette.divider}`,
        }}
      >
        <Typography variant="body2" sx={{ fontWeight: 'bold', mb: 1 }}>
          {label}
        </Typography>
        {payload.map((item: any, index: number) => (
          <Typography
            key={index}
            variant="body2"
            sx={{ color: item.color }}
          >
            {`${item.name}: $${item.value?.toLocaleString('en-US', {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            })}`}
          </Typography>
        ))}
      </Paper>
    );
  }
  return null;
};

// Pie Chart Component
export const CostPieChart: React.FC<ChartProps> = ({
  title,
  data,
  height = 300,
  showLegend = true,
}) => {
  return (
    <Box>
      {title && (
        <Typography variant="h6" gutterBottom sx={{ textAlign: 'center' }}>
          {title}
        </Typography>
      )}
      <ResponsiveContainer width="100%" height={height}>
        <PieChart>
          <Pie
            data={data}
            cx="50%"
            cy="50%"
            outerRadius={height * 0.3}
            fill="#8884d8"
            dataKey="value"
            label={({ name, percent }: any) => `${name}: ${((percent || 0) * 100).toFixed(1)}%`}
          >
            {data.map((_, index) => (
              <Cell
                key={`cell-${index}`}
                fill={CHART_COLORS[index % CHART_COLORS.length]}
              />
            ))}
          </Pie>
          <Tooltip content={<CustomTooltip />} />
          {showLegend && <Legend />}
        </PieChart>
      </ResponsiveContainer>
    </Box>
  );
};

// Bar Chart Component
export const CostBarChart: React.FC<ChartProps> = ({
  title,
  data,
  height = 300,
}) => {
  const theme = useTheme();

  return (
    <Box>
      {title && (
        <Typography variant="h6" gutterBottom sx={{ textAlign: 'center' }}>
          {title}
        </Typography>
      )}
      <ResponsiveContainer width="100%" height={height}>
        <BarChart data={data} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" stroke={theme.palette.divider} />
          <XAxis
            dataKey="name"
            tick={{ fontSize: 12 }}
            stroke={theme.palette.text.secondary}
          />
          <YAxis
            tick={{ fontSize: 12 }}
            stroke={theme.palette.text.secondary}
            tickFormatter={(value) => `$${value.toLocaleString()}`}
          />
          <Tooltip content={<CustomTooltip />} />
          <Bar dataKey="value" fill={CHART_COLORS[0]} />
        </BarChart>
      </ResponsiveContainer>
    </Box>
  );
};

// Line Chart Component for Trends
export const TrendLineChart: React.FC<{
  title?: string;
  data: TimeSeriesData[];
  height?: number;
  showComparison?: boolean;
}> = ({ title, data, height = 300, showComparison = false }) => {
  const theme = useTheme();

  return (
    <Box>
      {title && (
        <Typography variant="h6" gutterBottom sx={{ textAlign: 'center' }}>
          {title}
        </Typography>
      )}
      <ResponsiveContainer width="100%" height={height}>
        <LineChart data={data} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
          <CartesianGrid strokeDasharray="3 3" stroke={theme.palette.divider} />
          <XAxis
            dataKey="date"
            tick={{ fontSize: 12 }}
            stroke={theme.palette.text.secondary}
          />
          <YAxis
            tick={{ fontSize: 12 }}
            stroke={theme.palette.text.secondary}
            tickFormatter={(value) => `$${value.toLocaleString()}`}
          />
          <Tooltip content={<CustomTooltip />} />
          <Line
            type="monotone"
            dataKey="cost"
            stroke={CHART_COLORS[0]}
            strokeWidth={2}
            dot={{ fill: CHART_COLORS[0] }}
            name="Current Period"
          />
          {showComparison && (
            <Line
              type="monotone"
              dataKey="previousCost"
              stroke={CHART_COLORS[1]}
              strokeWidth={2}
              strokeDasharray="5 5"
              dot={{ fill: CHART_COLORS[1] }}
              name="Previous Period"
            />
          )}
        </LineChart>
      </ResponsiveContainer>
    </Box>
  );
};

// Area Chart Component
export const CostAreaChart: React.FC<ChartProps> = ({
  title,
  data,
  height = 300,
}) => {
  const theme = useTheme();

  return (
    <Box>
      {title && (
        <Typography variant="h6" gutterBottom sx={{ textAlign: 'center' }}>
          {title}
        </Typography>
      )}
      <ResponsiveContainer width="100%" height={height}>
        <AreaChart data={data} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
          <defs>
            <linearGradient id="costGradient" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor={CHART_COLORS[0]} stopOpacity={0.8} />
              <stop offset="95%" stopColor={CHART_COLORS[0]} stopOpacity={0.1} />
            </linearGradient>
          </defs>
          <CartesianGrid strokeDasharray="3 3" stroke={theme.palette.divider} />
          <XAxis
            dataKey="date"
            tick={{ fontSize: 12 }}
            stroke={theme.palette.text.secondary}
          />
          <YAxis
            tick={{ fontSize: 12 }}
            stroke={theme.palette.text.secondary}
            tickFormatter={(value) => `$${value.toLocaleString()}`}
          />
          <Tooltip content={<CustomTooltip />} />
          <Area
            type="monotone"
            dataKey="cost"
            stroke={CHART_COLORS[0]}
            fillOpacity={1}
            fill="url(#costGradient)"
          />
        </AreaChart>
      </ResponsiveContainer>
    </Box>
  );
};

// Metric Display Component
export const MetricCard: React.FC<{
  title: string;
  value: number;
  currency?: string;
  trend?: number;
  subtitle?: string;
  color?: 'primary' | 'secondary' | 'success' | 'warning' | 'error';
}> = ({ title, value, currency = 'USD', trend, subtitle, color = 'primary' }) => {
  const theme = useTheme();
  
  const formatValue = (val: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency,
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    }).format(val);
  };

  const getTrendColor = () => {
    if (trend === undefined) return theme.palette.text.secondary;
    return trend > 0 ? theme.palette.error.main : theme.palette.success.main;
  };

  const getTrendIcon = () => {
    if (trend === undefined) return '';
    return trend > 0 ? '↗️' : '↘️';
  };

  return (
    <Paper
      elevation={2}
      sx={{
        p: 3,
        textAlign: 'center',
        backgroundColor: theme.palette.background.paper,
        border: `2px solid ${theme.palette[color].main}`,
        borderRadius: 2,
      }}
    >
      <Typography
        variant="h6"
        sx={{
          color: theme.palette[color].main,
          fontWeight: 'bold',
          mb: 1,
        }}
      >
        {title}
      </Typography>
      
      <Typography
        variant="h4"
        sx={{
          color: theme.palette.text.primary,
          fontWeight: 'bold',
          mb: 1,
        }}
      >
        {formatValue(value)}
      </Typography>

      {trend !== undefined && (
        <Typography
          variant="body2"
          sx={{
            color: getTrendColor(),
            fontWeight: 'medium',
            mb: 1,
          }}
        >
          {getTrendIcon()} {Math.abs(trend).toFixed(1)}%
        </Typography>
      )}

      {subtitle && (
        <Typography
          variant="body2"
          sx={{ color: theme.palette.text.secondary }}
        >
          {subtitle}
        </Typography>
      )}
    </Paper>
  );
};