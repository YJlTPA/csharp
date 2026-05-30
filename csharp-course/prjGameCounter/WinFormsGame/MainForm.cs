using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GameCore;

namespace WinFormsGame;

/// <summary>
/// Главная форма игры "Устный счёт".
/// Все элементы управления создаются программно — без файла Designer.
/// </summary>
public class MainForm : Form
{
    // ── Настройки ────────────────────────────────────────────────────────
    private Panel       pnlSettings   = null!;
    private RadioButton rbEasy        = null!;
    private RadioButton rbMedium      = null!;
    private RadioButton rbHard        = null!;
    private RadioButton rbExpert      = null!;
    private CheckBox    cbAdd         = null!;
    private CheckBox    cbSub         = null!;
    private CheckBox    cbMul         = null!;
    private CheckBox    cbDiv         = null!;
    private NumericUpDown numQuestions = null!;
    private NumericUpDown numTime      = null!;
    private Button      btnStart      = null!;

    // ── Игровое поле ─────────────────────────────────────────────────────
    private Panel       pnlGame       = null!;
    private Label       lblQuestionNum = null!;
    private Label       lblScore       = null!;
    private Label       lblCoins       = null!;
    private ProgressBar pbarTimer      = null!;
    private Label       lblTimerSec    = null!;
    private Label       lblQuestion    = null!;
    private TextBox     txtAnswer      = null!;
    private Button      btnSubmit      = null!;
    private Label       lblMessage     = null!;
    private Button      btnNewGame     = null!;

    // ── Логика ───────────────────────────────────────────────────────────
    private System.Windows.Forms.Timer _timer = null!;
    private GameSession?   _session;
    private GameQuestion?  _currentQuestion;
    private int            _timeLeft;

    // ── Цвета ────────────────────────────────────────────────────────────
    private static readonly Color BgColor      = Color.FromArgb(230, 240, 255);
    private static readonly Color PanelColor   = Color.FromArgb(210, 225, 250);
    private static readonly Color AccentColor  = Color.FromArgb(50,  100, 200);
    private static readonly Color GreenColor   = Color.FromArgb(0,   140,  60);
    private static readonly Color RedColor     = Color.FromArgb(200,  40,  40);
    private static readonly Color GoldColor    = Color.FromArgb(180, 130,   0);

    // ─────────────────────────────────────────────────────────────────────
    public MainForm()
    {
        BuildUI();
        ShowSettingsPanel();
    }

    // ─── Построение интерфейса ────────────────────────────────────────────

    private void BuildUI()
    {
        Text            = "Устный Счёт";
        Size            = new Size(480, 620);
        MinimumSize     = new Size(480, 620);
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox     = false;
        BackColor       = BgColor;
        Font            = new Font("Segoe UI", 10f);

        // ── Заголовок ──────────────────────────────────────────────────
        var lblTitle = new Label
        {
            Text      = "УСТНЫЙ СЧЁТ",
            Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = AccentColor,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock      = DockStyle.Top,
            Height    = 56
        };
        Controls.Add(lblTitle);

        // ── Панель настроек ────────────────────────────────────────────
        BuildSettingsPanel();

        // ── Панель игры ────────────────────────────────────────────────
        BuildGamePanel();

        // ── Таймер ────────────────────────────────────────────────────
        _timer          = new System.Windows.Forms.Timer();
        _timer.Interval = 1000;
        _timer.Tick    += Timer_Tick;
    }

    private void BuildSettingsPanel()
    {
        pnlSettings = new Panel
        {
            Location  = new Point(20, 65),
            Size      = new Size(430, 440),
            BackColor = PanelColor
        };
        Controls.Add(pnlSettings);

        int y = 16;

        // Заголовок
        pnlSettings.Controls.Add(SectionLabel("Уровень сложности", 16, y));
        y += 30;

        rbEasy   = RadioBtn("Лёгкий  (до 20)",  26, y, true);  y += 28;
        rbMedium = RadioBtn("Средний (до 40)",  26, y);         y += 28;
        rbHard   = RadioBtn("Сложный (до 60)",  26, y);         y += 28;
        rbExpert = RadioBtn("Эксперт (до 100)", 26, y);         y += 36;

        pnlSettings.Controls.Add(rbEasy);
        pnlSettings.Controls.Add(rbMedium);
        pnlSettings.Controls.Add(rbHard);
        pnlSettings.Controls.Add(rbExpert);

        // Операции
        pnlSettings.Controls.Add(SectionLabel("Типы операций", 16, y));
        y += 30;

        cbAdd = CheckBtn("Сложение   (+)", 26, y, true); y += 28;
        cbSub = CheckBtn("Вычитание  (-)", 26, y, true); y += 28;
        cbMul = CheckBtn("Умножение  (*)", 26, y);        y += 28;
        cbDiv = CheckBtn("Деление    (/)", 26, y);        y += 36;

        pnlSettings.Controls.Add(cbAdd);
        pnlSettings.Controls.Add(cbSub);
        pnlSettings.Controls.Add(cbMul);
        pnlSettings.Controls.Add(cbDiv);

        // Параметры
        pnlSettings.Controls.Add(SectionLabel("Параметры", 16, y));
        y += 30;

        var lblQ = new Label { Text = "Количество вопросов:", Location = new Point(26, y), AutoSize = true };
        numQuestions = new NumericUpDown
        {
            Location = new Point(210, y - 2),
            Size     = new Size(70, 26),
            Minimum  = 5, Maximum = 50, Value = 10, Increment = 5
        };
        y += 32;

        var lblT = new Label { Text = "Время на вопрос (сек):", Location = new Point(26, y), AutoSize = true };
        numTime = new NumericUpDown
        {
            Location = new Point(210, y - 2),
            Size     = new Size(70, 26),
            Minimum  = 5, Maximum = 60, Value = 15, Increment = 5
        };
        y += 44;

        pnlSettings.Controls.Add(lblQ);
        pnlSettings.Controls.Add(numQuestions);
        pnlSettings.Controls.Add(lblT);
        pnlSettings.Controls.Add(numTime);

        btnStart = new Button
        {
            Text      = "НАЧАТЬ ИГРУ",
            Location  = new Point(110, y),
            Size      = new Size(210, 42),
            BackColor = AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 12f, FontStyle.Bold)
        };
        btnStart.FlatAppearance.BorderSize = 0;
        btnStart.Click += BtnStart_Click;
        pnlSettings.Controls.Add(btnStart);
    }

    private void BuildGamePanel()
    {
        pnlGame = new Panel
        {
            Location  = new Point(20, 65),
            Size      = new Size(430, 510),
            BackColor = PanelColor,
            Visible   = false
        };
        Controls.Add(pnlGame);

        // Строка статуса
        lblQuestionNum = MakeLabel("Вопрос 0/0", 16, 16, 130, 24, FontStyle.Bold);
        lblScore       = MakeLabel("[+] 0  [-] 0", 160, 16, 140, 24);
        lblCoins       = MakeLabel("$ 0", 314, 16, 100, 24);
        lblCoins.ForeColor = GoldColor;
        lblCoins.Font      = new Font("Segoe UI", 10f, FontStyle.Bold);

        pnlGame.Controls.Add(lblQuestionNum);
        pnlGame.Controls.Add(lblScore);
        pnlGame.Controls.Add(lblCoins);

        // Разделитель
        var sep = new Panel { Location = new Point(16, 46), Size = new Size(398, 2), BackColor = AccentColor };
        pnlGame.Controls.Add(sep);

        // Таймер
        pbarTimer = new ProgressBar
        {
            Location    = new Point(16, 58),
            Size        = new Size(320, 22),
            Minimum     = 0, Maximum = 100, Value = 100,
            ForeColor   = GreenColor
        };
        lblTimerSec = MakeLabel("15", 346, 58, 60, 22, FontStyle.Bold);
        lblTimerSec.ForeColor = GreenColor;

        pnlGame.Controls.Add(pbarTimer);
        pnlGame.Controls.Add(lblTimerSec);

        // Вопрос
        lblQuestion = new Label
        {
            Text      = "Нажмите «Начать»",
            Location  = new Point(16, 100),
            Size      = new Size(398, 100),
            Font      = new Font("Segoe UI", 28f, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 30, 80),
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlGame.Controls.Add(lblQuestion);

        // Ввод ответа
        txtAnswer = new TextBox
        {
            Location  = new Point(80, 215),
            Size      = new Size(200, 34),
            Font      = new Font("Segoe UI", 14f),
            TextAlign = HorizontalAlignment.Center
        };
        txtAnswer.KeyDown += TxtAnswer_KeyDown;
        pnlGame.Controls.Add(txtAnswer);

        btnSubmit = new Button
        {
            Text      = "Ответить",
            Location  = new Point(292, 213),
            Size      = new Size(110, 36),
            BackColor = AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold)
        };
        btnSubmit.FlatAppearance.BorderSize = 0;
        btnSubmit.Click += BtnSubmit_Click;
        pnlGame.Controls.Add(btnSubmit);

        // Сообщение-результат
        lblMessage = new Label
        {
            Text      = "",
            Location  = new Point(16, 262),
            Size      = new Size(398, 60),
            Font      = new Font("Segoe UI", 13f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlGame.Controls.Add(lblMessage);

        // Итоговая панель (скрытая)
        BuildResultsArea();

        // Новая игра
        btnNewGame = new Button
        {
            Text      = "Вернуться в меню",
            Location  = new Point(120, 470),
            Size      = new Size(190, 34),
            BackColor = Color.FromArgb(100, 100, 130),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Visible   = false
        };
        btnNewGame.FlatAppearance.BorderSize = 0;
        btnNewGame.Click += (_, _) => ShowSettingsPanel();
        pnlGame.Controls.Add(btnNewGame);
    }

    // Панель итогов (внутри pnlGame, скрытая до конца игры)
    private Label  _lblResultTitle    = null!;
    private Label  _lblResultDetails  = null!;

    private void BuildResultsArea()
    {
        _lblResultTitle = new Label
        {
            Location  = new Point(16, 335),
            Size      = new Size(398, 40),
            Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible   = false
        };
        _lblResultDetails = new Label
        {
            Location  = new Point(16, 380),
            Size      = new Size(398, 80),
            Font      = new Font("Segoe UI", 11f),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible   = false
        };
        pnlGame.Controls.Add(_lblResultTitle);
        pnlGame.Controls.Add(_lblResultDetails);
    }

    // ─── Состояния ────────────────────────────────────────────────────────

    private void ShowSettingsPanel()
    {
        _timer.Stop();
        pnlSettings.Visible = true;
        pnlGame.Visible     = false;
    }

    private void ShowGamePanel()
    {
        pnlSettings.Visible     = false;
        pnlGame.Visible         = true;
        lblMessage.Text         = "";
        _lblResultTitle.Visible = false;
        _lblResultDetails.Visible = false;
        btnNewGame.Visible      = false;
        txtAnswer.Enabled       = true;
        btnSubmit.Enabled       = true;
    }

    // ─── События ──────────────────────────────────────────────────────────

    private void BtnStart_Click(object? sender, EventArgs e)
    {
        var ops = GetSelectedOperations();
        if (ops.Count == 0)
        {
            MessageBox.Show("Выберите хотя бы одну операцию!", "Внимание",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DifficultyLevel diff = rbExpert.Checked ? DifficultyLevel.Expert
                             : rbHard.Checked   ? DifficultyLevel.Hard
                             : rbMedium.Checked ? DifficultyLevel.Medium
                             :                    DifficultyLevel.Easy;

        _session = new GameSession(diff, ops,
            totalQuestions:  (int)numQuestions.Value,
            timePerQuestion: (int)numTime.Value);

        ShowGamePanel();
        NextQuestion();
    }

    private void BtnSubmit_Click(object? sender, EventArgs e) => ProcessAnswer();

    private void TxtAnswer_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter) ProcessAnswer();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _timeLeft--;
        UpdateTimerDisplay();

        if (_timeLeft <= 0)
        {
            _timer.Stop();
            HandleTimeout();
        }
    }

    // ─── Игровая логика ───────────────────────────────────────────────────

    private void NextQuestion()
    {
        if (_session == null) return;

        _currentQuestion = _session.NextQuestion();

        lblQuestionNum.Text = $"Вопрос {_session.QuestionNumber}/{_session.TotalQuestions}";
        lblQuestion.Text    = _currentQuestion.ToString();
        lblMessage.Text     = "";
        txtAnswer.Clear();
        txtAnswer.Focus();

        _timeLeft = _session.TimePerQuestion;
        UpdateTimerDisplay();
        _timer.Start();
    }

    private void ProcessAnswer()
    {
        if (_session == null || _currentQuestion == null || !btnSubmit.Enabled) return;

        _timer.Stop();
        txtAnswer.Enabled = false;
        btnSubmit.Enabled = false;

        if (!int.TryParse(txtAnswer.Text.Trim(), out int answer))
        {
            ShowAnswerFeedback(false, 0, timedOut: true);
            return;
        }

        var (correct, coinChange) = _session.SubmitAnswer(answer);
        ShowAnswerFeedback(correct, coinChange, timedOut: false);
    }

    private void HandleTimeout()
    {
        if (_session == null) return;
        txtAnswer.Enabled = false;
        btnSubmit.Enabled = false;

        var (_, coinChange) = _session.SubmitTimeout();
        ShowAnswerFeedback(false, coinChange, timedOut: true);
    }

    private void ShowAnswerFeedback(bool correct, int coinChange, bool timedOut)
    {
        if (_session == null || _currentQuestion == null) return;

        UpdateScoreLabels();

        if (timedOut)
        {
            lblMessage.ForeColor = RedColor;
            lblMessage.Text = $"Время вышло!  Ответ: {_currentQuestion.CorrectAnswer}";
        }
        else if (correct)
        {
            lblMessage.ForeColor = GreenColor;
            string coinStr = coinChange > 0 ? $"  +{coinChange} монет" : "";
            lblMessage.Text = $"Верно!{coinStr}";
        }
        else
        {
            lblMessage.ForeColor = RedColor;
            lblMessage.Text = $"Неверно!  Ответ: {_currentQuestion.CorrectAnswer}";
        }

        if (coinChange != 0)
        {
            lblCoins.ForeColor = coinChange > 0 ? GreenColor : RedColor;
        }

        if (_session.IsFinished)
            ShowFinalResults();
        else
        {
            var t = new System.Windows.Forms.Timer { Interval = 1500 };
            t.Tick += (_, _) => { t.Stop(); txtAnswer.Enabled = true; btnSubmit.Enabled = true; NextQuestion(); };
            t.Start();
        }
    }

    private void ShowFinalResults()
    {
        lblQuestion.Visible   = false;
        txtAnswer.Visible     = false;
        btnSubmit.Visible     = false;
        lblMessage.Visible    = false;
        pbarTimer.Visible     = false;
        lblTimerSec.Visible   = false;

        double pct = _session!.TotalQuestions > 0
            ? _session.CorrectCount * 100.0 / _session.TotalQuestions : 0;

        string verdict = pct == 100 ? "Превосходно!"
                       : pct >= 80  ? "Отлично!"
                       : pct >= 60  ? "Хорошо!"
                       : pct >= 40  ? "Практикуйся!"
                       :              "Не сдавайся!";

        _lblResultTitle.Text      = verdict;
        _lblResultTitle.ForeColor = pct >= 60 ? GreenColor : RedColor;
        _lblResultTitle.Visible   = true;

        _lblResultDetails.Text =
            $"Правильных: {_session.CorrectCount}  /  Неверных: {_session.WrongCount}\n" +
            $"Точность: {pct:F1}%\n" +
            $"Монеты: {_session.Coins}";
        _lblResultDetails.Visible = true;

        btnNewGame.Visible = true;
    }

    // ─── Вспомогательные ──────────────────────────────────────────────────

    private void UpdateTimerDisplay()
    {
        int total = _session?.TimePerQuestion ?? 15;
        int pct   = total > 0 ? (int)(_timeLeft * 100.0 / total) : 0;
        pbarTimer.Value = Math.Max(0, Math.Min(100, pct));

        lblTimerSec.Text      = _timeLeft.ToString();
        lblTimerSec.ForeColor = _timeLeft <= 5 ? RedColor : _timeLeft <= 10 ? GoldColor : GreenColor;
    }

    private void UpdateScoreLabels()
    {
        if (_session == null) return;
        lblScore.Text = $"[+] {_session.CorrectCount}  [-] {_session.WrongCount}";
        lblCoins.Text = $"$ {_session.Coins}";
        lblCoins.ForeColor = GoldColor;
    }

    private List<OperationType> GetSelectedOperations()
    {
        var list = new List<OperationType>();
        if (cbAdd.Checked) list.Add(OperationType.Addition);
        if (cbSub.Checked) list.Add(OperationType.Subtraction);
        if (cbMul.Checked) list.Add(OperationType.Multiplication);
        if (cbDiv.Checked) list.Add(OperationType.Division);
        return list;
    }

    // ─── Фабрики элементов ────────────────────────────────────────────────

    private static Label SectionLabel(string text, int x, int y)
    {
        return new Label
        {
            Text      = text,
            Location  = new Point(x, y),
            AutoSize  = true,
            Font      = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = AccentColor
        };
    }

    private static RadioButton RadioBtn(string text, int x, int y, bool check = false)
    {
        return new RadioButton
        {
            Text     = text,
            Location = new Point(x, y),
            AutoSize = true,
            Checked  = check
        };
    }

    private static CheckBox CheckBtn(string text, int x, int y, bool check = false)
    {
        return new CheckBox
        {
            Text     = text,
            Location = new Point(x, y),
            AutoSize = true,
            Checked  = check
        };
    }

    private static Label MakeLabel(string text, int x, int y, int w, int h,
        FontStyle style = FontStyle.Regular)
    {
        return new Label
        {
            Text      = text,
            Location  = new Point(x, y),
            Size      = new Size(w, h),
            Font      = new Font("Segoe UI", 10f, style),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }
}
