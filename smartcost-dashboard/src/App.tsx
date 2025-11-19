import React, { useState } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { CssBaseline, Box, AppBar, Toolbar, Typography, Tabs, Tab, Container } from '@mui/material';
import { MsalProvider, useIsAuthenticated } from '@azure/msal-react';
import { PublicClientApplication } from '@azure/msal-browser';
import { msalConfig } from './authConfig';
import { Dashboard } from './components/Dashboard';
import { BudgetAlerts } from './components/BudgetAlerts';
import { SignInPage } from './components/AuthButton';

// Initialize MSAL
const msalInstance = new PublicClientApplication(msalConfig);

// Log MSAL config for debugging
console.log('🔐 MSAL Config:', {
  clientId: msalConfig.auth.clientId,
  authority: msalConfig.auth.authority,
  redirectUri: msalConfig.auth.redirectUri
});

// Create a client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 3,
      refetchOnWindowFocus: false,
      staleTime: 5 * 60 * 1000, // 5 minutes
    },
  },
});

// Create custom theme
const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#0078d4', // Azure blue
      light: '#40a2e4',
      dark: '#005a9e',
    },
    secondary: {
      main: '#00bcf2', // Light blue
      light: '#64d3ff',
      dark: '#008bb9',
    },
    success: {
      main: '#107c10', // Green
    },
    warning: {
      main: '#ff8c00', // Orange
    },
    error: {
      main: '#d13438', // Red
    },
    background: {
      default: '#f5f5f5',
      paper: '#ffffff',
    },
  },
  typography: {
    fontFamily: '"Segoe UI", "Roboto", "Helvetica Neue", Arial, sans-serif',
    h1: {
      fontWeight: 600,
    },
    h4: {
      fontWeight: 600,
    },
    h6: {
      fontWeight: 500,
    },
  },
  shape: {
    borderRadius: 8,
  },
  components: {
    MuiCard: {
      styleOverrides: {
        root: {
          boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          boxShadow: '0 2px 4px rgba(0,0,0,0.1)',
        },
      },
    },
  },
});

function App() {
  return (
    <MsalProvider instance={msalInstance}>
      <QueryClientProvider client={queryClient}>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          <MainContent />
        </ThemeProvider>
      </QueryClientProvider>
    </MsalProvider>
  );
}

function MainContent() {
  const isAuthenticated = useIsAuthenticated();
  const [currentTab, setCurrentTab] = useState(0);

  if (!isAuthenticated) {
    return <SignInPage />;
  }

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: 'background.default' }}>
      <AppBar position="static" elevation={1}>
        <Toolbar>
          <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
            SmartCost - Azure Cost Management
          </Typography>
          <Tabs 
            value={currentTab} 
            onChange={(_, newValue) => setCurrentTab(newValue)}
            textColor="inherit"
            indicatorColor="secondary"
          >
            <Tab label="Dashboard" />
            <Tab label="Budget Alerts" />
          </Tabs>
        </Toolbar>
      </AppBar>
      
      <Container maxWidth="xl" sx={{ mt: 2 }}>
        {currentTab === 0 && <Dashboard />}
        {currentTab === 1 && <BudgetAlerts />}
      </Container>
    </Box>
  );
}

export default App;
