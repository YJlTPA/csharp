namespace ProcessMonitor.Core.Models;

public class ProcessSnapshot
{
    public int Pid { get; init; }
    public int ParentPid { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ExecutablePath { get; init; }
    public double CpuPercent { get; init; }
    public long MemoryBytes { get; init; }
    public DateTime StartTime { get; init; }
    public string Status { get; init; } = "Running";
}
