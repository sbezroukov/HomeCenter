using HomeCenter.Models;

namespace HomeCenter.Services;

/// <summary>
/// Интерфейс сервиса для отправки уведомлений через Telegram
/// </summary>
public interface ITelegramNotificationService
{
    /// <summary>
    /// Отправка уведомления о начале задачи
    /// </summary>
    Task SendActivityStartNotificationAsync(ScheduledActivity activity);

    /// <summary>
    /// Отправка уведомления о просроченной задаче
    /// </summary>
    Task SendActivityOverdueNotificationAsync(ScheduledActivity activity);

    /// <summary>
    /// Отправка уведомления о завершении задачи
    /// </summary>
    Task SendActivityCompletedNotificationAsync(ScheduledActivity activity, ActivityCompletion completion);

    /// <summary>
    /// Отправка уведомления о незакрытой задаче
    /// </summary>
    Task SendActivityNotClosedNotificationAsync(ScheduledActivity activity);

    /// <summary>
    /// Отправка произвольного сообщения пользователю
    /// </summary>
    Task SendMessageAsync(long chatId, string message);

    /// <summary>
    /// Проверка, доступен ли бот
    /// </summary>
    Task<bool> IsBotAvailableAsync();
}
