using ProcessMonitor.Core.Interfaces;
using ProcessMonitor.Core.Models;
using ProcessMonitor.WPF.Commands;
using System.Windows.Input;

namespace ProcessMonitor.WPF.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private AppSettings _current;

    public string Theme
    {
        get => _current.Theme;
        set { _current.Theme = value; OnPropertyChanged(); }
    }

    public bool AutoRefresh
    {
        get => _current.AutoRefresh;
        set { _current.AutoRefresh = value; OnPropertyChanged(); }
    }

    public int AutoRefreshIntervalSeconds
    {
        get => _current.AutoRefreshIntervalSeconds;
        set { _current.AutoRefreshIntervalSeconds = value; OnPropertyChanged(); }
    }

    public bool ShowSystemProcesses
    {
        get => _current.ShowSystemProcesses;
        set { _current.ShowSystemProcesses = value; OnPropertyChanged(); }
    }

    public SortMode DefaultSortMode
    {
        get => _current.DefaultSortMode;
        set { _current.DefaultSortMode = value; OnPropertyChanged(); }
    }

    public IReadOnlyList<string> Themes { get; } = ["Dark", "Light"];
    public IReadOnlyList<SortMode> SortModes { get; } = Enum.GetValues<SortMode>();

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCommand { get; }

    public bool? DialogResult { get; private set; }

    public event EventHandler? RequestClose;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        _current = CopySettings(settingsService.Load());

        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(Cancel);
        ResetCommand = new RelayCommand(Reset);
    }

    private void Save()
    {
        _settingsService.Save(_current);
        DialogResult = true;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        DialogResult = false;
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void Reset()
    {
        _current = new AppSettings();
        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(AutoRefresh));
        OnPropertyChanged(nameof(AutoRefreshIntervalSeconds));
        OnPropertyChanged(nameof(ShowSystemProcesses));
        OnPropertyChanged(nameof(DefaultSortMode));
    }

    private static AppSettings CopySettings(AppSettings s) => new()
    {
        Theme = s.Theme,
        AutoRefresh = s.AutoRefresh,
        AutoRefreshIntervalSeconds = s.AutoRefreshIntervalSeconds,
        ShowSystemProcesses = s.ShowSystemProcesses,
        DefaultSortMode = s.DefaultSortMode
    };
}
