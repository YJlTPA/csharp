using System.Collections.ObjectModel;
using System.Windows.Threading;
using KbmTrainer.Core.Interfaces;
using KbmTrainer.Core.Models;
using KbmTrainer.WPF.Commands;

namespace KbmTrainer.WPF.ViewModels;

public class TypingViewModel : BaseViewModel
{
    private readonly IStatisticsRepository _statisticsRepo;
    private readonly DispatcherTimer _timer;

    private TypingDictionary? _dictionary;
    private string _dictionaryName = string.Empty;
    private int _currentIndex;
    private bool _isStarted;
    private bool _isFinished;
    private DateTime _startTime;
    private TimeSpan _elapsed;
    private int _totalTyped;
    private int _errorCount;
    private int _liveWpm;
    private double _liveAccuracy;
    private string _elapsedText = "00:00";
    private int _textLength = 50;
    private double _fontSize = 20;

    public ObservableCollection<CharSlot> CharSlots { get; } = new();

    public string DictionaryName
    {
        get => _dictionaryName;
        set => SetProperty(ref _dictionaryName, value);
    }

    public bool IsStarted
    {
        get => _isStarted;
        private set => SetProperty(ref _isStarted, value);
    }

    public bool IsFinished
    {
        get => _isFinished;
        private set => SetProperty(ref _isFinished, value);
    }

    public int LiveWpm
    {
        get => _liveWpm;
        private set => SetProperty(ref _liveWpm, value);
    }

    public double LiveAccuracy
    {
        get => _liveAccuracy;
        private set => SetProperty(ref _liveAccuracy, value);
    }

    public string ElapsedText
    {
        get => _elapsedText;
        private set => SetProperty(ref _elapsedText, value);
    }

    public double FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, value);
    }

    public RelayCommand RestartCommand { get; }
    public RelayCommand ChangeDictionaryCommand { get; }

    public event Action? RequestChangeDictionary;
    public event Action<SessionResult>? SessionCompleted;

    public TypingViewModel(IStatisticsRepository statisticsRepo)
    {
        _statisticsRepo = statisticsRepo;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += OnTimerTick;

        RestartCommand = new RelayCommand(Restart);
        ChangeDictionaryCommand = new RelayCommand(() => RequestChangeDictionary?.Invoke());
    }

    public void LoadDictionary(TypingDictionary dictionary, int textLength, double fontSize = 20)
    {
        _dictionary = dictionary;
        _textLength = textLength;
        FontSize = fontSize;
        DictionaryName = dictionary.Name;
        BuildText();
    }

    private void BuildText()
    {
        _timer.Stop();
        CharSlots.Clear();
        _currentIndex = 0;
        _totalTyped = 0;
        _errorCount = 0;
        IsStarted = false;
        IsFinished = false;
        LiveWpm = 0;
        LiveAccuracy = 100;
        ElapsedText = "00:00";

        if (_dictionary == null || _dictionary.Words.Count == 0) return;

        var rng = new Random();
        var words = new List<string>();
        var count = Math.Min(_textLength, _dictionary.Words.Count * 10);

        while (words.Count < count)
        {
            var word = _dictionary.Words[rng.Next(_dictionary.Words.Count)];
            words.Add(word);
        }

        var text = string.Join(" ", words);

        foreach (var ch in text)
            CharSlots.Add(new CharSlot(ch));

        if (CharSlots.Count > 0)
            CharSlots[0].State = CharState.Current;
    }

    private void Restart()
    {
        BuildText();
    }

    public void HandleChar(char ch)
    {
        if (IsFinished || CharSlots.Count == 0) return;
        if (_currentIndex >= CharSlots.Count) return;

        if (!IsStarted)
        {
            IsStarted = true;
            _startTime = DateTime.Now;
            _timer.Start();
        }

        var slot = CharSlots[_currentIndex];
        _totalTyped++;

        if (ch == slot.Expected)
        {
            slot.State = CharState.Correct;
        }
        else
        {
            slot.State = CharState.Incorrect;
            _errorCount++;
        }

        _currentIndex++;

        if (_currentIndex < CharSlots.Count)
        {
            CharSlots[_currentIndex].State = CharState.Current;
            UpdateLiveStats();
        }
        else
        {
            FinishSession();
        }
    }

    public void HandleBackspace()
    {
        if (!IsStarted || IsFinished) return;
        if (_currentIndex == 0) return;

        // Undo current marker
        if (_currentIndex < CharSlots.Count)
            CharSlots[_currentIndex].State = CharState.Pending;

        _currentIndex--;

        var slot = CharSlots[_currentIndex];
        if (slot.State == CharState.Incorrect)
            _errorCount = Math.Max(0, _errorCount - 1);
        if (slot.State != CharState.Pending)
            _totalTyped = Math.Max(0, _totalTyped - 1);

        slot.State = CharState.Current;
        UpdateLiveStats();
    }

    private void UpdateLiveStats()
    {
        _elapsed = DateTime.Now - _startTime;
        var minutes = _elapsed.TotalMinutes;
        if (minutes > 0)
            LiveWpm = (int)((_totalTyped / 5.0) / minutes);

        if (_totalTyped > 0)
            LiveAccuracy = Math.Round(100.0 * (_totalTyped - _errorCount) / _totalTyped, 1);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        _elapsed = DateTime.Now - _startTime;
        ElapsedText = $"{(int)_elapsed.TotalMinutes:D2}:{_elapsed.Seconds:D2}";
        UpdateLiveStats();
    }

    private void FinishSession()
    {
        _timer.Stop();
        IsFinished = true;
        _elapsed = DateTime.Now - _startTime;

        var minutes = _elapsed.TotalMinutes;
        var wpm = minutes > 0 ? (int)((_totalTyped / 5.0) / minutes) : 0;
        var accuracy = _totalTyped > 0 ? Math.Round(100.0 * (_totalTyped - _errorCount) / _totalTyped, 1) : 100.0;

        var result = new SessionResult
        {
            DictionaryId = _dictionary?.Id ?? string.Empty,
            DictionaryName = _dictionary?.Name ?? string.Empty,
            Date = DateTime.Now,
            Wpm = wpm,
            Accuracy = accuracy,
            TotalChars = _totalTyped,
            ErrorChars = _errorCount,
            Duration = _elapsed
        };

        _ = _statisticsRepo.AddAsync(result);
        SessionCompleted?.Invoke(result);
    }
}
