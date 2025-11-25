# AWS Version Development Roadmap

## 📅 Timeline: Janeiro-Fevereiro 2026 (60-90 horas)

---

## PHASE 1: Architecture Refactoring (15 horas)

### 1.1 Create Cloud Provider Abstraction Layer
```typescript
// src/interfaces/ICloudCostProvider.ts
export interface ICloudCostProvider {
  getCostData(subscriptionId: string, startDate: Date, endDate: Date): Promise<CostData>;
  getBudgets(subscriptionId: string): Promise<Budget[]>;
  createBudget(budget: BudgetConfig): Promise<void>;
  getRecommendations(subscriptionId: string): Promise<Recommendation[]>;
}
```

### 1.2 Refactor Azure Implementation
```typescript
// src/providers/AzureCostProvider.ts
export class AzureCostProvider implements ICloudCostProvider {
  // Move existing Azure Cost Management logic here
}
```

### 1.3 Update Frontend to Use Abstraction
- Replace direct Azure API calls with provider interface
- Add cloud provider selector in dashboard

**Deliverable:** Azure version still works, now via abstraction layer

---

## PHASE 2: AWS Cost Explorer Integration (25 horas)

### 2.1 Install AWS SDK
```bash
npm install @aws-sdk/client-cost-explorer
npm install @aws-sdk/client-budgets
npm install @aws-sdk/client-ce
```

### 2.2 Implement AWS Provider
```typescript
// src/providers/AwsCostProvider.ts
import { CostExplorerClient, GetCostAndUsageCommand } from "@aws-sdk/client-cost-explorer";

export class AwsCostProvider implements ICloudCostProvider {
  private client: CostExplorerClient;

  async getCostData(accountId: string, startDate: Date, endDate: Date): Promise<CostData> {
    const command = new GetCostAndUsageCommand({
      TimePeriod: {
        Start: startDate.toISOString().split('T')[0],
        End: endDate.toISOString().split('T')[0]
      },
      Granularity: "DAILY",
      Metrics: ["UnblendedCost"],
      GroupBy: [{ Type: "DIMENSION", Key: "SERVICE" }]
    });

    const response = await this.client.send(command);
    return this.transformAwsData(response);
  }

  async getBudgets(accountId: string): Promise<Budget[]> {
    // AWS Budgets API integration
  }

  async getRecommendations(accountId: string): Promise<Recommendation[]> {
    // AWS Cost Explorer Recommendations
  }
}
```

### 2.3 Authentication
- Replace Azure AD with AWS Cognito
- Support IAM roles for AWS accounts
- Cross-account access via AssumeRole

**Deliverable:** AWS cost data flows into dashboard

---

## PHASE 3: Backend Adaptation (20 horas)

### 3.1 Lambda Functions (instead of Azure Functions)
```bash
# Convert C# Functions to Node.js Lambda
src/AzureSmartCost.Functions/ → lambda/
```

**Key Lambda Functions:**
- `getCosts` - Fetch cost data from Cost Explorer
- `updateBudgets` - Sync AWS Budgets
- `sendAlerts` - Email notifications via SES

### 3.2 DynamoDB (instead of Cosmos DB)
```typescript
// Database schema for DynamoDB
{
  PK: "ACCOUNT#12345678",
  SK: "BUDGET#monthly",
  budgetAmount: 1000,
  currentSpend: 750,
  alertThreshold: 80
}
```

### 3.3 AWS SES (instead of SendGrid)
- Use Amazon SES for email alerts
- Template-based emails
- Bounce/complaint handling

**Deliverable:** Full AWS backend running on Lambda + DynamoDB

---

## PHASE 4: Frontend Updates (10 horas)

### 4.1 Cloud Provider Selector
```typescript
// Add dropdown in dashboard
<Select value={cloudProvider}>
  <MenuItem value="azure">Azure</MenuItem>
  <MenuItem value="aws">AWS</MenuItem>
  <MenuItem value="multi">Both (Multi-Cloud)</MenuItem>
</Select>
```

### 4.2 AWS-Specific UI
- AWS account ID input (instead of subscription ID)
- AWS service names (EC2, S3, RDS vs VMs, Storage, SQL)
- AWS region selector

### 4.3 Unified Multi-Cloud View
```typescript
// Combine Azure + AWS costs
const totalCosts = azureCosts + awsCosts;
const breakdown = [
  { provider: 'Azure', cost: azureCosts },
  { provider: 'AWS', cost: awsCosts }
];
```

**Deliverable:** Dashboard supports Azure, AWS, or both simultaneously

---

## PHASE 5: Deployment & Infrastructure (15 horas)

### 5.1 AWS CDK Deployment
```typescript
// infra/aws-cdk/lib/smartcost-stack.ts
import * as cdk from 'aws-cdk-lib';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as dynamodb from 'aws-cdk-lib/aws-dynamodb';

export class SmartCostStack extends cdk.Stack {
  constructor(scope: cdk.App, id: string) {
    // Lambda functions
    const getCostsFunction = new lambda.Function(this, 'GetCosts', {
      runtime: lambda.Runtime.NODEJS_18_X,
      handler: 'getCosts.handler',
      code: lambda.Code.fromAsset('lambda')
    });

    // DynamoDB table
    const table = new dynamodb.Table(this, 'SmartCostTable', {
      partitionKey: { name: 'PK', type: dynamodb.AttributeType.STRING },
      sortKey: { name: 'SK', type: dynamodb.AttributeType.STRING }
    });
  }
}
```

### 5.2 CloudFormation Template
- Convert CDK to CloudFormation for AWS Marketplace
- Similar to Azure ARM template structure

### 5.3 AWS Marketplace Submission
- Create product listing
- Submit CloudFormation template
- Pricing: $99/month multi-cloud edition

**Deliverable:** AWS version deployable via Marketplace

---

## PHASE 6: Testing & Launch (15 horas)

### 6.1 Integration Testing
- Test Azure provider still works
- Test AWS provider with real account
- Test multi-cloud dashboard

### 6.2 Beta Testing
- Invite 10 AWS users
- Collect feedback on AWS-specific features
- Fix AWS quirks (reserved instances, savings plans)

### 6.3 Documentation
- Update README with AWS setup
- AWS IAM policy requirements
- Cross-account role setup guide

### 6.4 Marketing Launch
- "Unified Azure + AWS Cost Management"
- LinkedIn/Reddit posts
- AWS Marketplace listing live

**Deliverable:** SmartCost Multi-Cloud Edition launched

---

## 🎯 SUCCESS METRICS

- [ ] AWS cost data displays correctly
- [ ] AWS budgets sync within 5 minutes
- [ ] Email alerts work via SES
- [ ] Multi-cloud dashboard shows combined costs
- [ ] AWS Marketplace listing approved
- [ ] First 10 AWS customers onboarded

---

## 💰 PRICING STRATEGY

| Plan | Azure Only | AWS Only | Multi-Cloud |
|------|-----------|----------|-------------|
| **Free Tier** | ✅ Self-hosted | ✅ Self-hosted | ❌ |
| **Pro** | $49/month | $49/month | $99/month |
| **Enterprise** | $199/month | $199/month | $299/month |

**Value Proposition:** "Pay $99 for both clouds instead of $98 for two separate tools"

---

## 📊 ESTIMATED EFFORT

| Phase | Hours | Weeks (Part-time) |
|-------|-------|-------------------|
| Architecture Refactoring | 15 | 1 week |
| AWS Cost Explorer | 25 | 1.5 weeks |
| Backend Adaptation | 20 | 1 week |
| Frontend Updates | 10 | 0.5 weeks |
| Deployment | 15 | 1 week |
| Testing & Launch | 15 | 1 week |
| **TOTAL** | **90 hours** | **6 weeks** |

---

## 🚀 LAUNCH PLAN

**Week 1-2 (Jan 2026):** Architecture + AWS integration  
**Week 3-4 (Jan-Feb):** Backend + Frontend  
**Week 5 (Feb):** Testing + Documentation  
**Week 6 (Feb):** Beta launch + AWS Marketplace submission  
**Week 8 (Mar):** Public launch "SmartCost Multi-Cloud"

---

## ✅ PREREQUISITES

- [ ] Azure version approved and live
- [ ] 50+ Azure users for stability testing
- [ ] AWS account for development/testing
- [ ] $100 AWS credits (initial setup)
- [ ] 2 months development time available
