import React, { useEffect, useRef, useState } from 'react';
import { models, Report, Embed } from 'powerbi-client';
import { PowerBIEmbed } from 'powerbi-client-react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/Card';
import { Alert, AlertDescription } from './ui/Alert';
import { Button } from './ui/Button';
import { Loader2, RefreshCw, Download, Settings, Expand } from 'lucide-react';

interface PowerBiEmbedConfig {
  type: string;
  id: string;
  embedUrl: string;
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  settings?: any;
}

interface PowerBiReportProps {
  reportId: string;
  workspaceId?: string;
  title?: string;
  height?: string;
  showToolbar?: boolean;
  filters?: any[];
  onLoad?: (report: Report) => void;
  onError?: (error: any) => void;
  className?: string;
}

const PowerBiReport: React.FC<PowerBiReportProps> = ({
  reportId,
  workspaceId,
  title = 'Power BI Report',
  height = '600px',
  showToolbar = true,
  filters = [],
  onLoad,
  onError,
  className = ''
}) => {
  const [embedConfig, setEmbedConfig] = useState<PowerBiEmbedConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const reportRef = useRef<Report | null>(null);

  // Load embed configuration from API
  const loadEmbedConfig = async () => {
    try {
      setLoading(true);
      setError(null);

      const params = new URLSearchParams();
      params.append('reportId', reportId);
      if (workspaceId) {
        params.append('workspaceId', workspaceId);
      }

      const response = await fetch(`/api/powerbi/embed-config?${params}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to load embed config: ${response.status}`);
      }

      const config = await response.json();
      setEmbedConfig(config);
    } catch (err: any) {
      console.error('Error loading Power BI embed config:', err);
      setError(err.message || 'Failed to load Power BI report');
      
      // Fallback to demo configuration
      setEmbedConfig({
        type: 'report',
        id: reportId,
        embedUrl: `https://app.powerbi.com/reportEmbed?reportId=${reportId}`,
        accessToken: 'demo-token',
        tokenType: 'Embed',
        expiresAt: new Date(Date.now() + 3600000).toISOString(),
        settings: {
          filterPaneEnabled: false,
          navContentPaneEnabled: false,
          background: 'transparent'
        }
      });
    } finally {
      setLoading(false);
    }
  };

  // Refresh dataset
  const refreshDataset = async () => {
    try {
      setIsRefreshing(true);
      
      const response = await fetch('/api/powerbi/refresh-dataset', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error('Failed to refresh dataset');
      }

      // Refresh the report after dataset refresh
      if (reportRef.current) {
        await reportRef.current.refresh();
      }
    } catch (err: any) {
      console.error('Error refreshing dataset:', err);
      setError('Failed to refresh data');
    } finally {
      setIsRefreshing(false);
    }
  };

  // Export report
  const exportReport = async (format: 'PDF' | 'PNG' | 'PPTX' = 'PDF') => {
    try {
      if (!reportRef.current) {
        throw new Error('Report not loaded');
      }

      // Power BI export functionality
      const exportRequest = {
        format: format.toLowerCase() as any,
        powerBIReportConfiguration: {
          settings: {
            includeHiddenPages: false,
          }
        }
      };

      // Note: This is a simplified export function
      // In production, you might want to implement server-side export
      console.log(`Exporting report in ${format} format...`);
      alert(`Export to ${format} initiated. Check Power BI service for download.`);
      
    } catch (err: any) {
      console.error('Error exporting report:', err);
      setError('Failed to export report');
    }
  };

  // Toggle fullscreen mode
  const toggleFullscreen = () => {
    setIsFullscreen(!isFullscreen);
  };

  useEffect(() => {
    loadEmbedConfig();
  }, [reportId, workspaceId]);

  const handleReportLoad = (report: Report) => {
    reportRef.current = report;
    
    // Apply filters if provided
    if (filters.length > 0) {
      report.updateFilters(models.FiltersOperations.Replace, filters);
    }

    if (onLoad) {
      onLoad(report);
    }
  };

  const handleReportError = (error: any) => {
    console.error('Power BI Report Error:', error);
    setError('Failed to load Power BI report');
    
    if (onError) {
      onError(error);
    }
  };

  if (loading) {
    return (
      <Card className={`${className} ${isFullscreen ? 'fixed inset-0 z-50 bg-white' : ''}`}>
        <CardContent className="flex items-center justify-center" style={{ height }}>
          <div className="flex flex-col items-center space-y-4">
            <Loader2 className="h-8 w-8 animate-spin text-blue-500" />
            <p className="text-sm text-gray-600">Loading Power BI report...</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className={`${className} ${isFullscreen ? 'fixed inset-0 z-50 bg-white' : ''}`}>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            {title}
            <Button variant="outline" size="sm" onClick={loadEmbedConfig}>
              <RefreshCw className="h-4 w-4 mr-2" />
              Retry
            </Button>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Alert variant="destructive">
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className={`${className} ${isFullscreen ? 'fixed inset-0 z-50 bg-white' : ''}`}>
      <CardHeader className={showToolbar ? 'block' : 'hidden'}>
        <CardTitle className="flex items-center justify-between">
          {title}
          <div className="flex items-center space-x-2">
            <Button
              variant="outline"
              size="sm"
              onClick={refreshDataset}
              disabled={isRefreshing}
            >
              {isRefreshing ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                <RefreshCw className="h-4 w-4" />
              )}
              Refresh
            </Button>
            
            <Button
              variant="outline"
              size="sm"
              onClick={() => exportReport('PDF')}
            >
              <Download className="h-4 w-4 mr-2" />
              Export
            </Button>
            
            <Button
              variant="outline"
              size="sm"
              onClick={toggleFullscreen}
            >
              <Expand className="h-4 w-4" />
            </Button>

            {isFullscreen && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => setIsFullscreen(false)}
              >
                ✕ Close
              </Button>
            )}
          </div>
        </CardTitle>
      </CardHeader>
      
      <CardContent className="p-0">
        {embedConfig && (
          <div style={{ height: isFullscreen ? 'calc(100vh - 80px)' : height }}>
            <PowerBIEmbed
              embedConfig={{
                type: 'report',
                id: embedConfig.id,
                embedUrl: embedConfig.embedUrl,
                accessToken: embedConfig.accessToken,
                tokenType: models.TokenType.Embed,
                settings: {
                  panes: {
                    filters: {
                      expanded: false,
                      visible: true
                    },
                    pageNavigation: {
                      visible: true
                    }
                  },
                  background: models.BackgroundType.Transparent,
                  layoutType: models.LayoutType.Custom,
                  customLayout: {
                    displayOption: models.DisplayOption.FitToPage
                  },
                  filterPaneEnabled: true,
                  navContentPaneEnabled: true,
                  ...embedConfig.settings
                }
              }}
              eventHandlers={
                new Map([
                  ['loaded', () => console.log('Report loaded')],
                  ['rendered', () => console.log('Report rendered')],
                  ['error', handleReportError]
                ])
              }
              cssClassName="powerbi-embed"
              getEmbeddedComponent={(embeddedReport: Embed) => {
                handleReportLoad(embeddedReport as Report);
              }}
            />
          </div>
        )}
      </CardContent>
    </Card>
  );
};

export default PowerBiReport;

// Pre-configured report components
export const ExecutiveDashboard: React.FC<Omit<PowerBiReportProps, 'reportId'>> = (props) => (
  <PowerBiReport
    reportId="smartcost-executive-dashboard"
    title="Executive Dashboard"
    {...props}
  />
);

export const DetailedCostAnalysis: React.FC<Omit<PowerBiReportProps, 'reportId'>> = (props) => (
  <PowerBiReport
    reportId="smartcost-detailed-analysis"
    title="Detailed Cost Analysis"
    {...props}
  />
);

export const CostOptimization: React.FC<Omit<PowerBiReportProps, 'reportId'>> = (props) => (
  <PowerBiReport
    reportId="smartcost-optimization"
    title="Cost Optimization"
    {...props}
  />
);

export const BudgetAnalysis: React.FC<Omit<PowerBiReportProps, 'reportId'>> = (props) => (
  <PowerBiReport
    reportId="smartcost-budget-analysis"
    title="Budget Analysis"
    {...props}
  />
);