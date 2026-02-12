using System;
using System.Collections.Generic;

namespace HomeCenter.Models;

/// <summary>
/// Информация о последнем прохождении теста текущим пользователем.
/// </summary>
public class TestLastResult
{
    public DateTime? LastCompletedAt { get; set; }
    public double? LastScorePercent { get; set; }
}

/// <summary>
/// Модель для страницы /Test: дерево папок слева и список тестов в выбранной папке справа.
/// </summary>
public class TestIndexViewModel
{
    /// <summary>Корневой узел дерева категорий.</summary>
    public TestTreeNode TreeRoot { get; set; } = null!;

    /// <summary>Список тестов, находящихся в текущей выбранной папке.</summary>
    public List<Topic> TopicsInFolder { get; set; } = new();

    /// <summary>Канонический путь выбранной папки (с разделителем текущей ОС) или пустая строка для корня.</summary>
    public string CurrentFolderPath { get; set; } = string.Empty;

    /// <summary>Текст для отображения выбранной папки (хлебные крошки).</summary>
    public string CurrentFolderDisplay { get; set; } = "Все тесты";

    /// <summary>
    /// Последние результаты текущего пользователя по тестам в текущем наборе,
    /// ключ — TopicId.
    /// </summary>
    public Dictionary<int, TestLastResult> LastResultsByTopicId { get; set; } = new();

    /// <summary>
    /// Всего тем в текущей папке (включая отключённые), для корректного сообщения.
    /// </summary>
    public int TotalTopicsInFolder { get; set; }
}

