using HomeCenter.Models;
using HomeCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeCenter.Controllers;

/// <summary>
/// Контроллер для управления справочником типов деятельности
/// </summary>
[Authorize(Roles = "Admin")]
public class ActivityTypeController : Controller
{
    private readonly ICalendarService _calendarService;
    private readonly ILogger<ActivityTypeController> _logger;

    public ActivityTypeController(
        ICalendarService calendarService,
        ILogger<ActivityTypeController> logger)
    {
        _calendarService = calendarService;
        _logger = logger;
    }

    /// <summary>
    /// Список всех типов деятельности
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var activityTypes = await _calendarService.GetAllActivityTypesAsync(includeInactive: true);
        return View(activityTypes);
    }

    /// <summary>
    /// Форма создания нового типа деятельности
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        var model = new ActivityTypeViewModel
        {
            Color = "#007bff",
            IsActive = true
        };
        return View(model);
    }

    /// <summary>
    /// Создание нового типа деятельности
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityTypeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var activityType = new ActivityType
        {
            Name = model.Name,
            Description = model.Description,
            Color = model.Color,
            IsActive = model.IsActive
        };

        await _calendarService.CreateActivityTypeAsync(activityType);

        TempData["SuccessMessage"] = "Тип деятельности успешно создан";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Форма редактирования типа деятельности
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var activityType = await _calendarService.GetActivityTypeByIdAsync(id);
        if (activityType == null)
        {
            return NotFound();
        }

        var model = new ActivityTypeViewModel
        {
            Id = activityType.Id,
            Name = activityType.Name,
            Description = activityType.Description,
            Color = activityType.Color,
            IsActive = activityType.IsActive
        };

        return View(model);
    }

    /// <summary>
    /// Обновление типа деятельности
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ActivityTypeViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var activityType = await _calendarService.GetActivityTypeByIdAsync(id);
        if (activityType == null)
        {
            return NotFound();
        }

        activityType.Name = model.Name;
        activityType.Description = model.Description;
        activityType.Color = model.Color;
        activityType.IsActive = model.IsActive;

        await _calendarService.UpdateActivityTypeAsync(activityType);

        TempData["SuccessMessage"] = "Тип деятельности успешно обновлен";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Деактивация типа деятельности
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _calendarService.DeleteActivityTypeAsync(id);

        TempData["SuccessMessage"] = "Тип деятельности деактивирован";
        return RedirectToAction(nameof(Index));
    }
}
