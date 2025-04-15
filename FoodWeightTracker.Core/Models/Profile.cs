namespace FoodWeightTracker.Core.Models
{
    public class Profile
    {
        public int Age { get; set; }
        public string Sex { get; set; } // "M" or "F"
        public int Height { get; set; } // cm
        public int InitialWeight { get; set; } // kg
        public int TargetWeight { get; set; } // kg
        public string SetupDate { get; set; } // YYYY-MM-DD
    }
} 