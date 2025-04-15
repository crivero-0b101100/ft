using System.Collections.Generic;

namespace FoodWeightTracker.Core.Models
{
    public class DailyRecord
    {
        public string Date { get; set; } // YYYY-MM-DD
        public List<FoodLog> FoodLogs { get; set; } = new List<FoodLog>();
        public List<ExerciseLog> ExerciseLogs { get; set; } = new List<ExerciseLog>();
        public int? ActualWeight { get; set; } // kg, null if not weighed
    }
} 