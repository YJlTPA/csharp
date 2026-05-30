using System;

namespace GameCore;

/// <summary>
/// Система монет с прогрессивной наградой/штрафом за серию ответов.
///
/// Серия правильных: +1, +2, +4, +8, ...
/// Серия неправильных: -1, -2, -4, -8, ...
/// При переключении серия сбрасывается.
/// </summary>
public class CoinSystem
{
    public int Coins { get; private set; } = 0;
    public int CorrectStreak { get; private set; } = 0;
    public int WrongStreak   { get; private set; } = 0;

    /// <summary>
    /// Обработать ответ. Возвращает изменение монет (положительное или отрицательное).
    /// </summary>
    public int ProcessAnswer(bool isCorrect)
    {
        int change;
        if (isCorrect)
        {
            WrongStreak = 0;
            CorrectStreak++;
            change = (int)Math.Pow(2, CorrectStreak - 1); // 1, 2, 4, 8 ...
            Coins += change;
        }
        else
        {
            CorrectStreak = 0;
            WrongStreak++;
            change = -(int)Math.Pow(2, WrongStreak - 1);  // -1, -2, -4, -8 ...
            Coins += change;
        }
        return change;
    }

    public void Reset()
    {
        Coins = 0;
        CorrectStreak = 0;
        WrongStreak = 0;
    }
}
