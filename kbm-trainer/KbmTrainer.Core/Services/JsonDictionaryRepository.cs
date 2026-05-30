using System.Text.Json;
using KbmTrainer.Core.Interfaces;
using KbmTrainer.Core.Models;

namespace KbmTrainer.Core.Services;

public class JsonDictionaryRepository : IDictionaryRepository
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public JsonDictionaryRepository()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "KbmTrainer");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "dictionaries.json");
    }

    public async Task<List<TypingDictionary>> GetAllAsync()
    {
        if (!File.Exists(_filePath))
        {
            var seeded = GetSeedDictionaries();
            await SaveAllAsync(seeded);
            return seeded;
        }

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<TypingDictionary>>(json, _jsonOptions) ?? new();
    }

    public async Task SaveAllAsync(List<TypingDictionary> dictionaries)
    {
        var json = JsonSerializer.Serialize(dictionaries, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    private static List<TypingDictionary> GetSeedDictionaries()
    {
        return new List<TypingDictionary>
        {
            new TypingDictionary
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Английские слова (топ 200)",
                Language = "en",
                Words = new List<string>
                {
                    "the", "be", "to", "of", "and", "a", "in", "that", "have", "it",
                    "for", "not", "on", "with", "he", "as", "you", "do", "at", "this",
                    "but", "his", "by", "from", "they", "we", "say", "her", "she", "or",
                    "an", "will", "my", "one", "all", "would", "there", "their", "what", "so",
                    "up", "out", "if", "about", "who", "get", "which", "go", "me", "when",
                    "make", "can", "like", "time", "no", "just", "him", "know", "take", "people",
                    "into", "year", "your", "good", "some", "could", "them", "see", "other", "than",
                    "then", "now", "look", "only", "come", "its", "over", "think", "also", "back"
                }
            },
            new TypingDictionary
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Русские слова (топ 200)",
                Language = "ru",
                Words = new List<string>
                {
                    "и", "в", "не", "он", "на", "я", "что", "тот", "быть", "с",
                    "а", "весь", "это", "как", "она", "по", "но", "они", "к", "у",
                    "ты", "из", "мы", "за", "вы", "так", "же", "от", "сказать", "этот",
                    "который", "мочь", "человек", "о", "один", "ещё", "бы", "такой", "только", "себя",
                    "своё", "другой", "думать", "знать", "стать", "большой", "даже", "эти", "ну", "под",
                    "где", "дело", "есть", "сам", "раз", "чтобы", "два", "там", "чем", "глаз",
                    "жизнь", "первый", "день", "туда", "во", "ничто", "потом", "очень", "со", "хотеть",
                    "ли", "при", "го", "голова", "надо", "без", "видеть", "идти", "теперь", "тоже"
                }
            },
            new TypingDictionary
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Цифры и символы",
                Language = "en",
                Words = new List<string>
                {
                    "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
                    "10", "20", "50", "100", "42", "99", "123", "456", "789", "2024",
                    "!", "@", "#", "$", "%", "^", "&", "*", "()", "[]",
                    "{}", "<>", "+=", "-=", "*=", "/=", "==", "!=", ">=", "<=",
                    "0.5", "3.14", "1.0", "100%", "$99", "#1", "@user", "A1", "B2", "C3"
                }
            },
            new TypingDictionary
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Программирование",
                Language = "en",
                Words = new List<string>
                {
                    "var", "let", "const", "function", "return", "if", "else", "for", "while", "do",
                    "class", "interface", "abstract", "public", "private", "protected", "static", "void", "int", "string",
                    "bool", "null", "true", "false", "new", "this", "base", "override", "virtual", "async",
                    "await", "try", "catch", "finally", "throw", "using", "namespace", "import", "export", "default",
                    "switch", "case", "break", "continue", "typeof", "instanceof", "List<T>", "Task<T>", "IEnumerable", "Dictionary",
                    "Console.WriteLine", "StringBuilder", "DateTime.Now", "string.Format", "Math.Abs", "Array.Sort", "File.ReadAll", "Path.Combine", "Guid.NewGuid", "Environment.GetFolder"
                }
            }
        };
    }
}
