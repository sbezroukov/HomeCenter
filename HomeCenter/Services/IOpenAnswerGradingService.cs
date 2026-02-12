using HomeCenter.Models;

namespace HomeCenter.Services;

public interface IOpenAnswerGradingService
{
    /// <summary>
    /// Оценивает открытые ответы через Qwen API. Возвращает массив оценок 0–100.
    /// При ошибке или отключении — возвращает null.
    /// </summary>
    Task<IReadOnlyList<double?>> GradeAsync(Topic topic, List<GradingItem> items, CancellationToken cancellationToken = default);
}
