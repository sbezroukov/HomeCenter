using System.ComponentModel;

namespace HomeCenterBackup;

public partial class MainFormExtended : Form
{
    private readonly DockerService _dockerService;
    private readonly BackupService _backupService;
    private readonly string _backupDirectory;
    private readonly string _projectPath;

    private TabControl tabControl = null!;
    private TextBox txtContainerName = null!;
    private TextBox txtDatabasePath = null!;
    private ComboBox cmbBackups = null!;
    private Button btnBackup = null!;
    private Button btnRestore = null!;
    private Button btnRefresh = null!;
    private Button btnDelete = null!;
    private Button btnCleanOld = null!;
    private TextBox txtLog = null!;
    private ProgressBar progressBar = null!;
    private Label lblStatus = null!;
    private DataGridView dgvBackups = null!;
    
    // Docker Management
    private Button btnCheckStatus = null!;
    private Button btnStop = null!;
    private Button btnStart = null!;
    private Button btnRestart = null!;
    private Button btnViewLogs = null!;
    private Label lblContainerStatus = null!;
    
    // Build Management
    private Button btnBuildSimple = null!;
    private Button btnBuildFull = null!;
    private Button btnBuildNoCache = null!;
    private Button btnPullImages = null!;
    private Button btnCleanImages = null!;
    private TextBox txtProjectPath = null!;

    public MainFormExtended()
    {
        _backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");
        _projectPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "HomeCenter");
        _dockerService = new DockerService();
        _backupService = new BackupService(_dockerService, _backupDirectory);

        InitializeComponent();
        InitializeCustomComponents();
        LoadBackups();
        _ = UpdateContainerStatusAsync();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        
        // Form
        this.ClientSize = new Size(1000, 750);
        this.Text = "HomeCenter Database Backup & Docker Manager";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1000, 750);

        this.ResumeLayout(false);
    }

    private void InitializeCustomComponents()
    {
        // TabControl
        tabControl = new TabControl
        {
            Location = new Point(10, 10),
            Size = new Size(970, 650)
        };

        // Tab 1: Бэкапы
        var tabBackup = new TabPage("Бэкапы");
        InitializeBackupTab(tabBackup);

        // Tab 2: Docker управление
        var tabDocker = new TabPage("Docker управление");
        InitializeDockerTab(tabDocker);

        // Tab 3: Сборка
        var tabBuild = new TabPage("Сборка приложения");
        InitializeBuildTab(tabBuild);

        tabControl.TabPages.AddRange(new[] { tabBackup, tabDocker, tabBuild });

        // Лог и прогресс (общие для всех вкладок)
        var lblLog = new Label
        {
            Text = "Лог операций:",
            Location = new Point(10, 670),
            AutoSize = true
        };

        txtLog = new TextBox
        {
            Location = new Point(10, 690),
            Size = new Size(870, 50),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            BackColor = Color.White
        };

        progressBar = new ProgressBar
        {
            Location = new Point(890, 690),
            Size = new Size(30, 50),
            Style = ProgressBarStyle.Marquee,
            Visible = false
        };

        lblStatus = new Label
        {
            Location = new Point(930, 690),
            Size = new Size(60, 50),
            Text = "Готов",
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Green
        };

        // Добавить все контролы на форму
        this.Controls.AddRange(new Control[] { tabControl, lblLog, txtLog, progressBar, lblStatus });
    }

    private void InitializeBackupTab(TabPage tab)
    {
        // Группа: Настройки Docker
        var grpDocker = new GroupBox
        {
            Text = "Настройки Docker",
            Location = new Point(10, 10),
            Size = new Size(930, 100)
        };

        var lblContainer = new Label
        {
            Text = "Имя контейнера:",
            Location = new Point(10, 25),
            AutoSize = true
        };

        txtContainerName = new TextBox
        {
            Location = new Point(130, 22),
            Size = new Size(200, 23),
            Text = "homecenter"
        };

        var lblDatabase = new Label
        {
            Text = "Путь к БД:",
            Location = new Point(10, 55),
            AutoSize = true
        };

        txtDatabasePath = new TextBox
        {
            Location = new Point(130, 52),
            Size = new Size(300, 23),
            Text = "/app/data/quiz.db"
        };

        var lblBackupDir = new Label
        {
            Text = "Папка бэкапов:",
            Location = new Point(450, 25),
            AutoSize = true
        };

        var lblBackupDirValue = new Label
        {
            Text = _backupDirectory,
            Location = new Point(450, 45),
            Size = new Size(470, 40),
            ForeColor = Color.Gray
        };

        grpDocker.Controls.AddRange(new Control[] { lblContainer, txtContainerName, lblDatabase, txtDatabasePath, lblBackupDir, lblBackupDirValue });

        // Группа: Создание бэкапа
        var grpBackup = new GroupBox
        {
            Text = "Создание бэкапа",
            Location = new Point(10, 120),
            Size = new Size(450, 80)
        };

        btnBackup = new Button
        {
            Text = "Создать бэкап",
            Location = new Point(10, 30),
            Size = new Size(150, 35),
            BackColor = Color.LightGreen
        };
        btnBackup.Click += BtnBackup_Click;

        var lblBackupInfo = new Label
        {
            Text = "Создаст бэкап базы данных с текущей датой и временем",
            Location = new Point(170, 35),
            Size = new Size(270, 30),
            ForeColor = Color.Gray
        };

        grpBackup.Controls.AddRange(new Control[] { btnBackup, lblBackupInfo });

        // Группа: Восстановление
        var grpRestore = new GroupBox
        {
            Text = "Восстановление из бэкапа",
            Location = new Point(480, 120),
            Size = new Size(460, 80)
        };

        cmbBackups = new ComboBox
        {
            Location = new Point(10, 30),
            Size = new Size(280, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        btnRestore = new Button
        {
            Text = "Восстановить",
            Location = new Point(300, 28),
            Size = new Size(140, 28),
            BackColor = Color.LightCoral
        };
        btnRestore.Click += BtnRestore_Click;

        grpRestore.Controls.AddRange(new Control[] { cmbBackups, btnRestore });

        // Группа: Управление бэкапами
        var grpManage = new GroupBox
        {
            Text = "Управление бэкапами",
            Location = new Point(10, 210),
            Size = new Size(930, 380)
        };

        dgvBackups = new DataGridView
        {
            Location = new Point(10, 25),
            Size = new Size(910, 300),
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };

        dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "FileName", HeaderText = "Имя файла", FillWeight = 30 });
        dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "Size", HeaderText = "Размер", FillWeight = 15 });
        dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "Created", HeaderText = "Создан", FillWeight = 25 });
        dgvBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "Age", HeaderText = "Возраст", FillWeight = 20 });

        btnRefresh = new Button
        {
            Text = "Обновить",
            Location = new Point(10, 335),
            Size = new Size(100, 30)
        };
        btnRefresh.Click += (s, e) => LoadBackups();

        btnDelete = new Button
        {
            Text = "Удалить выбранный",
            Location = new Point(120, 335),
            Size = new Size(150, 30),
            BackColor = Color.LightYellow
        };
        btnDelete.Click += BtnDelete_Click;

        btnCleanOld = new Button
        {
            Text = "Очистить старые (>30 дней)",
            Location = new Point(280, 335),
            Size = new Size(200, 30),
            BackColor = Color.LightYellow
        };
        btnCleanOld.Click += BtnCleanOld_Click;

        grpManage.Controls.AddRange(new Control[] { dgvBackups, btnRefresh, btnDelete, btnCleanOld });

        tab.Controls.AddRange(new Control[] { grpDocker, grpBackup, grpRestore, grpManage });
    }

    private void InitializeDockerTab(TabPage tab)
    {
        // Статус контейнера
        var grpStatus = new GroupBox
        {
            Text = "Статус контейнера",
            Location = new Point(10, 10),
            Size = new Size(930, 100)
        };

        lblContainerStatus = new Label
        {
            Location = new Point(20, 30),
            Size = new Size(890, 50),
            Text = "Проверка статуса...",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.Gray
        };

        btnCheckStatus = new Button
        {
            Text = "Обновить статус",
            Location = new Point(750, 30),
            Size = new Size(150, 35)
        };
        btnCheckStatus.Click += async (s, e) => await UpdateContainerStatusAsync();

        grpStatus.Controls.AddRange(new Control[] { lblContainerStatus, btnCheckStatus });

        // Управление контейнером
        var grpControl = new GroupBox
        {
            Text = "Управление контейнером",
            Location = new Point(10, 120),
            Size = new Size(930, 200)
        };

        // Остановить
        btnStop = new Button
        {
            Text = "Остановить контейнер",
            Location = new Point(20, 30),
            Size = new Size(200, 40),
            BackColor = Color.LightCoral
        };
        btnStop.Click += BtnStop_Click;

        var lblStopDesc = new Label
        {
            Text = "Остановит Docker контейнер.\nИспользуйте перед обновлением или обслуживанием.",
            Location = new Point(230, 30),
            Size = new Size(680, 40),
            ForeColor = Color.Gray
        };

        // Запустить
        btnStart = new Button
        {
            Text = "Запустить контейнер",
            Location = new Point(20, 80),
            Size = new Size(200, 40),
            BackColor = Color.LightGreen
        };
        btnStart.Click += BtnStart_Click;

        var lblStartDesc = new Label
        {
            Text = "Запустит остановленный контейнер.\nИспользуется после остановки или сбоя.",
            Location = new Point(230, 80),
            Size = new Size(680, 40),
            ForeColor = Color.Gray
        };

        // Перезапустить
        btnRestart = new Button
        {
            Text = "Перезапустить контейнер",
            Location = new Point(20, 130),
            Size = new Size(200, 40),
            BackColor = Color.LightBlue
        };
        btnRestart.Click += BtnRestart_Click;

        var lblRestartDesc = new Label
        {
            Text = "Перезапустит контейнер (stop + start).\nПрименяет изменения конфигурации без пересборки.",
            Location = new Point(230, 130),
            Size = new Size(680, 40),
            ForeColor = Color.Gray
        };

        grpControl.Controls.AddRange(new Control[] 
        { 
            btnStop, lblStopDesc, 
            btnStart, lblStartDesc, 
            btnRestart, lblRestartDesc 
        });

        // Логи
        var grpLogs = new GroupBox
        {
            Text = "Логи контейнера",
            Location = new Point(10, 330),
            Size = new Size(930, 100)
        };

        btnViewLogs = new Button
        {
            Text = "Показать последние 50 строк логов",
            Location = new Point(20, 30),
            Size = new Size(250, 40),
            BackColor = Color.LightYellow
        };
        btnViewLogs.Click += BtnViewLogs_Click;

        var lblLogsDesc = new Label
        {
            Text = "Показывает последние логи контейнера для диагностики проблем.",
            Location = new Point(280, 40),
            Size = new Size(630, 30),
            ForeColor = Color.Gray
        };

        grpLogs.Controls.AddRange(new Control[] { btnViewLogs, lblLogsDesc });

        tab.Controls.AddRange(new Control[] { grpStatus, grpControl, grpLogs });
    }

    private void InitializeBuildTab(TabPage tab)
    {
        // Путь к проекту
        var grpProject = new GroupBox
        {
            Text = "Путь к проекту",
            Location = new Point(10, 10),
            Size = new Size(930, 80)
        };

        var lblProject = new Label
        {
            Text = "Директория проекта:",
            Location = new Point(10, 25),
            AutoSize = true
        };

        txtProjectPath = new TextBox
        {
            Location = new Point(10, 45),
            Size = new Size(800, 23),
            Text = Path.GetFullPath(_projectPath)
        };

        var btnBrowse = new Button
        {
            Text = "Обзор...",
            Location = new Point(820, 43),
            Size = new Size(90, 27)
        };
        btnBrowse.Click += (s, e) =>
        {
            using var dialog = new FolderBrowserDialog();
            dialog.SelectedPath = txtProjectPath.Text;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtProjectPath.Text = dialog.SelectedPath;
            }
        };

        grpProject.Controls.AddRange(new Control[] { lblProject, txtProjectPath, btnBrowse });

        // Быстрые сборки
        var grpQuick = new GroupBox
        {
            Text = "Быстрые операции",
            Location = new Point(10, 100),
            Size = new Size(930, 200)
        };

        // Простая пересборка
        btnBuildSimple = new Button
        {
            Text = "Пересобрать (быстро)",
            Location = new Point(20, 30),
            Size = new Size(200, 40),
            BackColor = Color.LightGreen
        };
        btnBuildSimple.Click += BtnBuildSimple_Click;

        var lblBuildSimpleDesc = new Label
        {
            Text = "docker-compose up -d --build\n" +
                   "Пересобирает образ и перезапускает контейнер.\n" +
                   "Использует кэш Docker для ускорения.",
            Location = new Point(230, 30),
            Size = new Size(680, 60),
            ForeColor = Color.Gray
        };

        // Полная пересборка
        btnBuildFull = new Button
        {
            Text = "Полная пересборка",
            Location = new Point(20, 100),
            Size = new Size(200, 40),
            BackColor = Color.LightBlue
        };
        btnBuildFull.Click += BtnBuildFull_Click;

        var lblBuildFullDesc = new Label
        {
            Text = "docker-compose down && docker-compose up -d --build\n" +
                   "Останавливает контейнер, пересобирает образ, запускает заново.\n" +
                   "Рекомендуется после изменений в коде.",
            Location = new Point(230, 100),
            Size = new Size(680, 60),
            ForeColor = Color.Gray
        };

        grpQuick.Controls.AddRange(new Control[] 
        { 
            btnBuildSimple, lblBuildSimpleDesc,
            btnBuildFull, lblBuildFullDesc
        });

        // Расширенные операции
        var grpAdvanced = new GroupBox
        {
            Text = "Расширенные операции",
            Location = new Point(10, 310),
            Size = new Size(930, 270)
        };

        // Сборка без кэша
        btnBuildNoCache = new Button
        {
            Text = "Пересборка без кэша",
            Location = new Point(20, 30),
            Size = new Size(200, 40),
            BackColor = Color.Orange
        };
        btnBuildNoCache.Click += BtnBuildNoCache_Click;

        var lblBuildNoCacheDesc = new Label
        {
            Text = "docker-compose build --no-cache && docker-compose up -d\n" +
                   "Полная пересборка без использования кэша.\n" +
                   "Используйте при проблемах с зависимостями или странных ошибках.\n" +
                   "⚠️ МЕДЛЕННО! Может занять 5-10 минут.",
            Location = new Point(230, 30),
            Size = new Size(680, 80),
            ForeColor = Color.Gray
        };

        // Обновить базовые образы
        btnPullImages = new Button
        {
            Text = "Обновить базовые образы",
            Location = new Point(20, 120),
            Size = new Size(200, 40),
            BackColor = Color.LightCyan
        };
        btnPullImages.Click += BtnPullImages_Click;

        var lblPullDesc = new Label
        {
            Text = "docker-compose pull && docker-compose up -d --build\n" +
                   "Скачивает последние версии базовых образов (mcr.microsoft.com/dotnet/aspnet:8.0).\n" +
                   "Рекомендуется запускать раз в месяц для получения обновлений безопасности.",
            Location = new Point(230, 120),
            Size = new Size(680, 70),
            ForeColor = Color.Gray
        };

        // Очистка неиспользуемых образов
        btnCleanImages = new Button
        {
            Text = "Очистить старые образы",
            Location = new Point(20, 200),
            Size = new Size(200, 40),
            BackColor = Color.LightYellow
        };
        btnCleanImages.Click += BtnCleanImages_Click;

        var lblCleanDesc = new Label
        {
            Text = "docker image prune -a -f\n" +
                   "Удаляет все неиспользуемые Docker образы для освобождения места.\n" +
                   "⚠️ Следующая сборка будет медленнее из-за отсутствия кэша.",
            Location = new Point(230, 200),
            Size = new Size(680, 60),
            ForeColor = Color.Gray
        };

        grpAdvanced.Controls.AddRange(new Control[] 
        { 
            btnBuildNoCache, lblBuildNoCacheDesc,
            btnPullImages, lblPullDesc,
            btnCleanImages, lblCleanDesc
        });

        tab.Controls.AddRange(new Control[] { grpProject, grpQuick, grpAdvanced });
    }

    // Backup methods (same as before)
    private void LoadBackups()
    {
        var backups = _backupService.GetAvailableBackups();

        cmbBackups.Items.Clear();
        foreach (var backup in backups)
        {
            cmbBackups.Items.Add(backup);
        }
        cmbBackups.DisplayMember = "DisplayName";
        if (cmbBackups.Items.Count > 0)
        {
            cmbBackups.SelectedIndex = 0;
        }

        dgvBackups.Rows.Clear();
        foreach (var backup in backups)
        {
            dgvBackups.Rows.Add(
                backup.FileName,
                FormatFileSize(backup.Size),
                backup.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss"),
                backup.Age
            );
        }

        Log($"Найдено бэкапов: {backups.Count}");
    }

    private async void BtnBackup_Click(object? sender, EventArgs e)
    {
        var containerName = txtContainerName.Text.Trim();
        var databasePath = txtDatabasePath.Text.Trim();

        if (string.IsNullOrEmpty(containerName) || string.IsNullOrEmpty(databasePath))
        {
            MessageBox.Show("Укажите имя контейнера и путь к базе данных!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        txtLog.Clear();

        try
        {
            var progress = new Progress<string>(message => Log(message));
            var backupPath = await _backupService.CreateBackupAsync(containerName, databasePath, progress);

            MessageBox.Show($"Бэкап создан успешно!\n\n{Path.GetFileName(backupPath)}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadBackups();
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА: {ex.Message}");
            MessageBox.Show($"Ошибка при создании бэкапа:\n\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnRestore_Click(object? sender, EventArgs e)
    {
        if (cmbBackups.SelectedItem is not BackupInfo selectedBackup)
        {
            MessageBox.Show("Выберите бэкап для восстановления!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"ВНИМАНИЕ! Это заменит текущую базу данных!\n\n" +
            $"Будет восстановлен бэкап:\n{selectedBackup.FileName}\n\n" +
            $"Текущая база будет сохранена как резервная копия.\n\n" +
            $"Продолжить?",
            "Подтверждение",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        var containerName = txtContainerName.Text.Trim();
        var databasePath = txtDatabasePath.Text.Trim();

        SetBusy(true);
        txtLog.Clear();

        try
        {
            var progress = new Progress<string>(message => Log(message));
            await _backupService.RestoreBackupAsync(containerName, databasePath, selectedBackup.FilePath, progress);

            MessageBox.Show("База данных восстановлена успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            LoadBackups();
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА: {ex.Message}");
            MessageBox.Show($"Ошибка при восстановлении:\n\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (dgvBackups.SelectedRows.Count == 0)
        {
            MessageBox.Show("Выберите бэкап для удаления!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var selectedRow = dgvBackups.SelectedRows[0];
        var fileName = selectedRow.Cells["FileName"].Value?.ToString();
        if (string.IsNullOrEmpty(fileName)) return;

        var result = MessageBox.Show($"Удалить бэкап?\n\n{fileName}", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes)
        {
            var filePath = Path.Combine(_backupDirectory, fileName);
            _backupService.DeleteBackup(filePath);
            Log($"Удалён бэкап: {fileName}");
            LoadBackups();
        }
    }

    private void BtnCleanOld_Click(object? sender, EventArgs e)
    {
        var backups = _backupService.GetAvailableBackups();
        var oldBackups = backups.Where(b => b.CreatedDate < DateTime.Now.AddDays(-30)).ToList();

        if (oldBackups.Count == 0)
        {
            MessageBox.Show("Нет бэкапов старше 30 дней.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var result = MessageBox.Show($"Найдено {oldBackups.Count} бэкапов старше 30 дней.\n\nУдалить их?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes)
        {
            _backupService.DeleteOldBackups(30);
            Log($"Удалено {oldBackups.Count} старых бэкапов");
            LoadBackups();
        }
    }

    // Docker Management methods
    private async Task UpdateContainerStatusAsync()
    {
        try
        {
            var containerName = txtContainerName.Text.Trim();
            var isRunning = await _dockerService.IsContainerRunningAsync(containerName);

            if (isRunning)
            {
                lblContainerStatus.Text = $"✓ Контейнер '{containerName}' запущен и работает";
                lblContainerStatus.ForeColor = Color.Green;
            }
            else
            {
                lblContainerStatus.Text = $"✗ Контейнер '{containerName}' остановлен";
                lblContainerStatus.ForeColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            lblContainerStatus.Text = $"Ошибка проверки статуса: {ex.Message}";
            lblContainerStatus.ForeColor = Color.Orange;
        }
    }

    private async void BtnStop_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Остановить контейнер?\n\nПриложение станет недоступным до запуска.",
            "Подтверждение",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        SetBusy(true);
        txtLog.Clear();

        try
        {
            Log("Остановка контейнера...");
            await _dockerService.StopContainerAsync(txtContainerName.Text.Trim());
            Log("✓ Контейнер остановлен");
            await UpdateContainerStatusAsync();
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА: {ex.Message}");
            MessageBox.Show($"Ошибка при остановке:\n\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnStart_Click(object? sender, EventArgs e)
    {
        SetBusy(true);
        txtLog.Clear();

        try
        {
            Log("Запуск контейнера...");
            await _dockerService.StartContainerAsync(txtContainerName.Text.Trim());
            Log("✓ Контейнер запущен");
            await Task.Delay(2000);
            await UpdateContainerStatusAsync();
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА: {ex.Message}");
            MessageBox.Show($"Ошибка при запуске:\n\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnRestart_Click(object? sender, EventArgs e)
    {
        SetBusy(true);
        txtLog.Clear();

        try
        {
            Log("Остановка контейнера...");
            await _dockerService.StopContainerAsync(txtContainerName.Text.Trim());
            Log("✓ Контейнер остановлен");
            
            await Task.Delay(2000);
            
            Log("Запуск контейнера...");
            await _dockerService.StartContainerAsync(txtContainerName.Text.Trim());
            Log("✓ Контейнер запущен");
            
            await Task.Delay(2000);
            await UpdateContainerStatusAsync();
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА: {ex.Message}");
            MessageBox.Show($"Ошибка при перезапуске:\n\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnViewLogs_Click(object? sender, EventArgs e)
    {
        SetBusy(true);
        txtLog.Clear();

        try
        {
            Log("Получение логов контейнера...");
            var logs = await _dockerService.GetContainerLogsAsync(txtContainerName.Text.Trim(), 50);
            Log("=== Логи контейнера (последние 50 строк) ===");
            Log(logs);
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА: {ex.Message}");
            MessageBox.Show($"Ошибка при получении логов:\n\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    // Build methods
    private async void BtnBuildSimple_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Пересобрать приложение (быстро)?\n\n" +
            "Команда: docker-compose up -d --build\n\n" +
            "Это займёт 1-2 минуты.",
            "Подтверждение",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        await ExecuteBuildCommandAsync("up -d --build", "Быстрая пересборка");
    }

    private async void BtnBuildFull_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Полная пересборка приложения?\n\n" +
            "Команды:\n" +
            "1. docker-compose down\n" +
            "2. docker-compose up -d --build\n\n" +
            "Это займёт 2-3 минуты.",
            "Подтверждение",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        SetBusy(true);
        txtLog.Clear();

        try
        {
            Log("Остановка контейнера...");
            await _dockerService.ExecuteDockerComposeCommandAsync("down", txtProjectPath.Text);
            Log("✓ Контейнер остановлен");

            Log("\nПересборка образа...");
            await _dockerService.ExecuteDockerComposeCommandAsync("up -d --build", txtProjectPath.Text);
            Log("✓ Образ пересобран и контейнер запущен");

            MessageBox.Show("Полная пересборка завершена успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await UpdateContainerStatusAsync();
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА: {ex.Message}");
            MessageBox.Show($"Ошибка при пересборке:\n\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnBuildNoCache_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Пересборка без кэша?\n\n" +
            "⚠️ ВНИМАНИЕ: Это медленная операция!\n\n" +
            "Команды:\n" +
            "1. docker-compose build --no-cache\n" +
            "2. docker-compose up -d\n\n" +
            "Это займёт 5-10 минут.\n\n" +
            "Продолжить?",
            "Подтверждение",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        SetBusy(true);
        txtLog.Clear();

        try
        {
            Log("Пересборка образа без кэша...");
            Log("⚠️ Это может занять 5-10 минут...");
            await _dockerService.ExecuteDockerComposeCommandAsync("build --no-cache", txtProjectPath.Text);
            Log("✓ Образ пересобран");

            Log("\nЗапуск контейнера...");
            await _dockerService.ExecuteDockerComposeCommandAsync("up -d", txtProjectPath.Text);
            Log("✓ Контейнер запущен");

            MessageBox.Show("Пересборка без кэша завершена успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await UpdateContainerStatusAsync();
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА: {ex.Message}");
            MessageBox.Show($"Ошибка при пересборке:\n\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnPullImages_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Обновить базовые образы?\n\n" +
            "Команды:\n" +
            "1. docker-compose pull\n" +
            "2. docker-compose up -d --build\n\n" +
            "Это займёт 3-5 минут.",
            "Подтверждение",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        SetBusy(true);
        txtLog.Clear();

        try
        {
            Log("Скачивание последних версий базовых образов...");
            await _dockerService.ExecuteDockerComposeCommandAsync("pull", txtProjectPath.Text);
            Log("✓ Образы обновлены");

            Log("\nПересборка с новыми образами...");
            await _dockerService.ExecuteDockerComposeCommandAsync("up -d --build", txtProjectPath.Text);
            Log("✓ Контейнер пересобран и запущен");

            MessageBox.Show("Базовые образы обновлены успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await UpdateContainerStatusAsync();
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА: {ex.Message}");
            MessageBox.Show($"Ошибка при обновлении образов:\n\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void BtnCleanImages_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Очистить неиспользуемые Docker образы?\n\n" +
            "⚠️ ВНИМАНИЕ:\n" +
            "- Удалит все неиспользуемые образы\n" +
            "- Освободит место на диске\n" +
            "- Следующая сборка будет медленнее\n\n" +
            "Команда: docker image prune -a -f\n\n" +
            "Продолжить?",
            "Подтверждение",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (result != DialogResult.Yes) return;

        SetBusy(true);
        txtLog.Clear();

        try
        {
            Log("Очистка неиспользуемых образов...");
            var output = await _dockerService.ExecuteDockerCommandAsync("image prune -a -f");
            Log(output);
            Log("✓ Очистка завершена");

            MessageBox.Show("Неиспользуемые образы удалены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА: {ex.Message}");
            MessageBox.Show($"Ошибка при очистке:\n\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ExecuteBuildCommandAsync(string command, string description)
    {
        SetBusy(true);
        txtLog.Clear();

        try
        {
            Log($"{description}...");
            await _dockerService.ExecuteDockerComposeCommandAsync(command, txtProjectPath.Text);
            Log($"✓ {description} завершена");

            MessageBox.Show($"{description} завершена успешно!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await UpdateContainerStatusAsync();
        }
        catch (Exception ex)
        {
            Log($"ОШИБКА: {ex.Message}");
            MessageBox.Show($"Ошибка:\n\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    // Helper methods
    private void SetBusy(bool busy)
    {
        btnBackup.Enabled = !busy;
        btnRestore.Enabled = !busy;
        btnDelete.Enabled = !busy;
        btnCleanOld.Enabled = !busy;
        btnRefresh.Enabled = !busy;
        btnStop.Enabled = !busy;
        btnStart.Enabled = !busy;
        btnRestart.Enabled = !busy;
        btnViewLogs.Enabled = !busy;
        btnBuildSimple.Enabled = !busy;
        btnBuildFull.Enabled = !busy;
        btnBuildNoCache.Enabled = !busy;
        btnPullImages.Enabled = !busy;
        btnCleanImages.Enabled = !busy;
        
        progressBar.Visible = busy;
        lblStatus.Text = busy ? "Работаю..." : "Готов";
        lblStatus.ForeColor = busy ? Color.Orange : Color.Green;
        Application.DoEvents();
    }

    private void Log(string message)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(() => Log(message));
            return;
        }

        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        txtLog.SelectionStart = txtLog.Text.Length;
        txtLog.ScrollToCaret();
        Application.DoEvents();
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
