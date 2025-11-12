# Contributing to Azure SmartCost

Thank you for your interest in contributing to Azure SmartCost! This document provides guidelines and instructions for contributing to the project.

## 📋 Table of Contents
- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Workflow](#development-workflow)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Pull Request Process](#pull-request-process)
- [Commit Message Guidelines](#commit-message-guidelines)
- [Issue Guidelines](#issue-guidelines)

---

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inspiring community for all. Please be respectful and constructive in all interactions.

### Our Standards

**Positive behaviors include:**
- Using welcoming and inclusive language
- Being respectful of differing viewpoints
- Gracefully accepting constructive criticism
- Focusing on what is best for the community
- Showing empathy towards others

**Unacceptable behaviors include:**
- Harassment, trolling, or derogatory comments
- Personal or political attacks
- Publishing others' private information
- Any conduct that could be considered inappropriate

### Enforcement

Instances of abusive behavior may be reported to conduct@smartcost.com. All complaints will be reviewed and investigated.

---

## Getting Started

### Prerequisites

Before you begin, ensure you have:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [Git](https://git-scm.com/)
- Code editor (VS Code recommended)

### Fork and Clone

```bash
# Fork the repository on GitHub, then clone your fork
git clone https://github.com/YOUR-USERNAME/Azure-SmartCost.git
cd Azure-SmartCost

# Add upstream remote
git remote add upstream https://github.com/original-org/Azure-SmartCost.git
```

### Setup Development Environment

#### Backend

```bash
cd src/AzureSmartCost.Api
dotnet restore
dotnet build

# Run tests
dotnet test
```

#### Frontend

```bash
cd smartcost-dashboard
npm install
npm start
```

#### Local Services

**Option 1: Docker Compose (Recommended)**
```bash
docker-compose up -d

# Starts:
# - Cosmos DB Emulator (localhost:8081)
# - Redis (localhost:6379)
```

**Option 2: Manual Setup**
- Install [Cosmos DB Emulator](https://aka.ms/cosmosdb-emulator)
- Install [Redis](https://redis.io/download) or use Docker: `docker run -d -p 6379:6379 redis`

---

## Development Workflow

### 1. Create a Branch

```bash
# Update your fork
git checkout main
git pull upstream main

# Create feature branch
git checkout -b feature/your-feature-name

# Or bugfix branch
git checkout -b fix/issue-number-description
```

**Branch naming conventions:**
- `feature/` - New features
- `fix/` - Bug fixes
- `docs/` - Documentation updates
- `refactor/` - Code refactoring
- `test/` - Test additions/improvements
- `chore/` - Maintenance tasks

### 2. Make Changes

- Write clean, maintainable code
- Follow coding standards (see below)
- Add tests for new functionality
- Update documentation as needed

### 3. Test Locally

```bash
# Backend tests
cd src/AzureSmartCost.Tests
dotnet test

# Frontend tests (if applicable)
cd smartcost-dashboard
npm test

# Integration tests
dotnet test --filter Category=Integration
```

### 4. Commit Changes

```bash
git add .
git commit -m "feat: add new cost optimization algorithm"
```

See [Commit Message Guidelines](#commit-message-guidelines) below.

### 5. Push and Create PR

```bash
git push origin feature/your-feature-name
```

Then create a Pull Request on GitHub.

---

## Coding Standards

### C# / .NET

#### Style Guide

Follow [.NET Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions):

```csharp
// ✅ GOOD
public class TenantService : ITenantService
{
    private readonly CosmosClient _cosmosClient;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        CosmosClient cosmosClient,
        ILogger<TenantService> logger)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Tenant?> GetTenantByIdAsync(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be null or empty", nameof(tenantId));
        }

        try
        {
            var response = await _container.ReadItemAsync<Tenant>(
                tenantId,
                new PartitionKey(tenantId)
            );
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Tenant not found: {TenantId}", tenantId);
            return null;
        }
    }
}

// ❌ BAD
public class tenantservice
{
    CosmosClient client;
    
    public tenantservice(CosmosClient c)
    {
        client = c;
    }
    
    public Tenant GetTenant(string id)
    {
        var t = client.GetContainer("db", "tenants").ReadItemAsync<Tenant>(id, new PartitionKey(id)).Result;
        return t.Resource;
    }
}
```

#### Naming Conventions

- **Classes**: PascalCase (`TenantService`, `CostController`)
- **Interfaces**: PascalCase with `I` prefix (`ITenantService`, `ICacheService`)
- **Methods**: PascalCase (`GetTenantByIdAsync`, `CalculateTotalCost`)
- **Parameters**: camelCase (`tenantId`, `startDate`)
- **Private fields**: camelCase with `_` prefix (`_cosmosClient`, `_logger`)
- **Constants**: PascalCase (`MaxRetryAttempts`, `DefaultTimeout`)

#### Error Handling

```csharp
// ✅ GOOD: Specific exceptions with context
if (string.IsNullOrEmpty(tenantId))
{
    throw new ArgumentException(
        "Tenant ID cannot be null or empty",
        nameof(tenantId)
    );
}

try
{
    await ProcessCostsAsync(tenantId);
}
catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
{
    _logger.LogWarning("Rate limited. Retrying in {Delay}ms", retryDelay);
    await Task.Delay(retryDelay);
    throw;
}

// ❌ BAD: Generic catch-all
try
{
    ProcessCosts(id);
}
catch (Exception ex)
{
    // Silent failure
}
```

#### Async/Await

```csharp
// ✅ GOOD: Consistent async naming and patterns
public async Task<Cost[]> GetCostsAsync(string tenantId, DateTime startDate)
{
    var container = _cosmosClient.GetContainer("SmartCostDB", "Costs");
    
    var query = new QueryDefinition(
        "SELECT * FROM c WHERE c.tenantId = @tenantId AND c.date >= @startDate"
    )
    .WithParameter("@tenantId", tenantId)
    .WithParameter("@startDate", startDate);

    var iterator = container.GetItemQueryIterator<Cost>(query);
    var results = new List<Cost>();

    while (iterator.HasMoreResults)
    {
        var response = await iterator.ReadNextAsync();
        results.AddRange(response);
    }

    return results.ToArray();
}

// ❌ BAD: Blocking calls, no async suffix
public Cost[] GetCosts(string id, DateTime start)
{
    var container = client.GetContainer("db", "costs");
    return container.GetItemQueryIterator<Cost>($"SELECT * FROM c WHERE c.id = '{id}'")
        .ReadNextAsync().Result.ToArray();
}
```

### TypeScript / React

#### Style Guide

Follow [Airbnb React/JSX Style Guide](https://airbnb.io/javascript/react/):

```typescript
// ✅ GOOD
interface CostData {
  date: string;
  amount: number;
  service: string;
}

interface CostChartProps {
  data: CostData[];
  onPeriodChange: (period: string) => void;
}

export const CostChart: React.FC<CostChartProps> = ({ data, onPeriodChange }) => {
  const [selectedPeriod, setSelectedPeriod] = useState<string>('7d');

  const handlePeriodChange = useCallback((period: string) => {
    setSelectedPeriod(period);
    onPeriodChange(period);
  }, [onPeriodChange]);

  return (
    <div className="cost-chart">
      <LineChart data={data} />
      <PeriodSelector 
        selected={selectedPeriod} 
        onChange={handlePeriodChange} 
      />
    </div>
  );
};

// ❌ BAD
export function costchart(props) {
  let period = '7d';
  
  return (
    <div>
      <LineChart data={props.data} />
      <button onClick={() => { period = '30d'; props.onChange(period); }}>
        30 days
      </button>
    </div>
  );
}
```

#### Hooks

```typescript
// ✅ GOOD: Custom hooks with proper dependencies
export const useCostData = (tenantId: string, period: string) => {
  const [data, setData] = useState<CostData[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  useEffect(() => {
    let cancelled = false;

    const fetchData = async () => {
      try {
        setLoading(true);
        const response = await api.getCosts(tenantId, period);
        
        if (!cancelled) {
          setData(response.data);
          setError(null);
        }
      } catch (err) {
        if (!cancelled) {
          setError(err as Error);
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    fetchData();

    return () => {
      cancelled = true;
    };
  }, [tenantId, period]);

  return { data, loading, error };
};

// ❌ BAD: Missing dependencies, no cleanup
export const useCostData = (id) => {
  const [data, setData] = useState([]);
  
  useEffect(() => {
    api.getCosts(id).then(res => setData(res.data));
  }, []); // Missing 'id' dependency
  
  return data;
};
```

---

## Testing Guidelines

### Unit Tests

```csharp
// C# Unit Test Example
[Fact]
public async Task GetTenantByIdAsync_ValidId_ReturnsTenant()
{
    // Arrange
    var tenantId = "tenant-123";
    var expectedTenant = new Tenant { Id = tenantId, Name = "Contoso" };
    
    _mockContainer
        .Setup(x => x.ReadItemAsync<Tenant>(
            tenantId,
            It.IsAny<PartitionKey>(),
            null,
            default))
        .ReturnsAsync(new ItemResponse<Tenant>(expectedTenant));

    // Act
    var result = await _tenantService.GetTenantByIdAsync(tenantId);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(expectedTenant.Id, result.Id);
    Assert.Equal(expectedTenant.Name, result.Name);
}

[Fact]
public async Task GetTenantByIdAsync_InvalidId_ThrowsArgumentException()
{
    // Act & Assert
    await Assert.ThrowsAsync<ArgumentException>(
        () => _tenantService.GetTenantByIdAsync(null)
    );
}
```

### Integration Tests

```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task CostController_GetCosts_ReturnsCorrectData()
{
    // Arrange
    var client = _factory.CreateClient();
    var token = await GetAuthTokenAsync();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);

    // Act
    var response = await client.GetAsync(
        "/api/costs?tenantId=test-123&startDate=2025-01-01&endDate=2025-01-31"
    );

    // Assert
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    var costs = JsonSerializer.Deserialize<CostResponse>(content);
    
    Assert.NotNull(costs);
    Assert.True(costs.Costs.Length > 0);
}
```

### Test Coverage Requirements

- **Minimum coverage**: 80%
- **Critical paths**: 90%+
- **New features**: 100%

```bash
# Check coverage
dotnet test /p:CollectCoverage=true

# Generate report
reportgenerator \
  -reports:coverage.opencover.xml \
  -targetdir:TestResults/CoverageReport \
  -reporttypes:HtmlInline
```

---

## Pull Request Process

### Before Submitting

- [ ] Code follows style guidelines
- [ ] All tests pass (`dotnet test`)
- [ ] Test coverage >= 80%
- [ ] Documentation updated
- [ ] No linting errors
- [ ] Commit messages follow convention

### PR Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Related Issues
Closes #123

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed

## Screenshots (if applicable)
[Add screenshots here]

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Comments added for complex code
- [ ] Documentation updated
- [ ] No new warnings
- [ ] Tests pass locally
```

### Review Process

1. **Automated Checks**: CI must pass
2. **Code Review**: 1+ approvals required
3. **Testing**: Reviewer tests functionality
4. **Approval**: Maintainer merges

### Merge Strategy

We use **Squash and Merge** to keep history clean:
```
git merge --squash feature/your-feature
```

---

## Commit Message Guidelines

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting)
- `refactor`: Code refactoring
- `test`: Adding/updating tests
- `chore`: Maintenance tasks
- `perf`: Performance improvements
- `ci`: CI/CD changes

### Examples

```bash
# Feature
git commit -m "feat(api): add cost anomaly detection endpoint"

# Bug fix
git commit -m "fix(cache): resolve Redis connection timeout issue"

# Documentation
git commit -m "docs(readme): update installation instructions"

# Breaking change
git commit -m "feat(api)!: change authentication to OAuth 2.0

BREAKING CHANGE: API now requires OAuth 2.0 tokens instead of API keys"
```

### Scope

Common scopes:
- `api` - Backend API
- `functions` - Azure Functions
- `frontend` - React application
- `infra` - Bicep infrastructure
- `cache` - Redis caching
- `auth` - Authentication
- `tests` - Test suite

---

## Issue Guidelines

### Bug Reports

```markdown
**Describe the bug**
Clear description of the issue

**To Reproduce**
Steps to reproduce:
1. Go to '...'
2. Click on '...'
3. See error

**Expected behavior**
What should happen

**Screenshots**
If applicable, add screenshots

**Environment**
- OS: [e.g., Windows 11]
- Browser: [e.g., Chrome 120]
- Version: [e.g., 2.0.0]

**Additional context**
Any other relevant information
```

### Feature Requests

```markdown
**Is your feature request related to a problem?**
Clear description of the problem

**Describe the solution you'd like**
What you want to happen

**Describe alternatives you've considered**
Other solutions you've thought about

**Additional context**
Mockups, examples, etc.
```

---

## Recognition

Contributors will be:
- Listed in [CONTRIBUTORS.md](CONTRIBUTORS.md)
- Mentioned in release notes
- Eligible for swag (100+ commits)
- Invited to contributor calls

---

## Questions?

- 📧 Email: dev@smartcost.com
- 💬 Discord: https://discord.gg/smartcost
- 📚 Docs: https://docs.smartcost.com

Thank you for contributing to Azure SmartCost! 🚀
