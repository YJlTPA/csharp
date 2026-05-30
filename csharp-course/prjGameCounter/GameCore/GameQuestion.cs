namespace GameCore;

/// <summary>
/// Одна задача в игре: два операнда, операция и правильный ответ.
/// </summary>
public class GameQuestion
{
    public int Operand1 { get; }
    public int Operand2 { get; }
    public OperationType Operation { get; }
    public int CorrectAnswer { get; }

    public GameQuestion(int operand1, int operand2, OperationType operation)
    {
        Operand1  = operand1;
        Operand2  = operand2;
        Operation = operation;
        CorrectAnswer = Compute();
    }

    private int Compute() => Operation switch
    {
        OperationType.Addition       => Operand1 + Operand2,
        OperationType.Subtraction    => Operand1 - Operand2,
        OperationType.Multiplication => Operand1 * Operand2,
        OperationType.Division       => Operand1 / Operand2,
        _                            => throw new InvalidOperationException("Unknown operation")
    };

    public string Symbol => Operation switch
    {
        OperationType.Addition       => "+",
        OperationType.Subtraction    => "-",
        OperationType.Multiplication => "*",
        OperationType.Division       => "/",
        _                            => "?"
    };

    public override string ToString() => $"{Operand1} {Symbol} {Operand2} = ?";
}
