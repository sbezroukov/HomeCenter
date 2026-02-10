using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Services;

namespace QuizApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ITestFileService _testFileService;

    public AdminController(
        ApplicationDbContext db,
        IConfiguration configuration,
        ITestFileService testFileService)
    {
        _db = db;
        _configuration = configuration;
        _testFileService = testFileService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(string userName, string password, string? returnUrl = null)
    {
        var adminUser = _configuration["Admin:Username"];
        var adminPassword = _configuration["Admin:Password"];

        if (userName == adminUser && password == adminPassword)
        {
            var claims = new[]
            {
                new System.Security.Claims.Claim(ClaimTypes.Name, userName),
                new System.Security.Claims.Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index");
        }

        ModelState.AddModelError(string.Empty, "Неверный логин или пароль администратора.");
        return View();
    }

    public async Task<IActionResult> Index()
    {
        // Каждый раз при входе в админку перечитываем файлы из папки tests
        // и синхронизируем список тем.
        _testFileService.SyncTopicsFromFiles();

        var topics = await _db.Topics
            .OrderBy(t => t.Title)
            .ToListAsync();

        var totalUsers = await _db.Users.CountAsync();
        var totalAttempts = await _db.Attempts.CountAsync();

        ViewBag.TotalUsers = totalUsers;
        ViewBag.TotalAttempts = totalAttempts;

        return View(topics);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleTopic(int id)
    {
        var topic = await _db.Topics.FindAsync(id);
        if (topic == null)
            return NotFound();

        // Переключаем флаг на сервере, не полагаясь на значение из формы.
        topic.IsEnabled = !topic.IsEnabled;
        await _db.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> History()
    {
        var attempts = await _db.Attempts
            .Include(a => a.Topic)
            .Include(a => a.User)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync();

        return View(attempts);
    }
}

