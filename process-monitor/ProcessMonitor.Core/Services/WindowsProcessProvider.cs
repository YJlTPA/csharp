using System.Diagnostics;
using System.Runtime.InteropServices;
using ProcessMonitor.Core.Interfaces;
using ProcessMonitor.Core.Models;

namespace ProcessMonitor.Core.Services;

public class WindowsProcessProvider : IProcessProvider
{
    public async Task<List<ProcessSnapshot>> GetSnapshotsAsync(IProgress<RefreshProgress>? progress, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            var cores = Environment.ProcessorCount;

            // Get parent PIDs via CreateToolhelp32Snapshot
            var parentMap = GetParentPidMap();

            var processes = Process.GetProcesses();

            // First CPU sample
            var cpuSample1 = new Dictionary<int, TimeSpan>();
            var t1 = DateTime.UtcNow;
            foreach (var p in processes)
            {
                try { cpuSample1[p.Id] = p.TotalProcessorTime; }
                catch { cpuSample1[p.Id] = TimeSpan.Zero; }
            }

            progress?.Report(new RefreshProgress { ProcessesFound = processes.Length, StatusText = "Measuring CPU..." });

            Thread.Sleep(500);
            ct.ThrowIfCancellationRequested();
            var elapsed = (DateTime.UtcNow - t1).TotalSeconds;

            var snapshots = new List<ProcessSnapshot>();
            foreach (var p in processes)
            {
                try
                {
                    double cpu = 0;
                    try
                    {
                        p.Refresh();
                        if (cpuSample1.TryGetValue(p.Id, out var prevCpu))
                        {
                            var delta = (p.TotalProcessorTime - prevCpu).TotalSeconds;
                            cpu = Math.Clamp(delta / (elapsed * cores) * 100.0, 0, 100);
                            cpu = Math.Round(cpu, 1);
                        }
                    }
                    catch { }

                    var parentPid = parentMap.TryGetValue(p.Id, out var pp) ? pp : 0;

                    snapshots.Add(new ProcessSnapshot
                    {
                        Pid = p.Id,
                        ParentPid = parentPid,
                        Name = p.ProcessName,
                        ExecutablePath = TryGetPath(p),
                        CpuPercent = cpu,
                        MemoryBytes = p.WorkingSet64,
                        StartTime = TryGetStartTime(p),
                        Status = "Running"
                    });
                }
                catch { }
                finally { p.Dispose(); }
            }

            progress?.Report(new RefreshProgress { ProcessesFound = snapshots.Count, StatusText = $"Loaded {snapshots.Count} processes" });
            return snapshots;
        }, ct);
    }

    private static Dictionary<int, int> GetParentPidMap()
    {
        var result = new Dictionary<int, int>();
        var snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snapshot == IntPtr.Zero || snapshot == new IntPtr(-1)) return result;
        try
        {
            var entry = new PROCESSENTRY32 { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };
            if (Process32First(snapshot, ref entry))
            {
                do
                {
                    result[(int)entry.th32ProcessID] = (int)entry.th32ParentProcessID;
                } while (Process32Next(snapshot, ref entry));
            }
        }
        finally { CloseHandle(snapshot); }
        return result;
    }

    private static string? TryGetPath(Process p)
    {
        try { return p.MainModule?.FileName; }
        catch { return null; }
    }

    private static DateTime TryGetStartTime(Process p)
    {
        try { return p.StartTime; }
        catch { return DateTime.MinValue; }
    }

    // P/Invoke
    private const uint TH32CS_SNAPPROCESS = 0x00000002;

    [DllImport("kernel32.dll")] private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);
    [DllImport("kernel32.dll")] private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);
    [DllImport("kernel32.dll")] private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExeFile;
    }
}
