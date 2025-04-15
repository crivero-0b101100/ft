using System;
using System.Threading.Tasks;
using FoodWeightTracker.Core.Models;
using Microsoft.Azure.Cosmos;

namespace FoodWeightTracker.Core.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly CosmosClient _client;
        private readonly Database _database;
        private readonly Container _container;

        public CosmosDbService(string endpoint, string key, string databaseName, string containerName)
        {
            _client = new CosmosClient(endpoint, key);
            _database = _client.GetDatabase(databaseName);
            _container = _database.GetContainer(containerName);
        }

        public async Task<Models.User> GetUserAsync(string userId)
        {
            try
            {
                ItemResponse<Models.User> response = await _container.ReadItemAsync<Models.User>(userId, new PartitionKey(userId));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<Models.User> UpsertUserAsync(Models.User user)
        {
            ItemResponse<Models.User> response = await _container.UpsertItemAsync(user, new PartitionKey(user.Id));
            return response.Resource;
        }
    }
} 