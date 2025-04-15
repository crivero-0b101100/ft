using System.Threading.Tasks;
using FoodWeightTracker.Core.Models;

namespace FoodWeightTracker.Core.Services
{
    public interface ICosmosDbService
    {
        Task<User> GetUserAsync(string userId);
        Task<User> UpsertUserAsync(User user);
    }
} 