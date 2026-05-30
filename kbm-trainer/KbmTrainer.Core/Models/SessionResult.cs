namespace KbmTrainer.Core.Models;

public class SessionResult
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DictionaryId { get; set; } = string.Empty;
    public string DictionaryName { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public int Wpm { get; set; }
    public double Accuracy { get; set; }
    public int TotalChars { get; set; }
    public int ErrorChars { get; set; }
    public TimeSpan Duration { get; set; }
}
