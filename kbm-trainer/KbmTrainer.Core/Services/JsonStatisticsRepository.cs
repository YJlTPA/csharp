using System.Text.Json;
using KbmTrainer.Core.Interfaces;
using KbmTrainer.Core.Models;

namespace KbmTrainer.Core.Services;

public class JsonStatisticsRepository : IStatisticsRepository
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public JsonStatisticsRepository()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "KbmTrainer");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "statistics.json");
    }

    public async Task<List<SessionResult>> GetAllAsync()
    {
        if (!File.Exists(_filePath))
            return new List<SessionResult>();

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<SessionResult>>(json, _jsonOptions) ?? new();
    }

    public async Task AddAsync(SessionResult result)
    {
        var all = await GetAllAsync();
        all.Add(result);
        var json = JsonSerializer.Serialize(all, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task ClearAllAsync()
    {
        var json = JsonSerializer.Serialize(new List<SessionResult>(), _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
