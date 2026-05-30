using KbmTrainer.Core.Models;

namespace KbmTrainer.Core.Interfaces;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync();
    Task SaveAsync(AppSettings settings);
}
