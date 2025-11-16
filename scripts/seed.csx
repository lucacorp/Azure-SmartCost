using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

var cosmosEndpoint = "https://smartcost-cosmos-7016.documents.azure.com/";
var cosmosKey = args[0];
var databaseId = "SmartCostDB";

Console.WriteLine("=== Seeding Cosmos DB ===\n");

// Dados do tenant
var tenant = new
{
    id = "tenant-real-001",
    name = "Minha Conta Azure - Demo",
    domain = "demo.smartcost.com",
    subscriptionId = "e6b85c41-c45d-42a5-955f-d4dfb3b13ce9",
    createdAt = DateTime.UtcNow.AddDays(-30),
    isActive = true,
    settings = new { currency = "BRL", timezone = "America/Sao_Paulo" }
};

Console.WriteLine("Tenant criado: " + tenant.name);
Console.WriteLine("\nDados prontos para insert!");
Console.WriteLine(JsonSerializer.Serialize(tenant, new JsonSerializerOptions { WriteIndented = true }));
