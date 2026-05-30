using System.Collections.ObjectModel;
using ProcessMonitor.Core.Helpers;

namespace ProcessMonitor.Core.Models;

public class ProcessNode
{
    public int Pid { get; }
    public int ParentPid { get; }
    public string Name { get; }
    public string? ExecutablePath { get; }
    public double CpuPercent { get; set; }
    public long MemoryBytes { get; set; }
    public DateTime StartTime { get; }
    public string Status { get; }
    public ProcessNode? Parent { get; }
    public ObservableCollection<ProcessNode> Children { get; } = new();

    public string FormattedMemory => SizeFormatter.Format(MemoryBytes);
    public string FormattedCpu => $"{CpuPercent:F1}%";
    public string FormattedStartTime => StartTime == DateTime.MinValue ? "-" : StartTime.ToString("HH:mm:ss");
    public double TotalCpuPercent => CpuPercent + Children.Sum(c => c.TotalCpuPercent);
    public long TotalMemoryBytes => MemoryBytes + Children.Sum(c => c.TotalMemoryBytes);
    public int ChildCount => Children.Count;

    public ProcessNode(int pid, int parentPid, string name, string? executablePath,
        double cpuPercent, long memoryBytes, DateTime startTime, string status, ProcessNode? parent)
    {
        Pid = pid;
        ParentPid = parentPid;
        Name = name;
        ExecutablePath = executablePath;
        CpuPercent = cpuPercent;
        MemoryBytes = memoryBytes;
        StartTime = startTime;
        Status = status;
        Parent = parent;
    }

    public void AddChild(ProcessNode child) => Children.Add(child);
}
