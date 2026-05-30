using System.Windows;
using ProcessMonitor.Core.Services;
using ProcessMonitor.WPF.ViewModels;
using ProcessMonitor.WPF.Views;

namespace ProcessMonitor.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settingsService = new JsonSettingsService();
        var settings = settingsService.Load();

        // Apply theme before window opens
        ApplyTheme(settings.Theme);

        var processProvider = new WindowsProcessProvider();
        var exporter = new CsvReportExporter();
        var vm = new MainViewModel(processProvider, settingsService, exporter);

        var window = new MainWindow(vm);
        window.Show();
    }

    public void ApplyTheme(string theme)
    {
        var dict = new ResourceDictionary();
        dict.Source = theme == "Light"
            ? new Uri("Themes/LightTheme.xaml", UriKind.Relative)
            : new Uri("Themes/DarkTheme.xaml", UriKind.Relative);

        // Replace first merged dictionary (the theme)
        Resources.MergedDictionaries[0] = dict;
    }
}
