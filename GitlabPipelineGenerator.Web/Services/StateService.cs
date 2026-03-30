namespace GitlabPipelineGenerator.Web.Services;

public class StateService
{
    private readonly Dictionary<string, object> _state = new();
    
    public event Action? OnChange;

    public T? GetState<T>(string key) where T : class
    {
        return _state.TryGetValue(key, out var value) ? value as T : null;
    }

    public void SetState<T>(string key, T value) where T : class
    {
        _state[key] = value;
        OnChange?.Invoke();
    }

    public void ClearState(string key)
    {
        _state.Remove(key);
        OnChange?.Invoke();
    }

    public void ClearAllState()
    {
        _state.Clear();
        OnChange?.Invoke();
    }
}