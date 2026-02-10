using System.Collections.Generic;
using QuizApp.Models;

namespace QuizApp.Services;

public interface ITestFileService
{
    /// <summary>
    /// Сканирует папку tests, создает при необходимости записи Topic в БД.
    /// </summary>
    void SyncTopicsFromFiles();

    /// <summary>
    /// Загружает вопросы из txt-файла для указанной темы.
    /// </summary>
    IReadOnlyList<QuestionModel> LoadQuestionsForTopic(Topic topic);
}

