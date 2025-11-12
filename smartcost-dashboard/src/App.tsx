import React from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider, createTheme } from '@mui/material/styles';
import { CssBaseline, Box } from '@mui/material';
import { Dashboard } from './components/Dashboard';

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
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <Box sx={{ minHeight: '100vh', backgroundColor: 'background.default' }}>
          <Dashboard />
        </Box>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

export default App;
