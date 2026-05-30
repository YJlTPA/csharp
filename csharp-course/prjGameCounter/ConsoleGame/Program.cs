using System;
using System.Collections.Generic;
using GameCore;
using ConsoleGame;

// ─── Главный цикл игры ────────────────────────────────────────────────────

bool playAgain = true;

while (playAgain)
{
    ConsoleUI.ShowWelcome();

    DifficultyLevel difficulty = ConsoleUI.ChooseDifficulty();
    List<OperationType> operations = ConsoleUI.ChooseOperations();

    var session = new GameSession(difficulty, operations,
        totalQuestions: 10, timePerQuestion: 15);

    while (!session.IsFinished)
    {
        GameQuestion question = session.NextQuestion();
        ConsoleUI.DrawQuestionScreen(session, question);

        string? input = ConsoleUI.ReadAnswerWithTimer(session.TimePerQuestion, out bool timedOut);

        bool correct;
        int coinChange;

        if (timedOut)
        {
            // время вышло — засчитываем как неверный ответ
            (correct, coinChange) = session.SubmitTimeout();
            ConsoleUI.ShowResult(correct, coinChange, null, question);
        }
        else if (int.TryParse(input, out int answer))
        {
            (correct, coinChange) = session.SubmitAnswer(answer);
            ConsoleUI.ShowResult(correct, coinChange, input, question);
        }
        else
        {
            // пустой ввод или не-число
            (correct, coinChange) = session.SubmitTimeout();
            ConsoleUI.ShowResult(correct, coinChange, null, question);
        }
    }

    ConsoleUI.ShowFinalResults(session);
    playAgain = ConsoleUI.AskPlayAgain();
}
