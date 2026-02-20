using PiwikPROSamples.BlazorSample.Client.Models;

namespace PiwikPROSamples.BlazorSample.Client.Services;

public interface IFilterStateService
{
    event Action? OnFilterChanged;
    Task<List<DimensionalFilter>> GetFiltersAsync();
    Task AddFilterAsync(DimensionalFilter filter);
    Task RemoveFilterAsync(DimensionalFilter filter);
    Task ClearFiltersAsync();
}

public class FilterStateService : IFilterStateService
{
    private const string FilterStorageKey = "DimensionalFilters";
    private List<DimensionalFilter> _filters = new();

    public event Action? OnFilterChanged;

    public async Task<List<DimensionalFilter>> GetFiltersAsync()
    {
        // For WebAssembly, we use in-memory storage since ProtectedSessionStorage is for Server-side Blazor
        // The filters will be maintained during the session but lost on page refresh
        return await Task.FromResult(_filters);
    }

    public async Task AddFilterAsync(DimensionalFilter filter)
    {
        // Check if filter already exists
        var existing = _filters.FirstOrDefault(f => 
            f.Dimension == filter.Dimension && f.Value == filter.Value);
        
        if (existing == null)
        {
            _filters.Add(filter);
            OnFilterChanged?.Invoke();
        }
        
        await Task.CompletedTask;
    }

    public async Task RemoveFilterAsync(DimensionalFilter filter)
    {
        var toRemove = _filters.FirstOrDefault(f => 
            f.Dimension == filter.Dimension && f.Value == filter.Value);
        
        if (toRemove != null)
        {
            _filters.Remove(toRemove);
            OnFilterChanged?.Invoke();
        }
        
        await Task.CompletedTask;
    }

    public async Task ClearFiltersAsync()
    {
        if (_filters.Any())
        {
            _filters.Clear();
            OnFilterChanged?.Invoke();
        }
        
        await Task.CompletedTask;
    }
}
