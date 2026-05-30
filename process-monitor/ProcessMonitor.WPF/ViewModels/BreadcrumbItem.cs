namespace ProcessMonitor.WPF.ViewModels;

public class BreadcrumbItem
{
    public string Name { get; }
    public int Pid { get; }

    public BreadcrumbItem(string name, int pid)
    {
        Name = name;
        Pid = pid;
    }
}
