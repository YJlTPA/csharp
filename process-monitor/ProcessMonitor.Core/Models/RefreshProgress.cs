namespace ProcessMonitor.Core.Models;

public class RefreshProgress
{
    public int ProcessesFound { get; init; }
    public string StatusText { get; init; } = string.Empty;
}
