namespace HomeCenterBackup;

public class BackupService
{
    private readonly DockerService _dockerService;
    private readonly string _backupDirectory;
    private readonly string _projectPath;

    public BackupService(DockerService dockerService, string backupDirectory, string projectPath)
    {
        _dockerService = dockerService;
        _backupDirectory = backupDirectory;
        _projectPath = projectPath;

        // Создать директорию для бэкапов если не существует
        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }

    public async Task<string> CreateBackupAsync(string containerName, string databasePath, IProgress<string>? progress = null)
    {
        progress?.Report("Проверка контейнера...");

        // Проверить что контейнер запущен
        if (!await _dockerService.IsContainerRunningAsync(containerName))
        {
            throw new Exception($"Контейнер '{containerName}' не запущен!");
        }

        progress?.Report("Проверка базы данных...");

        // Проверить что файл существует
        var fileSize = await _dockerService.GetFileSizeInContainerAsync(containerName, databasePath);
        if (fileSize == 0)
        {
            throw new Exception($"База данных не найдена: {databasePath}");
        }

        // Создать имя файла с датой и временем
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var fileName = $"quiz-{timestamp}.db";
        var backupPath = Path.Combine(_backupDirectory, fileName);

        progress?.Report($"Создание бэкапа: {fileName}");

        // Скопировать файл из контейнера
        await _dockerService.CopyFileFromContainerAsync(containerName, databasePath, backupPath);

        // Проверить что файл создан
        if (!File.Exists(backupPath))
        {
            throw new Exception("Не удалось создать файл бэкапа!");
        }

        var backupSize = new FileInfo(backupPath).Length;
        progress?.Report($"✓ Бэкап создан: {fileName} ({FormatFileSize(backupSize)})");

        return backupPath;
    }

    public async Task RestoreBackupAsync(string containerName, string databasePath, string backupFilePath, IProgress<string>? progress = null)
    {
        // Проверить что файл бэкапа существует
        if (!File.Exists(backupFilePath))
        {
            throw new Exception($"Файл бэкапа не найден: {backupFilePath}");
        }

        progress?.Report("Проверка контейнера...");

        // Проверить что контейнер запущен
        if (!await _dockerService.IsContainerRunningAsync(containerName))
        {
            throw new Exception($"Контейнер '{containerName}' не запущен!");
        }

        // Создать бэкап текущей базы перед восстановлением
        progress?.Report("Создание бэкапа текущей базы...");
        try
        {
            var currentSize = await _dockerService.GetFileSizeInContainerAsync(containerName, databasePath);
            if (currentSize > 0)
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                var safetyBackupPath = Path.Combine(_backupDirectory, $"quiz-before-restore-{timestamp}.db");
                await _dockerService.CopyFileFromContainerAsync(containerName, databasePath, safetyBackupPath);
                progress?.Report($"✓ Текущая база сохранена: quiz-before-restore-{timestamp}.db");
            }
        }
        catch
        {
            progress?.Report("⚠ Не удалось создать бэкап текущей базы (возможно её нет)");
        }

        // Остановить контейнер
        progress?.Report("Остановка контейнера...");
        await _dockerService.StopContainerAsync(containerName, _projectPath);
        await Task.Delay(2000); // Подождать 2 секунды

        try
        {
            // Скопировать бэкап в контейнер
            progress?.Report("Копирование бэкапа в контейнер...");
            await _dockerService.CopyFileToContainerAsync(containerName, backupFilePath, databasePath);

            progress?.Report("✓ Бэкап скопирован");
        }
        finally
        {
            // Запустить контейнер обратно
            progress?.Report("Запуск контейнера...");
            await _dockerService.StartContainerAsync(containerName, _projectPath);
            await Task.Delay(3000); // Подождать 3 секунды
        }

        progress?.Report("✓ База данных восстановлена успешно!");
    }

    public List<BackupInfo> GetAvailableBackups()
    {
        if (!Directory.Exists(_backupDirectory))
        {
            return new List<BackupInfo>();
        }

        return Directory.GetFiles(_backupDirectory, "quiz-*.db")
            .Select(path => new BackupInfo
            {
                FilePath = path,
                FileName = Path.GetFileName(path),
                Size = new FileInfo(path).Length,
                CreatedDate = File.GetCreationTime(path)
            })
            .OrderByDescending(b => b.CreatedDate)
            .ToList();
    }

    public void DeleteBackup(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public void DeleteOldBackups(int daysToKeep)
    {
        var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
        var backups = GetAvailableBackups();

        foreach (var backup in backups.Where(b => b.CreatedDate < cutoffDate))
        {
            DeleteBackup(backup.FilePath);
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

public class BackupInfo
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public long Size { get; set; }
    public DateTime CreatedDate { get; set; }

    public string DisplayName => $"{FileName} ({FormatFileSize(Size)}) - {CreatedDate:yyyy-MM-dd HH:mm:ss}";

    public string Age
    {
        get
        {
            var age = DateTime.Now - CreatedDate;
            if (age.TotalDays >= 1)
                return $"{(int)age.TotalDays} дней назад";
            if (age.TotalHours >= 1)
                return $"{(int)age.TotalHours} часов назад";
            return $"{(int)age.TotalMinutes} минут назад";
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
