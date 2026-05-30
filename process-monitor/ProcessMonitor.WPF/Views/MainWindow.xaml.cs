using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using ProcessMonitor.Core.Models;
using ProcessMonitor.WPF.ViewModels;

namespace ProcessMonitor.WPF.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;

        vm.SaveFileRequested += SaveFile;
        vm.OpenSettingsRequested += OpenSettings;

        // Build TreeView when processes refresh
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.RootProcesses))
                RebuildTree();
        };

        if (vm.RootProcesses.Count > 0)
            RebuildTree();
    }

    private void RebuildTree()
    {
        ProcessTree.Items.Clear();
        foreach (var root in _vm.RootProcesses)
            ProcessTree.Items.Add(root);
    }

    private string? SaveFile(string format)
    {
        var dialog = new SaveFileDialog
        {
            Filter = format == "csv"
                ? "CSV files (*.csv)|*.csv"
                : "Text files (*.txt)|*.txt",
            DefaultExt = format
        };
        return dialog.ShowDialog(this) == true ? dialog.FileName : null;
    }

    private void OpenSettings()
    {
        var settingsService = new ProcessMonitor.Core.Services.JsonSettingsService();
        var settingsVm = new SettingsViewModel(settingsService);
        var win = new SettingsWindow(settingsVm);
        settingsVm.RequestClose += (_, _) => win.Close();
        win.Owner = this;
        win.ShowDialog();

        if (settingsVm.DialogResult == true)
        {
            ((App)Application.Current).ApplyTheme(settingsVm.Theme);
        }
    }

    private void ProcessTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is ProcessNode node)
        {
            _vm.SelectedNode = node;
        }
    }

    private void ProcessGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_vm.SelectedNode != null)
            _vm.NavigateInto(_vm.SelectedNode);
    }
}
