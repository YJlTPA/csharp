using ProcessMonitor.Core.Models;

namespace ProcessMonitor.Core.Interfaces;

public interface IReportExporter
{
    Task ExportToCsvAsync(IReadOnlyList<ProcessNode> roots, string path);
    Task ExportToTextAsync(IReadOnlyList<ProcessNode> roots, string path);
}
