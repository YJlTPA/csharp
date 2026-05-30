# ProcessMonitor

Инструмент мониторинга системных процессов Windows с двумя интерфейсами: **WPF-приложение** и **консольное приложение**. Отображает запущенные процессы в виде иерархического дерева в реальном времени с метриками CPU и памяти.

## Возможности

- Мониторинг CPU и памяти в реальном времени с настраиваемым авто-обновлением
- Иерархическое дерево процессов с навигацией родитель-потомок
- Завершение процессов, экспорт отчётов (CSV / текст)
- Тёмная и светлая темы (WPF)
- Сохранение настроек (`%APPDATA%/ProcessMonitor/settings.json`)
- Цветовые индикаторы CPU

## Требования

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Сборка

```bash
# Перейти в директорию решения
cd c:/dev/csharp-projects/griha/csharp/process-monitor

# Восстановить зависимости и собрать все проекты
dotnet build ProcessMonitor.sln
```

## Запуск

### WPF-приложение

```bash
dotnet run --project ProcessMonitor.WPF
```

Или собрать и запустить исполняемый файл напрямую:

```bash
dotnet publish ProcessMonitor.WPF -c Release -o ./publish/wpf
./publish/wpf/ProcessMonitor.WPF.exe
```

### Консольное приложение

Базовое использование (топ-50 процессов, отсортированных по CPU):

```bash
dotnet run --project ProcessMonitor.Console
```

Доступные параметры:

| Флаг | Описание | По умолчанию |
|------|-------------|---------|
| `--sort cpu\|memory\|name\|pid` | Порядок сортировки | `cpu` |
| `--top N` | Ограничить вывод топ-N процессами | `50` |
| `--tree` | Показать иерархическое дерево вместо плоской таблицы | выкл |
| `--export csv\|txt` | Экспортировать в файл | — |
| `--output <path>` | Путь к файлу для экспорта | — |
| `--help` | Показать справку | — |

Примеры:

```bash
# Показать топ-20 процессов, отсортированных по памяти
dotnet run --project ProcessMonitor.Console -- --sort memory --top 20

# Показать полное дерево процессов
dotnet run --project ProcessMonitor.Console -- --tree

# Экспортировать в CSV
dotnet run --project ProcessMonitor.Console -- --export csv --output report.csv

# Экспортировать дерево процессов в текст
dotnet run --project ProcessMonitor.Console -- --tree --export txt --output report.txt
```

## Горячие клавиши WPF

| Клавиша | Действие |
|-----|--------|
| `F5` | Обновить |
| `Delete` | Завершить выбранный процесс |
| `Backspace` | Перейти вверх (родительский уровень) |
| `Ctrl+E` | Экспортировать CSV |
| `Ctrl+T` | Экспортировать текст |
| `Ctrl+,` | Открыть настройки |

## Структура проекта

```
ProcessMonitor.sln
├── ProcessMonitor.Core/       Общая бизнес-логика, модели, интерфейсы
├── ProcessMonitor.Console/    Консольное приложение
└── ProcessMonitor.WPF/        WPF-приложение (только Windows)
```

Подробный обзор архитектуры см. в [ARCHITECTURE.md](ARCHITECTURE.md).
