using System.Collections.ObjectModel;
using KbmTrainer.Core.Interfaces;
using KbmTrainer.Core.Models;
using KbmTrainer.WPF.Commands;

namespace KbmTrainer.WPF.ViewModels;

public class DictionaryEditorViewModel : BaseViewModel
{
    private readonly IDictionaryRepository _repo;

    private TypingDictionary? _selectedDictionary;
    private string _selectedDictionaryName = string.Empty;
    private string _wordsText = string.Empty;
    private string _newDictionaryName = string.Empty;
    private string _statusMessage = string.Empty;

    public ObservableCollection<TypingDictionary> Dictionaries { get; } = new();

    public TypingDictionary? SelectedDictionary
    {
        get => _selectedDictionary;
        set
        {
            if (SetProperty(ref _selectedDictionary, value))
            {
                SelectedDictionaryName = value?.Name ?? string.Empty;
                WordsText = value != null ? string.Join(Environment.NewLine, value.Words) : string.Empty;
            }
        }
    }

    public string SelectedDictionaryName
    {
        get => _selectedDictionaryName;
        set => SetProperty(ref _selectedDictionaryName, value);
    }

    public string WordsText
    {
        get => _wordsText;
        set => SetProperty(ref _wordsText, value);
    }

    public string NewDictionaryName
    {
        get => _newDictionaryName;
        set => SetProperty(ref _newDictionaryName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public RelayCommand AddDictionaryCommand { get; }
    public RelayCommand DeleteDictionaryCommand { get; }
    public RelayCommand SaveDictionaryCommand { get; }
    public RelayCommand RenameDictionaryCommand { get; }

    public DictionaryEditorViewModel(IDictionaryRepository repo)
    {
        _repo = repo;

        AddDictionaryCommand = new RelayCommand(AddDictionary,
            () => !string.IsNullOrWhiteSpace(NewDictionaryName));
        DeleteDictionaryCommand = new RelayCommand(DeleteDictionary,
            () => SelectedDictionary != null);
        SaveDictionaryCommand = new RelayCommand(SaveDictionary,
            () => SelectedDictionary != null);
        RenameDictionaryCommand = new RelayCommand(RenameDictionary,
            () => SelectedDictionary != null && !string.IsNullOrWhiteSpace(SelectedDictionaryName));
    }

    public async Task LoadAsync()
    {
        var dicts = await _repo.GetAllAsync();
        Dictionaries.Clear();
        foreach (var d in dicts)
            Dictionaries.Add(d);

        if (Dictionaries.Count > 0)
            SelectedDictionary = Dictionaries[0];
    }

    private void AddDictionary()
    {
        if (string.IsNullOrWhiteSpace(NewDictionaryName)) return;

        var dict = new TypingDictionary
        {
            Name = NewDictionaryName.Trim(),
            Language = "en",
            Words = new List<string>()
        };

        Dictionaries.Add(dict);
        SelectedDictionary = dict;
        NewDictionaryName = string.Empty;
        _ = PersistAsync();
        StatusMessage = "Dictionary created.";
    }

    private void DeleteDictionary()
    {
        if (SelectedDictionary == null) return;
        Dictionaries.Remove(SelectedDictionary);
        SelectedDictionary = Dictionaries.FirstOrDefault();
        _ = PersistAsync();
        StatusMessage = "Dictionary deleted.";
    }

    private void SaveDictionary()
    {
        if (SelectedDictionary == null) return;

        var words = WordsText
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim())
            .Where(w => !string.IsNullOrEmpty(w))
            .ToList();

        SelectedDictionary.Words = words;
        _ = PersistAsync();
        StatusMessage = $"Saved {words.Count} words.";
    }

    private void RenameDictionary()
    {
        if (SelectedDictionary == null || string.IsNullOrWhiteSpace(SelectedDictionaryName)) return;
        SelectedDictionary.Name = SelectedDictionaryName.Trim();
        // Force list refresh
        var idx = Dictionaries.IndexOf(SelectedDictionary);
        if (idx >= 0)
        {
            Dictionaries.RemoveAt(idx);
            Dictionaries.Insert(idx, SelectedDictionary);
            SelectedDictionary = Dictionaries[idx];
        }
        _ = PersistAsync();
        StatusMessage = "Dictionary renamed.";
    }

    private async Task PersistAsync()
    {
        await _repo.SaveAllAsync(Dictionaries.ToList());
    }
}
