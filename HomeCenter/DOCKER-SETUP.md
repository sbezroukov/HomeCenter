# Docker Setup Guide

## Конфигурация через .env файл

Все настройки (пароли, API ключи) хранятся в `.env` файле, который работает как для локальной разработки, так и для Docker.

## Быстрый старт

1. Скопируйте файл с переменными окружения:
   ```bash
   cd HomeCenter
   cp .env.example .env
   ```

2. Отредактируйте `.env` и укажите свои значения:
   ```env
   Admin__Username=admin
   Admin__Password=ваш-безопасный-пароль
   AI__ApiKey=ваш-openrouter-ключ
   AI__Enabled=true
   ```

3. Запустите Docker:
   ```bash
   docker-compose up -d --build
   ```

**Важно:** `.env` файл добавлен в `.gitignore` и не будет коммититься в репозиторий!

## Локальная разработка

Тот же `.env` файл работает и для локальной разработки:

```bash
cd HomeCenter
cp .env.example .env
# Отредактируйте .env
dotnet run
```

Приложение автоматически загрузит переменные из `.env` при запуске.

## Проверка конфигурации

После запуска контейнера проверьте логи:

```bash
docker-compose logs homecenter
```

Вы увидите вывод:

```
=== HomeCenter Configuration ===
Environment: Production
ContentRootPath: /app

=== Configuration Status ===
Admin Username: admin
Admin Password: SET (length: 20)

AI Provider: OpenRouter
AI Enabled: true
AI Model: openrouter/free
AI ApiKey: SET (length: 67, starts with: sk-or-v1-9...)

Qwen Enabled: false
Qwen ApiKey: NOT SET

Connection String: Data Source=/app/data/quiz.db
================================
```

## Почему .env вместо appsettings?

Раньше секреты хранились в `appsettings.Development.json`, который нужно было добавлять в `.gitignore`. Теперь используется `.env` файл:

**Преимущества:**
- Один файл для всех окружений (Development и Production)
- Стандартный подход (как в Node.js, Python, etc.)
- `appsettings.Development.json` теперь можно коммитить (нет секретов)
- Не нужно пересобирать Docker образ при изменении настроек

## Приоритет конфигурации в .NET

.NET загружает конфигурацию в следующем порядке (последующие переопределяют предыдущие):

1. `appsettings.json`
2. `appsettings.Development.json` (только структура, без секретов)
3. Переменные окружения (из `.env` файла)

Переменные окружения имеют наивысший приоритет!
