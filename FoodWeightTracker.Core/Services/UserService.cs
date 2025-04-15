using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoodWeightTracker.Core.Models;

namespace FoodWeightTracker.Core.Services
{
    public class UserService
    {
        private readonly ICosmosDbService _cosmosDbService;
        private const double CALORIES_PER_KG = 7700.0;
        private const double EXERCISE_CALORIES_PER_MINUTE = 7.0;
        private const double BETA = 0.5;

        public UserService(ICosmosDbService cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }

        public async Task<string> SetupUserAsync(string userId, int age, string sex, int height, int initialWeight, int targetWeight)
        {
            var user = new User
            {
                Id = userId,
                Profile = new Profile
                {
                    Age = age,
                    Sex = sex,
                    Height = height,
                    InitialWeight = initialWeight,
                    TargetWeight = targetWeight,
                    SetupDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
                },
                CurrentStartingWeight = initialWeight,
                StartingDate = DateTime.UtcNow.ToString("yyyy-MM-dd")
            };

            await _cosmosDbService.UpsertUserAsync(user);
            return "User setup complete.";
        }

        public async Task<string> LogWeightAsync(string userId, int weight)
        {
            if (weight < 42 || weight > 1000)
            {
                return "Invalid weight. Must be between 42 and 1000 kg.";
            }

            var user = await _cosmosDbService.GetUserAsync(userId);
            if (user == null)
            {
                return "User not found.";
            }

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var dailyRecord = user.DailyRecords.FirstOrDefault(r => r.Date == today);
            if (dailyRecord == null)
            {
                dailyRecord = new DailyRecord { Date = today };
                user.DailyRecords.Add(dailyRecord);
            }

            dailyRecord.ActualWeight = weight;
            await _cosmosDbService.UpsertUserAsync(user);
            return "Weight logged successfully.";
        }

        public async Task<string> LogFoodAsync(string userId, string tag, int kcal)
        {
            var user = await _cosmosDbService.GetUserAsync(userId);
            if (user == null)
            {
                return "User not found.";
            }

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var dailyRecord = user.DailyRecords.FirstOrDefault(r => r.Date == today);
            if (dailyRecord == null)
            {
                dailyRecord = new DailyRecord { Date = today };
                user.DailyRecords.Add(dailyRecord);
            }

            dailyRecord.FoodLogs.Add(new FoodLog
            {
                Tag = tag,
                Kcal = kcal,
                Timestamp = DateTime.UtcNow.ToString("O")
            });

            await _cosmosDbService.UpsertUserAsync(user);
            return "Food logged successfully.";
        }

        public async Task<string> LogExerciseAsync(string userId, string tag, int minutes)
        {
            var user = await _cosmosDbService.GetUserAsync(userId);
            if (user == null)
            {
                return "User not found.";
            }

            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var dailyRecord = user.DailyRecords.FirstOrDefault(r => r.Date == today);
            if (dailyRecord == null)
            {
                dailyRecord = new DailyRecord { Date = today };
                user.DailyRecords.Add(dailyRecord);
            }

            dailyRecord.ExerciseLogs.Add(new ExerciseLog
            {
                Tag = tag,
                Minutes = minutes,
                Timestamp = DateTime.UtcNow.ToString("O")
            });

            await _cosmosDbService.UpsertUserAsync(user);
            return "Exercise logged successfully.";
        }

        public async Task<string> GetHistoryAsync(string userId)
        {
            var user = await _cosmosDbService.GetUserAsync(userId);
            if (user == null)
                return "User not found.";

            var history = new List<string>();
            history.Add("Date | Theoretical weight | Actual weight");
            history.Add("----------------------------------------");

            var currentDate = DateTime.Parse(user.Profile.SetupDate);
            var endDate = DateTime.UtcNow.Date;

            while (currentDate <= endDate)
            {
                string dateStr = currentDate.ToString("yyyy-MM-dd");
                var record = user.DailyRecords.FirstOrDefault(r => r.Date == dateStr);
                
                double theoreticalWeight = CalculateTheoreticalWeightEndOfDay(user, dateStr);
                string actualWeight = record?.ActualWeight?.ToString() ?? "No Data";
                
                history.Add($"{dateStr} | {theoreticalWeight:F1} kg | {actualWeight} kg");
                currentDate = currentDate.AddDays(1);
            }

            return string.Join("\n", history);
        }

        public async Task<string> GetProjectionAsync(string userId)
        {
            var user = await _cosmosDbService.GetUserAsync(userId);
            if (user == null)
                return "User not found.";

            var projection = new List<string>();
            projection.Add("Date | Projected weight | N/A");
            projection.Add("----------------------------------------");

            // Calculate average daily net balance from last 7 days
            var last7Days = user.DailyRecords
                .OrderByDescending(r => r.Date)
                .Take(7)
                .ToList();

            double totalNetBalance = 0;
            foreach (var day in last7Days)
            {
                double bmr = CalculateBMR(user.CurrentStartingWeight, user.Profile.Age, user.Profile.Sex, user.Profile.Height);
                double foodCalories = day.FoodLogs.Sum(f => f.Kcal);
                double exerciseCalories = day.ExerciseLogs.Sum(e => e.Minutes * EXERCISE_CALORIES_PER_MINUTE);
                totalNetBalance += foodCalories - bmr - exerciseCalories;
            }
            double averageDailyNetBalance = totalNetBalance / Math.Max(last7Days.Count, 1);

            // Project 30 days
            var currentDate = DateTime.UtcNow.Date;
            double currentWeight = CalculateTheoreticalWeightEndOfDay(user, currentDate.ToString("yyyy-MM-dd"));

            for (int i = 1; i <= 30; i++)
            {
                currentDate = currentDate.AddDays(1);
                double bmr = CalculateBMR(currentWeight, user.Profile.Age, user.Profile.Sex, user.Profile.Height);
                currentWeight += (averageDailyNetBalance - bmr) / CALORIES_PER_KG;
                projection.Add($"{currentDate:yyyy-MM-dd} | {currentWeight:F1} kg | N/A");
            }

            return string.Join("\n", projection);
        }

        public async Task<string> GetTheoreticalWeightAsync(string userId)
        {
            var user = await _cosmosDbService.GetUserAsync(userId);
            if (user == null)
            {
                return "User not found.";
            }

            // Calculate theoretical weight based on BMR and activity
            double theoreticalWeight = CalculateTheoreticalWeight(user);
            return $"Theoretical weight: {theoreticalWeight:F1} kg";
        }

        private double CalculateBMR(double weight, int age, string sex, int height)
        {
            if (sex == "M")
                return 10 * weight + 6.25 * height - 5 * age + 5;
            else
                return 10 * weight + 6.25 * height - 5 * age - 161;
        }

        private double CalculateTheoreticalWeightEndOfDay(User user, string date)
        {
            var record = user.DailyRecords.FirstOrDefault(r => r.Date == date);
            if (record == null)
                return user.CurrentStartingWeight;

            double bmr = CalculateBMR(user.CurrentStartingWeight, user.Profile.Age, user.Profile.Sex, user.Profile.Height);
            double foodCalories = record.FoodLogs.Sum(f => f.Kcal);
            double exerciseCalories = record.ExerciseLogs.Sum(e => e.Minutes * EXERCISE_CALORIES_PER_MINUTE);
            double netBalance = foodCalories - bmr - exerciseCalories;

            return user.CurrentStartingWeight + netBalance / CALORIES_PER_KG;
        }

        private double CalculateTheoreticalWeight(User user)
        {
            // Implementation of theoretical weight calculation based on the design document
            // This is a placeholder that returns the current starting weight
            return user.CurrentStartingWeight;
        }
    }
} 