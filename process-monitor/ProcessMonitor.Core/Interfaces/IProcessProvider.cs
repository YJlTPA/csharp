using ProcessMonitor.Core.Models;

namespace ProcessMonitor.Core.Interfaces;

public interface IProcessProvider
{
    Task<List<ProcessSnapshot>> GetSnapshotsAsync(IProgress<RefreshProgress>? progress, CancellationToken ct);
}
