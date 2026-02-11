using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using QuizApp.Models;

namespace QuizApp.Controllers;

[Authorize]
public class SchoolbookController : Controller
{
    private readonly IWebHostEnvironment _env;

    public SchoolbookController(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>
    /// Страница со списком учебников (PDF) с сохранением структуры папок.
    /// Корневая папка: ContentRoot/Schoolbook.
    /// </summary>
    public IActionResult Index(string? folder = null)
    {
        var vm = BuildIndexViewModel(folder);
        return View(vm);
    }

    [HttpGet]
    public IActionResult Folder(string? folder = null)
    {
        var vm = BuildIndexViewModel(folder);
        return PartialView("_SchoolbookFolderContent", vm);
    }

    /// <summary>
    /// Скачивание PDF-файла по относительному пути от папки Schoolbook.
    /// </summary>
    public IActionResult Download(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return NotFound();
        }

        var contentRoot = _env.ContentRootPath;
        var rootFolder = Path.Combine(contentRoot, "Schoolbook");

        // Нормализуем путь и не допускаем выхода за пределы корневой папки.
        var normalizedRelative = path.Replace('/', Path.DirectorySeparatorChar)
                                     .Replace('\\', Path.DirectorySeparatorChar);

        var fullPath = Path.GetFullPath(Path.Combine(rootFolder, normalizedRelative));
        var rootFullPath = Path.GetFullPath(rootFolder);

        if (!fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        if (!System.IO.File.Exists(fullPath))
        {
            return NotFound();
        }

        var fileName = Path.GetFileName(fullPath);
        const string contentType = "application/pdf";

        return PhysicalFile(fullPath, contentType, fileName);
    }

    private SchoolbookIndexViewModel BuildIndexViewModel(string? folder)
    {
        var contentRoot = _env.ContentRootPath;
        var rootFolder = Path.Combine(contentRoot, "Schoolbook");

        var tree = SchoolbookTreeNode.BuildFromDirectory(rootFolder);

        // Нормализуем путь папки из query-параметра, чтобы он совпадал с FolderPath.
        string currentFolderPath;
        if (string.IsNullOrWhiteSpace(folder))
        {
            currentFolderPath = string.Empty;
        }
        else
        {
            var normalized = folder
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            currentFolderPath = normalized;
        }

        // Собираем файлы из выбранной папки и всех её подпапок.
        var filesInFolder = new List<SchoolbookFile>();

        void CollectFiles(SchoolbookTreeNode node, bool within)
        {
            bool nowWithin = within || string.Equals(node.FolderPath, currentFolderPath, StringComparison.OrdinalIgnoreCase);

            if (nowWithin)
            {
                filesInFolder.AddRange(node.Files);
            }

            foreach (var child in node.Children)
            {
                CollectFiles(child, nowWithin);
            }
        }

        if (string.IsNullOrEmpty(currentFolderPath))
        {
            // Корень: берём все файлы дерева.
            CollectFiles(tree, true);
        }
        else
        {
            CollectFiles(tree, false);
        }

        string display;
        if (string.IsNullOrEmpty(currentFolderPath))
        {
            display = "Все учебники";
        }
        else
        {
            var parts = currentFolderPath
                .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            display = string.Join(" / ", parts);
        }

        return new SchoolbookIndexViewModel
        {
            TreeRoot = tree,
            FilesInFolder = filesInFolder
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CurrentFolderPath = currentFolderPath,
            CurrentFolderDisplay = display
        };
    }
}

