using System.Collections.ObjectModel;
using KbmTrainer.Core.Interfaces;
using KbmTrainer.Core.Models;
using KbmTrainer.WPF.Commands;

namespace KbmTrainer.WPF.ViewModels;

public class StatisticsViewModel : BaseViewModel
{
    private readonly IStatisticsRepository _repo;

    private int _totalSessions;
    private int _bestWpm;
    private double _averageWpm;
    private double _averageAccuracy;

    public ObservableCollection<SessionResult> Sessions { get; } = new();

    public int TotalSessions
    {
        get => _totalSessions;
        private set => SetProperty(ref _totalSessions, value);
    }

    public int BestWpm
    {
        get => _bestWpm;
        private set => SetProperty(ref _bestWpm, value);
    }

    public double AverageWpm
    {
        get => _averageWpm;
        private set => SetProperty(ref _averageWpm, value);
    }

    public double AverageAccuracy
    {
        get => _averageAccuracy;
        private set => SetProperty(ref _averageAccuracy, value);
    }

    public RelayCommand ClearHistoryCommand { get; }

    public StatisticsViewModel(IStatisticsRepository repo)
    {
        _repo = repo;
        ClearHistoryCommand = new RelayCommand(ClearHistory);
    }

    public async Task LoadAsync()
    {
        var sessions = await _repo.GetAllAsync();
        Sessions.Clear();

        // Show newest first
        foreach (var s in sessions.OrderByDescending(s => s.Date))
            Sessions.Add(s);

        TotalSessions = sessions.Count;
        BestWpm = sessions.Count > 0 ? sessions.Max(s => s.Wpm) : 0;
        AverageWpm = sessions.Count > 0 ? Math.Round(sessions.Average(s => s.Wpm), 1) : 0;
        AverageAccuracy = sessions.Count > 0 ? Math.Round(sessions.Average(s => s.Accuracy), 1) : 0;
    }

    private void ClearHistory()
    {
        _ = ClearAndReloadAsync();
    }

    private async Task ClearAndReloadAsync()
    {
        await _repo.ClearAllAsync();
        await LoadAsync();
    }
}
