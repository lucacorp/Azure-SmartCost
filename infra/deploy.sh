#!/bin/bash

# Azure SmartCost Deployment Script
# This script deploys the infrastructure and applications to Azure

set -e

# Configuration
RESOURCE_GROUP="rg-smartcost-dev"
LOCATION="eastus"
ENVIRONMENT="dev"
SUBSCRIPTION_ID=""
BICEP_FILE="./main.bicep"
PARAMETERS_FILE="./parameters.dev.json"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 Azure SmartCost Deployment Script${NC}"
echo -e "${BLUE}=====================================${NC}"

# Check if user is logged into Azure CLI
echo -e "${YELLOW}📋 Checking Azure CLI login...${NC}"
if ! az account show > /dev/null 2>&1; then
    echo -e "${RED}❌ You are not logged into Azure CLI. Please run 'az login' first.${NC}"
    exit 1
fi

# Set subscription if provided
if [ ! -z "$SUBSCRIPTION_ID" ]; then
    echo -e "${YELLOW}🔧 Setting Azure subscription to: $SUBSCRIPTION_ID${NC}"
    az account set --subscription "$SUBSCRIPTION_ID"
fi

# Get current subscription info
CURRENT_SUBSCRIPTION=$(az account show --query "name" -o tsv)
echo -e "${GREEN}✅ Using Azure subscription: $CURRENT_SUBSCRIPTION${NC}"

# Create resource group
echo -e "${YELLOW}📦 Creating resource group: $RESOURCE_GROUP${NC}"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output table

# Deploy Bicep template
echo -e "${YELLOW}🔧 Deploying infrastructure with Bicep...${NC}"
DEPLOYMENT_NAME="smartcost-deploy-$(date +%Y%m%d-%H%M%S)"

az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file "$BICEP_FILE" \
    --parameters "@$PARAMETERS_FILE" \
    --name "$DEPLOYMENT_NAME" \
    --output table

# Get deployment outputs
echo -e "${YELLOW}📋 Getting deployment outputs...${NC}"
API_APP_NAME=$(az deployment group show --resource-group "$RESOURCE_GROUP" --name "$DEPLOYMENT_NAME" --query "properties.outputs.apiAppName.value" -o tsv)
API_APP_URL=$(az deployment group show --resource-group "$RESOURCE_GROUP" --name "$DEPLOYMENT_NAME" --query "properties.outputs.apiAppUrl.value" -o tsv)
STATIC_WEB_APP_NAME=$(az deployment group show --resource-group "$RESOURCE_GROUP" --name "$DEPLOYMENT_NAME" --query "properties.outputs.staticWebAppName.value" -o tsv)
STATIC_WEB_APP_URL=$(az deployment group show --resource-group "$RESOURCE_GROUP" --name "$DEPLOYMENT_NAME" --query "properties.outputs.staticWebAppUrl.value" -o tsv)
FUNCTION_APP_NAME=$(az deployment group show --resource-group "$RESOURCE_GROUP" --name "$DEPLOYMENT_NAME" --query "properties.outputs.functionAppName.value" -o tsv)

echo -e "${GREEN}✅ Infrastructure deployment completed!${NC}"
echo -e "${BLUE}📋 Deployment Summary:${NC}"
echo -e "   • Resource Group: $RESOURCE_GROUP"
echo -e "   • API App: $API_APP_NAME"
echo -e "   • API URL: $API_APP_URL"
echo -e "   • Static Web App: $STATIC_WEB_APP_NAME"
echo -e "   • Frontend URL: $STATIC_WEB_APP_URL"
echo -e "   • Function App: $FUNCTION_APP_NAME"

# Deploy API application
echo -e "${YELLOW}🚀 Building and deploying API application...${NC}"
cd "../src/AzureSmartCost.Api"
dotnet publish -c Release -o ./publish

# Create deployment package
echo -e "${YELLOW}📦 Creating API deployment package...${NC}"
cd ./publish
zip -r ../api-deploy.zip . -x "*.pdb"
cd ..

# Deploy to App Service
echo -e "${YELLOW}🚀 Deploying API to App Service...${NC}"
az webapp deploy --resource-group "$RESOURCE_GROUP" --name "$API_APP_NAME" --src-path "./api-deploy.zip" --type zip

# Clean up
rm -f ./api-deploy.zip
rm -rf ./publish

echo -e "${GREEN}✅ API deployment completed!${NC}"

# Deploy Frontend (Static Web App will be deployed via GitHub Actions)
echo -e "${YELLOW}📋 Frontend deployment information:${NC}"
echo -e "   • Static Web App deployment token is needed for GitHub Actions"
echo -e "   • Get the deployment token with:"
echo -e "     az staticwebapp secrets list --name $STATIC_WEB_APP_NAME --resource-group $RESOURCE_GROUP --query 'properties.apiKey' -o tsv"

echo -e "${GREEN}🎉 Azure SmartCost deployment completed successfully!${NC}"
echo -e "${BLUE}📋 Next Steps:${NC}"
echo -e "   1. Configure Azure AD application registration"
echo -e "   2. Update Azure AD Client ID and Secret in Key Vault"
echo -e "   3. Set up GitHub Actions for Static Web App deployment"
echo -e "   4. Configure custom domain (if needed)"
echo -e "   5. Set up monitoring and alerts"

echo -e "${BLUE}🔗 Useful URLs:${NC}"
echo -e "   • API Swagger: $API_APP_URL/swagger"
echo -e "   • Frontend: $STATIC_WEB_APP_URL"
echo -e "   • Azure Portal: https://portal.azure.com"