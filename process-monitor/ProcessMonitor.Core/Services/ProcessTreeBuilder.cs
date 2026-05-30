using ProcessMonitor.Core.Models;

namespace ProcessMonitor.Core.Services;

public static class ProcessTreeBuilder
{
    public static List<ProcessNode> Build(List<ProcessSnapshot> snapshots)
    {
        var nodeMap = new Dictionary<int, ProcessNode>();

        // Create all nodes (without parent ref first)
        foreach (var s in snapshots)
        {
            nodeMap[s.Pid] = new ProcessNode(
                s.Pid, s.ParentPid, s.Name, s.ExecutablePath,
                s.CpuPercent, s.MemoryBytes, s.StartTime, s.Status, null);
        }

        var roots = new List<ProcessNode>();

        foreach (var node in nodeMap.Values)
        {
            if (node.ParentPid != 0 && nodeMap.TryGetValue(node.ParentPid, out var parent))
            {
                // Rebuild with parent reference
                var withParent = new ProcessNode(
                    node.Pid, node.ParentPid, node.Name, node.ExecutablePath,
                    node.CpuPercent, node.MemoryBytes, node.StartTime, node.Status, parent);
                parent.AddChild(withParent);
                nodeMap[node.Pid] = withParent; // replace in map
            }
            else
            {
                roots.Add(node);
            }
        }

        return roots.OrderByDescending(r => r.TotalCpuPercent).ToList();
    }
}
