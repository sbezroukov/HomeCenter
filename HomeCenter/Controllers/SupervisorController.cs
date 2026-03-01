using HomeCenter.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeCenter.Controllers;

/// <summary>
/// Контроллер для функций супервизора
/// </summary>
[Authorize(Roles = "Admin,Supervisor")]
public class SupervisorController : Controller
{
    private readonly ICalendarService _calendarService;
    private readonly IAuthService _authService;
    private readonly ILogger<SupervisorController> _logger;

    public SupervisorController(
        ICalendarService calendarService,
        IAuthService authService,
        ILogger<SupervisorController> logger)
    {
        _calendarService = calendarService;
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Список заданий, ожидающих подтверждения
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> PendingApprovals()
    {
        var fromDate = DateTime.Today.AddDays(-7);
        var toDate = DateTime.Today;

        var stats = await _calendarService.GetDetailedStatisticsAsync(fromDate, toDate);
        
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;

        return View(stats);
    }

    /// <summary>
    /// Подтвердить выполнение
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int completionId)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        try
        {
            await _calendarService.ApproveCompletionAsync(completionId, currentUser.Id);
            TempData["SuccessMessage"] = "Выполнение подтверждено";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving completion {CompletionId}", completionId);
            TempData["ErrorMessage"] = "Ошибка при подтверждении";
        }

        return RedirectToAction(nameof(PendingApprovals));
    }

    /// <summary>
    /// Отклонить выполнение
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int completionId, string reason)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        if (currentUser == null)
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Укажите причину отклонения";
            return RedirectToAction(nameof(PendingApprovals));
        }

        try
        {
            await _calendarService.RejectCompletionAsync(completionId, currentUser.Id, reason);
            TempData["SuccessMessage"] = "Выполнение отклонено";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting completion {CompletionId}", completionId);
            TempData["ErrorMessage"] = "Ошибка при отклонении";
        }

        return RedirectToAction(nameof(PendingApprovals));
    }
}
