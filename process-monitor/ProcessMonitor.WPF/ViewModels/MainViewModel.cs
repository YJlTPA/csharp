using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using ProcessMonitor.Core.Helpers;
using ProcessMonitor.Core.Interfaces;
using ProcessMonitor.Core.Models;
using ProcessMonitor.Core.Services;
using ProcessMonitor.WPF.Commands;

namespace ProcessMonitor.WPF.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly IProcessProvider _processProvider;
    private readonly ISettingsService _settingsService;
    private readonly IReportExporter _reportExporter;
    private AppSettings _settings;

    private CancellationTokenSource? _cts;
    private readonly DispatcherTimer _autoRefreshTimer;

    // --- Properties ---

    private ObservableCollection<ProcessNode> _rootProcesses = new();
    public ObservableCollection<ProcessNode> RootProcesses
    {
        get => _rootProcesses;
        set => SetProperty(ref _rootProcesses, value);
    }

    private ObservableCollection<ProcessNode> _currentLevelItems = new();
    public ObservableCollection<ProcessNode> CurrentLevelItems
    {
        get => _currentLevelItems;
        set => SetProperty(ref _currentLevelItems, value);
    }

    private ObservableCollection<BreadcrumbItem> _breadcrumbs = new();
    public ObservableCollection<BreadcrumbItem> Breadcrumbs
    {
        get => _breadcrumbs;
        set => SetProperty(ref _breadcrumbs, value);
    }

    private ProcessNode? _selectedNode;
    public ProcessNode? SelectedNode
    {
        get => _selectedNode;
        set => SetProperty(ref _selectedNode, value);
    }

    private bool _isRefreshing;
    public bool IsRefreshing
    {
        get => _isRefreshing;
        set => SetProperty(ref _isRefreshing, value);
    }

    private SortMode _currentSortMode = SortMode.ByCpuDescending;
    public SortMode CurrentSortMode
    {
        get => _currentSortMode;
        set { if (SetProperty(ref _currentSortMode, value)) ApplySort(); }
    }

    private bool _autoRefreshEnabled;
    public bool AutoRefreshEnabled
    {
        get => _autoRefreshEnabled;
        set
        {
            if (SetProperty(ref _autoRefreshEnabled, value))
            {
                if (value)
                    _autoRefreshTimer.Start();
                else
                    _autoRefreshTimer.Stop();
            }
        }
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private string _totalCpuFormatted = "CPU: 0.0%";
    public string TotalCpuFormatted
    {
        get => _totalCpuFormatted;
        set => SetProperty(ref _totalCpuFormatted, value);
    }

    private string _totalMemoryFormatted = "Memory: 0 B";
    public string TotalMemoryFormatted
    {
        get => _totalMemoryFormatted;
        set => SetProperty(ref _totalMemoryFormatted, value);
    }

    private string _lastRefreshTime = "-";
    public string LastRefreshTime
    {
        get => _lastRefreshTime;
        set => SetProperty(ref _lastRefreshTime, value);
    }

    private int _processCount;
    public int ProcessCount
    {
        get => _processCount;
        set => SetProperty(ref _processCount, value);
    }

    // --- Commands ---

    public ICommand RefreshCommand { get; }
    public ICommand KillProcessCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand ExportTextCommand { get; }
    public ICommand ToggleAutoRefreshCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand CopyPidCommand { get; }
    public ICommand OpenInExplorerCommand { get; }
    public ICommand NavigateIntoCommand { get; }
    public ICommand NavigateUpCommand { get; }
    public ICommand NavigateToBreadcrumbCommand { get; }
    public ICommand SortCommand { get; }

    // Events for dialogs
    public event Func<string, string?>? SaveFileRequested;
    public event Action? OpenSettingsRequested;

    public MainViewModel(IProcessProvider processProvider, ISettingsService settingsService, IReportExporter reportExporter)
    {
        _processProvider = processProvider;
        _settingsService = settingsService;
        _reportExporter = reportExporter;
        _settings = settingsService.Load();
        _currentSortMode = _settings.DefaultSortMode;

        RefreshCommand = new RelayCommand(async _ => await RefreshAsync(), _ => !IsRefreshing);
        KillProcessCommand = new RelayCommand(p => KillProcess(p as ProcessNode), p => p is ProcessNode);
        ExportCsvCommand = new RelayCommand(async _ => await ExportAsync("csv"), _ => RootProcesses.Count > 0 && !IsRefreshing);
        ExportTextCommand = new RelayCommand(async _ => await ExportAsync("txt"), _ => RootProcesses.Count > 0 && !IsRefreshing);
        ToggleAutoRefreshCommand = new RelayCommand(() => AutoRefreshEnabled = !AutoRefreshEnabled);
        OpenSettingsCommand = new RelayCommand(() => OpenSettings());
        CopyPidCommand = new RelayCommand(_ => CopyPid(), _ => SelectedNode != null);
        OpenInExplorerCommand = new RelayCommand(_ => OpenInExplorer(), _ => SelectedNode?.ExecutablePath != null);
        NavigateIntoCommand = new RelayCommand(p => NavigateInto(p as ProcessNode));
        NavigateUpCommand = new RelayCommand(_ => NavigateUp(), _ => Breadcrumbs.Count > 0);
        NavigateToBreadcrumbCommand = new RelayCommand(p => NavigateToBreadcrumb(p as BreadcrumbItem));
        SortCommand = new RelayCommand(p => { if (p is SortMode m) CurrentSortMode = m; });

        // DispatcherTimer must be created on UI thread — constructor is called from UI thread in App.xaml.cs
        _autoRefreshTimer = new DispatcherTimer();
        _autoRefreshTimer.Tick += async (_, _) => await RefreshAsync();

        UpdateTimerInterval();

        if (_settings.AutoRefresh)
            AutoRefreshEnabled = true;

        // Initial refresh
        _ = RefreshAsync();
    }

    private void UpdateTimerInterval()
    {
        _autoRefreshTimer.Interval = TimeSpan.FromSeconds(
            Math.Max(1, _settings.AutoRefreshIntervalSeconds));
    }

    public async Task RefreshAsync()
    {
        if (IsRefreshing) return;

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        IsRefreshing = true;
        StatusText = "Refreshing...";

        var progress = new Progress<RefreshProgress>(p =>
        {
            StatusText = p.StatusText;
        });

        try
        {
            var snapshots = await _processProvider.GetSnapshotsAsync(progress, _cts.Token);

            if (!_settings.ShowSystemProcesses)
                snapshots = snapshots.Where(s => s.Pid > 4).ToList();

            var roots = ProcessTreeBuilder.Build(snapshots);
            roots = ApplySortToList(roots);

            // Remember selected PID
            var selectedPid = SelectedNode?.Pid;

            RootProcesses.Clear();
            foreach (var r in roots)
                RootProcesses.Add(r);

            // Update current level items
            if (Breadcrumbs.Count > 0)
            {
                // Find the node matching last breadcrumb
                var lastBreadcrumb = Breadcrumbs[^1];
                var matchingNode = FindNodeByPid(roots, lastBreadcrumb.Pid);
                if (matchingNode != null)
                {
                    ShowProcessChildren(matchingNode);
                }
                else
                {
                    // Breadcrumb node no longer exists, reset to root
                    Breadcrumbs.Clear();
                    ShowAllRoots(roots);
                }
            }
            else
            {
                ShowAllRoots(roots);
            }

            // Restore selection
            if (selectedPid.HasValue)
            {
                SelectedNode = FindNodeByPid(roots, selectedPid.Value);
            }

            // Update totals
            var totalCpu = roots.Sum(r => r.TotalCpuPercent);
            var totalMem = roots.Sum(r => r.TotalMemoryBytes);
            var allSnapshots = FlattenNodes(roots);
            ProcessCount = allSnapshots.Count;
            TotalCpuFormatted = $"CPU: {totalCpu:F1}%";
            TotalMemoryFormatted = $"Memory: {SizeFormatter.Format(totalMem)}";
            LastRefreshTime = DateTime.Now.ToString("HH:mm:ss");
            StatusText = $"Processes: {ProcessCount} | CPU: {totalCpu:F1}% | Memory: {SizeFormatter.Format(totalMem)} | Refreshed: {LastRefreshTime}";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Refresh cancelled.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private static List<ProcessNode> FlattenNodes(IEnumerable<ProcessNode> nodes)
    {
        var result = new List<ProcessNode>();
        foreach (var n in nodes)
        {
            result.Add(n);
            result.AddRange(FlattenNodes(n.Children));
        }
        return result;
    }

    private static ProcessNode? FindNodeByPid(IEnumerable<ProcessNode> nodes, int pid)
    {
        foreach (var n in nodes)
        {
            if (n.Pid == pid) return n;
            var found = FindNodeByPid(n.Children, pid);
            if (found != null) return found;
        }
        return null;
    }

    private void ShowAllRoots(List<ProcessNode> roots)
    {
        CurrentLevelItems.Clear();
        foreach (var r in roots)
            CurrentLevelItems.Add(r);
    }

    private void ShowProcessChildren(ProcessNode node)
    {
        var sorted = ApplySortToList(node.Children.ToList());
        CurrentLevelItems.Clear();
        foreach (var c in sorted)
            CurrentLevelItems.Add(c);
    }

    public void NavigateInto(ProcessNode? node)
    {
        if (node == null) return;
        Breadcrumbs.Add(new BreadcrumbItem(node.Name, node.Pid));
        ShowProcessChildren(node);
        SelectedNode = node;
    }

    private void NavigateUp()
    {
        if (Breadcrumbs.Count == 0) return;
        Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);

        if (Breadcrumbs.Count == 0)
        {
            ShowAllRoots(_rootProcesses.ToList());
        }
        else
        {
            var parentBreadcrumb = Breadcrumbs[^1];
            var parentNode = FindNodeByPid(_rootProcesses, parentBreadcrumb.Pid);
            if (parentNode != null)
                ShowProcessChildren(parentNode);
            else
                ShowAllRoots(_rootProcesses.ToList());
        }
    }

    private void NavigateToBreadcrumb(BreadcrumbItem? item)
    {
        if (item == null) return;
        var index = Breadcrumbs.IndexOf(item);
        if (index < 0) return;

        while (Breadcrumbs.Count > index + 1)
            Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);

        if (index == 0 && Breadcrumbs.Count == 1)
        {
            // Check if this is the root level item (no parent)
            var node = FindNodeByPid(_rootProcesses, item.Pid);
            if (node != null)
                ShowProcessChildren(node);
            else
                ShowAllRoots(_rootProcesses.ToList());
        }
        else
        {
            var node = FindNodeByPid(_rootProcesses, item.Pid);
            if (node != null)
                ShowProcessChildren(node);
        }
    }

    private void ApplySort()
    {
        if (Breadcrumbs.Count > 0)
        {
            var lastPid = Breadcrumbs[^1].Pid;
            var node = FindNodeByPid(_rootProcesses, lastPid);
            if (node != null)
            {
                ShowProcessChildren(node);
                return;
            }
        }

        var sorted = ApplySortToList(_rootProcesses.ToList());
        CurrentLevelItems.Clear();
        foreach (var item in sorted)
            CurrentLevelItems.Add(item);
    }

    private List<ProcessNode> ApplySortToList(List<ProcessNode> items) =>
        CurrentSortMode switch
        {
            SortMode.ByCpuDescending => items.OrderByDescending(i => i.TotalCpuPercent).ToList(),
            SortMode.ByMemoryDescending => items.OrderByDescending(i => i.TotalMemoryBytes).ToList(),
            SortMode.ByNameAscending => items.OrderBy(i => i.Name).ToList(),
            SortMode.ByNameDescending => items.OrderByDescending(i => i.Name).ToList(),
            SortMode.ByPidAscending => items.OrderBy(i => i.Pid).ToList(),
            _ => items.OrderByDescending(i => i.TotalCpuPercent).ToList()
        };

    private void KillProcess(ProcessNode? node)
    {
        if (node == null) return;
        try
        {
            var p = Process.GetProcessById(node.Pid);
            p.Kill();
            StatusText = $"Killed process {node.Name} (PID {node.Pid})";
            _ = RefreshAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"Could not kill {node.Name}: {ex.Message}";
        }
    }

    private async Task ExportAsync(string format)
    {
        if (RootProcesses.Count == 0) return;
        var ext = format == "csv" ? "csv" : "txt";
        var path = SaveFileRequested?.Invoke(ext);
        if (path == null) return;

        try
        {
            var roots = RootProcesses.ToList();
            if (format == "csv")
                await _reportExporter.ExportToCsvAsync(roots, path);
            else
                await _reportExporter.ExportToTextAsync(roots, path);

            StatusText = $"Exported to {path}";
        }
        catch (Exception ex)
        {
            StatusText = $"Export failed: {ex.Message}";
        }
    }

    private void OpenInExplorer()
    {
        if (SelectedNode?.ExecutablePath == null) return;
        var dir = System.IO.Path.GetDirectoryName(SelectedNode.ExecutablePath);
        if (dir != null)
            Process.Start("explorer.exe", dir);
    }

    private void CopyPid()
    {
        if (SelectedNode == null) return;
        System.Windows.Clipboard.SetText(SelectedNode.Pid.ToString());
    }

    private void OpenSettings()
    {
        OpenSettingsRequested?.Invoke();
        _settings = _settingsService.Load();
        _currentSortMode = _settings.DefaultSortMode;
        OnPropertyChanged(nameof(CurrentSortMode));
        UpdateTimerInterval();
    }
}
