#!/bin/bash

# Azure SmartCost - Power BI Environment Configuration Script
# This script sets up all required environment variables for Power BI integration

set -e

echo "🚀 Azure SmartCost - Power BI Configuration Setup"
echo "================================================="

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "❌ Azure CLI is not installed. Please install it first."
    echo "   Visit: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
    exit 1
fi

# Function to prompt for input with validation
prompt_input() {
    local var_name=$1
    local description=$2
    local required=${3:-true}
    
    while true; do
        read -p "📝 Enter $description: " value
        
        if [[ $required == true && -z "$value" ]]; then
            echo "❌ This field is required. Please try again."
        else
            echo "✅ $var_name set"
            break
        fi
    done
    
    echo "$value"
}

# Function to set app service environment variable
set_app_setting() {
    local app_name=$1
    local resource_group=$2
    local setting_name=$3
    local setting_value=$4
    
    az webapp config appsettings set \
        --name "$app_name" \
        --resource-group "$resource_group" \
        --settings "$setting_name=$setting_value" \
        --output none
}

echo ""
echo "🔧 Step 1: Azure App Service Information"
echo "========================================"

# Get Azure App Service details
APP_NAME=$(prompt_input "APP_NAME" "Azure App Service name")
RESOURCE_GROUP=$(prompt_input "RESOURCE_GROUP" "Resource Group name")

# Login to Azure (if not already logged in)
echo ""
echo "🔐 Checking Azure login status..."
if ! az account show &> /dev/null; then
    echo "🔑 Please log in to Azure..."
    az login
fi

# Verify app service exists
echo "🔍 Verifying App Service exists..."
if ! az webapp show --name "$APP_NAME" --resource-group "$RESOURCE_GROUP" &> /dev/null; then
    echo "❌ App Service '$APP_NAME' not found in resource group '$RESOURCE_GROUP'"
    exit 1
fi

echo "✅ App Service found: $APP_NAME"

echo ""
echo "🎯 Step 2: Azure AD Configuration"
echo "================================="

# Azure AD Configuration
AZURE_TENANT_ID=$(prompt_input "AZURE_TENANT_ID" "Azure Tenant ID")
AZURE_CLIENT_ID=$(prompt_input "AZURE_CLIENT_ID" "Azure AD App Registration Client ID")
AZURE_CLIENT_SECRET=$(prompt_input "AZURE_CLIENT_SECRET" "Azure AD App Registration Client Secret")

echo ""
echo "📊 Step 3: Power BI Configuration"
echo "================================="

# Power BI Configuration
POWERBI_WORKSPACE_ID=$(prompt_input "POWERBI_WORKSPACE_ID" "Power BI Workspace ID")
POWERBI_DATASET_ID=$(prompt_input "POWERBI_DATASET_ID" "Power BI Dataset ID (optional)" false)

# Use same Azure AD app for Power BI if not specified differently
echo ""
read -p "🤔 Use the same Azure AD app for Power BI? (y/N): " use_same_app
if [[ $use_same_app =~ ^[Yy]$ ]]; then
    POWERBI_CLIENT_ID=$AZURE_CLIENT_ID
    POWERBI_CLIENT_SECRET=$AZURE_CLIENT_SECRET
    echo "✅ Using same Azure AD app for Power BI"
else
    POWERBI_CLIENT_ID=$(prompt_input "POWERBI_CLIENT_ID" "Power BI Client ID")
    POWERBI_CLIENT_SECRET=$(prompt_input "POWERBI_CLIENT_SECRET" "Power BI Client Secret")
fi

echo ""
echo "🗄️ Step 4: Database Configuration"
echo "=================================="

# Database Configuration
COSMOSDB_CONNECTION_STRING=$(prompt_input "COSMOSDB_CONNECTION_STRING" "CosmosDB Connection String")

echo ""
echo "🔐 Step 5: Security Configuration"
echo "================================="

# Security Configuration
JWT_SECRET=$(prompt_input "JWT_SECRET" "JWT Secret (min 32 characters)")

# Validate JWT secret length
if [[ ${#JWT_SECRET} -lt 32 ]]; then
    echo "❌ JWT Secret must be at least 32 characters long"
    exit 1
fi

echo ""
echo "🌐 Step 6: Frontend Configuration"
echo "================================="

# Frontend Configuration
FRONTEND_URL=$(prompt_input "FRONTEND_URL" "Frontend URL (e.g., https://your-domain.com)")

echo ""
echo "📈 Step 7: Application Insights (Optional)"
echo "=========================================="

# Application Insights
read -p "📊 Do you want to configure Application Insights? (y/N): " setup_appinsights
if [[ $setup_appinsights =~ ^[Yy]$ ]]; then
    APPINSIGHTS_CONNECTION=$(prompt_input "APPINSIGHTS_CONNECTION" "Application Insights Connection String")
fi

echo ""
echo "🚀 Step 8: Applying Configuration"
echo "================================="

echo "⚙️ Setting environment variables..."

# Core Azure AD Settings
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "AZURE_TENANT_ID" "$AZURE_TENANT_ID"
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "AZURE_CLIENT_ID" "$AZURE_CLIENT_ID"
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "AZURE_CLIENT_SECRET" "$AZURE_CLIENT_SECRET"

# Power BI Settings
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "POWERBI_CLIENT_ID" "$POWERBI_CLIENT_ID"
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "POWERBI_CLIENT_SECRET" "$POWERBI_CLIENT_SECRET"
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "POWERBI_WORKSPACE_ID" "$POWERBI_WORKSPACE_ID"

if [[ -n "$POWERBI_DATASET_ID" ]]; then
    set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "POWERBI_DATASET_ID" "$POWERBI_DATASET_ID"
fi

# Database Settings
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "COSMOSDB_CONNECTION_STRING" "$COSMOSDB_CONNECTION_STRING"

# Security Settings
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "JWT_SECRET" "$JWT_SECRET"

# Frontend Settings
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "FRONTEND_URL" "$FRONTEND_URL"

# Feature Flags
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "USE_REAL_POWERBI_API" "true"
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "USE_REAL_COST_API" "true"
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "FEATURE_POWERBI" "true"
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "FEATURE_COST_ALERTS" "true"
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "FEATURE_BUDGET_FORECASTING" "true"
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "FEATURE_OPTIMIZATION" "true"

# Application Insights (if configured)
if [[ -n "$APPINSIGHTS_CONNECTION" ]]; then
    set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "APPLICATIONINSIGHTS_CONNECTION_STRING" "$APPINSIGHTS_CONNECTION"
fi

# Environment Info
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "ENVIRONMENT_NAME" "Production"
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "APP_VERSION" "1.0.0"
set_app_setting "$APP_NAME" "$RESOURCE_GROUP" "DEPLOYMENT_DATE" "$(date -u +%Y-%m-%dT%H:%M:%SZ)"

echo ""
echo "✅ Configuration Complete!"
echo "========================="
echo ""
echo "📋 Summary of configured settings:"
echo "  🆔 Azure Tenant ID: $AZURE_TENANT_ID"
echo "  🔑 Azure Client ID: ${AZURE_CLIENT_ID:0:8}..."
echo "  📊 Power BI Workspace: $POWERBI_WORKSPACE_ID"
echo "  🌐 Frontend URL: $FRONTEND_URL"
echo "  🗄️ CosmosDB: Configured"
echo "  🔐 JWT Secret: Configured"
if [[ -n "$APPINSIGHTS_CONNECTION" ]]; then
echo "  📈 Application Insights: Configured"
fi
echo ""
echo "🔄 Next Steps:"
echo "1. Restart your App Service for settings to take effect"
echo "2. Monitor application logs for any configuration issues"
echo "3. Test Power BI integration through the dashboard"
echo "4. Set up monitoring and alerts"
echo ""
echo "🔄 To restart the App Service, run:"
echo "   az webapp restart --name $APP_NAME --resource-group $RESOURCE_GROUP"
echo ""

read -p "🔄 Do you want to restart the App Service now? (y/N): " restart_app
if [[ $restart_app =~ ^[Yy]$ ]]; then
    echo "🔄 Restarting App Service..."
    az webapp restart --name "$APP_NAME" --resource-group "$RESOURCE_GROUP"
    echo "✅ App Service restarted successfully!"
fi

echo ""
echo "🎉 Azure SmartCost Power BI integration is now configured!"
echo "   You can access your application at: https://$APP_NAME.azurewebsites.net"
echo ""