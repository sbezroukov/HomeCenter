using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace HomeCenter.Data;

/// <summary>
/// Применяет схемные изменения при обновлении модели (для проектов без dotnet-ef).
/// </summary>
public static class DatabaseMigrator
{
    public static void EnsureVersioningSchema(ApplicationDbContext db)
    {
        // Добавляем колонку IsDeleted в Topics, если её нет
        AddColumnIfNotExists(db, "Topics", "IsDeleted", "INTEGER NOT NULL DEFAULT 0");

        // Создаём таблицу TestHistory, если её нет
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS TestHistory (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                FileName TEXT NOT NULL,
                FolderPath TEXT,
                Action INTEGER NOT NULL,
                Content TEXT,
                Timestamp TEXT NOT NULL DEFAULT (datetime('now'))
            )");

        // Добавляем колонки для асинхронной обработки AI оценок в Attempts
        AddColumnIfNotExists(db, "Attempts", "GradingStatus", "INTEGER NOT NULL DEFAULT 0");
        
        if (AddColumnIfNotExists(db, "Attempts", "LastUpdatedAt", "TEXT NOT NULL DEFAULT '2024-01-01 00:00:00'"))
        {
            // Обновляем существующие записи, используя StartedAt/CompletedAt как базу
            db.Database.ExecuteSqlRaw(
                "UPDATE Attempts SET LastUpdatedAt = COALESCE(CompletedAt, StartedAt) WHERE LastUpdatedAt = '2024-01-01 00:00:00'");
        }
        
        AddColumnIfNotExists(db, "Attempts", "GradingError", "TEXT");

        // Добавляем поле для Telegram Chat ID в Users
        AddColumnIfNotExists(db, "Users", "TelegramChatId", "INTEGER");

        // Создаём таблицы для календаря
        EnsureCalendarSchema(db);
    }

    /// <summary>
    /// Создаёт таблицы для функционала календаря
    /// </summary>
    private static void EnsureCalendarSchema(ApplicationDbContext db)
    {
        // Таблица типов деятельности
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ActivityTypes (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Description TEXT,
                Color TEXT NOT NULL DEFAULT '#007bff',
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT
            )");

        // Таблица запланированных активностей
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ScheduledActivities (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
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
            )");

        // Таблица отметок о выполнении
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ActivityCompletions (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ScheduledActivityId INTEGER NOT NULL,
                CompletedByUserId INTEGER NOT NULL,
                Status INTEGER NOT NULL,
                Comment TEXT,
                CompletedAt TEXT NOT NULL,
                IsOnTime INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY (ScheduledActivityId) REFERENCES ScheduledActivities(Id) ON DELETE CASCADE,
                FOREIGN KEY (CompletedByUserId) REFERENCES Users(Id)
            )");

        // Создаём индексы
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ActivityTypes_Name ON ActivityTypes(Name)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ScheduledActivities_StartDate ON ScheduledActivities(StartDate)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ScheduledActivities_DeadlineDateTime ON ScheduledActivities(DeadlineDateTime)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ActivityCompletions_CompletedAt ON ActivityCompletions(CompletedAt)");

        // Добавляем новые поля в существующие таблицы
        AddColumnIfNotExists(db, "ScheduledActivities", "TestTopicId", "INTEGER");
        AddColumnIfNotExists(db, "ActivityCompletions", "IsApprovedBySupervisor", "INTEGER NOT NULL DEFAULT 0");
        AddColumnIfNotExists(db, "ActivityCompletions", "ApprovedByUserId", "INTEGER");
        AddColumnIfNotExists(db, "ActivityCompletions", "ApprovedAt", "TEXT");
        AddColumnIfNotExists(db, "ActivityCompletions", "TestAttemptId", "INTEGER");

        // Таблица фотографий активностей
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ActivityPhotos (
                Id INTEGER NOT NULL PRIMARY AUTOINCREMENT,
                ScheduledActivityId INTEGER NOT NULL,
                UploadedByUserId INTEGER NOT NULL,
                FilePath TEXT NOT NULL,
                OriginalFileName TEXT,
                FileSize INTEGER NOT NULL,
                ContentType TEXT,
                Description TEXT,
                UploadedAt TEXT NOT NULL,
                FOREIGN KEY (ScheduledActivityId) REFERENCES ScheduledActivities(Id) ON DELETE CASCADE,
                FOREIGN KEY (UploadedByUserId) REFERENCES Users(Id)
            )");

        // Таблица комментариев к активностям
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS ActivityComments (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                ScheduledActivityId INTEGER NOT NULL,
                AuthorUserId INTEGER NOT NULL,
                Text TEXT NOT NULL,
                ParentCommentId INTEGER,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT,
                IsDeleted INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY (ScheduledActivityId) REFERENCES ScheduledActivities(Id) ON DELETE CASCADE,
                FOREIGN KEY (AuthorUserId) REFERENCES Users(Id),
                FOREIGN KEY (ParentCommentId) REFERENCES ActivityComments(Id)
            )");

        // Индексы для новых таблиц
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ActivityPhotos_UploadedAt ON ActivityPhotos(UploadedAt)");
        db.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_ActivityComments_CreatedAt ON ActivityComments(CreatedAt)");
    }

    /// <summary>
    /// Добавляет колонку в таблицу, если её ещё нет.
    /// </summary>
    /// <returns>True если колонка была добавлена, False если уже существовала</returns>
    private static bool AddColumnIfNotExists(ApplicationDbContext db, string tableName, string columnName, string columnDefinition)
    {
        // Проверяем существование колонки
        var checkSql = $"SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name = '{columnName}'";
        
        using var connection = db.Database.GetDbConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = checkSql;
        var count = Convert.ToInt32(command.ExecuteScalar());
        
        if (count > 0)
        {
            // Колонка уже существует
            return false;
        }

        // Добавляем колонку
        try
        {
            db.Database.ExecuteSqlRaw($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}");
            return true;
        }
        catch (SqliteException)
        {
            // Колонка уже существует (race condition)
            return false;
        }
    }
}
