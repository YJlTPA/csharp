namespace ProcessMonitor.Core.Models;

public class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public SortMode DefaultSortMode { get; set; } = SortMode.ByCpuDescending;
    public bool AutoRefresh { get; set; } = true;
    public int AutoRefreshIntervalSeconds { get; set; } = 1;
    public bool ShowSystemProcesses { get; set; } = true;
}
