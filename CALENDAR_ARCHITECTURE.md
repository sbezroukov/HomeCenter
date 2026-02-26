# 🏗️ Архитектура календаря - Техническая документация

## Обзор архитектуры

Система календаря построена на основе ASP.NET Core 8.0 с использованием паттернов MVC и Repository.

```
┌─────────────────────────────────────────────────────────────┐
│                         Presentation Layer                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Views      │  │ Controllers  │  │  API         │      │
│  │  (Razor)     │  │   (MVC)      │  │ Controllers  │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                        Business Layer                        │
│  ┌──────────────────────────────────────────────────┐       │
│  │              Services                             │       │
│  │  • CalendarService                                │       │
│  │  • TelegramNotificationService                    │       │
│  │  • CalendarNotificationBackgroundService          │       │
│  └──────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                         Data Layer                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Models     │  │  DbContext   │  │  Migrations  │      │
│  │              │  │              │  │              │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
                           ↓
                   ┌──────────────┐
                   │   SQLite DB  │
                   └──────────────┘
```

## Компоненты системы

### 1. Models (Модели данных)

#### ActivityType
Справочник типов деятельности.

**Поля:**
- `Id` - первичный ключ
- `Name` - название типа
- `Description` - описание
- `Color` - цвет в HEX формате
- `IsActive` - флаг активности
- `CreatedAt`, `UpdatedAt` - временные метки

**Связи:**
- One-to-Many с `ScheduledActivity`

#### ScheduledActivity
Запланированная активность.

**Поля:**
- `Id` - первичный ключ
- `ActivityTypeId` - внешний ключ на `ActivityType`
- `Title` - название (опционально)
- `Description` - описание
- `StartDate` - дата начала
- `StartTime` - время начала (nullable)
- `EndTime` - время окончания (nullable)
- `DeadlineDateTime` - дедлайн
- `AssignedToUserId` - ответственный (nullable)
- `CreatedByUserId` - создатель
- `IsRecurring` - флаг повторяющейся задачи
- `RecurringDayOfWeek` - день недели для повторения
- `IsActive` - флаг активности
- `CreatedAt`, `UpdatedAt` - временные метки

**Связи:**
- Many-to-One с `ActivityType`
- Many-to-One с `ApplicationUser` (AssignedTo)
- Many-to-One с `ApplicationUser` (CreatedBy)
- One-to-Many с `ActivityCompletion`

**Вычисляемые свойства:**
- `IsAllDay` - задача на весь день
- `DisplayTitle` - отображаемое название

#### ActivityCompletion
Отметка о выполнении.

**Поля:**
- `Id` - первичный ключ
- `ScheduledActivityId` - внешний ключ на `ScheduledActivity`
- `CompletedByUserId` - кто завершил
- `Status` - статус выполнения (enum)
- `Comment` - комментарий
- `CompletedAt` - время завершения
- `IsOnTime` - выполнено в срок

**Связи:**
- Many-to-One с `ScheduledActivity`
- Many-to-One с `ApplicationUser`

#### CompletionStatus (Enum)
```csharp
public enum CompletionStatus
{
    Completed = 1,          // Выполнена
    NotCompleted = 2,       // Не выполнена
    PartiallyCompleted = 3, // Частично выполнена
    Cancelled = 4           // Отменена
}
```

### 2. Services (Сервисы)

#### ICalendarService / CalendarService
Основной сервис для работы с календарем.

**Методы:**
- `GetAllActivityTypesAsync()` - получить все типы деятельности
- `GetActivityTypeByIdAsync(id)` - получить тип по ID
- `CreateActivityTypeAsync(activityType)` - создать тип
- `UpdateActivityTypeAsync(activityType)` - обновить тип
- `DeleteActivityTypeAsync(id)` - деактивировать тип
- `GetActivitiesForWeekAsync(weekStart, userId)` - получить активности на неделю
- `GetActivitiesForDayAsync(date, userId)` - получить активности на день
- `GetActivityByIdAsync(id)` - получить активность по ID
- `CreateActivityAsync(activity)` - создать активность
- `UpdateActivityAsync(activity)` - обновить активность
- `DeleteActivityAsync(id)` - деактивировать активность
- `GetOverdueActivitiesAsync()` - получить просроченные активности
- `GetUpcomingActivitiesAsync(hours)` - получить предстоящие активности
- `CompleteActivityAsync(activityId, userId, status, comment)` - отметить выполнение
- `GetActivityCompletionAsync(activityId)` - получить отметку выполнения
- `GetUserCompletionsAsync(userId, from, to)` - получить отметки пользователя
- `GetCompletionStatisticsAsync(userId, from, to)` - получить статистику

**Особенности:**
- Использует Entity Framework Core для работы с БД
- Применяет Eager Loading для связанных данных
- Логирует все операции

#### ITelegramNotificationService / TelegramNotificationService
Сервис для отправки уведомлений через Telegram.

**Методы:**
- `SendActivityStartNotificationAsync(activity)` - уведомление о начале
- `SendActivityOverdueNotificationAsync(activity)` - уведомление о просрочке
- `SendActivityCompletedNotificationAsync(activity, completion)` - уведомление о завершении
- `SendActivityNotClosedNotificationAsync(activity)` - уведомление о незакрытой задаче
- `SendMessageAsync(chatId, message)` - отправка произвольного сообщения
- `IsBotAvailableAsync()` - проверка доступности бота

**Особенности:**
- Использует Telegram.Bot SDK v22.0.0
- Поддерживает HTML форматирование
- Graceful degradation при отключенном боте
- Централизованное логирование

#### CalendarNotificationBackgroundService
Фоновая служба для автоматической отправки уведомлений.

**Функции:**
- Запускается при старте приложения
- Работает каждые 5 минут
- Отправляет уведомления:
  - О начале задач (за 15 минут)
  - О просроченных задачах (раз в день)
  - О незакрытых задачах (через час после дедлайна)

**Особенности:**
- Наследуется от `BackgroundService`
- Использует Scoped сервисы через `IServiceProvider`
- Обрабатывает исключения без остановки службы
- Поддерживает graceful shutdown

### 3. Controllers (Контроллеры)

#### CalendarController
MVC контроллер для работы с календарем через веб-интерфейс.

**Actions:**
- `Index(date)` - главная страница календаря (недельный вид)
- `Create(date)` - форма создания активности
- `Create(model)` [POST] - создание активности
- `Edit(id)` - форма редактирования
- `Edit(id, model)` [POST] - обновление активности
- `Delete(id)` [POST] - удаление активности
- `Complete(id)` - форма отметки выполнения
- `Complete(model)` [POST] - отметка выполнения

**Авторизация:**
- Требуется авторизация для всех действий
- Проверка прав на редактирование (создатель или админ)

#### ActivityTypeController
MVC контроллер для управления типами деятельности.

**Actions:**
- `Index()` - список типов
- `Create()` - форма создания
- `Create(model)` [POST] - создание типа
- `Edit(id)` - форма редактирования
- `Edit(id, model)` [POST] - обновление типа
- `Delete(id)` [POST] - деактивация типа

**Авторизация:**
- Требуется роль Admin для всех действий

#### CalendarApiController
REST API контроллер для интеграций.

**Endpoints:**
- `GET /api/calendar/week?date={date}` - активности на неделю
- `GET /api/calendar/day?date={date}` - активности на день
- `GET /api/calendar/{id}` - конкретная активность
- `POST /api/calendar/{id}/complete` - отметить выполнение
- `GET /api/calendar/overdue` - просроченные активности
- `GET /api/calendar/upcoming?hours={hours}` - предстоящие активности
- `GET /api/calendar/statistics?from={from}&to={to}` - статистика

**Особенности:**
- Возвращает JSON
- Требует авторизации
- Использует стандартные HTTP статус-коды

### 4. Views (Представления)

#### Calendar/Index.cshtml
Главная страница календаря с недельным видом.

**Особенности:**
- Адаптивный дизайн (Bootstrap)
- Цветовая индикация типов задач
- Навигация между неделями
- Быстрые действия (выполнить, редактировать, удалить)
- Автообновление каждые 5 минут

#### Calendar/Create.cshtml & Edit.cshtml
Формы создания и редактирования активностей.

**Особенности:**
- Валидация на клиенте и сервере
- Поддержка date/time пикеров
- Динамическое отображение полей повторяющихся задач

#### Calendar/Complete.cshtml
Форма отметки выполнения.

**Особенности:**
- Выбор статуса с иконками
- Поле для комментария
- Валидация

#### ActivityType/Index.cshtml
Список типов деятельности.

**Особенности:**
- Таблица с цветовой индикацией
- Фильтрация активных/неактивных
- Быстрые действия

#### ActivityType/Create.cshtml & Edit.cshtml
Формы управления типами.

**Особенности:**
- Color picker
- Синхронизация HEX и color input
- Валидация формата цвета

### 5. Database Schema

```sql
-- ActivityTypes
CREATE TABLE ActivityTypes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    Color TEXT NOT NULL DEFAULT '#007bff',
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT
);

-- ScheduledActivities
CREATE TABLE ScheduledActivities (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ActivityTypeId INTEGER NOT NULL,
    Title TEXT,
    Description TEXT,
    StartDate TEXT NOT NULL,
    StartTime TEXT,
    EndTime TEXT,
    DeadlineDateTime TEXT,
    AssignedToUserId INTEGER,
    CreatedByUserId INTEGER NOT NULL,
    IsRecurring INTEGER NOT NULL DEFAULT 0,
    RecurringDayOfWeek INTEGER,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT,
    FOREIGN KEY (ActivityTypeId) REFERENCES ActivityTypes(Id),
    FOREIGN KEY (AssignedToUserId) REFERENCES Users(Id),
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
);

-- ActivityCompletions
CREATE TABLE ActivityCompletions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScheduledActivityId INTEGER NOT NULL,
    CompletedByUserId INTEGER NOT NULL,
    Status INTEGER NOT NULL,
    Comment TEXT,
    CompletedAt TEXT NOT NULL,
    IsOnTime INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (ScheduledActivityId) REFERENCES ScheduledActivities(Id) ON DELETE CASCADE,
    FOREIGN KEY (CompletedByUserId) REFERENCES Users(Id)
);

-- Indexes
CREATE INDEX IX_ActivityTypes_Name ON ActivityTypes(Name);
CREATE INDEX IX_ScheduledActivities_StartDate ON ScheduledActivities(StartDate);
CREATE INDEX IX_ScheduledActivities_DeadlineDateTime ON ScheduledActivities(DeadlineDateTime);
CREATE INDEX IX_ActivityCompletions_CompletedAt ON ActivityCompletions(CompletedAt);
```

## Потоки данных

### 1. Создание активности

```
User → CalendarController.Create [POST]
  → CalendarService.CreateActivityAsync
    → ApplicationDbContext.ScheduledActivities.Add
      → SaveChangesAsync
        → Database
```

### 2. Отметка выполнения

```
User → CalendarController.Complete [POST]
  → CalendarService.CompleteActivityAsync
    → ApplicationDbContext.ActivityCompletions.Add
      → SaveChangesAsync
        → Database
    → TelegramNotificationService.SendActivityCompletedNotificationAsync
      → Telegram Bot API
```

### 3. Фоновые уведомления

```
CalendarNotificationBackgroundService (каждые 5 минут)
  → CalendarService.GetUpcomingActivitiesAsync
    → Database
  → TelegramNotificationService.SendActivityStartNotificationAsync
    → Telegram Bot API
```

## Конфигурация

### appsettings.json
```json
{
  "Telegram": {
    "BotToken": "",
    "Enabled": false
  }
}
```

### Environment Variables
```
TELEGRAM_BOT_TOKEN
TELEGRAM_ENABLED
```

## Безопасность

### Аутентификация
- Cookie-based authentication
- ASP.NET Core Identity (упрощенная версия)

### Авторизация
- Role-based authorization (Admin, User)
- Resource-based authorization (владелец или админ)

### Защита данных
- CSRF токены для POST запросов
- Валидация на клиенте и сервере
- Параметризованные SQL запросы (EF Core)

## Производительность

### Оптимизации
- Eager Loading для связанных данных
- Индексы на часто запрашиваемых полях
- Кэширование типов деятельности (можно добавить)

### Масштабируемость
- Фоновая служба работает в одном экземпляре
- Можно добавить Redis для распределенного кэша
- Можно использовать Hangfire для более сложных задач

## Расширяемость

### Возможные улучшения

1. **Дополнительные типы уведомлений:**
   - Email
   - SMS
   - Push notifications

2. **Расширенная аналитика:**
   - Dashboard с графиками
   - Отчеты по эффективности
   - Прогнозирование нагрузки

3. **Интеграции:**
   - Google Calendar
   - Microsoft Outlook
   - Apple Calendar

4. **Telegram Bot команды:**
   - Просмотр задач
   - Отметка выполнения
   - Создание задач

5. **Мобильное приложение:**
   - React Native / Flutter
   - Использование REST API

## Тестирование

### Unit Tests
Рекомендуется покрыть тестами:
- `CalendarService` - бизнес-логика
- `TelegramNotificationService` - отправка уведомлений
- Валидация моделей

### Integration Tests
- Тестирование API endpoints
- Тестирование контроллеров
- Тестирование фоновой службы

### E2E Tests
- Тестирование пользовательских сценариев
- Selenium / Playwright

## Мониторинг и логирование

### Логирование
- Используется встроенный ILogger
- Логи сохраняются в консоль
- Можно добавить Serilog для расширенного логирования

### Метрики
Рекомендуется отслеживать:
- Количество созданных задач
- Процент выполненных задач
- Среднее время выполнения
- Количество отправленных уведомлений

## Deployment

### Требования
- .NET 8.0 Runtime
- SQLite (встроен)
- Telegram Bot Token (опционально)

### Docker
Можно создать Dockerfile:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY bin/Release/net8.0/publish/ .
ENTRYPOINT ["dotnet", "HomeCenter.dll"]
```

### Переменные окружения
```
ASPNETCORE_ENVIRONMENT=Production
TELEGRAM_BOT_TOKEN=...
TELEGRAM_ENABLED=true
```

---

**Документация актуальна на:** 26.02.2026
