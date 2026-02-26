using HomeCenter.Models;
using HomeCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace HomeCenter.Controllers;

/// <summary>
/// Контроллер для работы с календарем и расписанием
/// </summary>
[Authorize]
public class CalendarController : Controller
{
    private readonly ICalendarService _calendarService;
    private readonly IAuthService _authService;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(
        ICalendarService calendarService,
        IAuthService authService,
        ILogger<CalendarController> logger)
    {
        _calendarService = calendarService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Главная страница календаря - недельный вид
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? date)
    {
        var targetDate = date ?? DateTime.Today;
        var weekStart = GetWeekStart(targetDate);
        var weekEnd = weekStart.AddDays(7);

        var currentUser = await _authService.GetCurrentUserAsync(User);
        var isAdmin = User.IsInRole("Admin");

        // Получаем активности для недели
        var activities = await _calendarService.GetActivitiesForWeekAsync(
            weekStart, 
            isAdmin ? null : currentUser?.Id);

        // Получаем типы деятельности и пользователей для фильтров
        var activityTypes = await _calendarService.GetAllActivityTypesAsync();
        var users = isAdmin ? await _authService.GetAllUsersAsync() : new List<ApplicationUser>();

        // Формируем модель представления
        var model = new WeekCalendarViewModel
        {
            WeekStartDate = weekStart,
            WeekEndDate = weekEnd,
            AvailableActivityTypes = activityTypes,
            AvailableUsers = users,
            Days = new List<DayScheduleViewModel>()
        };

        // Заполняем дни недели
        var russianCulture = new CultureInfo("ru-RU");
        for (int i = 0; i < 7; i++)
        {
            var dayDate = weekStart.AddDays(i);
            var dayActivities = activities.Where(a => a.StartDate.Date == dayDate.Date).ToList();

            var daySchedule = new DayScheduleViewModel
            {
                Date = dayDate,
                DayName = russianCulture.DateTimeFormat.GetDayName(dayDate.DayOfWeek),
                IsToday = dayDate.Date == DateTime.Today,
                Activities = dayActivities.Select(a => MapToViewModel(a)).ToList()
            };

            model.Days.Add(daySchedule);
        }

        return View(model);
    }

    /// <summary>
    /// Форма создания новой активности
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create(DateTime? date)
    {
        var activityTypes = await _calendarService.GetAllActivityTypesAsync();
        var users = await _authService.GetAllUsersAsync();

        // Получаем список доступных тестов
        var tests = await _calendarService.GetAvailableTestsAsync();

        var model = new CreateEditActivityViewModel
        {
            StartDate = date ?? DateTime.Today,
            AvailableActivityTypes = activityTypes,
            AvailableUsers = users,
            AvailableTests = tests
        };

        return View(model);
    }

    /// <summary>
    /// Создание новой активности
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateEditActivityViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableActivityTypes = await _calendarService.GetAllActivityTypesAsync();
            model.AvailableUsers = await _authService.GetAllUsersAsync();
            return View(model);
        }

        var currentUser = await _authService.GetCurrentUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var activity = new ScheduledActivity
        {
            ActivityTypeId = model.ActivityTypeId,
            Title = model.Title,
            Description = model.Description,
            StartDate = model.StartDate,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            DeadlineDateTime = model.DeadlineDateTime,
            AssignedToUserId = model.AssignedToUserId,
            CreatedByUserId = currentUser.Id,
            IsRecurring = model.IsRecurring,
            RecurringDayOfWeek = model.RecurringDayOfWeek
        };

        await _calendarService.CreateActivityAsync(activity);

        TempData["SuccessMessage"] = "Активность успешно создана";
        return RedirectToAction(nameof(Index), new { date = model.StartDate });
    }

    /// <summary>
    /// Форма редактирования активности
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var activity = await _calendarService.GetActivityByIdAsync(id);
        if (activity == null)
        {
            return NotFound();
        }

        var currentUser = await _authService.GetCurrentUserAsync(User);
        var isAdmin = User.IsInRole("Admin");

        // Проверяем права на редактирование
        if (!isAdmin && activity.CreatedByUserId != currentUser?.Id)
        {
            return Forbid();
        }

        var activityTypes = await _calendarService.GetAllActivityTypesAsync();
        var users = await _authService.GetAllUsersAsync();

        var model = new CreateEditActivityViewModel
        {
            Id = activity.Id,
            ActivityTypeId = activity.ActivityTypeId,
            Title = activity.Title,
            Description = activity.Description,
            StartDate = activity.StartDate,
            StartTime = activity.StartTime,
            EndTime = activity.EndTime,
            DeadlineDateTime = activity.DeadlineDateTime,
            AssignedToUserId = activity.AssignedToUserId,
            IsRecurring = activity.IsRecurring,
            RecurringDayOfWeek = activity.RecurringDayOfWeek,
            AvailableActivityTypes = activityTypes,
            AvailableUsers = users
        };

        return View(model);
    }

    /// <summary>
    /// Обновление активности
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreateEditActivityViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            model.AvailableActivityTypes = await _calendarService.GetAllActivityTypesAsync();
            model.AvailableUsers = await _authService.GetAllUsersAsync();
            return View(model);
        }

        var activity = await _calendarService.GetActivityByIdAsync(id);
        if (activity == null)
        {
            return NotFound();
        }

        var currentUser = await _authService.GetCurrentUserAsync(User);
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && activity.CreatedByUserId != currentUser?.Id)
        {
            return Forbid();
        }

        activity.ActivityTypeId = model.ActivityTypeId;
        activity.Title = model.Title;
        activity.Description = model.Description;
        activity.StartDate = model.StartDate;
        activity.StartTime = model.StartTime;
        activity.EndTime = model.EndTime;
        activity.DeadlineDateTime = model.DeadlineDateTime;
        activity.AssignedToUserId = model.AssignedToUserId;
        activity.IsRecurring = model.IsRecurring;
        activity.RecurringDayOfWeek = model.RecurringDayOfWeek;

        await _calendarService.UpdateActivityAsync(activity);

        TempData["SuccessMessage"] = "Активность успешно обновлена";
        return RedirectToAction(nameof(Index), new { date = model.StartDate });
    }

    /// <summary>
    /// Удаление активности
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var activity = await _calendarService.GetActivityByIdAsync(id);
        if (activity == null)
        {
            return NotFound();
        }

        var currentUser = await _authService.GetCurrentUserAsync(User);
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin && activity.CreatedByUserId != currentUser?.Id)
        {
            return Forbid();
        }

        await _calendarService.DeleteActivityAsync(id);

        TempData["SuccessMessage"] = "Активность успешно удалена";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Форма отметки выполнения
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Complete(int id)
    {
        var activity = await _calendarService.GetActivityByIdAsync(id);
        if (activity == null)
        {
            return NotFound();
        }

        var model = new CompleteActivityViewModel
        {
            ActivityId = activity.Id,
            ActivityTitle = activity.DisplayTitle,
            Status = CompletionStatus.Completed
        };

        return View(model);
    }

    /// <summary>
    /// Отметка выполнения активности
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(CompleteActivityViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var currentUser = await _authService.GetCurrentUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        await _calendarService.CompleteActivityAsync(
            model.ActivityId,
            currentUser.Id,
            model.Status,
            model.Comment);

        TempData["SuccessMessage"] = "Статус активности обновлен";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Копировать расписание с прошлой недели
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyFromPreviousWeek(DateTime weekStart)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        var previousWeekStart = weekStart.AddDays(-7);
        await _calendarService.CopyWeekScheduleAsync(previousWeekStart, weekStart, currentUser.Id);

        TempData["SuccessMessage"] = "Расписание с прошлой недели успешно скопировано";
        return RedirectToAction(nameof(Index), new { date = weekStart });
    }

    /// <summary>
    /// Копировать день
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CopyDay(DateTime sourceDate, DateTime targetDate)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        await _calendarService.CopyDayScheduleAsync(sourceDate, targetDate, currentUser.Id);

        TempData["SuccessMessage"] = $"День {sourceDate:dd.MM.yyyy} скопирован на {targetDate:dd.MM.yyyy}";
        return RedirectToAction(nameof(Index), new { date = targetDate });
    }

    /// <summary>
    /// Перенести день
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> MoveDay(DateTime sourceDate, DateTime targetDate)
    {
        await _calendarService.MoveDayScheduleAsync(sourceDate, targetDate);

        TempData["SuccessMessage"] = $"День {sourceDate:dd.MM.yyyy} перенесен на {targetDate:dd.MM.yyyy}";
        return RedirectToAction(nameof(Index), new { date = targetDate });
    }

    /// <summary>
    /// Страница статистики
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Statistics(DateTime? from, DateTime? to)
    {
        var fromDate = from ?? DateTime.Today.AddMonths(-1);
        var toDate = to ?? DateTime.Today;

        var currentUser = await _authService.GetCurrentUserAsync(User);
        var isAdmin = User.IsInRole("Admin");

        var stats = await _calendarService.GetDetailedStatisticsAsync(
            fromDate,
            toDate,
            isAdmin ? null : currentUser?.Id);

        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.IsAdmin = isAdmin;

        return View(stats);
    }

    /// <summary>
    /// Получение начала недели (понедельник)
    /// </summary>
    private DateTime GetWeekStart(DateTime date)
    {
        var dayOfWeek = (int)date.DayOfWeek;
        var diff = dayOfWeek == 0 ? -6 : 1 - dayOfWeek; // Понедельник как начало недели
        return date.AddDays(diff).Date;
    }

    /// <summary>
    /// Маппинг модели в ViewModel
    /// </summary>
    private ScheduledActivityViewModel MapToViewModel(ScheduledActivity activity)
    {
        var completion = activity.Completions.OrderByDescending(c => c.CompletedAt).FirstOrDefault();
        var isOverdue = activity.DeadlineDateTime.HasValue 
            && activity.DeadlineDateTime.Value < DateTime.UtcNow 
            && completion?.Status != CompletionStatus.Completed;

        return new ScheduledActivityViewModel
        {
            Id = activity.Id,
            Title = activity.DisplayTitle,
            Description = activity.Description,
            ActivityTypeName = activity.ActivityType.Name,
            ActivityTypeColor = activity.ActivityType.Color,
            StartDate = activity.StartDate,
            StartTime = activity.StartTime,
            EndTime = activity.EndTime,
            DeadlineDateTime = activity.DeadlineDateTime,
            AssignedToUserName = activity.AssignedToUser?.UserName,
            IsAllDay = activity.IsAllDay,
            IsCompleted = completion?.Status == CompletionStatus.Completed,
            CompletionStatus = completion?.Status,
            IsOverdue = isOverdue,
            CompletionComment = completion?.Comment
        };
    }
}
