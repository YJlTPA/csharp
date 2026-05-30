using System.Windows;
using System.Windows.Input;
using KbmTrainer.Core.Services;
using KbmTrainer.WPF.ViewModels;

namespace KbmTrainer.WPF.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();

        var dictRepo = new JsonDictionaryRepository();
        var statsRepo = new JsonStatisticsRepository();
        var settingsService = new JsonSettingsService();

        _vm = new MainViewModel(dictRepo, statsRepo, settingsService);
        DataContext = _vm;

        Loaded += async (_, _) => await _vm.InitializeAsync();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (vm.CurrentView is not TypingViewModel typingVm) return;
        if (vm.IsSelectingDictionary) return;

        if (e.Key == Key.Back)
        {
            typingVm.HandleBackspace();
            e.Handled = true;
        }
    }

    private void Window_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (vm.CurrentView is not TypingViewModel typingVm) return;
        if (vm.IsSelectingDictionary) return;

        foreach (var ch in e.Text)
            typingVm.HandleChar(ch);

        e.Handled = true;
    }
}
