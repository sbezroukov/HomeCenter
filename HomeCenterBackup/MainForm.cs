using System.ComponentModel;

namespace HomeCenterBackup;

public partial class MainForm : Form
{
    private readonly DockerService _dockerService;
    private readonly BackupService _backupService;
    private readonly string _backupDirectory;

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

    public MainForm()
    {
        _backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");
        _dockerService = new DockerService();
        _backupService = new BackupService(_dockerService, _backupDirectory);

        InitializeComponent();
        InitializeCustomComponents();
        LoadBackups();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();
        
        // Form
        this.ClientSize = new Size(900, 700);
        this.Text = "HomeCenter Database Backup Manager";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(900, 700);

        this.ResumeLayout(false);
    }

    private void InitializeCustomComponents()
    {
        // Группа: Настройки Docker
        var grpDocker = new GroupBox
        {
            Text = "Настройки Docker",
            Location = new Point(10, 10),
            Size = new Size(860, 100)
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
            Size = new Size(400, 40),
            ForeColor = Color.Gray
        };

        grpDocker.Controls.AddRange(new Control[] { lblContainer, txtContainerName, lblDatabase, txtDatabasePath, lblBackupDir, lblBackupDirValue });

        // Группа: Создание бэкапа
        var grpBackup = new GroupBox
        {
            Text = "Создание бэкапа",
            Location = new Point(10, 120),
            Size = new Size(420, 80)
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
            Size = new Size(240, 30),
            ForeColor = Color.Gray
        };

        grpBackup.Controls.AddRange(new Control[] { btnBackup, lblBackupInfo });

        // Группа: Восстановление
        var grpRestore = new GroupBox
        {
            Text = "Восстановление из бэкапа",
            Location = new Point(450, 120),
            Size = new Size(420, 80)
        };

        cmbBackups = new ComboBox
        {
            Location = new Point(10, 30),
            Size = new Size(250, 23),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        btnRestore = new Button
        {
            Text = "Восстановить",
            Location = new Point(270, 28),
            Size = new Size(130, 28),
            BackColor = Color.LightCoral
        };
        btnRestore.Click += BtnRestore_Click;

        grpRestore.Controls.AddRange(new Control[] { cmbBackups, btnRestore });

        // Группа: Управление бэкапами
        var grpManage = new GroupBox
        {
            Text = "Управление бэкапами",
            Location = new Point(10, 210),
            Size = new Size(860, 280)
        };

        dgvBackups = new DataGridView
        {
            Location = new Point(10, 25),
            Size = new Size(840, 200),
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
            Location = new Point(10, 235),
            Size = new Size(100, 30)
        };
        btnRefresh.Click += (s, e) => LoadBackups();

        btnDelete = new Button
        {
            Text = "Удалить выбранный",
            Location = new Point(120, 235),
            Size = new Size(150, 30),
            BackColor = Color.LightYellow
        };
        btnDelete.Click += BtnDelete_Click;

        btnCleanOld = new Button
        {
            Text = "Очистить старые (>30 дней)",
            Location = new Point(280, 235),
            Size = new Size(200, 30),
            BackColor = Color.LightYellow
        };
        btnCleanOld.Click += BtnCleanOld_Click;

        grpManage.Controls.AddRange(new Control[] { dgvBackups, btnRefresh, btnDelete, btnCleanOld });

        // Лог и прогресс
        var lblLog = new Label
        {
            Text = "Лог операций:",
            Location = new Point(10, 500),
            AutoSize = true
        };

        txtLog = new TextBox
        {
            Location = new Point(10, 520),
            Size = new Size(860, 120),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            BackColor = Color.White
        };

        progressBar = new ProgressBar
        {
            Location = new Point(10, 650),
            Size = new Size(760, 23),
            Style = ProgressBarStyle.Marquee,
            Visible = false
        };

        lblStatus = new Label
        {
            Location = new Point(780, 650),
            Size = new Size(90, 23),
            Text = "Готов",
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.Green
        };

        // Добавить все контролы на форму
        this.Controls.AddRange(new Control[] 
        { 
            grpDocker, grpBackup, grpRestore, grpManage, 
            lblLog, txtLog, progressBar, lblStatus 
        });
    }

    private void LoadBackups()
    {
        var backups = _backupService.GetAvailableBackups();

        // Обновить ComboBox
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

        // Обновить DataGridView
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

        if (string.IsNullOrEmpty(containerName))
        {
            MessageBox.Show("Укажите имя контейнера!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrEmpty(databasePath))
        {
            MessageBox.Show("Укажите путь к базе данных!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        if (result != DialogResult.Yes)
        {
            return;
        }

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

        if (string.IsNullOrEmpty(fileName))
            return;

        var result = MessageBox.Show(
            $"Удалить бэкап?\n\n{fileName}",
            "Подтверждение",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

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

        var result = MessageBox.Show(
            $"Найдено {oldBackups.Count} бэкапов старше 30 дней.\n\n" +
            $"Удалить их?",
            "Подтверждение",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _backupService.DeleteOldBackups(30);
            Log($"Удалено {oldBackups.Count} старых бэкапов");
            LoadBackups();
        }
    }

    private void SetBusy(bool busy)
    {
        btnBackup.Enabled = !busy;
        btnRestore.Enabled = !busy;
        btnDelete.Enabled = !busy;
        btnCleanOld.Enabled = !busy;
        btnRefresh.Enabled = !busy;
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
