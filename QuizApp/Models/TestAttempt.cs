using System;

namespace QuizApp.Models;

public class TestAttempt
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int TopicId { get; set; }
    public Topic Topic { get; set; } = null!;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Количество вопросов в момент прохождения.
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Количество правильных ответов (для Test).
    /// </summary>
    public int? CorrectAnswers { get; set; }

    /// <summary>
    /// Итоговый процент/оценка (0-100). Для открытых/самопроверки может быть null.
    /// </summary>
    public double? ScorePercent { get; set; }

    /// <summary>
    /// Сырые данные ответов в виде JSON (вопросы, ответы пользователя, правильные ответы и т.п.).
    /// </summary>
    public string? ResultJson { get; set; }
}

