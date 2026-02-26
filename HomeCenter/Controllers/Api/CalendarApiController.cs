using HomeCenter.Models;
using HomeCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeCenter.Controllers.Api;

/// <summary>
/// API контроллер для работы с календарем (для мобильных приложений и интеграций)
/// </summary>
[ApiController]
[Route("api/calendar")]
[Authorize]
public class CalendarApiController : ControllerBase
{
    private readonly ICalendarService _calendarService;
    private readonly IAuthService _authService;
    private readonly ITelegramNotificationService _telegramService;
    private readonly ILogger<CalendarApiController> _logger;

    public CalendarApiController(
        ICalendarService calendarService,
        IAuthService authService,
        ITelegramNotificationService telegramService,
        ILogger<CalendarApiController> logger)
    {
        _calendarService = calendarService;
        _authService = authService;
        _telegramService = telegramService;
        _logger = logger;
    }

    /// <summary>
    /// Получить активности на неделю
    /// </summary>
    [HttpGet("week")]
    public async Task<IActionResult> GetWeekActivities([FromQuery] DateTime? date)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var targetDate = date ?? DateTime.Today;
        var weekStart = GetWeekStart(targetDate);

        var isAdmin = User.IsInRole("Admin");
        var activities = await _calendarService.GetActivitiesForWeekAsync(
            weekStart,
            isAdmin ? null : currentUser.Id);

        return Ok(activities);
    }

    /// <summary>
    /// Получить активности на день
    /// </summary>
    [HttpGet("day")]
    public async Task<IActionResult> GetDayActivities([FromQuery] DateTime? date)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var targetDate = date ?? DateTime.Today;
        var isAdmin = User.IsInRole("Admin");
        var activities = await _calendarService.GetActivitiesForDayAsync(
            targetDate,
            isAdmin ? null : currentUser.Id);

        return Ok(activities);
    }

    /// <summary>
    /// Получить конкретную активность
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetActivity(int id)
    {
        var activity = await _calendarService.GetActivityByIdAsync(id);
        if (activity == null)
        {
            return NotFound();
        }

        return Ok(activity);
    }

    /// <summary>
    /// Отметить выполнение активности
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteActivity(int id, [FromBody] CompleteActivityRequest request)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var activity = await _calendarService.GetActivityByIdAsync(id);
        if (activity == null)
        {
            return NotFound(new { message = "Активность не найдена" });
        }

        try
        {
            var completion = await _calendarService.CompleteActivityAsync(
                id,
                currentUser.Id,
                request.Status,
                request.Comment);

            // Отправляем уведомление о завершении
            await _telegramService.SendActivityCompletedNotificationAsync(activity, completion);

            return Ok(new
            {
                message = "Статус активности обновлен",
                completion
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing activity {ActivityId}", id);
            return BadRequest(new { message = "Ошибка при обновлении статуса" });
        }
    }

    /// <summary>
    /// Получить просроченные активности
    /// </summary>
    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdueActivities()
    {
        var activities = await _calendarService.GetOverdueActivitiesAsync();
        return Ok(activities);
    }

    /// <summary>
    /// Получить предстоящие активности
    /// </summary>
    [HttpGet("upcoming")]
    public async Task<IActionResult> GetUpcomingActivities([FromQuery] int hours = 24)
    {
        var activities = await _calendarService.GetUpcomingActivitiesAsync(hours);
        return Ok(activities);
    }

    /// <summary>
    /// Получить статистику выполнения
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var fromDate = from ?? DateTime.Today.AddMonths(-1);
        var toDate = to ?? DateTime.Today;

        var statistics = await _calendarService.GetCompletionStatisticsAsync(
            currentUser.Id,
            fromDate,
            toDate);

        return Ok(statistics);
    }

    private DateTime GetWeekStart(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var diff = dayOfWeek == 0 ? -6 : 1 - dayOfWeek;
        return date.AddDays(diff).Date;
    }
}

/// <summary>
/// Модель запроса для отметки выполнения
/// </summary>
public class CompleteActivityRequest
{
    public CompletionStatus Status { get; set; }
    public string? Comment { get; set; }
}
