using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

// Script para popular Cosmos DB com dados de demonstração
// Compilar: dotnet script SeedCosmosDb.cs
// Executar: dotnet script SeedCosmosDb.cs -- "<connection-string>"

public class SeedData
{
    private static CosmosClient? cosmosClient;
    private static Database? database;
    
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("❌ Erro: Informe a connection string do Cosmos DB");
            Console.WriteLine("Uso: dotnet script SeedCosmosDb.cs -- \"<connection-string>\"");
            return;
        }

        var connectionString = args[0];
        
        try
        {
            Console.WriteLine("🚀 Iniciando seed do Cosmos DB...\n");
            
            cosmosClient = new CosmosClient(connectionString);
            database = cosmosClient.GetDatabase("SmartCostDB");
            
            await SeedTenants();
            await SeedUsers();
            await SeedSubscriptions();
            await SeedCostData();
            
            Console.WriteLine("\n✅ Seed concluído com sucesso!");
            Console.WriteLine("\n📊 Dados criados:");
            Console.WriteLine("   • 3 Tenants (empresas)");
            Console.WriteLine("   • 8 Usuários");
            Console.WriteLine("   • 6 Subscrições Azure");
            Console.WriteLine("   • ~270 registros de custo (3 meses)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Erro: {ex.Message}");
        }
    }

    private static async Task SeedTenants()
    {
        Console.WriteLine("📁 Criando Tenants...");
        var container = database!.GetContainer("Tenants");
        
        var tenants = new[]
        {
            new {
                id = "tenant-contoso",
                name = "Contoso Ltd",
                createdAt = DateTime.UtcNow.AddMonths(-6),
                plan = "Premium",
                isActive = true,
                settings = new { currency = "USD", timezone = "America/Sao_Paulo" }
            },
            new {
                id = "tenant-fabrikam",
                name = "Fabrikam Inc",
                createdAt = DateTime.UtcNow.AddMonths(-3),
                plan = "Professional",
                isActive = true,
                settings = new { currency = "USD", timezone = "America/Sao_Paulo" }
            },
            new {
                id = "tenant-adventure",
                name = "Adventure Works",
                createdAt = DateTime.UtcNow.AddMonths(-1),
                plan = "Starter",
                isActive = true,
                settings = new { currency = "USD", timezone = "America/Sao_Paulo" }
            }
        };

        foreach (var tenant in tenants)
        {
            await container.UpsertItemAsync(tenant, new PartitionKey(tenant.id));
            Console.WriteLine($"   ✅ {tenant.name}");
        }
    }

    private static async Task SeedUsers()
    {
        Console.WriteLine("\n👥 Criando Usuários...");
        var container = database!.GetContainer("Users");
        
        var users = new[]
        {
            new { id = Guid.NewGuid().ToString(), tenantId = "tenant-contoso", email = "admin@contoso.com", name = "John Admin", role = "Admin" },
            new { id = Guid.NewGuid().ToString(), tenantId = "tenant-contoso", email = "finance@contoso.com", name = "Mary Finance", role = "FinanceManager" },
            new { id = Guid.NewGuid().ToString(), tenantId = "tenant-contoso", email = "analyst@contoso.com", name = "Bob Analyst", role = "CostAnalyst" },
            new { id = Guid.NewGuid().ToString(), tenantId = "tenant-fabrikam", email = "cfo@fabrikam.com", name = "Alice CFO", role = "Admin" },
            new { id = Guid.NewGuid().ToString(), tenantId = "tenant-fabrikam", email = "dev@fabrikam.com", name = "Charlie Dev", role = "Viewer" },
            new { id = Guid.NewGuid().ToString(), tenantId = "tenant-adventure", email = "owner@adventure.com", name = "Diana Owner", role = "Admin" },
            new { id = Guid.NewGuid().ToString(), tenantId = "tenant-adventure", email = "ops@adventure.com", name = "Eve Ops", role = "CostAnalyst" },
            new { id = Guid.NewGuid().ToString(), tenantId = "tenant-adventure", email = "intern@adventure.com", name = "Frank Intern", role = "Viewer" }
        };

        foreach (var user in users)
        {
            await container.UpsertItemAsync(user, new PartitionKey(user.tenantId));
            Console.WriteLine($"   ✅ {user.name} ({user.email})");
        }
    }

    private static async Task SeedSubscriptions()
    {
        Console.WriteLine("\n☁️  Criando Subscrições Azure...");
        var container = database!.GetContainer("Subscriptions");
        
        var subscriptions = new[]
        {
            new { id = "sub-contoso-prod", tenantId = "tenant-contoso", name = "Contoso Production", azureSubscriptionId = Guid.NewGuid().ToString(), monthlyBudget = 15000.0, currency = "USD" },
            new { id = "sub-contoso-dev", tenantId = "tenant-contoso", name = "Contoso Development", azureSubscriptionId = Guid.NewGuid().ToString(), monthlyBudget = 5000.0, currency = "USD" },
            new { id = "sub-fabrikam-main", tenantId = "tenant-fabrikam", name = "Fabrikam Main", azureSubscriptionId = Guid.NewGuid().ToString(), monthlyBudget = 8000.0, currency = "USD" },
            new { id = "sub-fabrikam-test", tenantId = "tenant-fabrikam", name = "Fabrikam Test", azureSubscriptionId = Guid.NewGuid().ToString(), monthlyBudget = 2000.0, currency = "USD" },
            new { id = "sub-adventure-prod", tenantId = "tenant-adventure", name = "Adventure Production", azureSubscriptionId = Guid.NewGuid().ToString(), monthlyBudget = 3000.0, currency = "USD" },
            new { id = "sub-adventure-dev", tenantId = "tenant-adventure", name = "Adventure Dev/Test", azureSubscriptionId = Guid.NewGuid().ToString(), monthlyBudget = 1000.0, currency = "USD" }
        };

        foreach (var sub in subscriptions)
        {
            await container.UpsertItemAsync(sub, new PartitionKey(sub.tenantId));
            Console.WriteLine($"   ✅ {sub.name} (Budget: ${sub.monthlyBudget:N0})");
        }
    }

    private static async Task SeedCostData()
    {
        Console.WriteLine("\n💰 Criando dados de custo (3 meses)...");
        var container = database!.GetContainer("CostData");
        var random = new Random(42); // Seed fixo para dados reproduzíveis
        
        var subscriptions = new[]
        {
            ("sub-contoso-prod", 450.0, 550.0),   // $450-550/dia
            ("sub-contoso-dev", 150.0, 200.0),    // $150-200/dia
            ("sub-fabrikam-main", 240.0, 300.0),  // $240-300/dia
            ("sub-fabrikam-test", 60.0, 80.0),    // $60-80/dia
            ("sub-adventure-prod", 90.0, 110.0),  // $90-110/dia
            ("sub-adventure-dev", 30.0, 40.0)     // $30-40/dia
        };

        var services = new[] { "Virtual Machines", "App Service", "Storage", "Cosmos DB", "SQL Database", "Networking", "Functions", "Monitor" };
        
        int totalRecords = 0;
        var startDate = DateTime.UtcNow.AddMonths(-3);
        
        foreach (var (subId, minCost, maxCost) in subscriptions)
        {
            for (int day = 0; day < 90; day++)
            {
                var date = startDate.AddDays(day);
                var dailyCost = minCost + (random.NextDouble() * (maxCost - minCost));
                
                // Distribuir custo entre serviços
                var serviceCosts = new Dictionary<string, double>();
                var remainingCost = dailyCost;
                
                for (int i = 0; i < services.Length - 1; i++)
                {
                    var serviceCost = remainingCost * random.NextDouble() * 0.4; // Max 40% do restante
                    serviceCosts[services[i]] = serviceCost;
                    remainingCost -= serviceCost;
                }
                serviceCosts[services[^1]] = remainingCost; // Último serviço pega o resto
                
                // Criar um registro por serviço
                foreach (var (service, cost) in serviceCosts)
                {
                    var costRecord = new
                    {
                        id = Guid.NewGuid().ToString(),
                        subscriptionId = subId,
                        date = date.ToString("yyyy-MM-dd"),
                        service = service,
                        cost = Math.Round(cost, 2),
                        currency = "USD",
                        resourceGroup = $"rg-{service.ToLower().Replace(" ", "-")}",
                        region = "Brazil South"
                    };
                    
                    await container.UpsertItemAsync(costRecord, new PartitionKey(costRecord.subscriptionId));
                    totalRecords++;
                }
            }
            
            Console.WriteLine($"   ✅ {subId}: {90 * services.Length} registros");
        }
        
        Console.WriteLine($"   📊 Total: {totalRecords} registros de custo");
    }
}

await SeedData.Main(Args.ToArray());
