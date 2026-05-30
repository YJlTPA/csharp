using KbmTrainer.Core.Interfaces;
using KbmTrainer.Core.Models;
using KbmTrainer.WPF.Commands;

namespace KbmTrainer.WPF.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;

    private string _theme = "Dark";
    private double _fontSize = 20;
    private int _textLength = 50;
    private bool _showKeyboard = false;
    private string _statusMessage = string.Empty;

    public string[] Themes { get; } = { "Dark", "Light" };

    public string Theme
    {
        get => _theme;
        set
        {
            if (SetProperty(ref _theme, value))
                ThemeChanged?.Invoke(value);
        }
    }

    public double FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, value);
    }

    public int TextLength
    {
        get => _textLength;
        set => SetProperty(ref _textLength, value);
    }

    public bool ShowKeyboard
    {
        get => _showKeyboard;
        set => SetProperty(ref _showKeyboard, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public RelayCommand SaveCommand { get; }

    public event Action<string>? ThemeChanged;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        SaveCommand = new RelayCommand(Save);
    }

    public async Task LoadAsync()
    {
        var settings = await _settingsService.LoadAsync();
        _theme = settings.Theme;
        _fontSize = settings.FontSize;
        _textLength = settings.TextLength;
        _showKeyboard = settings.ShowKeyboard;

        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(FontSize));
        OnPropertyChanged(nameof(TextLength));
        OnPropertyChanged(nameof(ShowKeyboard));
    }

    public AppSettings GetCurrentSettings() => new AppSettings
    {
        Theme = Theme,
        FontSize = FontSize,
        TextLength = TextLength,
        ShowKeyboard = ShowKeyboard
    };

    private void Save()
    {
        _ = SaveAsync();
    }

    private async Task SaveAsync()
    {
        await _settingsService.SaveAsync(GetCurrentSettings());
        StatusMessage = "Settings saved.";
    }
}
