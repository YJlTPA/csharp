using ProcessMonitor.Core.Models;

namespace ProcessMonitor.Core.Interfaces;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
