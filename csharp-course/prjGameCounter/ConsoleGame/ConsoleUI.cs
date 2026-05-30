using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using GameCore;

namespace ConsoleGame;

/// <summary>
/// Весь пользовательский интерфейс в консоли: цветной вывод,
/// обратный отсчёт, ввод с таймаутом.
/// </summary>
public static class ConsoleUI
{
    // Строки экрана для текущего вопроса
    private static int _timerRow;
    private static int _inputRow;
    private static int _inputCol;

    private const int BarWidth = 28;

    // ─── Вступительный экран ───────────────────────────────────────────────

    private static void SafeClear()
    {
        try { Console.Clear(); } catch { /* не интерактивный терминал */ }
    }

    public static void ShowWelcome()
    {
        Console.OutputEncoding = Encoding.UTF8;
        SafeClear();
        try { Console.CursorVisible = false; } catch { }

        Write(ConsoleColor.Cyan,
            "╔════════════════════════════════════════════╗\n" +
            "║        У С Т Н Ы Й   С Ч Ё Т             ║\n" +
            "╚════════════════════════════════════════════╝\n");
        Console.WriteLine();
    }

    // ─── Выбор настроек ───────────────────────────────────────────────────

    public static DifficultyLevel ChooseDifficulty()
    {
        Write(ConsoleColor.Yellow, "  Уровень сложности:\n");
        Write(ConsoleColor.Gray,
            "  1 - Лёгкий   (числа до 20)\n" +
            "  2 - Средний  (числа до 40)\n" +
            "  3 - Сложный  (числа до 60)\n" +
            "  4 - Эксперт  (числа до 100)\n");
        Console.WriteLine();

        while (true)
        {
            Console.Write("  Ваш выбор: ");
            var key = Console.ReadKey(true);
            switch (key.KeyChar)
            {
                case '1': Console.WriteLine("1 - Лёгкий\n");   return DifficultyLevel.Easy;
                case '2': Console.WriteLine("2 - Средний\n");  return DifficultyLevel.Medium;
                case '3': Console.WriteLine("3 - Сложный\n");  return DifficultyLevel.Hard;
                case '4': Console.WriteLine("4 - Эксперт\n");  return DifficultyLevel.Expert;
            }
        }
    }

    public static List<OperationType> ChooseOperations()
    {
        Write(ConsoleColor.Yellow, "  Операции (выберите несколько, затем Enter):\n");
        Write(ConsoleColor.Gray,
            "  1 - Сложение  (+)\n" +
            "  2 - Вычитание (-)\n" +
            "  3 - Умножение (*)\n" +
            "  4 - Деление   (/)\n");
        Console.WriteLine();

        var selected = new HashSet<OperationType>();
        Console.Write("  Выбрано: ");
        int labelCol = 10;

        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter)
            {
                if (selected.Count == 0) selected.Add(OperationType.Addition);
                Console.WriteLine();
                Console.WriteLine();
                break;
            }
            switch (key.KeyChar)
            {
                case '1': selected.Add(OperationType.Addition);       break;
                case '2': selected.Add(OperationType.Subtraction);    break;
                case '3': selected.Add(OperationType.Multiplication); break;
                case '4': selected.Add(OperationType.Division);       break;
            }
            // перерисовать список
            Console.SetCursorPosition(labelCol, Console.CursorTop);
            Console.Write(new string(' ', 20));
            Console.SetCursorPosition(labelCol, Console.CursorTop);

            var names = new List<string>();
            if (selected.Contains(OperationType.Addition))       names.Add("+");
            if (selected.Contains(OperationType.Subtraction))    names.Add("-");
            if (selected.Contains(OperationType.Multiplication)) names.Add("*");
            if (selected.Contains(OperationType.Division))       names.Add("/");
            Write(ConsoleColor.White, string.Join("  ", names));
        }

        return new List<OperationType>(selected);
    }

    // ─── Экран вопроса ────────────────────────────────────────────────────

    public static void DrawQuestionScreen(GameSession session, GameQuestion question)
    {
        SafeClear();
        try { Console.CursorVisible = false; } catch { }

        // Шапка
        Write(ConsoleColor.Cyan,
            "╔════════════════════════════════════════════╗\n" +
            "║        У С Т Н Ы Й   С Ч Ё Т             ║\n" +
            "╚════════════════════════════════════════════╝\n");
        Console.WriteLine();

        // Строка статуса
        Console.Write("  ");
        Write(ConsoleColor.DarkCyan,  $"[{GameSession.DifficultyName(session.Difficulty)}]");
        Console.Write("  ");
        Write(ConsoleColor.White,     $"Вопрос {session.QuestionNumber}/{session.TotalQuestions}");
        Console.Write("   ");
        Write(ConsoleColor.Green,     $"[+] {session.CorrectCount}");
        Console.Write("  ");
        Write(ConsoleColor.Red,       $"[-] {session.WrongCount}");
        Console.Write("  ");
        Write(ConsoleColor.Yellow,    $"$ {session.Coins}");
        Console.WriteLine();
        Console.WriteLine();

        // Таймер (запомним строку)
        _timerRow = Console.CursorTop;
        RedrawTimer(session.TimePerQuestion, session.TimePerQuestion);
        Console.WriteLine();
        Console.WriteLine();

        // Вопрос
        Console.Write("  ");
        Write(ConsoleColor.White, question.ToString());
        Console.WriteLine();
        Console.WriteLine();

        // Строка ввода
        _inputRow = Console.CursorTop;
        Console.Write("  Ваш ответ: ");
        _inputCol = Console.CursorLeft;   // = 14
        Console.CursorVisible = true;
    }

    // ─── Ввод с таймером ──────────────────────────────────────────────────

    /// <summary>
    /// Читает ввод пользователя с обратным отсчётом.
    /// Возвращает строку или null при истечении времени.
    /// </summary>
    public static string? ReadAnswerWithTimer(int totalSeconds, out bool timedOut)
    {
        var input  = new StringBuilder();
        var sw     = Stopwatch.StartNew();
        int lastSec = totalSeconds + 1;
        timedOut = false;

        while (true)
        {
            int remaining = totalSeconds - (int)sw.Elapsed.TotalSeconds;

            if (remaining <= 0)
            {
                timedOut = true;
                Console.CursorVisible = false;
                return null;
            }

            if (remaining != lastSec)
            {
                RedrawTimerAt(_timerRow, remaining, totalSeconds);
                lastSec = remaining;
            }

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.CursorVisible = false;
                    return input.ToString();
                }

                if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input.Remove(input.Length - 1, 1);
                }
                else if ((char.IsDigit(key.KeyChar) || (key.KeyChar == '-' && input.Length == 0))
                         && input.Length < 10)
                {
                    input.Append(key.KeyChar);
                }

                // перерисовать поле ввода
                Console.SetCursorPosition(_inputCol, _inputRow);
                Console.Write(new string(' ', 15));
                Console.SetCursorPosition(_inputCol, _inputRow);
                Console.Write(input.ToString());
            }
            else
            {
                System.Threading.Thread.Sleep(40);
            }
        }
    }

    // ─── Результат ────────────────────────────────────────────────────────

    public static void ShowResult(bool correct, int coinChange,
        string? userInput, GameQuestion question)
    {
        int row = _inputRow + 2;
        Console.SetCursorPosition(0, row);

        if (correct)
        {
            Write(ConsoleColor.Green,  "  ВЕРНО!\n");
        }
        else if (userInput == null)
        {
            Write(ConsoleColor.DarkRed, $"  ВРЕМЯ ВЫШЛО! Ответ: {question.CorrectAnswer}\n");
        }
        else
        {
            Write(ConsoleColor.Red,    $"  НЕВЕРНО! Правильный ответ: {question.CorrectAnswer}\n");
        }

        if (coinChange > 0)
            Write(ConsoleColor.Yellow, $"  +{coinChange} монет (серия: {coinChange} подряд)\n");
        else if (coinChange < 0)
            Write(ConsoleColor.DarkYellow, $"  {coinChange} монет\n");

        System.Threading.Thread.Sleep(1600);
    }

    // ─── Итоги ────────────────────────────────────────────────────────────

    public static void ShowFinalResults(GameSession session)
    {
        SafeClear();
        try { Console.CursorVisible = true; } catch { }

        Write(ConsoleColor.Cyan,
            "╔════════════════════════════════════════════╗\n" +
            "║               И Т О Г И                   ║\n" +
            "╚════════════════════════════════════════════╝\n");
        Console.WriteLine();

        Console.WriteLine($"  Уровень:          {GameSession.DifficultyName(session.Difficulty)}");
        Console.WriteLine($"  Всего вопросов:   {session.TotalQuestions}");
        Console.WriteLine();

        Write(ConsoleColor.Green,  $"  [+] Правильных:   {session.CorrectCount}\n");
        Write(ConsoleColor.Red,    $"  [-] Неверных:     {session.WrongCount}\n");
        Console.WriteLine();

        double pct = session.TotalQuestions > 0
            ? session.CorrectCount * 100.0 / session.TotalQuestions : 0;

        ConsoleColor pctColor = pct >= 80 ? ConsoleColor.Green
                              : pct >= 50 ? ConsoleColor.Yellow
                              : ConsoleColor.Red;
        Write(pctColor, $"  Точность:         {pct:F1}%\n");
        Console.WriteLine();

        Write(ConsoleColor.Yellow, $"  Монеты:           {session.Coins}\n");
        Console.WriteLine();

        string verdict = pct == 100 ? "Превосходно!"
                       : pct >= 80  ? "Отлично!"
                       : pct >= 60  ? "Хорошо!"
                       : pct >= 40  ? "Нужно практиковаться!"
                       :              "Не сдавайся!";

        Write(ConsoleColor.Cyan, $"  {verdict}\n");
        Console.WriteLine();
        Write(ConsoleColor.Gray, "  Нажмите Enter...\n");
        while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
    }

    public static bool AskPlayAgain()
    {
        Console.WriteLine();
        Console.Write("  Сыграть ещё раз? (1 = да / 2 = нет): ");
        while (true)
        {
            var k = Console.ReadKey(true);
            if (k.KeyChar == '1') { Console.WriteLine("Да\n"); return true;  }
            if (k.KeyChar == '2') { Console.WriteLine("Нет\n"); return false; }
        }
    }

    // ─── Вспомогательные ──────────────────────────────────────────────────

    private static void RedrawTimer(int remaining, int total)
    {
        RedrawTimerAt(Console.CursorTop, remaining, total);
    }

    private static void RedrawTimerAt(int row, int remaining, int total)
    {
        int savedLeft = Console.CursorLeft;
        int savedTop  = Console.CursorTop;
        Console.CursorVisible = false;

        Console.SetCursorPosition(0, row);

        int filled = total > 0 ? (int)((double)remaining / total * BarWidth) : 0;
        filled = Math.Max(0, Math.Min(BarWidth, filled));

        ConsoleColor color = remaining <= 5 ? ConsoleColor.Red
                           : remaining <= 10 ? ConsoleColor.Yellow
                           : ConsoleColor.Green;

        Write(color, $"  Время: {remaining,3} сек  [");
        Write(color, new string('█', filled));
        Write(ConsoleColor.DarkGray, new string('░', BarWidth - filled));
        Write(color, "]");

        Console.SetCursorPosition(savedLeft, savedTop);
        Console.CursorVisible = true;
    }

    private static void Write(ConsoleColor color, string text)
    {
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }
}
