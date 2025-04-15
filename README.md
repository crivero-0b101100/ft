# Bot

A Telegram chatbot system that enables users to track their food intake, exercise, and weight. The system calculates theoretical weight based on Basal Metabolic Rate (BMR), food intake, and exercise logs, adjusts theoretical weights with actual weigh-ins, and provides historical data and future projections.

## Features

- User setup with age, sex, height, initial weight, and target weight
- Daily weight tracking
- Food intake logging with calorie counting
- Exercise logging with duration tracking
- Theoretical weight calculation based on BMR and activity
- Historical weight data visualization
- Future weight projections based on current patterns

## Commands

1. `/s <sex M/F>` - Set up a user with age, sex, height, initial weight, and target weight
2. `/wi` - Update the user's current weight
3. `/tw` - Get the theoretical weight
4. `/f` - Log food intake with tag and calories
5. `/e` - Log exercise with tag and duration
6. `/h` - Display daily history of theoretical and actual weights
7. `/p` - Project future weights based on historical patterns

## Technical Details

- Built with .NET 9.0
- Uses Azure Cosmos DB for data storage
- Implements the Mifflin-St Jeor equation for BMR calculation
- Weight change calculation: 7700 kcal surplus/deficit = 1 kg weight change
- Smoothing factor (beta = 0.5) for weight adjustments

## Project Structure

```
FoodWeightTracker/
├── FoodWeightTracker.Core/
│   ├── Models/
│   │   ├── User.cs
│   │   ├── Profile.cs
│   │   ├── DailyRecord.cs
│   │   ├── FoodLog.cs
│   │   └── ExerciseLog.cs
│   └── Services/
│       ├── CosmosDbService.cs
│       └── UserService.cs
└── FoodWeightTracker.Tests/
    └── UserServiceTests.cs
```

## Getting Started

1. Clone the repository
2. Install .NET 9.0 SDK
3. Set up Azure Cosmos DB and update connection settings
4. Build and run the project

## Testing

Run the unit tests using:

```bash
dotnet test
```

## License

MIT License 
