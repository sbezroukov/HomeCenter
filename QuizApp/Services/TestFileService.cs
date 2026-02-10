using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizApp.Data;
using QuizApp.Models;

namespace QuizApp.Services;

/// <summary>
/// Чтение и разбор txt-файлов из папки tests.
///
/// Формат файла:
///   Первая непустая строка: MODE: Test | Open | Self
///   Остальное — блоки вопросов, разделенные пустой строкой.
///
///   Пример для теста с вариантами:
///     MODE: Test
///
///     Q: Сколько будет 2+2?
///     1) 3
///     *2) 4
///     3) 5
///
///   Строка с * в начале варианта считается правильным ответом.
///   Для MODE: Open и MODE: Self после "Q:" дополнительных строк не требуется.
/// </summary>
public class TestFileService : ITestFileService
{
    private readonly ApplicationDbContext _db;
    private readonly string _testsFolder;
    private readonly ILogger<TestFileService> _logger;

    public TestFileService(
        ApplicationDbContext db,
        ILogger<TestFileService> logger,
        IWebHostEnvironment env)
    {
        _db = db;
        _logger = logger;

        // Папка tests теперь ищется относительно корня приложения (ContentRoot),
        // а не bin/Debug..., чтобы одинаково работать и локально, и в Docker.
        var contentRoot = env.ContentRootPath;
        _testsFolder = Path.Combine(contentRoot, "tests");

        if (!Directory.Exists(_testsFolder))
        {
            Directory.CreateDirectory(_testsFolder);
        }
    }

    public void SyncTopicsFromFiles()
    {
        if (!Directory.Exists(_testsFolder))
        {
            return;
        }

        // Рекурсивно обходим папку tests и все подпапки (категории)
        var files = Directory.GetFiles(_testsFolder, "*.txt", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            // Относительный путь от папки tests (например "География\Урок 5\test.txt")
            var relativePath = Path.GetRelativePath(_testsFolder, file);
            if (Path.DirectorySeparatorChar != '\\')
                relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '\\');

            try
            {
                var (type, _) = ParseFile(file);

                var existing = _db.Topics.SingleOrDefault(t => t.FileName == relativePath);
                if (existing == null)
                {
                    var title = Path.GetFileNameWithoutExtension(relativePath);
                    var topic = new Topic
                    {
                        Title = title,
                        FileName = relativePath,
                        Type = type,
                        IsEnabled = false // по умолчанию выключено, админ включает
                    };
                    _db.Topics.Add(topic);
                }
                else
                {
                    existing.Title = Path.GetFileNameWithoutExtension(relativePath);
                    existing.Type = type;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка разбора файла теста {FileName}", relativePath);
            }
        }

        _db.SaveChanges();
    }

    public IReadOnlyList<QuestionModel> LoadQuestionsForTopic(Topic topic)
    {
        // FileName хранит относительный путь (в т.ч. с подпапками)
        var path = Path.Combine(_testsFolder, topic.FileName.Replace('\\', Path.DirectorySeparatorChar));
        var (_, questions) = ParseFile(path);
        return questions;
    }

    private static (TopicType Type, List<QuestionModel> Questions) ParseFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Файл теста не найден", path);

        var allLines = File.ReadAllLines(path)
            .Select(l => l.TrimEnd('\r', '\n'))
            .ToList();

        int index = 0;
        TopicType type = TopicType.Test;

        // Найти первую строку MODE:
        while (index < allLines.Count && string.IsNullOrWhiteSpace(allLines[index]))
        {
            index++;
        }

        if (index < allLines.Count && allLines[index].StartsWith("MODE:", StringComparison.OrdinalIgnoreCase))
        {
            var modeValue = allLines[index].Substring("MODE:".Length).Trim();
            type = modeValue.ToLower() switch
            {
                "test" => TopicType.Test,
                "open" => TopicType.Open,
                "self" => TopicType.SelfStudy,
                "selfstudy" => TopicType.SelfStudy,
                _ => TopicType.Test
            };
            index++;
        }

        var questions = new List<QuestionModel>();

        while (index < allLines.Count)
        {
            // Пропускаем пустые строки между блоками
            while (index < allLines.Count && string.IsNullOrWhiteSpace(allLines[index]))
            {
                index++;
            }

            if (index >= allLines.Count)
                break;

            if (!allLines[index].StartsWith("Q:", StringComparison.OrdinalIgnoreCase))
            {
                index++;
                continue;
            }

            var questionText = allLines[index].Substring(2).Trim(':', ' ', '\t');
            index++;

            var question = new QuestionModel
            {
                Text = questionText
            };

            if (type == TopicType.Test)
            {
                // Читаем варианты до пустой строки или следующего Q:
                while (index < allLines.Count && !string.IsNullOrWhiteSpace(allLines[index]) &&
                       !allLines[index].StartsWith("Q:", StringComparison.OrdinalIgnoreCase))
                {
                    var line = allLines[index];
                    bool isCorrect = false;
                    if (line.StartsWith("*"))
                    {
                        isCorrect = true;
                        line = line.Substring(1).TrimStart();
                    }

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        question.Options.Add(new AnswerOption
                        {
                            Text = line,
                            IsCorrect = isCorrect
                        });
                    }

                    index++;
                }
            }
            else
            {
                // Для Open / SelfStudy дополнительных строк не требуется; просто переходим к следующему вопросу
                while (index < allLines.Count && !string.IsNullOrWhiteSpace(allLines[index]) &&
                       !allLines[index].StartsWith("Q:", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                }
            }

            if (!string.IsNullOrWhiteSpace(question.Text))
            {
                questions.Add(question);
            }
        }

        return (type, questions);
    }
}

