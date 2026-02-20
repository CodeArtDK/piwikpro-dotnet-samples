namespace PiwikPROSamples.BlazorSample.Client.Models;

public class DimensionalFilter
{
    public string Dimension { get; set; } = string.Empty;
    public string Operator { get; set; } = "eq"; // Default operator
    public object Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public DimensionalFilter()
    {
    }

    public DimensionalFilter(string dimension, string op, object value, string displayName = "")
    {
        Dimension = dimension;
        Operator = op;
        Value = value;
        DisplayName = displayName;
    }
}

/// <summary>
/// Available filter operators for different data types
/// </summary>
public static class FilterOperators
{
    // String operators
    public static readonly string[] String = { "eq", "neq", "starts_with", "ends_with", "contains", "matches", "not_matches" };
    
    // Numeric operators
    public static readonly string[] Numeric = { "eq", "neq", "gt", "gte", "lt", "lte" };
    
    // Boolean operators
    public static readonly string[] Boolean = { "eq", "neq" };
    
    // Enum operators
    public static readonly string[] Enum = { "eq", "neq", "empty", "not_empty" };

    public static string GetDisplayName(string op) => op switch
    {
        "eq" => "Equals",
        "neq" => "Not Equals",
        "starts_with" => "Starts With",
        "ends_with" => "Ends With",
        "contains" => "Contains",
        "matches" => "Matches (regex)",
        "not_matches" => "Not Matches (regex)",
        "gt" => "Greater Than",
        "gte" => "Greater Than or Equal",
        "lt" => "Less Than",
        "lte" => "Less Than or Equal",
        "empty" => "Is Empty",
        "not_empty" => "Is Not Empty",
        _ => op
    };
}
