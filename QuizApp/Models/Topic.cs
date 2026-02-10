using System.Collections.Generic;

namespace QuizApp.Models;

public enum TopicType
{
    Test = 0,      // Варианты ответов + правильный
    Open = 1,      // Открытые вопросы без правильного ответа
    SelfStudy = 2  // Вопросы для самопроверки, без ввода ответов
}

public class Topic
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Имя txt-файла в папке tests (без пути).
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    public TopicType Type { get; set; }

    /// <summary>
    /// Разрешена ли тема для прохождения обычным пользователям.
    /// </summary>
    public bool IsEnabled { get; set; }

    public ICollection<TestAttempt> Attempts { get; set; } = new List<TestAttempt>();
}

