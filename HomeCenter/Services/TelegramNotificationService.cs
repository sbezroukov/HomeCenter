using HomeCenter.Models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace HomeCenter.Services;

/// <summary>
/// Сервис для отправки уведомлений через Telegram Bot
/// </summary>
public class TelegramNotificationService : ITelegramNotificationService
{
    private readonly ITelegramBotClient? _botClient;
    private readonly ILogger<TelegramNotificationService> _logger;
    private readonly bool _isEnabled;

    public TelegramNotificationService(IConfiguration configuration, ILogger<TelegramNotificationService> logger)
    {
        _logger = logger;
        var botToken = configuration["Telegram:BotToken"];
        _isEnabled = !string.IsNullOrEmpty(botToken) && 
                     bool.TryParse(configuration["Telegram:Enabled"], out var enabled) && enabled;

        if (_isEnabled && !string.IsNullOrEmpty(botToken))
        {
            try
            {
                _botClient = new TelegramBotClient(botToken);
                _logger.LogInformation("Telegram bot initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Telegram bot");
                _isEnabled = false;
            }
        }
        else
        {
            _logger.LogInformation("Telegram notifications are disabled");
        }
    }

    public async Task SendActivityStartNotificationAsync(ScheduledActivity activity)
    {
        if (!_isEnabled || _botClient == null) return;

        var chatId = activity.AssignedToUser?.TelegramChatId;
        if (!chatId.HasValue) return;

        var timeInfo = activity.StartTime.HasValue 
            ? $"в {activity.StartTime.Value:hh\\:mm}" 
            : "на весь день";

        var message = $"🔔 <b>Напоминание о задаче</b>\n\n" +
                     $"📋 <b>{activity.DisplayTitle}</b>\n" +
                     $"📅 {activity.StartDate:dd.MM.yyyy} {timeInfo}\n";

        if (!string.IsNullOrEmpty(activity.Description))
        {
            message += $"📝 {activity.Description}\n";
        }

        if (activity.DeadlineDateTime.HasValue)
        {
            message += $"⏰ Дедлайн: {activity.DeadlineDateTime.Value:dd.MM.yyyy HH:mm}\n";
        }

        message += "\nУдачи! 💪";

        await SendMessageAsync(chatId.Value, message);
    }

    public async Task SendActivityOverdueNotificationAsync(ScheduledActivity activity)
    {
        if (!_isEnabled || _botClient == null) return;

        var chatId = activity.AssignedToUser?.TelegramChatId;
        if (!chatId.HasValue) return;

        var message = $"⚠️ <b>ЗАДАЧА ПРОСРОЧЕНА</b>\n\n" +
                     $"📋 <b>{activity.DisplayTitle}</b>\n" +
                     $"📅 Дата: {activity.StartDate:dd.MM.yyyy}\n" +
                     $"⏰ Дедлайн был: {activity.DeadlineDateTime:dd.MM.yyyy HH:mm}\n\n" +
                     $"Пожалуйста, завершите задачу как можно скорее!";

        await SendMessageAsync(chatId.Value, message);
    }

    public async Task SendActivityCompletedNotificationAsync(ScheduledActivity activity, ActivityCompletion completion)
    {
        if (!_isEnabled || _botClient == null) return;

        var chatId = activity.AssignedToUser?.TelegramChatId;
        if (!chatId.HasValue) return;

        var statusEmoji = completion.Status switch
        {
            CompletionStatus.Completed => "✅",
            CompletionStatus.NotCompleted => "❌",
            CompletionStatus.PartiallyCompleted => "◐",
            CompletionStatus.Cancelled => "⊘",
            _ => "❓"
        };

        var statusText = completion.Status switch
        {
            CompletionStatus.Completed => "выполнена",
            CompletionStatus.NotCompleted => "не выполнена",
            CompletionStatus.PartiallyCompleted => "частично выполнена",
            CompletionStatus.Cancelled => "отменена",
            _ => "обновлена"
        };

        var message = $"{statusEmoji} <b>Задача {statusText}</b>\n\n" +
                     $"📋 <b>{activity.DisplayTitle}</b>\n" +
                     $"👤 Исполнитель: {completion.CompletedByUser.UserName}\n" +
                     $"🕐 Время: {completion.CompletedAt:dd.MM.yyyy HH:mm}\n";

        if (completion.IsOnTime)
        {
            message += "⏰ В срок ✓\n";
        }
        else
        {
            message += "⏰ С опозданием\n";
        }

        if (!string.IsNullOrEmpty(completion.Comment))
        {
            message += $"\n💬 Комментарий: {completion.Comment}";
        }

        await SendMessageAsync(chatId.Value, message);
    }

    public async Task SendActivityNotClosedNotificationAsync(ScheduledActivity activity)
    {
        if (!_isEnabled || _botClient == null) return;

        var chatId = activity.AssignedToUser?.TelegramChatId;
        if (!chatId.HasValue) return;

        var message = $"⚠️ <b>Задача не закрыта</b>\n\n" +
                     $"📋 <b>{activity.DisplayTitle}</b>\n" +
                     $"📅 Дата: {activity.StartDate:dd.MM.yyyy}\n\n" +
                     $"Не забудьте отметить статус выполнения задачи!";

        await SendMessageAsync(chatId.Value, message);
    }

    public async Task SendMessageAsync(long chatId, string message)
    {
        if (!_isEnabled || _botClient == null)
        {
            _logger.LogWarning("Attempted to send Telegram message but bot is not enabled");
            return;
        }

        try
        {
            await _botClient.SendMessage(
                chatId: chatId,
                text: message,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
            );

            _logger.LogInformation("Telegram notification sent to chat {ChatId}", chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram notification to chat {ChatId}", chatId);
        }
    }

    public async Task<bool> IsBotAvailableAsync()
    {
        if (!_isEnabled || _botClient == null) return false;

        try
        {
            var me = await _botClient.GetMe();
            return me != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check Telegram bot availability");
            return false;
        }
    }
}
