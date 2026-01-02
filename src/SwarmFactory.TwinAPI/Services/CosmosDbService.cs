using Microsoft.Azure.Cosmos;
using SwarmFactory.TwinAPI.Models;

namespace SwarmFactory.TwinAPI.Services
{
    public interface ICosmosDbService
    {
        Task<IEnumerable<Machine>> GetMachinesAsync();
        Task<Machine?> GetMachineAsync(string id); // Helper, though not strictly in spec list
        Task AddMachineAsync(Machine machine);
        Task<IEnumerable<Alert>> GetAlertsAsync(string? status);
    }

    public class CosmosDbService : ICosmosDbService
    {
        private readonly Container _machineContainer;
        private readonly Container _alertContainer;

        public CosmosDbService(CosmosClient cosmosClient, string databaseName)
        {
            var database = cosmosClient.GetDatabase(databaseName);
            _machineContainer = database.GetContainer("machines");
            _alertContainer = database.GetContainer("alerts");
        }

        public async Task<IEnumerable<Machine>> GetMachinesAsync()
        {
            var query = _machineContainer.GetItemQueryIterator<Machine>(new QueryDefinition("SELECT * FROM c"));
            var results = new List<Machine>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
        }

        public async Task<Machine?> GetMachineAsync(string id)
        {
            try
            {
                // Cross-partition query if we don't know the type, but for ID lookup we usually need partition key.
                // For simplicity in this demo, we query by ID.
                var query = new QueryDefinition("SELECT * FROM c WHERE c.id = @id")
                    .WithParameter("@id", id);
                
                using var iterator = _machineContainer.GetItemQueryIterator<Machine>(query);
                if (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    return response.FirstOrDefault();
                }
                return null;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task AddMachineAsync(Machine machine)
        {
            // PartitionKey is 'type' as defined in Bicep
            await _machineContainer.CreateItemAsync(machine, new PartitionKey(machine.Type.ToString()));
        }

        public async Task<IEnumerable<Alert>> GetAlertsAsync(string? status)
        {
            var queryString = "SELECT * FROM c";
            if (!string.IsNullOrEmpty(status))
            {
                queryString += " WHERE c.status = @status";
            }

            var queryDef = new QueryDefinition(queryString);
            if (!string.IsNullOrEmpty(status))
            {
                queryDef.WithParameter("@status", status);
            }

            var query = _alertContainer.GetItemQueryIterator<Alert>(queryDef);
            var results = new List<Alert>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response.ToList());
            }
            return results;
        }
    }
}