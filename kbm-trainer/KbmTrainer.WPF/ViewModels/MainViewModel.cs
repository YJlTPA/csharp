using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using KbmTrainer.Core.Interfaces;
using KbmTrainer.Core.Models;
using KbmTrainer.WPF.Commands;

namespace KbmTrainer.WPF.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly IDictionaryRepository _dictionaryRepo;
    private readonly IStatisticsRepository _statisticsRepo;
    private readonly ISettingsService _settingsService;

    private object? _currentView;
    private bool _isSelectingDictionary = true;
    private TypingDictionary? _selectedDictionary;
    private string _sessionResultMessage = string.Empty;
    private bool _hasSessionResult;

    public TypingViewModel TypingViewModel { get; }
    public DictionaryEditorViewModel DictionaryEditorViewModel { get; }
    public StatisticsViewModel StatisticsViewModel { get; }
    public SettingsViewModel SettingsViewModel { get; }

    public ObservableCollection<TypingDictionary> Dictionaries { get; } = new();

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public bool IsSelectingDictionary
    {
        get => _isSelectingDictionary;
        set => SetProperty(ref _isSelectingDictionary, value);
    }

    public TypingDictionary? SelectedDictionary
    {
        get => _selectedDictionary;
        set => SetProperty(ref _selectedDictionary, value);
    }

    public string SessionResultMessage
    {
        get => _sessionResultMessage;
        set => SetProperty(ref _sessionResultMessage, value);
    }

    public bool HasSessionResult
    {
        get => _hasSessionResult;
        set => SetProperty(ref _hasSessionResult, value);
    }

    public RelayCommand NavigateTypingCommand { get; }
    public RelayCommand NavigateDictionariesCommand { get; }
    public RelayCommand NavigateStatisticsCommand { get; }
    public RelayCommand NavigateSettingsCommand { get; }
    public ICommand StartSessionCommand { get; }

    public MainViewModel(
        IDictionaryRepository dictionaryRepo,
        IStatisticsRepository statisticsRepo,
        ISettingsService settingsService)
    {
        _dictionaryRepo = dictionaryRepo;
        _statisticsRepo = statisticsRepo;
        _settingsService = settingsService;

        TypingViewModel = new TypingViewModel(statisticsRepo);
        DictionaryEditorViewModel = new DictionaryEditorViewModel(dictionaryRepo);
        StatisticsViewModel = new StatisticsViewModel(statisticsRepo);
        SettingsViewModel = new SettingsViewModel(settingsService);

        TypingViewModel.RequestChangeDictionary += () =>
        {
            IsSelectingDictionary = true;
            CurrentView = null;
        };

        TypingViewModel.SessionCompleted += result =>
        {
            SessionResultMessage = $"Done! WPM: {result.Wpm}  Accuracy: {result.Accuracy:F1}%  Time: {result.Duration:mm\\:ss}";
            HasSessionResult = true;
        };

        SettingsViewModel.ThemeChanged += ApplyTheme;

        NavigateTypingCommand = new RelayCommand(NavigateTyping);
        NavigateDictionariesCommand = new RelayCommand(NavigateDictionaries);
        NavigateStatisticsCommand = new RelayCommand(NavigateStatistics);
        NavigateSettingsCommand = new RelayCommand(NavigateSettings);
        StartSessionCommand = new RelayCommand(p => StartSession(p as TypingDictionary));
    }

    public async Task InitializeAsync()
    {
        await SettingsViewModel.LoadAsync();
        ApplyTheme(SettingsViewModel.Theme);

        var dicts = await _dictionaryRepo.GetAllAsync();
        Dictionaries.Clear();
        foreach (var d in dicts)
            Dictionaries.Add(d);

        if (Dictionaries.Count > 0)
            SelectedDictionary = Dictionaries[0];

        IsSelectingDictionary = true;
        CurrentView = null;
    }

    private void NavigateTyping()
    {
        HasSessionResult = false;
        IsSelectingDictionary = true;
        CurrentView = null;
    }

    private void NavigateDictionaries()
    {
        HasSessionResult = false;
        IsSelectingDictionary = false;
        CurrentView = DictionaryEditorViewModel;
        _ = DictionaryEditorViewModel.LoadAsync();
    }

    private void NavigateStatistics()
    {
        HasSessionResult = false;
        IsSelectingDictionary = false;
        CurrentView = StatisticsViewModel;
        _ = StatisticsViewModel.LoadAsync();
    }

    private void NavigateSettings()
    {
        HasSessionResult = false;
        IsSelectingDictionary = false;
        CurrentView = SettingsViewModel;
    }

    private void StartSession(TypingDictionary? dictionary = null)
    {
        var dict = dictionary ?? SelectedDictionary;
        if (dict == null) return;

        SelectedDictionary = dict;
        HasSessionResult = false;
        IsSelectingDictionary = false;
        TypingViewModel.LoadDictionary(dict, SettingsViewModel.TextLength, SettingsViewModel.FontSize);
        CurrentView = TypingViewModel;
    }

    private void ApplyTheme(string theme)
    {
        var app = Application.Current;
        var themeUri = theme == "Light"
            ? new Uri("pack://application:,,,/KbmTrainer.WPF;component/Themes/LightTheme.xaml")
            : new Uri("pack://application:,,,/KbmTrainer.WPF;component/Themes/DarkTheme.xaml");

        // Remove old theme dictionaries
        var toRemove = app.Resources.MergedDictionaries
            .Where(d => d.Source != null &&
                        (d.Source.ToString().Contains("DarkTheme") || d.Source.ToString().Contains("LightTheme")))
            .ToList();

        foreach (var d in toRemove)
            app.Resources.MergedDictionaries.Remove(d);

        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
    }
}
