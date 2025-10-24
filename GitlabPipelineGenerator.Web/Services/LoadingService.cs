namespace GitlabPipelineGenerator.Web.Services;

public class LoadingService
{
    private bool _isLoading = false;
    private string _loadingMessage = "Loading...";

    public event Action? OnChange;

    public bool IsLoading => _isLoading;
    public string LoadingMessage => _loadingMessage;

    public void SetLoading(bool isLoading, string message = "Loading...")
    {
        _isLoading = isLoading;
        _loadingMessage = message;
        OnChange?.Invoke();
    }
}