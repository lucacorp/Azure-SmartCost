import React from 'react';
import { useMsal } from '@azure/msal-react';
import { Button, Box, Typography, Paper, Avatar } from '@mui/material';
import { LoginOutlined, LogoutOutlined } from '@mui/icons-material';
import { loginRequest } from '../authConfig';

export const AuthButton: React.FC = () => {
    const { instance, accounts } = useMsal();
    const isAuthenticated = accounts.length > 0;
    const account = accounts[0];

    const handleLogin = () => {
        instance.loginPopup(loginRequest).catch((error) => {
            console.error('Login failed:', error);
        });
    };

    const handleLogout = () => {
        instance.logoutPopup().catch((error) => {
            console.error('Logout failed:', error);
        });
    };

    if (isAuthenticated) {
        return (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <Avatar 
                        sx={{ width: 32, height: 32, bgcolor: 'primary.main' }}
                    >
                        {account?.name?.charAt(0) || account?.username?.charAt(0) || '?'}
                    </Avatar>
                    <Typography variant="body2" sx={{ display: { xs: 'none', sm: 'block' } }}>
                        {account?.name || account?.username}
                    </Typography>
                </Box>
                <Button
                    variant="outlined"
                    size="small"
                    startIcon={<LogoutOutlined />}
                    onClick={handleLogout}
                >
                    Sair
                </Button>
            </Box>
        );
    }

    return (
        <Button
            variant="contained"
            color="primary"
            startIcon={<LoginOutlined />}
            onClick={handleLogin}
        >
            Entrar
        </Button>
    );
};

export const SignInPage: React.FC = () => {
    const { instance } = useMsal();
    const [error, setError] = React.useState<string | null>(null);
    const [loading, setLoading] = React.useState(false);

    const handleLogin = async () => {
        try {
            setLoading(true);
            setError(null);
            console.log('🔐 Iniciando login...');
            
            const response = await instance.loginPopup(loginRequest);
            console.log('✅ Login bem-sucedido:', response);
        } catch (err: any) {
            console.error('❌ Erro no login:', err);
            setError(err.message || 'Erro ao fazer login');
        } finally {
            setLoading(false);
        }
    };

    return (
        <Box
            sx={{
                minHeight: '100vh',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
            }}
        >
            <Paper
                elevation={3}
                sx={{
                    p: 4,
                    maxWidth: 400,
                    textAlign: 'center',
                }}
            >
                <Typography variant="h4" gutterBottom sx={{ fontWeight: 600 }}>
                    Azure SmartCost
                </Typography>
                <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                    Plataforma FinOps para otimização de custos Azure
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 4 }}>
                    Faça login com sua conta Microsoft para acessar o dashboard
                </Typography>
                <Button
                    variant="contained"
                    color="primary"
                    size="large"
                    fullWidth
                    startIcon={<LoginOutlined />}
                    onClick={handleLogin}
                    disabled={loading}
                    sx={{ py: 1.5 }}
                >
                    {loading ? 'Entrando...' : 'Entrar com Microsoft'}
                </Button>
                
                {error && (
                    <Typography 
                        variant="body2" 
                        color="error" 
                        sx={{ mt: 2, p: 1, bgcolor: 'error.light', borderRadius: 1 }}
                    >
                        {error}
                    </Typography>
                )}
                
                <Typography variant="caption" color="text.secondary" sx={{ mt: 2, display: 'block' }}>
                    v1.0.0-beta • Ambiente de demonstração
                </Typography>
            </Paper>
        </Box>
    );
};
