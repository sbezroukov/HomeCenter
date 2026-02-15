# SQLite Web - Руководство по работе с базой данных

## Обзор

SQLite Web - это веб-интерфейс для просмотра и редактирования базы данных SQLite. Автоматически запускается вместе с HomeCenter в Docker.

## Доступ

**URL:** http://localhost:8050

Откройте в браузере после запуска `docker-compose up -d`

## Возможности

### 1. Просмотр таблиц

- Список всех таблиц в левой панели
- Клик по таблице показывает все записи
- Пагинация для больших таблиц
- Сортировка по любому столбцу

### 2. SQL запросы

Вкладка **"Query"** позволяет выполнять произвольные SQL запросы:

```sql
-- Примеры полезных запросов
SELECT * FROM Attempts WHERE ScorePercent > 80;
SELECT * FROM Topics WHERE Title LIKE '%История%';
```

### 3. Редактирование данных

- Клик по записи открывает форму редактирования
- Можно изменить любое поле
- Изменения сохраняются сразу в базу

⚠️ **Осторожно:** Изменения применяются немедленно, без подтверждения!

### 4. Экспорт данных

- Кнопка **"Export"** в правом верхнем углу
- Форматы: CSV, JSON, SQL
- Можно экспортировать всю таблицу или результат запроса

## Структура базы данных

### Таблица: Topics

Темы тестов из файлов.

| Столбец | Тип | Описание |
|---------|-----|----------|
| Id | INTEGER | Уникальный ID |
| Title | TEXT | Название темы |
| FileName | TEXT | Имя файла теста |
| DisplayPath | TEXT | Путь для отображения |
| FolderPath | TEXT | Путь к папке |

### Таблица: Attempts

Попытки прохождения тестов.

| Столбец | Тип | Описание |
|---------|-----|----------|
| Id | INTEGER | Уникальный ID |
| TopicId | INTEGER | ID темы |
| StartedAt | TEXT | Время начала (UTC) |
| FinishedAt | TEXT | Время окончания (UTC) |
| ScorePercent | REAL | Средний балл (0-100) |
| ResultJson | TEXT | JSON с результатами |
| GradingStatus | INTEGER | Статус оценки (0-3) |
| GradingError | TEXT | Текст ошибки (если есть) |
| LastUpdatedAt | TEXT | Последнее обновление |

#### GradingStatus значения:

- **0** - `None` - без AI оценки
- **1** - `Pending` - ожидает обработки
- **2** - `Failed` - ошибка обработки
- **3** - `Completed` - успешно обработано
- **4** - `Processing` - в процессе обработки

### Таблица: SchemaVersions

История миграций базы данных.

| Столбец | Тип | Описание |
|---------|-----|----------|
| Version | TEXT | Версия схемы |
| AppliedAt | TEXT | Время применения |
| Description | TEXT | Описание изменений |

## Полезные SQL запросы

### Статистика по попыткам

```sql
-- Общая статистика
SELECT 
    COUNT(*) as TotalAttempts,
    AVG(ScorePercent) as AvgScore,
    MIN(ScorePercent) as MinScore,
    MAX(ScorePercent) as MaxScore
FROM Attempts
WHERE ScorePercent IS NOT NULL;
```

### Попытки с ошибками AI

```sql
-- Все попытки с ошибками
SELECT 
    Id,
    TopicId,
    StartedAt,
    GradingError,
    LastUpdatedAt
FROM Attempts
WHERE GradingStatus = 2
ORDER BY LastUpdatedAt DESC;
```

### Топ тем по количеству попыток

```sql
-- Самые популярные темы
SELECT 
    t.Title,
    t.DisplayPath,
    COUNT(a.Id) as AttemptsCount,
    AVG(a.ScorePercent) as AvgScore
FROM Topics t
LEFT JOIN Attempts a ON t.Id = a.TopicId
GROUP BY t.Id
ORDER BY AttemptsCount DESC
LIMIT 10;
```

### Последние попытки

```sql
-- 20 последних попыток
SELECT 
    a.Id,
    t.Title as TopicTitle,
    a.ScorePercent,
    a.StartedAt,
    a.GradingStatus,
    CASE a.GradingStatus
        WHEN 0 THEN 'None'
        WHEN 1 THEN 'Pending'
        WHEN 2 THEN 'Failed'
        WHEN 3 THEN 'Completed'
        WHEN 4 THEN 'Processing'
    END as StatusName
FROM Attempts a
JOIN Topics t ON a.TopicId = t.Id
ORDER BY a.StartedAt DESC
LIMIT 20;
```

### Попытки в обработке

```sql
-- Попытки, ожидающие обработки AI
SELECT 
    Id,
    TopicId,
    StartedAt,
    LastUpdatedAt,
    julianday('now') - julianday(LastUpdatedAt) as DaysSinceUpdate
FROM Attempts
WHERE GradingStatus IN (1, 4) -- Pending или Processing
ORDER BY LastUpdatedAt;
```

### Анализ времени обработки

```sql
-- Среднее время обработки AI
SELECT 
    AVG(julianday(LastUpdatedAt) - julianday(StartedAt)) * 24 * 60 as AvgMinutes,
    MIN(julianday(LastUpdatedAt) - julianday(StartedAt)) * 24 * 60 as MinMinutes,
    MAX(julianday(LastUpdatedAt) - julianday(StartedAt)) * 24 * 60 as MaxMinutes
FROM Attempts
WHERE GradingStatus = 3; -- Completed
```

### Поиск по содержимому ответов

```sql
-- Поиск попыток по содержимому ответов
SELECT 
    Id,
    TopicId,
    StartedAt,
    ScorePercent,
    ResultJson
FROM Attempts
WHERE ResultJson LIKE '%ключевое слово%'
ORDER BY StartedAt DESC;
```

## Редактирование данных

### Исправление ошибочных попыток

Если попытка застряла в статусе `Processing`:

```sql
-- Сбросить статус на Pending для повторной обработки
UPDATE Attempts 
SET GradingStatus = 1, LastUpdatedAt = datetime('now')
WHERE Id = 123 AND GradingStatus = 4;
```

### Удаление тестовых данных

```sql
-- Удалить все попытки старше 30 дней
DELETE FROM Attempts 
WHERE julianday('now') - julianday(StartedAt) > 30;

-- Удалить все попытки с ошибками
DELETE FROM Attempts WHERE GradingStatus = 2;
```

⚠️ **Внимание:** Удаление необратимо! Сделайте бэкап перед удалением данных.

## Экспорт данных

### Экспорт всех попыток

1. Откройте таблицу `Attempts`
2. Нажмите кнопку **"Export"**
3. Выберите формат (CSV, JSON, SQL)
4. Сохраните файл

### Экспорт результатов запроса

1. Перейдите на вкладку **"Query"**
2. Выполните SQL запрос
3. Нажмите **"Export"** над результатами
4. Выберите формат и сохраните

## Безопасность

### ⚠️ Важные предупреждения:

1. **Локальный доступ только:** SQLite Web доступен только на localhost:8050
2. **Нет аутентификации:** Любой, кто имеет доступ к localhost, может редактировать БД
3. **Прямые изменения:** Все изменения применяются немедленно, без подтверждения
4. **Не для продакшена:** Не используйте в продакшене без дополнительной защиты

### Рекомендации:

- ✅ Используйте только для разработки и отладки
- ✅ Делайте бэкапы перед массовыми изменениями
- ✅ Не открывайте порт 8050 для внешнего доступа
- ✅ Для продакшена удалите сервис `sqlite-web` из `docker-compose.yml`

## Отключение SQLite Web

Если не нужен веб-интерфейс, закомментируйте или удалите сервис в `docker-compose.yml`:

```yaml
# sqlite-web:
#   image: coleifer/sqlite-web
#   ...
```

Затем перезапустите:

```powershell
docker-compose up -d
```

## Альтернативы

Если SQLite Web не подходит, можно использовать:

1. **DB Browser for SQLite** (GUI приложение для Windows/Mac/Linux)
2. **DBeaver** (универсальный клиент для баз данных)
3. **sqlite3 CLI** (командная строка)
4. **VS Code расширения** (SQLite Viewer, SQLite)

## Устранение проблем

### Проблема: Порт 8050 уже занят

**Решение:** Измените порт в `docker-compose.yml`:

```yaml
sqlite-web:
  ports:
    - "8051:8080"  # Используйте другой порт
```

### Проблема: База данных заблокирована

**Причина:** HomeCenter использует базу данных.

**Решение:** Это нормально. SQLite Web может читать БД, но запись может быть заблокирована.

### Проблема: Изменения не сохраняются

**Причина:** База данных в режиме только для чтения или заблокирована.

**Решение:** 
1. Убедитесь, что volume `quizdb` смонтирован правильно
2. Проверьте права доступа к файлу БД

## Дополнительная информация

- **Официальный репозиторий:** https://github.com/coleifer/sqlite-web
- **Документация SQLite:** https://www.sqlite.org/docs.html
- **Руководство по бэкапам:** [BACKUP-GUIDE.md](BACKUP-GUIDE.md)
