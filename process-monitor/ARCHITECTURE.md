# Архитектура

## Обзор

ProcessMonitor — решение на .NET 10 из трёх проектов. Общая бизнес-логика находится в `ProcessMonitor.Core`; консольное и WPF-приложения зависят от него и не добавляют логики за пределами задач интерфейса.

```
┌─────────────────────────────────────────────────────────┐
│                   ProcessMonitor.Core                   │
│  Interfaces · Models · Services · Helpers               │
└────────────────────────┬────────────────────────────────┘
                         │ referenced by
           ┌─────────────┴──────────────┐
           │                            │
  ┌────────▼──────────┐      ┌──────────▼──────────┐
  │ ProcessMonitor    │      │  ProcessMonitor.WPF  │
  │ .Console          │      │  (MVVM · WPF)        │
  └───────────────────┘      └─────────────────────┘
```

## Ответственности слоёв

### ProcessMonitor.Core

Чистая бизнес-логика без зависимостей от UI.

| Путь | Назначение |
|------|---------|
| `Interfaces/` | Контракты для всех внедряемых сервисов |
| `Models/` | Модели данных (`ProcessNode`, `ProcessSnapshot`, `AppSettings`, …) |
| `Services/` | Конкретные реализации (провайдер процессов, построитель дерева, экспортёр, настройки) |
| `Helpers/` | Утилиты без состояния (`SizeFormatter`) |

**Ключевые классы:**

- **`WindowsProcessProvider`** (`IProcessProvider`) — перечисляет все запущенные процессы через `Process.GetProcesses()` + P/Invoke (`CreateToolhelp32Snapshot`) для определения родительских PID. Процент CPU вычисляется из двух замеров с интервалом 500 мс: `(Δtime / elapsed / cores) × 100`.
- **`ProcessTreeBuilder`** — преобразует плоский `List<ProcessSnapshot>` в иерархический `List<ProcessNode>`, сопоставляя связи по `ParentPid`.
- **`CsvReportExporter`** (`IReportExporter`) — записывает отчёты в формате CSV или ASCII-дерева.
- **`JsonSettingsService`** (`ISettingsService`) — сохраняет `AppSettings` в `%APPDATA%/ProcessMonitor/settings.json` через `System.Text.Json`.

### ProcessMonitor.WPF

Паттерн MVVM; никакой бизнес-логики во View и code-behind.

```
Views/          XAML + минимальный code-behind
ViewModels/     Всё состояние и команды
Commands/       RelayCommand (обёртка над ICommand)
Converters/     Конвертеры значений для привязок XAML
Themes/         DarkTheme.xaml, LightTheme.xaml (словари ресурсов)
```

**`MainViewModel`** управляет всем состоянием UI:
- `RootProcesses` / `CurrentLevelItems` — `ObservableCollection<ProcessNode>` для TreeView и DataGrid
- `Breadcrumbs` — путь навигации для перехода к дочерним процессам
- 12 `RelayCommand`-ов (Обновить, Завершить, Экспорт, Войти в/Выйти из узла, Сортировка, Копировать PID, Открыть в проводнике, Настройки)
- Авто-обновление на основе `DispatcherTimer`

**`SettingsViewModel`** ограничен диалогом; генерирует `RequestClose` с `DialogResult` и делегирует сохранение `ISettingsService`.

### ProcessMonitor.Console

Только верхнеуровневые операторы (`Program.cs`). Разбирает флаги CLI, вызывает `WindowsProcessProvider` + `ProcessTreeBuilder`, выводит плоскую таблицу или ASCII-дерево в stdout, и при необходимости делегирует экспорт `CsvReportExporter`.

## Поток данных

### Обновление (WPF)

```
MainViewModel.RefreshAsync()
  → IProcessProvider.GetSnapshotsAsync()
      Process.GetProcesses()          // перечисление
      P/Invoke карта родительских PID // метаданные иерархии
      2× замер CPU (интервал 500 мс)  // CPU%
      → List<ProcessSnapshot>
  → ProcessTreeBuilder.Build(snapshots)
      → List<ProcessNode>             // иерархический
  → ApplySortToList()                 // текущий SortMode
  → ObservableCollection-ы обновлены // запускает привязки UI
  → Восстановлены строка статуса, выделение, хлебные крошки
```

### Цикл настроек

```
Запуск приложения → JsonSettingsService.Load() → применены значения AppSettings по умолчанию
Нажато ОК         → JsonSettingsService.Save() → записан settings.json
                  → MainViewModel перезагружает тему и интервалы
```

## Ключевые архитектурные решения

| Решение | Обоснование |
|----------|-----------|
| Сервисы через интерфейсы | Позволяет юнит-тестирование с mock-провайдерами без обращения к реальным процессам |
| Иммутабельный `ProcessSnapshot`, изменяемый `ProcessNode` | Снимки — сырые данные; узлы — живые объекты, привязанные к UI, с рекурсивными вычисляемыми свойствами |
| Двухточечный замер CPU | Один замер не имеет базовой линии; 500 мс даёт достаточно точное короткое окно без блокировки UI (асинхронно) |
| `ObservableCollection` только в дочерних элементах `ProcessNode` | Уведомления об изменениях WPF именно там, где они нужны TreeView |
| Темы через объединённые словари ресурсов | Позволяет переключать тему во время работы, заменяя одну запись в `App.Resources.MergedDictionaries` |
| Настройки в `%APPDATA%` | Стандартное расположение Windows; сохраняется при переустановке приложения и не требует прав администратора |

## Модели

```
ProcessSnapshot          ProcessNode
  Pid                      Pid
  ParentPid                ParentPid
  Name                     Name
  ExecutablePath           ExecutablePath
  CpuPercent               CpuPercent
  MemoryBytes              MemoryBytes          ← TotalMemoryBytes (рекурсивная сумма)
  StartTime                StartTime
  Status                   Status
                           Children: ObservableCollection<ProcessNode>
                           FormattedMemory / FormattedCpu / FormattedStartTime
                           TotalCpuPercent (рекурсивная сумма)
```

## Файлы проектов и целевые платформы

| Проект | Target | OutputType |
|---------|--------|------------|
| `ProcessMonitor.Core` | `net10.0` | Library |
| `ProcessMonitor.Console` | `net10.0` | Exe |
| `ProcessMonitor.WPF` | `net10.0-windows` | WinExe |

Во всех проектах включены `Nullable` и `ImplicitUsings`. Сторонних NuGet-зависимостей нет.
