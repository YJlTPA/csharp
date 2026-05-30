using ProcessMonitor.Core.Models;
using ProcessMonitor.Core.Services;
using ProcessMonitor.Core.Helpers;

// Parse arguments
string sortArg = "cpu";
int topN = 50;
bool showTree = false;
string? exportFormat = null;
string? outputFile = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i].ToLowerInvariant())
    {
        case "--sort" when i + 1 < args.Length:
            sortArg = args[++i].ToLowerInvariant();
            break;
        case "--top" when i + 1 < args.Length:
            int.TryParse(args[++i], out topN);
            break;
        case "--tree":
            showTree = true;
            break;
        case "--export" when i + 1 < args.Length:
            exportFormat = args[++i].ToLowerInvariant();
            break;
        case "--output" when i + 1 < args.Length:
            outputFile = args[++i];
            break;
        case "--help":
        case "-h":
            PrintHelp();
            return;
    }
}

var sortMode = sortArg switch
{
    "memory" or "mem" => SortMode.ByMemoryDescending,
    "name"            => SortMode.ByNameAscending,
    "pid"             => SortMode.ByPidAscending,
    _                 => SortMode.ByCpuDescending
};

// Load processes
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("Process Monitor — loading processes...");
Console.ResetColor();

var provider = new WindowsProcessProvider();
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var progress = new Progress<RefreshProgress>(p =>
    Console.Write($"\r  {p.StatusText}    "));

List<ProcessMonitor.Core.Models.ProcessSnapshot> snapshots;
try
{
    snapshots = await provider.GetSnapshotsAsync(progress, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("\nCancelled.");
    return;
}

Console.WriteLine();

var roots = ProcessTreeBuilder.Build(snapshots);

// Apply sort
roots = sortMode switch
{
    SortMode.ByMemoryDescending => roots.OrderByDescending(r => r.TotalMemoryBytes).ToList(),
    SortMode.ByNameAscending    => roots.OrderBy(r => r.Name).ToList(),
    SortMode.ByPidAscending     => roots.OrderBy(r => r.Pid).ToList(),
    _                           => roots.OrderByDescending(r => r.TotalCpuPercent).ToList()
};

// Flatten for table view
var allProcesses = FlattenNodes(roots).OrderBy(n => sortMode switch
{
    SortMode.ByMemoryDescending => (object)(-n.MemoryBytes),
    SortMode.ByNameAscending    => n.Name,
    SortMode.ByPidAscending     => (object)n.Pid,
    _                           => (object)(-n.CpuPercent)
}).ToList();

if (showTree)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  {"PID",6}  {"Name",-35}  {"CPU%",6}  {"Memory",12}  Path");
    Console.WriteLine($"  {new string('-', 80)}");
    Console.ResetColor();
    foreach (var r in roots.Take(topN))
        PrintTreeNode(r, "", true);
}
else
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  {"PID",6}  {"Name",-35}  {"CPU%",6}  {"Memory",12}  Path");
    Console.WriteLine($"  {new string('-', 80)}");
    Console.ResetColor();

    foreach (var p in allProcesses.Take(topN))
        PrintProcessLine(p);
}

Console.WriteLine();
Console.ForegroundColor = ConsoleColor.DarkGray;
Console.WriteLine($"Total: {allProcesses.Count} processes  |  CPU: {allProcesses.Sum(p => p.CpuPercent):F1}%  |  Memory: {SizeFormatter.Format(allProcesses.Sum(p => p.MemoryBytes))}");
Console.ResetColor();

// Export
if (!string.IsNullOrEmpty(exportFormat))
{
    var exporter = new CsvReportExporter();
    var outPath = outputFile ?? $"process-report.{exportFormat}";
    if (exportFormat == "csv")
        await exporter.ExportToCsvAsync(roots, outPath);
    else
        await exporter.ExportToTextAsync(roots, outPath);

    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Exported to: {outPath}");
    Console.ResetColor();
}

static void PrintHelp()
{
    Console.WriteLine("processmonitor [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --sort cpu|memory|name|pid    Sort order (default: cpu)");
    Console.WriteLine("  --top N                       Show top N processes (default: 50)");
    Console.WriteLine("  --tree                        Show as process tree");
    Console.WriteLine("  --export csv|txt              Export report");
    Console.WriteLine("  --output <file>               Output file path");
    Console.WriteLine("  --help                        Show this help");
}

static void PrintProcessLine(ProcessMonitor.Core.Models.ProcessNode p)
{
    if (p.CpuPercent >= 50.0)
        Console.ForegroundColor = ConsoleColor.Red;
    else if (p.CpuPercent >= 10.0)
        Console.ForegroundColor = ConsoleColor.Yellow;
    else
        Console.ForegroundColor = ConsoleColor.Gray;

    var path = p.ExecutablePath ?? string.Empty;
    if (path.Length > 40) path = "..." + path[^37..];
    Console.WriteLine($"  {p.Pid,6}  {p.Name,-35}  {p.CpuPercent,5:F1}%  {SizeFormatter.Format(p.MemoryBytes),12}  {path}");
    Console.ResetColor();
}

static void PrintTreeNode(ProcessMonitor.Core.Models.ProcessNode node, string indent, bool isLast, int depth = 0)
{
    var connector = isLast ? "└── " : "├── ";
    if (node.CpuPercent >= 50.0) Console.ForegroundColor = ConsoleColor.Red;
    else if (node.CpuPercent >= 10.0) Console.ForegroundColor = ConsoleColor.Yellow;
    else Console.ForegroundColor = ConsoleColor.Gray;

    Console.WriteLine($"  {indent}{connector}[{node.Pid,6}] {node.Name,-30}  {node.CpuPercent,5:F1}%  {SizeFormatter.Format(node.MemoryBytes),12}");
    Console.ResetColor();

    if (depth < 4)
    {
        var childIndent = indent + (isLast ? "    " : "│   ");
        var children = node.Children.OrderByDescending(c => c.TotalCpuPercent).ToList();
        for (int i = 0; i < children.Count; i++)
            PrintTreeNode(children[i], childIndent, i == children.Count - 1, depth + 1);
    }
}

static List<ProcessMonitor.Core.Models.ProcessNode> FlattenNodes(IEnumerable<ProcessMonitor.Core.Models.ProcessNode> nodes)
{
    var result = new List<ProcessMonitor.Core.Models.ProcessNode>();
    foreach (var n in nodes)
    {
        result.Add(n);
        result.AddRange(FlattenNodes(n.Children));
    }
    return result;
}
