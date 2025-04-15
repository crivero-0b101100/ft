namespace FoodWeightTracker.Core.Models
{
    public class FoodLog
    {
        public string Tag { get; set; }
        public int Kcal { get; set; }
        public string Timestamp { get; set; } // YYYY-MM-DDTHH:MM:SSZ
    }
} 