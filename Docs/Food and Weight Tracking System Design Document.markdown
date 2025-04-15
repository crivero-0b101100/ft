# Food and Weight Tracking System Design Document

## Overview

This document outlines the design for a Telegram chatbot system that enables users to track their food intake, exercise, and weight. The system calculates theoretical weight based on Basal Metabolic Rate (BMR), food intake, and exercise logs, adjusts theoretical weights with actual weigh-ins, and provides historical data and future projections. The solution is built using .NET 9.0, Azure Cosmos DB for storage, and NUnit for unit testing, with a focus on minimizing database costs.

---

## System Requirements

### Commands

The Telegram chatbot supports the following commands:

1. **/s &lt;sex M/F&gt;**

   - Sets up a user with age, sex, height, initial weight, and target weight.
   - One-time setup per user.

2. **/wi**

   - Updates the user's current weight (integer between 42 and 1000 kg).
   - Recorded with a timestamp, assumed to occur at the end of the day for simplicity.

3. **/tw**

   - Returns the theoretical weight, calculated using BMR, food intake, and exercise logs since the last weigh-in, including the current day's data up to the command's timestamp.

4. **/f**

   - Logs a food intake with a descriptive tag and calorie count.

5. **/e**

   - Logs an exercise with a tag and duration in minutes.
   - Assumes 60 minutes of exercise burns 420 calories (7 calories per minute).

6. **/h**

   - Displays a daily history of theoretical and actual weights since setup in the format:

     ```
     Date | Theoretical weight | Actual weight
     ```
   - Shows "No Data" for days without actual weight.

7. **/p**

   - Projects future weights based on historical eating and exercise patterns, in the same format as /h.

### Theoretical Weight Calculation

- **BMR Formula**: Uses the Mifflin-St Jeor equation:
  - Men: `BMR = 10 * weight (kg) + 6.25 * height (cm) - 5 * age (years) + 5`
  - Women: `BMR = 10 * weight (kg) + 6.25 * height (cm) - 5 * age (years) - 161`
- **Weight Change**: A 7700 kcal surplus increases weight by 1 kg; a 7700 kcal deficit decreases weight by 1 kg.
- **Adjustment**: When an actual weigh-in differs from the theoretical weight, adjust the theoretical weight partially (not fully) towards the actual weight using a smoothing factor (beta = 0.5).

### Development Considerations

- **Framework**: .NET 9.0
- **Datastore**: Azure Cosmos DB, designed for minimal cost
- **Testing**: Comprehensive unit tests using NUnit

---

## System Architecture

### Technology Stack

- **Backend**: .NET 9.0 (C#) for robust, modern development.
- **Datastore**: Azure Cosmos DB (NoSQL) for scalability and flexible schema.
- **Testing**: NUnit for unit testing.
- **Integration**: Telegram Bot API (assumed to be handled by a separate controller layer).

### High-Level Components

1. **UserService**: Core business logic for command processing, weight calculations, and data management.
2. **CosmosDbClient**: Handles interactions with Azure Cosmos DB (CRUD operations).
3. **Command Handlers**: Processes Telegram commands and invokes UserService methods.
4. **Unit Tests**: Validates each command and calculation logic.

---

## Data Model

### Azure Cosmos DB Design

To minimize costs (Request Units, RU/s) and simplify queries:

- **Single Container**: One container for all users.
- **Partition Key**: `user_id` (Telegram user ID), ensuring even distribution and efficient per-user queries.
- **Document Structure**: One document per user, containing profile and daily records.

#### User Document Schema

```json
{
  "id": "user_id", // Telegram user ID, string
  "profile": {
    "age": int,
    "sex": "M" or "F",
    "height": int, // cm
    "initial_weight": int, // kg
    "target_weight": int, // kg
    "setup_date": "YYYY-MM-DD"
  },
  "daily_records": [
    {
      "date": "YYYY-MM-DD",
      "food_logs": [
        {
          "tag": string,
          "kcal": int,
          "timestamp": "YYYY-MM-DDTHH:MM:SSZ" // UTC
        }
      ],
      "exercise_logs": [
        {
          "tag": string,
          "minutes": int,
          "timestamp": "YYYY-MM-DDTHH:MM:SSZ" // UTC
        }
      ],
      "actual_weight": int or null // kg
    }
  ],
  "current_starting_weight": int, // kg, adjusted at weigh-ins
  "starting_date": "YYYY-MM-DD" // Date after last weigh-in
}
```

#### Cost Considerations

- **Single Document per User**: Reduces the number of read/write operations compared to one document per day.
- **Size Limit**: Cosmos DB documents have a 2MB limit, sufficient for months of daily records (e.g., 100 days ≈ 50KB with typical usage).
- **Indexing**: Default indexing on all fields for flexibility; custom indexing can be optimized later if costs increase.

---

## Theoretical Weight Calculation Logic

### Key Concepts

- **Starting Weight**: Adjusted at each weigh-in, begins as initial weight.
- **Daily Net Balance**: `sum(food kcal) - BMR - sum(exercise calories)`, where exercise calories = `7 * minutes`.
- **Cumulative Balance**: Tracks calorie surplus/deficit since the last weigh-in or setup.
- **Adjustment**: When a weigh-in occurs, the starting weight is updated as `new_starting_weight = beta * actual_weight + (1 - beta) * theoretical_weight`, with `beta = 0.5`.

### Pseudocode for Historical Calculation (Used in /h)

```
Initialize:
  starting_weight = profile.initial_weight
  starting_date = profile.setup_date
  W_theo_previous = starting_weight

For each day in daily_records from starting_date to current_date - 1:
  W_theo_start = W_theo_previous
  BMR = calculate_BMR(W_theo_start, profile.age, profile.sex, profile.height)
  daily_net_balance = sum(food_logs.kcal) - BMR - sum(exercise_logs.minutes * 7)
  W_theo_end = W_theo_start + daily_net_balance / 7700
  If actual_weight is not null:
    new_starting_weight = 0.5 * actual_weight + 0.5 * W_theo_end
    starting_weight = new_starting_weight
    starting_date = next_day
    W_theo_previous = starting_weight
  Else:
    W_theo_previous = W_theo_end
  Store W_theo_end for this day
```

### Pseudocode for Current Theoretical Weight (/tw)

```
Get current_day from daily_records where date = today
If starting_date is today:
  W_theo_start = current_starting_weight
Else:
  W_theo_start = W_theo_end from yesterday (calculated as above)
BMR = calculate_BMR(W_theo_start, profile.age, profile.sex, profile.height)
fraction = (current_time - start_of_day) / 24 hours // Using message timestamp
net_balance_today = sum(current_day.food_logs.kcal) - (BMR * fraction) - sum(current_day.exercise_logs.minutes * 7)
W_theo_current = W_theo_start + net_balance_today / 7700
Return W_theo_current
```

### Pseudocode for Projections (/p)

```
Calculate last 7 days' daily_net_balance using historical calculation
average_daily_net_balance = sum(daily_net_balance) / 7
W_theo_current = last calculated W_theo_end or current_starting_weight if no records
For k = 1 to 30: // Project 30 days
  W_theo_start = W_theo_current
  BMR = calculate_BMR(W_theo_start, profile.age, profile.sex, profile.height)
  W_theo_end = W_theo_start + (average_daily_net_balance - BMR) / 7700
  W_theo_current = W_theo_end
  Store date + k, W_theo_end
```

### BMR Calculation

```
Function calculate_BMR(weight, age, sex, height):
  If sex == "M":
    Return 10 * weight + 6.25 * height - 5 * age + 5
  Else:
    Return 10 * weight + 6.25 * height - 5 * age - 161
```

---

## Command Processing

### /s - Setup User

- **Input**: age, sex, height, initial_weight, target_weight
- **Action**: Create a new user document with profile, set `current_starting_weight = initial_weight`, `starting_date = today`.
- **Output**: Confirmation message.

### /wi - Weigh-In

- **Input**: weight (42–1000 kg)
- **Action**: Add actual_weight to the current day's record. If the day is complete (end of day assumed), calculate W_theo_end and adjust `current_starting_weight`.
- **Output**: Confirmation message.

### /f - Log Food

- **Input**: food_tag, kcal
- **Action**: Append to current day's `food_logs` with current timestamp.
- **Output**: Confirmation message.

### /e - Log Exercise

- **Input**: exercise_tag, minutes
- **Action**: Append to current day's `exercise_logs` with current timestamp.
- **Output**: Confirmation message.

### /h - History

- **Action**: Calculate theoretical weight for each day up to yesterday using the historical pseudocode. Format as:

  ```
  YYYY-MM-DD | W_theo_end | actual_weight or "No Data"
  ```
- **Output**: Table string.

### /p - Projection

- **Action**: Calculate projections for 30 days using the projection pseudocode. Format as:

  ```
  YYYY-MM-DD | W_theo_end | N/A
  ```
- **Output**: Table string.

### /tw - Theoretical Weight

- **Action**: Calculate current theoretical weight using the /tw pseudocode.
- **Output**: "Theoretical weight: X kg".

---

## Implementation Details

### .NET 9.0 Structure

- **Models**:
  - `User`: Represents the Cosmos DB document.
  - `DailyRecord`, `FoodLog`, `ExerciseLog`: Nested classes.
- **Services**:
  - `UserService`: Core logic with methods for each command.
  - `CosmosDbService`: Encapsulates database operations.
- **Controllers**: Assumed Telegram integration layer calling UserService methods.

### Cosmos DB Operations

- **Read**: Fetch user document by `user_id`.
- **Write**: Update entire document after modifications.
- **Cost Optimization**: Batch updates where possible, minimize reads by computing on-demand.

---

## Unit Testing with NUnit

### Test Cases

1. **TestSetupUser**:

   - Input: Valid parameters (e.g., 30, "M", 175, 80, 70)
   - Assert: User document created with correct profile.

2. **TestLogWeight**:

   - Input: 75 kg
   - Assert: Weight added to current day, range validated.

3. **TestLogFood**:

   - Input: "apple", 52
   - Assert: Food log added with timestamp.

4. **TestLogExercise**:

   - Input: "running", 30
   - Assert: Exercise log added, calories = 210.

5. **TestHistory**:

   - Setup: User with 3 days of data (food, exercise, one weigh-in).
   - Assert: Correct table format, weights calculated accurately.

6. **TestProjection**:

   - Setup: User with 7 days of data.
   - Assert: 30-day projection matches expected pattern.

7. **TestTheoreticalWeight**:

   - Setup: User with past weigh-in, current day logs.
   - Assert: W_theo_current reflects BMR, food, and exercise.

### Approach

- **Mocking**: Use in-memory user objects to test logic independently of Cosmos DB.
- **Dependencies**: Inject CosmosDbService mock if needed.

---

## Assumptions and Simplifications

- Weigh-ins are assumed to occur at the end of the day for historical calculations.
- Current day logs use timestamps, but BMR is approximated linearly for partial days.
- Projections use a 7-day average, iteratively adjusting BMR for accuracy.

---

## Future Enhancements

- **Real-Time Day Completion**: Detect day transitions dynamically.
- **Cost Optimization**: Precompute and store theoretical weights if RU/s costs rise.
- **Validation**: Add more robust input checks (e.g., negative kcal).

This design provides a scalable, cost-effective solution meeting all requirements while leveraging .NET 9.0 and Azure Cosmos DB capabilities.