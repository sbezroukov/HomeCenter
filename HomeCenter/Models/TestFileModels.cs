using System.Collections.Generic;

namespace HomeCenter.Models;

public class AnswerOption
{
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class QuestionModel
{
    public string Text { get; set; } = string.Empty;
    public List<AnswerOption> Options { get; set; } = new();
    /// <summary>Правильный ответ для открытых вопросов (MODE: Open с секцией Ответы).</summary>
    public string? CorrectAnswer { get; set; }
    /// <summary>Несколько правильных вариантов — использовать чекбоксы вместо радио.</summary>
    public bool IsMultipleCorrect => Options.Count(o => o.IsCorrect) > 1;
}

