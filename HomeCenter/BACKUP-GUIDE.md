# Руководство по бэкапу SQLite в Docker Desktop

## Обзор

Простая система резервного копирования SQLite базы данных для Docker Desktop на Windows.

### Что включено:

- **Ручной бэкап** - создать бэкап в любой момент
- **Автоматический бэкап** - настроить ежедневный бэкап через Windows Task Scheduler
- **Восстановление** - восстановить базу из любого бэкапа
- **Управление** - просмотр и очистка старых бэкапов

## Быстрый старт

### 1. Создать бэкап прямо сейчас

```powershell
cd HomeCenter
.\scripts\backup.ps1
```

Бэкап сохранится в `.\backups\quiz-YYYYMMDD-HHMMSS.db`

### 2. Посмотреть список бэкапов

```powershell
.\scripts\list-backups.ps1
```

### 3. Восстановить из последнего бэкапа

```powershell
.\scripts\restore.ps1
```

Или из конкретного файла:

```powershell
.\scripts\restore.ps1 -BackupFile .\backups\quiz-20240214-120000.db
```

### 4. Настроить автоматический бэкап

```powershell
# Запустить PowerShell от имени Администратора
.\scripts\auto-backup.ps1
```

Это создаст задачу в Windows Task Scheduler для ежедневного бэкапа в 2:00 ночи.

## Подробное описание

### Ручной бэкап

Скрипт `backup.ps1` делает следующее:

1. Проверяет что Docker контейнер запущен
2. Копирует базу данных из контейнера
3. Сохраняет в `.\backups\` с меткой времени
4. Показывает список всех бэкапов
5. Предлагает удалить старые бэкапы (>30 дней)

**Использование:**

```powershell
# Создать бэкап
.\scripts\backup.ps1

# Пример вывода:
# Creating backup...
#   Container: homecenter
#   Source: /app/data/quiz.db
#   Destination: .\backups\quiz-20240214-153045.db
#
# ✓ Backup created successfully!
#   File: .\backups\quiz-20240214-153045.db
#   Size: 256.50 KB
```

### Восстановление

Скрипт `restore.ps1` делает следующее:

1. Находит последний бэкап (или использует указанный файл)
2. Запрашивает подтверждение
3. Создаёт бэкап текущей базы (на всякий случай)
4. Останавливает контейнер
5. Копирует бэкап в контейнер
6. Запускает контейнер обратно

**Использование:**

```powershell
# Восстановить из последнего бэкапа
.\scripts\restore.ps1

# Восстановить из конкретного файла
.\scripts\restore.ps1 -BackupFile .\backups\quiz-20240214-120000.db

# Пример вывода:
# WARNING: This will replace the current database!
#   Backup file: .\backups\quiz-20240214-120000.db
#   Container: homecenter
#   Destination: /app/data/quiz.db
#
# Are you sure? (yes/no): yes
#
# Step 1: Creating backup of current database...
# ✓ Current database backed up to: .\backups\quiz-before-restore-20240214-153100.db
#
# Step 2: Stopping container...
# ✓ Container stopped
#
# Step 3: Copying backup file to container...
# ✓ Backup file copied
#
# Step 4: Starting container...
# ✓ Container started
#
# ✓ Database restored successfully!
```

### Автоматический бэкап

Скрипт `auto-backup.ps1` настраивает Windows Task Scheduler для автоматического бэкапа.

**Требования:**
- PowerShell запущен от имени Администратора

**Использование:**

```powershell
# Запустить PowerShell от имени Администратора
# Правый клик на PowerShell → "Запуск от имени администратора"

cd C:\HomeRepositories\HomeCenter\HomeCenter
.\scripts\auto-backup.ps1
```

**Что создаётся:**
- Задача "HomeCenter Database Backup" в Task Scheduler
- Запуск каждый день в 2:00 ночи
- Бэкапы сохраняются в `.\backups\`

**Управление задачей:**

1. Открыть Task Scheduler: `Win+R` → `taskschd.msc`
2. Найти задачу "HomeCenter Database Backup"
3. Правый клик → Выполнить (для тестирования)
4. Правый клик → Отключить (для отключения)
5. Правый клик → Удалить (для удаления)

**Изменить расписание:**

1. Открыть Task Scheduler
2. Найти задачу "HomeCenter Database Backup"
3. Правый клик → Свойства
4. Вкладка "Триггеры" → Изменить
5. Установить нужное время

### Просмотр бэкапов

Скрипт `list-backups.ps1` показывает все доступные бэкапы.

**Использование:**

```powershell
.\scripts\list-backups.ps1

# Пример вывода:
# === Available Backups ===
#
# Backup File                    Size      Created              Age
# -----------                    ----      -------              ---
# quiz-20240214-153045.db       256.50 KB 2024-02-14 15:30:45  2 hours ago
# quiz-20240214-120000.db       254.20 KB 2024-02-14 12:00:00  5 hours ago
# quiz-20240213-020000.db       248.10 KB 2024-02-13 02:00:00  1 days ago
#
# Total backups: 3
# Total size: 0.76 MB
# Oldest backup: 2024-02-13 02:00:00
# Newest backup: 2024-02-14 15:30:45
```

## Где хранятся данные

### В Docker

База данных хранится в Docker volume `quizdb`:

```powershell
# Посмотреть информацию о volume
docker volume inspect quizdb

# Путь в контейнере
/app/data/quiz.db
```

### Локальные бэкапы

Бэкапы сохраняются в:

```
C:\HomeRepositories\HomeCenter\HomeCenter\backups\
  ├── quiz-20240214-153045.db
  ├── quiz-20240214-120000.db
  └── quiz-20240213-020000.db
```

**Важно:** Директория `backups\` добавлена в `.gitignore` и не коммитится в репозиторий!

## Сценарии использования

### Перед обновлением приложения

```powershell
# 1. Создать бэкап
.\scripts\backup.ps1

# 2. Обновить приложение
docker-compose down
docker-compose pull
docker-compose up -d --build

# 3. Проверить что всё работает
# Если что-то сломалось:
.\scripts\restore.ps1
```

### Перенос на другой компьютер

**На старом компьютере:**

```powershell
# Создать бэкап
.\scripts\backup.ps1

# Скопировать файл бэкапа на флешку или в облако
# Файл находится в .\backups\quiz-YYYYMMDD-HHMMSS.db
```

**На новом компьютере:**

```powershell
# 1. Склонировать репозиторий
git clone <repo-url>
cd HomeCenter

# 2. Создать .env файл
cp .env.example .env
# Отредактировать .env

# 3. Запустить приложение
docker-compose up -d

# 4. Скопировать бэкап в директорию backups
mkdir backups
copy D:\backup\quiz-20240214-153045.db .\backups\

# 5. Восстановить базу
.\scripts\restore.ps1 -BackupFile .\backups\quiz-20240214-153045.db
```

### Тестирование изменений

```powershell
# 1. Создать бэкап перед тестированием
.\scripts\backup.ps1

# 2. Внести изменения, протестировать

# 3. Если что-то пошло не так, откатиться
.\scripts\restore.ps1
```

### Очистка старых бэкапов

```powershell
# Посмотреть список
.\scripts\list-backups.ps1

# Удалить вручную
Remove-Item .\backups\quiz-20240101-*.db

# Или при создании нового бэкапа скрипт предложит удалить старые (>30 дней)
.\scripts\backup.ps1
```

## Troubleshooting

### Контейнер не запущен

**Ошибка:**
```
ERROR: Container 'homecenter' is not running!
```

**Решение:**
```powershell
# Запустить контейнер
docker-compose up -d

# Проверить статус
docker ps
```

### Не хватает прав

**Ошибка при настройке автобэкапа:**
```
ERROR: This script requires Administrator privileges!
```

**Решение:**
1. Закрыть PowerShell
2. Правый клик на PowerShell → "Запуск от имени администратора"
3. Перейти в директорию проекта
4. Запустить скрипт снова

### База данных заблокирована

Если база данных используется приложением, копирование может создать неконсистентную копию.

**Решение:**

```powershell
# Вариант 1: Остановить контейнер перед бэкапом
docker-compose stop
.\scripts\backup.ps1
docker-compose start

# Вариант 2: Использовать SQLite backup команду (требует exec в контейнер)
docker exec homecenter sqlite3 /app/data/quiz.db ".backup /app/data/quiz-backup.db"
docker cp homecenter:/app/data/quiz-backup.db .\backups\quiz-manual.db
```

### Бэкап не создаётся

**Проверить:**

```powershell
# 1. Контейнер запущен?
docker ps | findstr homecenter

# 2. База данных существует?
docker exec homecenter ls -la /app/data/

# 3. Есть права на запись в директорию backups?
Test-Path .\backups -PathType Container
```

## Рекомендации

### Частота бэкапов

- **Минимум:** 1 раз в день (автоматически в 2:00)
- **Рекомендуется:** Перед каждым важным изменением (вручную)
- **Для production:** Каждые 6-12 часов + перед обновлениями

### Хранение бэкапов

1. **Локально:** `.\backups\` (удобно для быстрого восстановления)
2. **Облако:** Google Drive, OneDrive, Dropbox (защита от потери диска)
3. **Внешний диск:** USB флешка или внешний HDD (защита от сбоя компьютера)

**Правило 3-2-1:**
- 3 копии данных
- 2 разных носителя
- 1 копия вне офиса/дома

### Тестирование восстановления

Регулярно проверяйте что бэкапы работают:

```powershell
# 1 раз в месяц
.\scripts\restore.ps1

# Проверить что приложение работает
# http://localhost:8080
```

### Размер базы данных

SQLite хорошо работает до ~100GB, но для Docker Desktop рекомендуется:

- **Отлично:** < 1 GB
- **Хорошо:** 1-10 GB
- **Нужно думать о PostgreSQL:** > 10 GB

Проверить размер:

```powershell
docker exec homecenter ls -lh /app/data/quiz.db
```

## Миграция на PostgreSQL (будущее)

Если база вырастет или понадобится высокая доступность, можно мигрировать на PostgreSQL:

1. Экспортировать данные из SQLite
2. Установить PostgreSQL в Docker Compose
3. Импортировать данные
4. Изменить Connection String в `.env`

Подробнее см. `docs/POSTGRESQL-MIGRATION.md` (когда понадобится).

## Дополнительные скрипты

### Экспорт в SQL

```powershell
# Экспортировать базу в SQL файл
docker exec homecenter sqlite3 /app/data/quiz.db .dump > .\backups\quiz-dump.sql

# Импортировать обратно
Get-Content .\backups\quiz-dump.sql | docker exec -i homecenter sqlite3 /app/data/quiz.db
```

### Проверка целостности

```powershell
# Проверить целостность базы данных
docker exec homecenter sqlite3 /app/data/quiz.db "PRAGMA integrity_check;"

# Должно вывести: ok
```

### Оптимизация базы

```powershell
# Оптимизировать базу (VACUUM)
docker exec homecenter sqlite3 /app/data/quiz.db "VACUUM;"
```

## Поддержка

Если возникли проблемы:

1. Проверьте логи контейнера: `docker-compose logs`
2. Проверьте что контейнер запущен: `docker ps`
3. Создайте issue в репозитории с описанием проблемы
