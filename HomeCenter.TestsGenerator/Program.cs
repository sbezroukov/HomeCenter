using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

// Пути относительно корня репозитория (запуск из HomeCenter.TestsGenerator или из корня)
var repoRoot = FindRepoRoot();
var schoolbookPath = Path.Combine(repoRoot, "HomeCenter", "Schoolbook");
var testsPath = Path.Combine(repoRoot, "HomeCenter", "tests");

if (!Directory.Exists(schoolbookPath))
{
    Console.WriteLine("Папка Schoolbook не найдена: " + schoolbookPath);
    return 1;
}

Directory.CreateDirectory(testsPath);

var subjectByFileName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["Алгебра.pdf"] = "Алгебра",
    ["Геометрия.pdf"] = "Геометрия",
    ["География.pdf"] = "География",
    ["Физика.pdf"] = "Физика",
    ["Химия.pdf"] = "Химия",
    ["Вероятность и статистика 7-9 1 часть.pdf"] = "Вероятность и статистика 1 часть",
    ["Вероятность и статистика 7-9 2 часть.pdf"] = "Вероятность и статистика 2 часть",
    ["Математика_3 класс (2 часть).pdf"] = "Математика",
};
var classFolders = Directory.GetDirectories(schoolbookPath);
var generated = 0;
var encoding = new UTF8Encoding(false);

foreach (var classDir in classFolders)
{
    var className = Path.GetFileName(classDir);
    var pdfFiles = Directory.GetFiles(classDir, "*.pdf");
    foreach (var pdfPath in pdfFiles)
    {
        var fileName = Path.GetFileName(pdfPath);
        var subject = subjectByFileName.TryGetValue(fileName, out var s) ? s : Path.GetFileNameWithoutExtension(fileName);
        // Очищаем имя предмета для пути (без недопустимых символов)
        var subjectFolder = SanitizeFolderName(subject);
        var classFolder = SanitizeFolderName(className);
        try
        {
            var paragraphs = ExtractParagraphsFromPdf(pdfPath);
            foreach (var para in paragraphs)
            {
                var testContent = BuildTestContent(para.Num, para.Title, para.Content);
                var safeName = $"Параграф_{para.Num}.txt";
                var relativePath = Path.Combine(subjectFolder, classFolder!, safeName);
                var fullPath = Path.Combine(testsPath, relativePath);
                var dir = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(fullPath, testContent, encoding);
                generated++;
                Console.WriteLine($"  {relativePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке {fileName}: {ex.Message}");
        }
    }
}

Console.WriteLine($"Создано файлов тестов: {generated}");
return 0;

static string FindRepoRoot()
{
    var dir = AppContext.BaseDirectory;
    while (!string.IsNullOrEmpty(dir))
    {
        if (File.Exists(Path.Combine(dir, "HomeCenter.sln")))
            return dir;
        dir = Path.GetDirectoryName(dir);
    }
    return Path.Combine(AppContext.BaseDirectory, "..", "..");
}

static string SanitizeFolderName(string name)
{
    var sb = new StringBuilder();
    foreach (var c in name)
    {
        if (char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-')
            sb.Append(c);
    }
    return sb.ToString().Trim().Replace(' ', '_');
}

static List<(int Num, string Title, string Content)> ExtractParagraphsFromPdf(string pdfPath)
{
    var fullText = new StringBuilder();
    using (var doc = PdfDocument.Open(pdfPath))
    {
        for (int i = 1; i <= doc.NumberOfPages; i++)
        {
            var page = doc.GetPage(i);
            var words = page.GetWords();
            foreach (var w in words)
                fullText.Append(w.Text).Append(' ');
            fullText.AppendLine();
        }
    }

    var text = fullText.ToString();
    // Разбиваем по маркерам параграфов: § 1, § 2, §1, Параграф 1, и т.д.
    var pattern = @"(?:§\s*(\d+)|Параграф\s*(\d+)|§\s*(\d+)\s*[\.\)])";
    var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
    var list = new List<(int Num, string Title, string Content)>();

    var seen = new HashSet<int>();
    for (int i = 0; i < matches.Count; i++)
    {
        var m = matches[i];
        var numStr = m.Groups[1].Success ? m.Groups[1].Value : (m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value);
        var num = int.Parse(numStr);
        if (seen.Contains(num)) continue; // один параграф с одним номером на учебник
        seen.Add(num);
        var start = m.Index;
        var end = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
        var content = text.Substring(start, end - start).Trim();
        if (content.Length < 50) continue; // пропускаем слишком короткие (часто ложные срабатывания)
        var firstLineEnd = content.IndexOf('\n');
        var title = firstLineEnd > 0 ? content.Substring(0, firstLineEnd).Trim() : content.Substring(0, Math.Min(80, content.Length));
        list.Add((num, title, content));
    }

    if (list.Count == 0 && text.Length > 100)
        list.Add((1, "Содержание учебника", text));

    return list;
}

static string BuildTestContent(int num, string title, string content)
{
    // Извлекаем предложения, похожие на определения (с тире или "это")
    var definitionLines = new List<string>();
    var sentences = Regex.Split(content, @"(?<=[.!?])\s+");
    foreach (var s in sentences)
    {
        var t = s.Trim();
        if (t.Length < 15) continue;
        if (t.Contains(" — ") || t.Contains(" это ") || t.Contains(" называется ") || t.Contains(" называют "))
            definitionLines.Add(t);
    }

    var sb = new StringBuilder();
    sb.AppendLine("MODE: Open");
    sb.AppendLine();

    sb.AppendLine($"Q: Опишите содержание параграфа §{num}. Какие основные понятия в нём приведены?");
    sb.AppendLine();
    sb.AppendLine($"Q: Перечислите ключевые термины и определения из параграфа «{Truncate(title, 60)}».");
    sb.AppendLine();

    if (definitionLines.Count > 0)
    {
        foreach (var def in definitionLines.Take(3))
        {
            var term = ExtractTermFromDefinition(def);
            if (!string.IsNullOrWhiteSpace(term))
            {
                sb.AppendLine($"Q: Что такое {term}?");
                sb.AppendLine();
            }
        }
    }

    sb.AppendLine("---");
    sb.AppendLine("Ответы:");
    var answerNum = 1;
    sb.AppendLine($"{answerNum}. По параграфу §{num}: основные понятия и определения см. в тексте параграфа.");
    answerNum++;
    sb.AppendLine($"{answerNum}. Ключевые термины перечислены в параграфе §{num}.");
    answerNum++;
    if (definitionLines.Count > 0)
    {
        foreach (var def in definitionLines.Take(3))
        {
            var term = ExtractTermFromDefinition(def);
            if (!string.IsNullOrWhiteSpace(term))
            {
                sb.AppendLine($"{answerNum}. {def}");
                answerNum++;
            }
        }
    }

    return sb.ToString();
}

static string? ExtractTermFromDefinition(string definition)
{
    var m = Regex.Match(definition, @"^([^—\.!?]+?)\s*[—\-]\s*", RegexOptions.Multiline);
    if (m.Success) return m.Groups[1].Value.Trim();
    m = Regex.Match(definition, @"^([^\.!?]+?)\s+это\s+", RegexOptions.IgnoreCase);
    if (m.Success) return m.Groups[1].Value.Trim();
    m = Regex.Match(definition, @"^([^\.!?]+?)\s+называется\s+", RegexOptions.IgnoreCase);
    if (m.Success) return m.Groups[1].Value.Trim();
    return null;
}

static string Truncate(string s, int maxLen)
{
    if (string.IsNullOrEmpty(s)) return s;
    s = s.Trim();
    return s.Length <= maxLen ? s : s.Substring(0, maxLen) + "…";
}
