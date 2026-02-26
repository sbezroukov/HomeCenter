using HomeCenter.Data;
using Microsoft.EntityFrameworkCore;

namespace HomeCenter.Services;

/// <summary>
/// Фоновая служба для отправки уведомлений по расписанию
/// </summary>
public class CalendarNotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CalendarNotificationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);

    public CalendarNotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<CalendarNotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Calendar Notification Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing calendar notifications");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Calendar Notification Background Service stopped");
    }

    private async Task ProcessNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var calendarService = scope.ServiceProvider.GetRequiredService<ICalendarService>();
        var telegramService = scope.ServiceProvider.GetRequiredService<ITelegramNotificationService>();

        var now = DateTime.UtcNow;

        // 1. Уведомления о начале задач (за 15 минут до начала)
        await SendUpcomingActivityNotificationsAsync(calendarService, telegramService, cancellationToken);

        // 2. Уведомления о просроченных задачах
        await SendOverdueActivityNotificationsAsync(calendarService, telegramService, cancellationToken);

        // 3. Уведомления о незакрытых задачах (прошло больше часа после дедлайна)
        await SendNotClosedActivityNotificationsAsync(calendarService, telegramService, cancellationToken);
    }

    private async Task SendUpcomingActivityNotificationsAsync(
        ICalendarService calendarService,
        ITelegramNotificationService telegramService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Получаем задачи, которые начнутся в ближайший час
            var upcomingActivities = await calendarService.GetUpcomingActivitiesAsync(hours: 1);

            foreach (var activity in upcomingActivities)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Проверяем, что у пользователя есть Telegram Chat ID
                if (activity.AssignedToUser?.TelegramChatId.HasValue == true)
                {
                    await telegramService.SendActivityStartNotificationAsync(activity);
                    _logger.LogInformation("Sent start notification for activity {ActivityId}", activity.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending upcoming activity notifications");
        }
    }

    private async Task SendOverdueActivityNotificationsAsync(
        ICalendarService calendarService,
        ITelegramNotificationService telegramService,
        CancellationToken cancellationToken)
    {
        try
        {
            var overdueActivities = await calendarService.GetOverdueActivitiesAsync();

            foreach (var activity in overdueActivities)
            {
                if (cancellationToken.IsCancellationRequested) break;

                if (activity.AssignedToUser?.TelegramChatId.HasValue == true)
                {
                    // Отправляем уведомление о просрочке раз в день
                    var lastNotificationTime = activity.UpdatedAt ?? activity.CreatedAt;
                    if (DateTime.UtcNow - lastNotificationTime > TimeSpan.FromHours(24))
                    {
                        await telegramService.SendActivityOverdueNotificationAsync(activity);
                        _logger.LogInformation("Sent overdue notification for activity {ActivityId}", activity.Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending overdue activity notifications");
        }
    }

    private async Task SendNotClosedActivityNotificationsAsync(
        ICalendarService calendarService,
        ITelegramNotificationService telegramService,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var oneHourAgo = DateTime.UtcNow.AddHours(-1);

            // Находим задачи, у которых прошел дедлайн более часа назад и нет отметки о выполнении
            var notClosedActivities = await context.ScheduledActivities
                .Include(sa => sa.AssignedToUser)
                .Include(sa => sa.Completions)
                .Where(sa => sa.IsActive
                    && sa.DeadlineDateTime.HasValue
                    && sa.DeadlineDateTime.Value < oneHourAgo
                    && !sa.Completions.Any())
                .ToListAsync(cancellationToken);

            foreach (var activity in notClosedActivities)
            {
                if (cancellationToken.IsCancellationRequested) break;

                if (activity.AssignedToUser?.TelegramChatId.HasValue == true)
                {
                    await telegramService.SendActivityNotClosedNotificationAsync(activity);
                    _logger.LogInformation("Sent not closed notification for activity {ActivityId}", activity.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending not closed activity notifications");
        }
    }
}
