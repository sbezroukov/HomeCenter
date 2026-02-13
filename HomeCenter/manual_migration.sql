-- Ручная миграция для добавления полей асинхронной обработки AI
-- Выполните этот скрипт, если автоматическая миграция не сработала

-- Добавляем колонку GradingStatus
ALTER TABLE Attempts ADD COLUMN GradingStatus INTEGER NOT NULL DEFAULT 0;

-- Добавляем колонку LastUpdatedAt
ALTER TABLE Attempts ADD COLUMN LastUpdatedAt TEXT NOT NULL DEFAULT (datetime('now'));

-- Добавляем колонку GradingError
ALTER TABLE Attempts ADD COLUMN GradingError TEXT;

-- Проверка: показать структуру таблицы Attempts
-- PRAGMA table_info(Attempts);
