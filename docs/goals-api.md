# Goals API

The Goals API lets you manage goals (create, read, update, delete) and analyze conversion performance in Piwik PRO.

## Setup

```csharp
services.AddPiwikProAnalytics(options =>
{
    options.BaseUrl = "https://your-instance.piwik.pro";
    options.ClientId = "your-client-id";
    options.ClientSecret = "your-client-secret";
    options.WebSiteId = "your-website-id";
});
```

Then inject `IGoalsService`.

## List Goals

```csharp
var goals = await goalsService.GetGoalsAsync();

foreach (var goal in goals)
{
    Console.WriteLine($"{goal.Name} (ID: {goal.Id})");
    Console.WriteLine($"  Type: {goal.Type}, Active: {goal.Active}, Revenue: {goal.Revenue:C}");
}
```

## Get a Specific Goal

```csharp
var goal = await goalsService.GetGoalAsync("goal-id");
```

## Create a Goal

```csharp
var request = new GoalRequest
{
    Name = "Newsletter Signup",
    Description = "User signs up for newsletter",
    Type = "event",
    Pattern = "newsletter_signup",
    PatternType = "exact",
    Revenue = 5.0m,
    Active = true
};

var created = await goalsService.CreateGoalAsync(request);
Console.WriteLine($"Created goal: {created.Id}");
```

## Update a Goal

```csharp
var request = new GoalRequest
{
    Name = "Newsletter Signup (Updated)",
    Revenue = 10.0m,
    Active = true
};

var updated = await goalsService.UpdateGoalAsync("goal-id", request);
```

## Delete a Goal

```csharp
await goalsService.DeleteGoalAsync("goal-id");
```

## Goal Types

- **URL**: Triggered when a visitor reaches a specific URL
- **Event**: Triggered when a custom event occurs
- **Duration**: Triggered when a session lasts a certain duration
- **Pages per session**: Triggered when a visitor views a minimum number of pages

## Pattern Matching

When creating goals, `PatternType` can be:
- `exact` - Exact match
- `contains` - Pattern is contained in the value
- `regex` - Regular expression match

## See Also

- [Console Sample](../samples/PiwikPROSamples.ConsoleSample/) - `GoalsExamples.cs` with full CRUD examples
- [Analytics Queries](analytics-queries.md) - Query conversion data with the analytics API
