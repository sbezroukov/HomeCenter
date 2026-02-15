# Быстрый старт SQLite Web

## Запуск

```powershell
cd HomeCenter
docker-compose up -d
```

## Доступ

Откройте в браузере: **http://localhost:8050**

## Что можно делать

### 1. Просмотр таблиц
- Клик по таблице в левой панели
- Просмотр всех записей
- Сортировка по столбцам

### 2. SQL запросы

Вкладка **"Query"** → введите SQL:

```sql
-- Последние 10 попыток
SELECT * FROM Attempts ORDER BY StartedAt DESC LIMIT 10;

-- Попытки с ошибками
SELECT * FROM Attempts WHERE GradingStatus = 2;

-- Статистика по темам
SELECT TopicId, COUNT(*) as Count, AVG(ScorePercent) as AvgScore
FROM Attempts
GROUP BY TopicId;
```

### 3. Редактирование

- Клик по записи → форма редактирования
- Изменить поля → Save

⚠️ Изменения сразу сохраняются в БД!

### 4. Экспорт

- Кнопка **"Export"** → выбрать формат (CSV/JSON/SQL)

## Основные таблицы

- **Topics** - темы тестов
- **Attempts** - попытки прохождения
- **SchemaVersions** - версии схемы БД

## Полная документация

См. **[SQLITE-WEB-GUIDE.md](SQLITE-WEB-GUIDE.md)** для подробной информации.

## Остановка

```powershell
docker-compose down
```

## Безопасность

⚠️ SQLite Web доступен только локально (localhost:8050)  
⚠️ Не используйте в продакшене без дополнительной защиты
