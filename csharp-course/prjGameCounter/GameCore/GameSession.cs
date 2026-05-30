using System;
using System.Collections.Generic;

namespace GameCore;

/// <summary>
/// Управляет одной игровой сессией: генерирует вопросы,
/// считает очки и монеты.
/// </summary>
public class GameSession
{
    private readonly ExpressionGenerator _generator = new ExpressionGenerator();
    private readonly CoinSystem _coins = new CoinSystem();

    public DifficultyLevel          Difficulty         { get; }
    public IReadOnlyList<OperationType> AllowedOperations { get; }
    public int TotalQuestions  { get; }
    public int TimePerQuestion { get; }   // секунды на вопрос

    public int QuestionNumber { get; private set; } = 0;
    public int CorrectCount   { get; private set; } = 0;
    public int WrongCount     { get; private set; } = 0;
    public int Coins          => _coins.Coins;
    public int CorrectStreak  => _coins.CorrectStreak;
    public int WrongStreak    => _coins.WrongStreak;

    public GameQuestion? CurrentQuestion { get; private set; }

    public bool IsFinished => QuestionNumber >= TotalQuestions;

    public GameSession(
        DifficultyLevel difficulty,
        IReadOnlyList<OperationType> operations,
        int totalQuestions  = 10,
        int timePerQuestion = 15)
    {
        Difficulty         = difficulty;
        AllowedOperations  = operations;
        TotalQuestions     = totalQuestions;
        TimePerQuestion    = timePerQuestion;
    }

    /// <summary>
    /// Создать следующий вопрос и вернуть его.
    /// </summary>
    public GameQuestion NextQuestion()
    {
        QuestionNumber++;
        CurrentQuestion = _generator.Generate(Difficulty, AllowedOperations);
        return CurrentQuestion;
    }

    /// <summary>
    /// Принять ответ пользователя. Возвращает (isCorrect, coinChange).
    /// </summary>
    public (bool isCorrect, int coinChange) SubmitAnswer(int answer)
    {
        if (CurrentQuestion == null)
            throw new InvalidOperationException("Вопрос не задан");

        bool correct = answer == CurrentQuestion.CorrectAnswer;
        if (correct) CorrectCount++; else WrongCount++;
        int change = _coins.ProcessAnswer(correct);
        return (correct, change);
    }

    /// <summary>
    /// Засчитать таймаут — как неправильный ответ.
    /// </summary>
    public (bool isCorrect, int coinChange) SubmitTimeout()
    {
        WrongCount++;
        int change = _coins.ProcessAnswer(false);
        return (false, change);
    }

    /// <summary>
    /// Текстовое название уровня для отображения.
    /// </summary>
    public static string DifficultyName(DifficultyLevel d) => d switch
    {
        DifficultyLevel.Easy   => "Лёгкий",
        DifficultyLevel.Medium => "Средний",
        DifficultyLevel.Hard   => "Сложный",
        DifficultyLevel.Expert => "Эксперт",
        _                      => "?"
    };
}
