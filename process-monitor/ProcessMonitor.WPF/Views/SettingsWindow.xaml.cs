using System.Windows;
using ProcessMonitor.WPF.ViewModels;

namespace ProcessMonitor.WPF.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
