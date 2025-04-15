using System;
using System.Collections.Generic;

namespace FoodWeightTracker.Core.Models
{
    public class User
    {
        public string Id { get; set; } // Telegram user ID
        public Profile Profile { get; set; }
        public List<DailyRecord> DailyRecords { get; set; } = new List<DailyRecord>();
        public int CurrentStartingWeight { get; set; }
        public string StartingDate { get; set; } // YYYY-MM-DD
    }
} 