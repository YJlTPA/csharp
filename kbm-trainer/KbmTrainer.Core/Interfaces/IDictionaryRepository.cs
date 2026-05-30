using KbmTrainer.Core.Models;

namespace KbmTrainer.Core.Interfaces;

public interface IDictionaryRepository
{
    Task<List<TypingDictionary>> GetAllAsync();
    Task SaveAllAsync(List<TypingDictionary> dictionaries);
}
