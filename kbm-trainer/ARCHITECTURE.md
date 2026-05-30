# Архитектура

## Обзор

Решение на .NET 10 из двух проектов. Бизнес-логика изолирована в `KbmTrainer.Core`; WPF-проект содержит только код интерфейса.

```
┌──────────────────────────────────────────┐
│           KbmTrainer.Core                │
│  Models · Interfaces · Services          │
└─────────────────┬────────────────────────┘
                  │ referenced by
        ┌─────────▼──────────┐
        │  KbmTrainer.WPF    │
        │  MVVM · WPF        │
        └────────────────────┘
```

## Ответственности слоёв

### KbmTrainer.Core

| Путь | Назначение |
|------|---------|
| `Models/` | `TypingDictionary`, `SessionResult`, `AppSettings` |
| `Interfaces/` | `IDictionaryRepository`, `IStatisticsRepository`, `ISettingsService` |
| `Services/` | JSON-реализации всех интерфейсов |

**Сервисы:**
- **`JsonDictionaryRepository`** — читает/пишет `%APPDATA%/KbmTrainer/dictionaries.json`; при первом запуске заполняет 4 встроенных словаря (английский, русский, цифры, программирование)
- **`JsonStatisticsRepository`** — читает/пишет `%APPDATA%/KbmTrainer/statistics.json`; предоставляет `Add`, `GetAll`, `Clear`
- **`JsonSettingsService`** — читает/пишет `%APPDATA%/KbmTrainer/settings.json`; возвращает значения по умолчанию, если файл отсутствует

### KbmTrainer.WPF

Паттерн MVVM. Никакой бизнес-логики во View и code-behind.

```
Commands/       RelayCommand — обёртка над ICommand
Converters/     CharStateToColorConverter, BoolToVisibilityConverter
Themes/         DarkTheme.xaml, LightTheme.xaml (словари ресурсов)
ViewModels/     Всё состояние и команды
Views/          XAML + минимальный code-behind (только передача клавиш)
```

## Навигация

`MainViewModel` владеет всеми дочерними ViewModel-ами и предоставляет `CurrentView` (object). В `MainWindow` есть `ContentControl`, привязанный к `CurrentView`. В `App.xaml` каждый тип ViewModel сопоставляется с соответствующим UserControl через `DataTemplate`.

```
MainViewModel.CurrentView
  ├── TypingViewModel        → TypingView.xaml
  ├── DictionaryEditorViewModel → DictionaryEditorView.xaml
  ├── StatisticsViewModel    → StatisticsView.xaml
  └── SettingsViewModel      → SettingsView.xaml
```

Главный экран (`IsSelectingDictionary = true`) показывает сетку выбора словаря непосредственно в `MainWindow` до начала сессии.

## Процесс сессии набора текста

```
Пользователь выбирает словарь и нажимает «Начать»
  → MainViewModel.StartSession(dictionary)
  → TypingViewModel.Load(words, textLength)
      Выбрать N случайных слов из словаря
      Построить ObservableCollection<CharSlot>
      Каждый CharSlot: { char Expected, CharState State }

Пользователь вводит символ
  → MainWindow.PreviewTextInput → TypingViewModel.HandleChar(c)
      Если первый символ → запустить DispatcherTimer (100 мс тик)
      Сравнить c с CharSlots[_cursor].Expected
        Правильно  → State = Correct
        Неправильно → State = Incorrect, _errors++
      Переместить _cursor, установить State следующего слота = Current
      Обновить WPM в реальном времени = (_cursor / 5) / elapsed_minutes

Пользователь нажимает Backspace
  → MainWindow.PreviewKeyDown → TypingViewModel.HandleBackspace()
      Вернуть курсор назад, сбросить слот в Pending, установить предыдущий как Current

Все символы введены
  → SessionFinished = true
  → Вычислить итоговый WPM, Accuracy
  → IStatisticsRepository.Add(SessionResult)
  → Показать баннер результата в TypingView
```

## Модели

```
TypingDictionary          SessionResult
  Id: string (Guid)         Id: string (Guid)
  Name: string              DictionaryId: string
  Language: string          DictionaryName: string
  Words: List<string>       Date: DateTime
                            Wpm: int
AppSettings                 Accuracy: double
  Theme: string = "Dark"    TotalChars: int
  FontSize: double = 20     ErrorChars: int
  TextLength: int = 50      Duration: TimeSpan
  ShowKeyboard: bool = false
```

## CharSlot и CharState

```csharp
public enum CharState { Pending, Current, Correct, Incorrect }

public class CharSlot : INotifyPropertyChanged
{
    public char Expected { get; }
    public CharState State { get; set; }   // уведомляет при изменении
}
```

`CharStateToColorConverter` сопоставляет `CharState` → `Brush`, обращаясь к ключам ресурсов темы (`CorrectColor`, `IncorrectColor`, `CurrentColor`, `PendingColor`).

## Система тем

Оба файла — `DarkTheme.xaml` и `LightTheme.xaml` — определяют одинаковые ключи ресурсов:

| Ключ | Тёмная (Catppuccin) | Светлая |
|-----|-------------------|-------|
| `WindowBackground` | `#1E1E2E` | `#F8FAFC` |
| `SecondaryBackground` | `#181825` | `#FFFFFF` |
| `AccentColor` | `#89B4FA` | `#3B82F6` |
| `CorrectColor` | `#A6E3A1` | `#22C55E` |
| `IncorrectColor` | `#F38BA8` | `#EF4444` |
| `CurrentColor` | `#CDD6F4` | `#1E293B` |
| `PendingColor` | `#6C7086` | `#94A3B8` |

Переключение темы во время работы: `SettingsViewModel` вызывает `App.ApplyTheme(name)`, который заменяет первую запись в `Application.Resources.MergedDictionaries`.

## Файлы проектов

| Проект | Target | OutputType |
|---------|--------|------------|
| `KbmTrainer.Core` | `net10.0` | Library |
| `KbmTrainer.WPF` | `net10.0-windows` | WinExe |

В обоих проектах включены `Nullable` и `ImplicitUsings`. Сторонних NuGet-пакетов нет.

## Директория данных

```
%APPDATA%/KbmTrainer/
├── dictionaries.json   ← заполняется при первом запуске
├── statistics.json
└── settings.json
```
