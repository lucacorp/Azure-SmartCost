from azure.identity import ClientSecretCredential
from azure.mgmt.costmanagement import CostManagementClient
from azure.mgmt.subscription import SubscriptionClient
from datetime import datetime, timedelta
from typing import Dict, List, Any
from config import settings


class AzureCostService:
    """Service for interacting with Azure Cost Management API."""
    
    def __init__(self):
        """Initialize Azure clients with credentials."""
        self.credential = ClientSecretCredential(
            tenant_id=settings.azure_tenant_id,
            client_id=settings.azure_client_id,
            client_secret=settings.azure_client_secret
        )
        self.cost_client = CostManagementClient(
            credential=self.credential,
            base_url="https://management.azure.com"
        )
        self.subscription_client = SubscriptionClient(self.credential)
    
    def get_subscription_info(self) -> Dict[str, Any]:
        """Get Azure subscription information."""
        try:
            subscription = self.subscription_client.subscriptions.get(
                settings.azure_subscription_id
            )
            return {
                "subscription_id": subscription.subscription_id,
                "display_name": subscription.display_name,
                "state": subscription.state
            }
        except Exception as e:
            raise Exception(f"Error fetching subscription info: {str(e)}")
    
    def get_cost_summary(self, days: int = 30) -> Dict[str, Any]:
        """
        Get cost summary for the specified number of days.
        
        Args:
            days: Number of days to query (default: 30)
            
        Returns:
            Dictionary containing cost summary data
        """
        try:
            end_date = datetime.now()
            start_date = end_date - timedelta(days=days)
            
            scope = f"/subscriptions/{settings.azure_subscription_id}"
            
            # Prepare query parameters
            query_params = {
                "type": "Usage",
                "timeframe": "Custom",
                "time_period": {
                    "from": start_date.strftime("%Y-%m-%dT00:00:00Z"),
                    "to": end_date.strftime("%Y-%m-%dT23:59:59Z")
                },
                "dataset": {
                    "granularity": "Daily",
                    "aggregation": {
                        "totalCost": {
                            "name": "PreTaxCost",
                            "function": "Sum"
                        }
                    },
                    "grouping": [
                        {
                            "type": "Dimension",
                            "name": "ServiceName"
                        }
                    ]
                }
            }
            
            # Note: This is a placeholder structure
            # The actual API call may need adjustment based on SDK version
            return {
                "scope": scope,
                "period": f"{days} days",
                "start_date": start_date.isoformat(),
                "end_date": end_date.isoformat(),
                "query_params": query_params,
                "message": "Cost query prepared successfully"
            }
            
        except Exception as e:
            raise Exception(f"Error fetching cost summary: {str(e)}")
    
    def get_cost_by_resource_group(self) -> List[Dict[str, Any]]:
        """Get costs grouped by resource group."""
        try:
            # Placeholder for resource group cost analysis
            return [{
                "resource_group": "example-rg",
                "cost": 0.0,
                "currency": "USD"
            }]
        except Exception as e:
            raise Exception(f"Error fetching costs by resource group: {str(e)}")
    
    def get_cost_forecast(self, days: int = 30) -> Dict[str, Any]:
        """
        Get cost forecast for the specified number of days.
        
        Args:
            days: Number of days to forecast (default: 30)
            
        Returns:
            Dictionary containing forecast data
        """
        try:
            # Placeholder for cost forecast
            return {
                "forecast_days": days,
                "estimated_cost": 0.0,
                "currency": "USD",
                "message": "Forecast feature to be implemented"
            }
        except Exception as e:
            raise Exception(f"Error fetching cost forecast: {str(e)}")
