# Глобальный индикатор загрузки

## Описание

В приложении реализован глобальный индикатор загрузки, который автоматически показывается при любых асинхронных операциях:

- AJAX-запросы (fetch, jQuery.ajax)
- Отправка форм
- Навигация через Pjax

## Возможности

### ✅ Автоматическое отображение

- Индикатор показывается автоматически для всех fetch-запросов
- Автоматически отслеживает jQuery AJAX (если используется)
- Показывается при отправке форм
- Скрывается при завершении операции

### 🎨 Визуальные эффекты

- Полупрозрачный оверлей с размытием фона (backdrop-filter)
- Анимированный спиннер Bootstrap
- Плавные анимации появления/исчезновения
- Минимальное время показа (300мс) для избежания мигания

### ⚙️ Умное поведение

- Задержка показа (200мс) — не показывается для очень быстрых запросов
- Счетчик активных запросов — корректно работает при параллельных операциях
- Автоматический сброс при ошибках и таймаутах

## Использование

### Автоматический режим (рекомендуется)

Индикатор работает автоматически для:

**Fetch запросов:**

```javascript
fetch('/api/data')
    .then(response => response.json())
    .then(data => console.log(data));
// Индикатор показывается автоматически
```

**jQuery AJAX:**

```javascript
$.ajax({
    url: '/api/data',
    success: function(data) {
        console.log(data);
    }
});
// Индикатор показывается автоматически
```

**Отправка форм:**

```html
<form method="post" action="/submit">
    <!-- Индикатор показывается автоматически при submit -->
    <button type="submit">Отправить</button>
</form>
```

### Ручное управление

Если нужно показать индикатор вручную:

```javascript
// Показать индикатор
LoadingIndicator.show();

// Выполнить операцию
doSomething();

// Скрыть индикатор
LoadingIndicator.hide();
```

**Показать немедленно (без задержки):**

```javascript
LoadingIndicator.show(true); // immediate = true
```

**Сбросить состояние:**

```javascript
LoadingIndicator.reset(); // Сбрасывает счетчик и скрывает индикатор
```

### Отключение для конкретных запросов

**Для fetch:**

```javascript
fetch('/api/data', {
    headers: {
        'X-Skip-Loading': 'true'
    }
});
// Индикатор НЕ будет показан
```

**Для форм:**

```html
<form method="post" data-no-loading>
    <!-- Индикатор НЕ будет показан -->
    <button type="submit">Отправить</button>
</form>
```

## CSS классы

### Индикатор для кнопок

Добавьте класс `btn-loading` к кнопке для показа inline-спиннера:

```html
<button class="btn btn-primary btn-loading">
    Загрузка...
</button>
```

Или программно:

```javascript
button.classList.add('btn-loading');
// ... выполнить операцию ...
button.classList.remove('btn-loading');
```

### Inline индикатор

Для показа маленького спиннера внутри текста:

```html
<span>Обработка <span class="loading-inline"></span></span>
```

## Настройка

Параметры можно изменить в `site.js`:

```javascript
var minDisplayTime = 300; // Минимальное время показа (мс)
var showDelay = 200;      // Задержка перед показом (мс)
```

### Изменение стиля

Стили находятся в `site.css`:

```css
.loading-overlay {
    background-color: rgba(0, 0, 0, 0.5); /* Прозрачность фона */
    backdrop-filter: blur(3px);            /* Размытие фона */
}

.loading-spinner {
    background: white;                     /* Цвет фона спиннера */
    padding: 2rem 3rem;                    /* Отступы */
    border-radius: 12px;                   /* Скругление углов */
}
```

## Примеры использования

### Пример 1: AJAX загрузка контента

```javascript
function loadContent(url) {
    // Индикатор показывается автоматически
    fetch(url)
        .then(r => r.text())
        .then(html => {
            document.getElementById('content').innerHTML = html;
        });
    // Индикатор скрывается автоматически
}
```

### Пример 2: Асинхронная операция с ручным управлением

```javascript
async function processData() {
    LoadingIndicator.show();
    
    try {
        await someAsyncOperation();
        await anotherAsyncOperation();
        console.log('Готово!');
    } catch (error) {
        console.error('Ошибка:', error);
    } finally {
        LoadingIndicator.hide();
    }
}
```

### Пример 3: Форма с кастомной обработкой

```html
<form id="myForm" data-no-loading>
    <input type="text" name="data" />
    <button type="submit">Отправить</button>
</form>

<script>
document.getElementById('myForm').addEventListener('submit', function(e) {
    e.preventDefault();
    
    LoadingIndicator.show(true); // Показать немедленно
    
    fetch(this.action, {
        method: 'POST',
        body: new FormData(this)
    })
    .then(r => r.json())
    .then(data => {
        alert('Успех!');
    })
    .finally(() => {
        LoadingIndicator.hide();
    });
});
</script>
```

### Пример 4: Кнопка с индикатором загрузки

```html
<button id="saveBtn" class="btn btn-primary">Сохранить</button>

<script>
document.getElementById('saveBtn').addEventListener('click', async function() {
    const btn = this;
    btn.classList.add('btn-loading');
    btn.disabled = true;
    
    try {
        await saveData();
        alert('Сохранено!');
    } finally {
        btn.classList.remove('btn-loading');
        btn.disabled = false;
    }
});
</script>
```

## Совместимость

- ✅ Работает с vanilla JavaScript (fetch)
- ✅ Работает с jQuery AJAX
- ✅ Работает с Bootstrap 5
- ✅ Поддержка всех современных браузеров
- ✅ Корректно работает с Pjax навигацией

## Отладка

Для отладки можно использовать консоль браузера:

```javascript
// Проверить текущее состояние
console.log('Active requests:', LoadingIndicator.activeRequests);

// Принудительно показать
LoadingIndicator.show(true);

// Принудительно скрыть
LoadingIndicator.reset();
```

## Известные ограничения

1. **Минимальное время показа** — индикатор показывается минимум 300мс, даже если запрос выполнился быстрее. Это сделано для избежания мигания.
2. **Задержка показа** — индикатор показывается с задержкой 200мс. Если запрос выполнится быстрее, индикатор вообще не появится.
3. **Формы с target="_blank"** — индикатор не показывается для форм, открывающихся в новой вкладке.
4. **Таймаут** — при отправке форм индикатор автоматически скрывается через 10 секунд на случай зависания.

## Будущие улучшения

Возможные улучшения:

- Прогресс-бар для длительных операций
- Настраиваемые сообщения (например, "Загрузка...", "Сохранение...", "Обработка...")
- Поддержка отмены операций
- Индикатор для WebSocket соединений

