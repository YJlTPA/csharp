namespace KbmTrainer.Core.Models;

public class AppSettings
{
    public string Theme { get; set; } = "Dark";
    public double FontSize { get; set; } = 20;
    public int TextLength { get; set; } = 50;
    public bool ShowKeyboard { get; set; } = false;
}
