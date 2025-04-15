namespace FoodWeightTracker.Core.Models
{
    public class ExerciseLog
    {
        public string Tag { get; set; }
        public int Minutes { get; set; }
        public string Timestamp { get; set; } // YYYY-MM-DDTHH:MM:SSZ
    }
} 