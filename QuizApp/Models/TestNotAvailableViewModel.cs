namespace QuizApp.Models;

public class TestNotAvailableViewModel
{
    public int TopicId { get; set; }
    public string Title { get; set; } = "Тест недоступен";
    public string Message { get; set; } = "Тест не найден или недоступен.";
}

