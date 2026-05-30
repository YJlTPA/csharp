namespace KbmTrainer.WPF.ViewModels;

public enum CharState
{
    Pending,
    Current,
    Correct,
    Incorrect
}

public class CharSlot : BaseViewModel
{
    private CharState _state;

    public char Expected { get; }
    public string ExpectedText => Expected.ToString();

    public CharState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    public CharSlot(char expected, CharState state = CharState.Pending)
    {
        Expected = expected;
        _state = state;
    }
}
