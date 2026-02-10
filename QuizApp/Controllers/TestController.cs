using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.Controllers;

[Authorize]
public class TestController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITestFileService _testService;

    public TestController(ApplicationDbContext db, ITestFileService testService)
    {
        _db = db;
        _testService = testService;
    }

    public async Task<IActionResult> Index()
    {
        // На всякий случай обновляем список тем из файлов при каждом заходе
        // на страницу "Тесты", чтобы новые/измененные файлы сразу подтягивались.
        _testService.SyncTopicsFromFiles();

        var topics = await _db.Topics
            .Where(t => t.IsEnabled)
            .OrderBy(t => t.Title)
            .ToListAsync();

        return View(topics);
    }

    public async Task<IActionResult> Take(int id)
    {
        var topic = await _db.Topics.FindAsync(id);
        if (topic == null || !topic.IsEnabled)
            return NotFound();

        var questions = _testService.LoadQuestionsForTopic(topic).ToList();

        ViewBag.Topic = topic;
        return View((topic, questions));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(int id, string? dummy = null)
    {
        var topic = await _db.Topics.FindAsync(id);
        if (topic == null || !topic.IsEnabled)
            return NotFound();

        var questions = _testService.LoadQuestionsForTopic(topic).ToList();
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var attempt = new TestAttempt
        {
            UserId = userId,
            TopicId = topic.Id,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            TotalQuestions = questions.Count
        };

        var resultDetails = new List<object>();

        if (topic.Type == TopicType.Test)
        {
            int correct = 0;
            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                var value = Request.Form[$"q{i}"];
                int selectedIndex = int.TryParse(value, out var idx) ? idx : -1;

                var selectedOption = selectedIndex >= 0 && selectedIndex < q.Options.Count
                    ? q.Options[selectedIndex]
                    : null;
                var correctOption = q.Options.FirstOrDefault(o => o.IsCorrect);

                bool isCorrect = selectedOption != null && correctOption != null &&
                                 ReferenceEquals(selectedOption, correctOption);

                if (isCorrect)
                    correct++;

                resultDetails.Add(new
                {
                    Question = q.Text,
                    Selected = selectedOption?.Text,
                    Correct = correctOption?.Text,
                    IsCorrect = isCorrect
                });
            }

            attempt.CorrectAnswers = correct;
            attempt.ScorePercent = questions.Count > 0
                ? Math.Round(correct * 100.0 / questions.Count, 2)
                : 0;
        }
        else if (topic.Type == TopicType.Open)
        {
            for (int i = 0; i < questions.Count; i++)
            {
                var q = questions[i];
                var value = Request.Form[$"q{i}"];
                resultDetails.Add(new
                {
                    Question = q.Text,
                    Answer = value
                });
            }

            attempt.CorrectAnswers = null;
            attempt.ScorePercent = null;
        }
        else // SelfStudy
        {
            attempt.CorrectAnswers = null;
            attempt.ScorePercent = null;
        }

        attempt.ResultJson = JsonSerializer.Serialize(resultDetails, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        _db.Attempts.Add(attempt);
        await _db.SaveChangesAsync();

        return RedirectToAction("Result", new { id = attempt.Id });
    }

    public async Task<IActionResult> Result(int id)
    {
        var attempt = await _db.Attempts
            .Include(a => a.Topic)
            .Include(a => a.User)
            .SingleOrDefaultAsync(a => a.Id == id);

        if (attempt == null)
            return NotFound();

        var details = new List<object>();
        if (!string.IsNullOrEmpty(attempt.ResultJson))
        {
            try
            {
                // Десериализуем как JsonElement для более удобной работы в представлении
                var jsonDoc = JsonDocument.Parse(attempt.ResultJson);
                details = jsonDoc.RootElement.EnumerateArray()
                    .Select(e => (object)e.Clone())
                    .ToList();
            }
            catch
            {
                // игнорируем ошибки парсинга
            }
        }

        ViewBag.Details = details;
        return View(attempt);
    }
}

