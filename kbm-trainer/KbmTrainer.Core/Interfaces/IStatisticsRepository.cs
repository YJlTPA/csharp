using KbmTrainer.Core.Models;

namespace KbmTrainer.Core.Interfaces;

public interface IStatisticsRepository
{
    Task<List<SessionResult>> GetAllAsync();
    Task AddAsync(SessionResult result);
    Task ClearAllAsync();
}
