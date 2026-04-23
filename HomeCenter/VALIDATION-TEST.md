# Тестирование валидации конфигурации

Этот документ описывает, как протестировать новую систему валидации параметров.

## Тест 1: Все параметры заданы (успешная конфигурация)

1. Убедитесь, что `.env` файл содержит все параметры:
  ```env
   Admin__Username=admin
   Admin__Password=admin123
   AI__ApiKey=sk-or-v1-...
   AI__Enabled=true
   Qwen__Enabled=false
  ```
2. Запустите:
  ```powershell
   cd HomeCenter
   dotnet run
  ```
3. **Ожидаемый результат:**
  ```
   === HomeCenter Configuration ===
   Current Directory: C:\HomeRepositories\HomeCenter\HomeCenter
   Looking for .env file at: C:\HomeRepositories\HomeCenter\HomeCenter\.env
   ✓ .env file found, loading...
   ✓ .env file loaded successfully
     - Admin__Username from env: SET
     - AI__ApiKey from env: SET

   === Configuration Status ===
   Admin Username: admin
   Admin Password: SET (length: 9)

   AI Provider: OpenRouter
   AI Enabled: true
   AI Model: openrouter/free
   AI ApiKey: SET (length: 67, starts with: sk-or-v1-9...)

   Qwen Enabled: false
   Qwen ApiKey: NOT SET (Qwen is disabled)

   ✓ All critical configuration parameters are set correctly
   ================================
  ```

## Тест 2: Admin Password не задан

1. Временно закомментируйте в `.env`:
  ```env
   Admin__Username=admin
   # Admin__Password=admin123
  ```
2. Запустите:
  ```powershell
   dotnet run
  ```
3. **Ожидаемый результат (красная ошибка):**
  ```
   === Configuration Status ===
   Admin Username: admin
   ❌ ERROR: Admin Password is NOT SET!
      Please set Admin__Password in .env file or environment variables

   ⚠️  WARNING: Configuration has errors! Please fix them before using the application.
  ```

## Тест 3: AI ApiKey не задан

1. Временно закомментируйте в `.env`:
  ```env
   # AI__ApiKey=sk-or-v1-...
   AI__Enabled=true
  ```
2. Запустите:
  ```powershell
   dotnet run
  ```
3. **Ожидаемый результат (красная ошибка):**
  ```
   AI Provider: OpenRouter
   AI Enabled: true
   AI Model: openrouter/free
   ❌ ERROR: AI ApiKey is NOT SET!
      AI features will NOT work without API key
      Please set AI__ApiKey in .env file or environment variables

   ⚠️  WARNING: Configuration has errors! Please fix them before using the application.
  ```

## Тест 4: Qwen включен, но ApiKey не задан

1. Измените в `.env`:
  ```env
   Qwen__Enabled=true
   # Qwen__ApiKey=sk-...
  ```
2. Запустите:
  ```powershell
   dotnet run
  ```
3. **Ожидаемый результат (красная ошибка):**
  ```
   Qwen Enabled: true
   ❌ ERROR: Qwen is enabled but ApiKey is NOT SET!
      Qwen features will NOT work without API key
      Please set Qwen__ApiKey in .env file or disable Qwen (Qwen__Enabled=false)

   ⚠️  WARNING: Configuration has errors! Please fix them before using the application.
  ```

## Тест 5: Файл .env не найден

1. Временно переименуйте `.env`:
  ```powershell
   Rename-Item .env .env.backup
  ```
2. Запустите:
  ```powershell
   dotnet run
  ```
3. **Ожидаемый результат:**
  ```
   === HomeCenter Configuration ===
   Current Directory: C:\HomeRepositories\HomeCenter\HomeCenter
   Looking for .env file at: C:\HomeRepositories\HomeCenter\HomeCenter\.env
   ✗ .env file NOT FOUND - will use environment variables from Docker/system
  ```
4. Верните файл обратно:
  ```powershell
   Rename-Item .env.backup .env
  ```

## Тест 6: Docker с правильной конфигурацией

1. Убедитесь, что `.env` содержит все параметры
2. Запустите Docker:
  ```powershell
   docker-compose down
   docker-compose up -d --build
   docker-compose logs homecenter
  ```
3. **Ожидаемый результат:**
  ```
   === HomeCenter Configuration ===
   Current Directory: /app
   Looking for .env file at: /app/.env
   ✓ .env file found, loading...
   ✓ .env file loaded successfully
     - Admin__Username from env: SET
     - AI__ApiKey from env: SET

   ✓ All critical configuration parameters are set correctly
   ================================
  ```

## Тест 7: Docker без .env файла

1. Временно переименуйте `.env`:
  ```powershell
   Rename-Item .env .env.backup
  ```
2. Пересоберите Docker:
  ```powershell
   docker-compose down
   docker-compose up -d --build
   docker-compose logs homecenter
  ```
3. **Ожидаемый результат:**
  ```
   ✗ .env file NOT FOUND - will use environment variables from Docker/system

   ❌ ERROR: Admin Password is NOT SET!
   ❌ ERROR: AI ApiKey is NOT SET!

   ⚠️  WARNING: Configuration has errors! Please fix them before using the application.
  ```
4. Верните файл и пересоберите:
  ```powershell
   Rename-Item .env.backup .env
   docker-compose up -d --build
  ```

## Цветовая индикация

- 🟢 **Зеленый текст** - все параметры настроены правильно
- 🔴 **Красный текст** - критичная ошибка
- 🟡 **Желтый текст** - предупреждение о наличии ошибок
- ⚪ **Белый текст** - обычная информация

## Восстановление после тестов

После всех тестов убедитесь, что:

1. Файл `.env` существует и содержит все параметры
2. Все параметры раскомментированы
3. Docker контейнер пересобран с правильной конфигурацией:
  ```powershell
   docker-compose up -d --build
  ```

