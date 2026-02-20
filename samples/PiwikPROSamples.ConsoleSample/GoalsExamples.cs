using Microsoft.Extensions.DependencyInjection;
using PiwikPRO.Analytics.Extensions;
using PiwikPRO.Analytics.Models.Goals;
using PiwikPRO.Analytics.Services;
using PiwikPRO.Core;

namespace PiwikPROSamples.ConsoleSample;

/// <summary>
/// Examples demonstrating how to use the Goals API
/// </summary>
public class GoalsExamples
{
    private readonly IGoalsService _goalsService;

    public GoalsExamples(IGoalsService goalsService)
    {
        _goalsService = goalsService;
    }

    /// <summary>
    /// Example: Get all goals for a website
    /// </summary>
    public async Task GetAllGoalsExample()
    {
        System.Console.WriteLine("=== Get All Goals Example ===");

        var goals = await _goalsService.GetGoalsAsync();

        if (goals != null && goals.Count > 0)
        {
            foreach (var goal in goals)
            {
                System.Console.WriteLine($"Goal ID: {goal.Id}");
                System.Console.WriteLine($"  Name: {goal.Name}");
                System.Console.WriteLine($"  Type: {goal.Type}");
                System.Console.WriteLine($"  Active: {goal.Active}");
                System.Console.WriteLine($"  Revenue: {goal.Revenue:C}");
                System.Console.WriteLine();
            }
        }
        else
        {
            System.Console.WriteLine("No goals found.");
        }
    }

    /// <summary>
    /// Example: Create a new goal
    /// </summary>
    public async Task CreateGoalExample()
    {
        System.Console.WriteLine("=== Create Goal Example ===");

        var goalRequest = new GoalRequest
        {
            Name = "Newsletter Signup",
            Description = "User signs up for newsletter",
            Type = "event",
            Pattern = "newsletter_signup",
            PatternType = "exact",
            Revenue = 5.0m,
            Active = true
        };

        var createdGoal = await _goalsService.CreateGoalAsync(goalRequest);

        if (createdGoal != null)
        {
            System.Console.WriteLine($"Goal created successfully!");
            System.Console.WriteLine($"  ID: {createdGoal.Id}");
            System.Console.WriteLine($"  Name: {createdGoal.Name}");
        }
    }

    /// <summary>
    /// Example: Update an existing goal
    /// </summary>
    public async Task UpdateGoalExample(string goalId)
    {
        System.Console.WriteLine("=== Update Goal Example ===");

        var goalRequest = new GoalRequest
        {
            Name = "Newsletter Signup (Updated)",
            Description = "Updated description",
            Type = "event",
            Pattern = "newsletter_signup",
            PatternType = "exact",
            Revenue = 10.0m,
            Active = true
        };

        var updatedGoal = await _goalsService.UpdateGoalAsync(goalId, goalRequest);

        if (updatedGoal != null)
        {
            System.Console.WriteLine($"Goal updated successfully!");
            System.Console.WriteLine($"  Name: {updatedGoal.Name}");
            System.Console.WriteLine($"  Revenue: {updatedGoal.Revenue:C}");
        }
    }

    /// <summary>
    /// Example: Delete a goal
    /// </summary>
    public async Task DeleteGoalExample(string goalId)
    {
        System.Console.WriteLine("=== Delete Goal Example ===");

        await _goalsService.DeleteGoalAsync(goalId);
        System.Console.WriteLine($"Goal {goalId} deleted successfully!");
    }

    /// <summary>
    /// Setup method showing how to configure the service
    /// </summary>
    public static IGoalsService SetupGoalsService(string baseUrl, string clientId, string clientSecret, string webSiteId)
    {
        var services = new ServiceCollection();

        services.AddPiwikProAnalytics(options =>
        {
            options.BaseUrl = baseUrl;
            options.ClientId = clientId;
            options.ClientSecret = clientSecret;
            options.WebSiteId = webSiteId;
        });

        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IGoalsService>();
    }
}
