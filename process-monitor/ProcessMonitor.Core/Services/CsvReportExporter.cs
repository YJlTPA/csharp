using System.Text;
using ProcessMonitor.Core.Helpers;
using ProcessMonitor.Core.Interfaces;
using ProcessMonitor.Core.Models;

namespace ProcessMonitor.Core.Services;

public class CsvReportExporter : IReportExporter
{
    public async Task ExportToCsvAsync(IReadOnlyList<ProcessNode> roots, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Pid,ParentPid,Name,CPU%,Memory,Path,StartTime");
        foreach (var root in roots)
            AppendCsvNode(root, sb);
        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
    }

    private static void AppendCsvNode(ProcessNode node, StringBuilder sb)
    {
        var execPath = node.ExecutablePath ?? string.Empty;
        sb.AppendLine($"{node.Pid},{node.ParentPid},\"{node.Name}\",{node.CpuPercent:F1},{SizeFormatter.Format(node.MemoryBytes)},\"{execPath}\",{node.FormattedStartTime}");
        foreach (var child in node.Children)
            AppendCsvNode(child, sb);
    }

    public async Task ExportToTextAsync(IReadOnlyList<ProcessNode> roots, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Process Monitor Report");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(new string('-', 80));
        var rootList = roots.ToList();
        for (int i = 0; i < rootList.Count; i++)
            AppendTextNode(rootList[i], sb, "", i == rootList.Count - 1);
        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
    }

    private static void AppendTextNode(ProcessNode node, StringBuilder sb, string indent, bool isLast)
    {
        var connector = isLast ? "└── " : "├── ";
        var mem = SizeFormatter.Format(node.MemoryBytes).PadLeft(10);
        sb.AppendLine($"{indent}{connector}[{node.Pid,6}] {node.Name,-35} CPU:{node.CpuPercent,5:F1}%  Mem:{mem}");

        var childIndent = indent + (isLast ? "    " : "│   ");
        var children = node.Children.ToList();
        for (int i = 0; i < children.Count; i++)
            AppendTextNode(children[i], sb, childIndent, i == children.Count - 1);
    }
}
