#!/bin/bash

# Azure SmartCost - Pre-deployment Setup Script
# This script prepares the environment for deployment to Azure

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
AZURE_REGION="eastus"
PROJECT_NAME="smartcost"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🚀 Azure SmartCost - Pre-deployment Setup${NC}"
echo -e "${BLUE}========================================${NC}"

# Function to print status
print_status() {
    echo -e "${YELLOW}📋 $1${NC}"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

# Check prerequisites
print_status "Checking prerequisites..."

# Check Azure CLI
if ! command -v az &> /dev/null; then
    print_error "Azure CLI is not installed. Please install it from https://docs.microsoft.com/cli/azure/"
    exit 1
fi

# Check dotnet
if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK is not installed. Please install .NET 8 SDK"
    exit 1
fi

# Check Node.js
if ! command -v node &> /dev/null; then
    print_error "Node.js is not installed. Please install Node.js 18+"
    exit 1
fi

print_success "All prerequisites are installed"

# Check Azure login
print_status "Checking Azure CLI login..."
if ! az account show &> /dev/null; then
    print_error "You are not logged into Azure CLI. Please run 'az login'"
    exit 1
fi

SUBSCRIPTION_NAME=$(az account show --query "name" -o tsv)
SUBSCRIPTION_ID=$(az account show --query "id" -o tsv)
print_success "Logged into Azure subscription: $SUBSCRIPTION_NAME ($SUBSCRIPTION_ID)"

# Create Azure AD App Registration
print_status "Creating Azure AD App Registration..."

APP_NAME="Azure SmartCost API"
EXISTING_APP=$(az ad app list --display-name "$APP_NAME" --query "[0].appId" -o tsv 2>/dev/null || echo "")

if [ -z "$EXISTING_APP" ]; then
    print_status "Creating new Azure AD App Registration..."
    
    # Create the app registration
    APP_ID=$(az ad app create \
        --display-name "$APP_NAME" \
        --web-redirect-uris "https://localhost:5001/signin-oidc" "https://smartcost-api-prod.azurewebsites.net/signin-oidc" \
        --query "appId" -o tsv)
    
    print_success "Created Azure AD App Registration: $APP_ID"
    
    # Create client secret
    print_status "Creating client secret..."
    CLIENT_SECRET=$(az ad app credential reset \
        --id $APP_ID \
        --display-name "SmartCost-Secret" \
        --query "password" -o tsv)
    
    print_success "Created client secret (save this securely!)"
    
else
    APP_ID=$EXISTING_APP
    print_success "Using existing Azure AD App Registration: $APP_ID"
    
    # Reset client secret
    print_status "Resetting client secret..."
    CLIENT_SECRET=$(az ad app credential reset \
        --id $APP_ID \
        --display-name "SmartCost-Secret-$(date +%Y%m%d)" \
        --query "password" -o tsv)
    
    print_success "Reset client secret (save this securely!)"
fi

# Get tenant ID
TENANT_ID=$(az account show --query "tenantId" -o tsv)

# Create Service Principal for GitHub Actions
print_status "Creating Service Principal for GitHub Actions..."

SP_NAME="smartcost-github-actions"
EXISTING_SP=$(az ad sp list --display-name "$SP_NAME" --query "[0].appId" -o tsv 2>/dev/null || echo "")

if [ -z "$EXISTING_SP" ]; then
    print_status "Creating new Service Principal..."
    
    SP_CREDENTIALS=$(az ad sp create-for-rbac \
        --name "$SP_NAME" \
        --role contributor \
        --scopes "/subscriptions/$SUBSCRIPTION_ID" \
        --sdk-auth)
    
    print_success "Created Service Principal for GitHub Actions"
    
else
    print_status "Resetting Service Principal credentials..."
    
    SP_CREDENTIALS=$(az ad sp create-for-rbac \
        --name "$SP_NAME" \
        --role contributor \
        --scopes "/subscriptions/$SUBSCRIPTION_ID" \
        --sdk-auth)
    
    print_success "Reset Service Principal credentials"
fi

# Generate JWT secret
JWT_SECRET=$(openssl rand -base64 32)

# Update parameter files
print_status "Updating parameter files..."

# Update dev parameters
DEV_PARAMS_FILE="$SCRIPT_DIR/parameters.dev.json"
jq --arg clientId "$APP_ID" \
   --arg clientSecret "$CLIENT_SECRET" \
   --arg jwtSecret "$JWT_SECRET" \
   '.parameters.azureAdClientId.value = $clientId | 
    .parameters.azureAdClientSecret.value = $clientSecret |
    .parameters.jwtSecret.value = $jwtSecret' \
   "$DEV_PARAMS_FILE" > "$DEV_PARAMS_FILE.tmp" && mv "$DEV_PARAMS_FILE.tmp" "$DEV_PARAMS_FILE"

print_success "Updated dev parameters"

# Create GitHub secrets template
print_status "Creating GitHub secrets template..."

SECRETS_FILE="$SCRIPT_DIR/github-secrets.env"
cat > "$SECRETS_FILE" << EOF
# GitHub Secrets for Azure SmartCost
# Add these secrets to your GitHub repository: Settings > Secrets and variables > Actions

# Service Principal for infrastructure deployment
AZURE_CREDENTIALS='${SP_CREDENTIALS}'

# Authentication secrets
JWT_SECRET='${JWT_SECRET}'
AZURE_AD_CLIENT_ID='${APP_ID}'
AZURE_AD_CLIENT_SECRET='${CLIENT_SECRET}'

# Azure subscription info
AZURE_SUBSCRIPTION_ID='${SUBSCRIPTION_ID}'
AZURE_TENANT_ID='${TENANT_ID}'
EOF

print_success "Created GitHub secrets template: $SECRETS_FILE"

# Validate Bicep files
print_status "Validating Bicep templates..."

if command -v az bicep &> /dev/null; then
    az bicep build --file "$SCRIPT_DIR/main.bicep" --stdout > /dev/null
    print_success "Bicep template validation passed"
else
    print_error "Bicep CLI not installed. Install with: az bicep install"
fi

# Build and test the application
print_status "Building and testing the application..."

cd "$PROJECT_ROOT"

# Build the solution
dotnet restore
dotnet build --configuration Release

print_success "Application build completed"

# Build frontend (if Node.js modules are installed)
if [ -d "$PROJECT_ROOT/smartcost-dashboard/node_modules" ]; then
    print_status "Building frontend..."
    cd "$PROJECT_ROOT/smartcost-dashboard"
    npm run build
    print_success "Frontend build completed"
else
    print_status "Skipping frontend build (node_modules not found)"
fi

cd "$PROJECT_ROOT"

print_success "Pre-deployment setup completed!"

echo -e "${BLUE}📋 Summary:${NC}"
echo -e "   • Azure AD App ID: $APP_ID"
echo -e "   • Tenant ID: $TENANT_ID"
echo -e "   • Subscription: $SUBSCRIPTION_NAME"
echo -e "   • Service Principal: Created for GitHub Actions"
echo -e "   • Secrets File: $SECRETS_FILE"

echo -e "${BLUE}🔧 Next Steps:${NC}"
echo -e "   1. Add GitHub secrets from: $SECRETS_FILE"
echo -e "   2. Review and customize parameter files"
echo -e "   3. Run deployment script: ./infra/deploy.ps1 or ./infra/deploy.sh"
echo -e "   4. Configure custom domains (if needed)"
echo -e "   5. Set up monitoring alerts"

echo -e "${YELLOW}⚠️  Important:${NC}"
echo -e "   • Save the client secret securely - it won't be shown again"
echo -e "   • Review the parameter files before deployment"
echo -e "   • Delete the secrets file after adding to GitHub: rm $SECRETS_FILE"

print_success "Setup complete! Ready for deployment to Azure."