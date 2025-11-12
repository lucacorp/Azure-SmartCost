namespace AzureSmartCost.Shared.Models
{
    public class CosmosDbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = "SmartCostDB";
        public string CostRecordsContainer { get; set; } = "CostRecords";
        public string EventsContainer { get; set; } = "Events";
    }
}