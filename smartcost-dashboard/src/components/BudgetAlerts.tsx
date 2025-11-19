import React, { useState } from 'react';
import {
  Container,
  Paper,
  Typography,
  Box,
  Button,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
  Chip,
  Alert,
  CircularProgress,
} from '@mui/material';
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  Notifications as NotificationsIcon,
  NotificationsActive as NotificationsActiveIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '../services/api';

interface BudgetAlert {
  id: string;
  subscriptionId: string;
  name: string;
  amount: number;
  currentSpend: number;
  threshold: number;
  email: string;
  isActive: boolean;
  createdAt: string;
}

export const BudgetAlerts: React.FC = () => {
  const [openDialog, setOpenDialog] = useState(false);
  const [formData, setFormData] = useState({
    name: '',
    amount: '',
    threshold: '80',
    email: '',
  });
  const queryClient = useQueryClient();

  // Get subscription ID (you can get this from context or env)
  const subscriptionId = process.env.REACT_APP_SUBSCRIPTION_ID || 'e6b85c41-c45d-42a5-955f-d4dfb3b13ce9';

  // Fetch alerts
  const { data: alertsData, isLoading, error } = useQuery({
    queryKey: ['budget-alerts', subscriptionId],
    queryFn: async () => {
      const response = await api.alerts.getAlerts(subscriptionId);
      return response;
    },
    refetchInterval: 30000,
  });

  // Create alert mutation
  const createMutation = useMutation({
    mutationFn: async (alertData: any) => {
      return await api.alerts.createAlert({
        subscriptionId,
        ...alertData,
        amount: parseFloat(alertData.amount),
        threshold: parseInt(alertData.threshold),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['budget-alerts'] });
      setOpenDialog(false);
      resetForm();
    },
  });

  // Delete alert mutation
  const deleteMutation = useMutation({
    mutationFn: async (alertId: string) => {
      return await api.alerts.deleteAlert(alertId);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['budget-alerts'] });
    },
  });

  const resetForm = () => {
    setFormData({
      name: '',
      amount: '',
      threshold: '80',
      email: '',
    });
  };

  const handleSubmit = () => {
    if (formData.name && formData.amount && formData.email) {
      createMutation.mutate(formData);
    }
  };

  const handleDelete = (alertId: string) => {
    if (window.confirm('Tem certeza que deseja deletar este alerta?')) {
      deleteMutation.mutate(alertId);
    }
  };

  const alerts = alertsData?.data || [];

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
        <Typography variant="h4" gutterBottom>
          Budget Alerts
        </Typography>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => setOpenDialog(true)}
        >
          Novo Alerta
        </Button>
      </Box>

      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', p: 4 }}>
          <CircularProgress />
        </Box>
      )}

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          Erro ao carregar alertas: {(error as Error).message}
        </Alert>
      )}

      {!isLoading && !error && (
        <Paper elevation={2}>
          <TableContainer>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Nome</TableCell>
                  <TableCell align="right">Orçamento</TableCell>
                  <TableCell align="right">Gasto Atual</TableCell>
                  <TableCell align="right">Limite (%)</TableCell>
                  <TableCell>Email</TableCell>
                  <TableCell align="center">Status</TableCell>
                  <TableCell align="center">Ações</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {alerts.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} align="center">
                      <Box sx={{ py: 4 }}>
                        <NotificationsIcon sx={{ fontSize: 60, color: 'text.secondary', mb: 2 }} />
                        <Typography variant="body1" color="text.secondary">
                          Nenhum alerta configurado. Crie seu primeiro alerta de orçamento!
                        </Typography>
                      </Box>
                    </TableCell>
                  </TableRow>
                ) : (
                  alerts.map((alert: BudgetAlert) => {
                    const percentage = (alert.currentSpend / alert.amount) * 100;
                    const isNearLimit = percentage >= alert.threshold;
                    
                    return (
                      <TableRow key={alert.id}>
                        <TableCell>{alert.name}</TableCell>
                        <TableCell align="right">R$ {alert.amount.toFixed(2)}</TableCell>
                        <TableCell align="right">
                          R$ {alert.currentSpend.toFixed(2)}
                          <Typography variant="caption" display="block" color="text.secondary">
                            {percentage.toFixed(1)}%
                          </Typography>
                        </TableCell>
                        <TableCell align="right">{alert.threshold}%</TableCell>
                        <TableCell>{alert.email}</TableCell>
                        <TableCell align="center">
                          {alert.isActive ? (
                            <Chip
                              icon={isNearLimit ? <NotificationsActiveIcon /> : <NotificationsIcon />}
                              label={isNearLimit ? 'Alerta!' : 'Ativo'}
                              color={isNearLimit ? 'warning' : 'success'}
                              size="small"
                            />
                          ) : (
                            <Chip label="Inativo" size="small" />
                          )}
                        </TableCell>
                        <TableCell align="center">
                          <IconButton
                            size="small"
                            color="error"
                            onClick={() => handleDelete(alert.id)}
                            disabled={deleteMutation.isPending}
                          >
                            <DeleteIcon />
                          </IconButton>
                        </TableCell>
                      </TableRow>
                    );
                  })
                )}
              </TableBody>
            </Table>
          </TableContainer>
        </Paper>
      )}

      {/* Create Alert Dialog */}
      <Dialog open={openDialog} onClose={() => setOpenDialog(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Criar Novo Alerta de Orçamento</DialogTitle>
        <DialogContent>
          <Box sx={{ pt: 2, display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              label="Nome do Alerta"
              fullWidth
              value={formData.name}
              onChange={(e) => setFormData({ ...formData, name: e.target.value })}
              placeholder="Ex: Budget Produção"
            />
            <TextField
              label="Valor do Orçamento (R$)"
              type="number"
              fullWidth
              value={formData.amount}
              onChange={(e) => setFormData({ ...formData, amount: e.target.value })}
              placeholder="Ex: 500.00"
            />
            <TextField
              label="Limite de Alerta (%)"
              type="number"
              fullWidth
              value={formData.threshold}
              onChange={(e) => setFormData({ ...formData, threshold: e.target.value })}
              helperText="Alerta será disparado quando atingir esta porcentagem"
              inputProps={{ min: 1, max: 100 }}
            />
            <TextField
              label="Email para Notificações"
              type="email"
              fullWidth
              value={formData.email}
              onChange={(e) => setFormData({ ...formData, email: e.target.value })}
              placeholder="seu@email.com"
            />
          </Box>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setOpenDialog(false)}>Cancelar</Button>
          <Button
            onClick={handleSubmit}
            variant="contained"
            disabled={createMutation.isPending || !formData.name || !formData.amount || !formData.email}
          >
            {createMutation.isPending ? 'Criando...' : 'Criar Alerta'}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};
