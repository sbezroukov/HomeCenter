using System;
using System.Collections.Generic;

namespace QuizApp.Models;

/// <summary>
/// Модель для страницы /Schoolbook: дерево папок слева и список файлов в выбранной папке справа.
/// </summary>
public class SchoolbookIndexViewModel
{
    /// <summary>Корневой узел дерева папок и файлов.</summary>
    public SchoolbookTreeNode TreeRoot { get; set; } = null!;

    /// <summary>Список файлов, находящихся в текущей выбранной папке (и её подпапках).</summary>
    public List<SchoolbookFile> FilesInFolder { get; set; } = new();

    /// <summary>Канонический путь выбранной папки (с разделителем текущей ОС) или пустая строка для корня.</summary>
    public string CurrentFolderPath { get; set; } = string.Empty;

    /// <summary>Текст для отображения выбранной папки (хлебные крошки).</summary>
    public string CurrentFolderDisplay { get; set; } = "Все учебники";
}

