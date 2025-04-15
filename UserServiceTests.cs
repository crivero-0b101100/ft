using System;
using System.Threading.Tasks;
using FoodWeightTracker.Core.Models;
using FoodWeightTracker.Core.Services;
using NUnit.Framework;

namespace FoodWeightTracker.Tests
{
    [TestFixture]
    public class UserServiceTests
    {
        private UserService _userService;
        private MockCosmosDbService _mockDbService;

        [SetUp]
        public void Setup()
        {
            _mockDbService = new MockCosmosDbService();
            _userService = new UserService(_mockDbService);
        }

        [Test]
        public async Task SetupUser_ValidParameters_CreatesUser()
        {
            // Arrange
            string userId = "test123";
            int age = 30;
            string sex = "M";
            int height = 175;
            int initialWeight = 80;
            int targetWeight = 70;

            // Act
            string result = await _userService.SetupUserAsync(userId, age, sex, height, initialWeight, targetWeight);

            // Assert
            Assert.That(result, Is.EqualTo("User setup complete."));
            var user = _mockDbService.GetUser(userId);
            Assert.That(user, Is.Not.Null);
            Assert.That(user.Profile.Age, Is.EqualTo(age));
            Assert.That(user.Profile.Sex, Is.EqualTo(sex));
            Assert.That(user.Profile.Height, Is.EqualTo(height));
            Assert.That(user.Profile.InitialWeight, Is.EqualTo(initialWeight));
            Assert.That(user.Profile.TargetWeight, Is.EqualTo(targetWeight));
        }

        [Test]
        public async Task LogWeight_ValidWeight_UpdatesUser()
        {
            // Arrange
            string userId = "test123";
            await _userService.SetupUserAsync(userId, 30, "M", 175, 80, 70);
            int weight = 75;

            // Act
            string result = await _userService.LogWeightAsync(userId, weight);

            // Assert
            Assert.That(result, Is.EqualTo("Weight logged successfully."));
            var user = _mockDbService.GetUser(userId);
            var todayRecord = user.DailyRecords.FirstOrDefault(r => r.Date == DateTime.UtcNow.ToString("yyyy-MM-dd"));
            Assert.That(todayRecord, Is.Not.Null);
            Assert.That(todayRecord.ActualWeight, Is.EqualTo(weight));
        }

        [Test]
        public async Task LogWeight_InvalidWeight_ReturnsError()
        {
            // Arrange
            string userId = "test123";
            await _userService.SetupUserAsync(userId, 30, "M", 175, 80, 70);
            int weight = 1001; // Invalid weight

            // Act
            string result = await _userService.LogWeightAsync(userId, weight);

            // Assert
            Assert.That(result, Is.EqualTo("Invalid weight. Must be between 42 and 1000 kg."));
        }

        [Test]
        public async Task LogFood_ValidInput_UpdatesUser()
        {
            // Arrange
            string userId = "test123";
            await _userService.SetupUserAsync(userId, 30, "M", 175, 80, 70);
            string foodTag = "apple";
            int kcal = 95;

            // Act
            string result = await _userService.LogFoodAsync(userId, foodTag, kcal);

            // Assert
            Assert.That(result, Is.EqualTo("Food logged successfully."));
            var user = _mockDbService.GetUser(userId);
            var todayRecord = user.DailyRecords.FirstOrDefault(r => r.Date == DateTime.UtcNow.ToString("yyyy-MM-dd"));
            Assert.That(todayRecord, Is.Not.Null);
            Assert.That(todayRecord.FoodLogs.Count, Is.EqualTo(1));
            Assert.That(todayRecord.FoodLogs[0].Tag, Is.EqualTo(foodTag));
            Assert.That(todayRecord.FoodLogs[0].Kcal, Is.EqualTo(kcal));
        }

        [Test]
        public async Task LogExercise_ValidInput_UpdatesUser()
        {
            // Arrange
            string userId = "test123";
            await _userService.SetupUserAsync(userId, 30, "M", 175, 80, 70);
            string exerciseTag = "running";
            int minutes = 30;

            // Act
            string result = await _userService.LogExerciseAsync(userId, exerciseTag, minutes);

            // Assert
            Assert.That(result, Is.EqualTo("Exercise logged successfully."));
            var user = _mockDbService.GetUser(userId);
            var todayRecord = user.DailyRecords.FirstOrDefault(r => r.Date == DateTime.UtcNow.ToString("yyyy-MM-dd"));
            Assert.That(todayRecord, Is.Not.Null);
            Assert.That(todayRecord.ExerciseLogs.Count, Is.EqualTo(1));
            Assert.That(todayRecord.ExerciseLogs[0].Tag, Is.EqualTo(exerciseTag));
            Assert.That(todayRecord.ExerciseLogs[0].Minutes, Is.EqualTo(minutes));
        }

        [Test]
        public async Task GetTheoreticalWeight_ValidUser_ReturnsWeight()
        {
            // Arrange
            string userId = "test123";
            await _userService.SetupUserAsync(userId, 30, "M", 175, 80, 70);

            // Act
            string result = await _userService.GetTheoreticalWeightAsync(userId);

            // Assert
            Assert.That(result, Does.StartWith("Theoretical weight:"));
            Assert.That(result, Does.Contain("kg"));
        }
    }

    // Mock CosmosDbService for testing
    public class MockCosmosDbService : ICosmosDbService
    {
        private readonly Dictionary<string, User> _users = new Dictionary<string, User>();

        public Task<User> GetUserAsync(string userId)
        {
            _users.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public Task<User> UpsertUserAsync(User user)
        {
            _users[user.Id] = user;
            return Task.FromResult(user);
        }

        public User GetUser(string userId)
        {
            _users.TryGetValue(userId, out var user);
            return user;
        }
    }
} 