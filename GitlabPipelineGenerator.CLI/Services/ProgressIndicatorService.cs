using System.Diagnostics;

namespace GitlabPipelineGenerator.CLI.Services;

/// <summary>
/// Service for displaying progress indicators during long-running operations
/// </summary>
public class ProgressIndicatorService : IDisposable
{
    private readonly Timer? _timer;
    private readonly string _message;
    private readonly string[] _spinnerChars = { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
    private int _spinnerIndex = 0;
    private bool _isRunning = false;
    private readonly object _lock = new object();
    private readonly Stopwatch _stopwatch;

    public ProgressIndicatorService(string message)
    {
        _message = message;
        _stopwatch = Stopwatch.StartNew();
        
        // Only show spinner in interactive console
        if (!Console.IsOutputRedirected)
        {
            _timer = new Timer(UpdateSpinner, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
            _isRunning = true;
        }
        else
        {
            // For non-interactive output, just show the message once
            Console.WriteLine($"🔄 {_message}...");
        }
    }

    private void UpdateSpinner(object? state)
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            var elapsed = _stopwatch.Elapsed;
            var elapsedText = elapsed.TotalSeconds > 60 
                ? $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}" 
                : $"{elapsed.TotalSeconds:F1}s";

            Console.Write($"\r{_spinnerChars[_spinnerIndex]} {_message}... ({elapsedText})");
            _spinnerIndex = (_spinnerIndex + 1) % _spinnerChars.Length;
        }
    }

    public void UpdateMessage(string newMessage)
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            if (!Console.IsOutputRedirected)
            {
                Console.Write($"\r{_spinnerChars[_spinnerIndex]} {newMessage}...");
            }
            else
            {
                Console.WriteLine($"🔄 {newMessage}...");
            }
        }
    }

    public void Complete(string? completionMessage = null)
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            _isRunning = false;
            _timer?.Dispose();
            _stopwatch.Stop();

            var elapsed = _stopwatch.Elapsed;
            var elapsedText = elapsed.TotalSeconds > 60 
                ? $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}" 
                : $"{elapsed.TotalSeconds:F1}s";

            if (!Console.IsOutputRedirected)
            {
                Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
            }

            var message = completionMessage ?? _message;
            Console.WriteLine($"✅ {message} completed ({elapsedText})");
        }
    }

    public void Fail(string? errorMessage = null)
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            _isRunning = false;
            _timer?.Dispose();
            _stopwatch.Stop();

            var elapsed = _stopwatch.Elapsed;
            var elapsedText = elapsed.TotalSeconds > 60 
                ? $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}" 
                : $"{elapsed.TotalSeconds:F1}s";

            if (!Console.IsOutputRedirected)
            {
                Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
            }

            var message = errorMessage ?? $"{_message} failed";
            Console.WriteLine($"❌ {message} ({elapsedText})");
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                _isRunning = false;
                _timer?.Dispose();
                
                if (!Console.IsOutputRedirected)
                {
                    Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
                }
            }
        }
    }

    /// <summary>
    /// Creates a progress indicator for a specific operation
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <returns>A new progress indicator instance</returns>
    public static ProgressIndicatorService Create(string message)
    {
        return new ProgressIndicatorService(message);
    }

    /// <summary>
    /// Executes an async operation with a progress indicator
    /// </summary>
    /// <typeparam name="T">The return type of the operation</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="message">The progress message</param>
    /// <param name="completionMessage">Optional completion message</param>
    /// <returns>The result of the operation</returns>
    public static async Task<T> ExecuteWithProgressAsync<T>(
        Func<Task<T>> operation, 
        string message, 
        string? completionMessage = null)
    {
        using var progress = new ProgressIndicatorService(message);
        try
        {
            var result = await operation();
            progress.Complete(completionMessage);
            return result;
        }
        catch (Exception)
        {
            progress.Fail();
            throw;
        }
    }

    /// <summary>
    /// Executes an async operation with a progress indicator (no return value)
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="message">The progress message</param>
    /// <param name="completionMessage">Optional completion message</param>
    public static async Task ExecuteWithProgressAsync(
        Func<Task> operation, 
        string message, 
        string? completionMessage = null)
    {
        using var progress = new ProgressIndicatorService(message);
        try
        {
            await operation();
            progress.Complete(completionMessage);
        }
        catch (Exception)
        {
            progress.Fail();
            throw;
        }
    }
}