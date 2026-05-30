using System;
using System.Collections.Generic;

namespace GameCore;

/// <summary>
/// Генератор задач с учётом уровня сложности и допустимых операций.
/// </summary>
public class ExpressionGenerator
{
    private readonly Random _rng = new Random();

    public GameQuestion Generate(DifficultyLevel difficulty, IReadOnlyList<OperationType> operations)
    {
        int max = (int)difficulty;
        var op = operations[_rng.Next(operations.Count)];

        return op switch
        {
            OperationType.Addition       => GenAddition(max),
            OperationType.Subtraction    => GenSubtraction(max),
            OperationType.Multiplication => GenMultiplication(max),
            OperationType.Division       => GenDivision(max),
            _                            => GenAddition(max)
        };
    }

    // a + b, оба числа в диапазоне [1, max]
    private GameQuestion GenAddition(int max)
    {
        int a = _rng.Next(1, max + 1);
        int b = _rng.Next(1, max + 1);
        return new GameQuestion(a, b, OperationType.Addition);
    }

    // a - b, гарантируем a >= b (нет отрицательного ответа)
    private GameQuestion GenSubtraction(int max)
    {
        int a = _rng.Next(2, max + 1);
        int b = _rng.Next(1, a);          // b < a
        return new GameQuestion(a, b, OperationType.Subtraction);
    }

    // Таблица умножения до sqrt(max)
    private GameQuestion GenMultiplication(int max)
    {
        int tableMax = Math.Max(2, (int)Math.Sqrt(max));
        int a = _rng.Next(2, tableMax + 1);
        int b = _rng.Next(2, tableMax + 1);
        return new GameQuestion(a, b, OperationType.Multiplication);
    }

    // Деление без остатка: сначала выбираем делитель q и частное r,
    // затем делимое = q * r
    private GameQuestion GenDivision(int max)
    {
        int tableMax = Math.Max(2, (int)Math.Sqrt(max));
        int divisor  = _rng.Next(2, tableMax + 1);   // делитель
        int quotient = _rng.Next(2, tableMax + 1);   // частное
        int dividend = divisor * quotient;             // делимое
        return new GameQuestion(dividend, divisor, OperationType.Division);
    }
}
