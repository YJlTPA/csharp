namespace KbmTrainer.Core.Models;

public class TypingDictionary
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public List<string> Words { get; set; } = new();
}
