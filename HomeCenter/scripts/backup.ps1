# Бэкап SQLite базы данных из Docker контейнера

$BackupDir = ".\backups"
$ContainerName = "homecenter"
$DbPath = "/app/data/quiz.db"

# Создать директорию для бэкапов
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

# Проверить что контейнер запущен
$containerRunning = docker ps --filter "name=$ContainerName" --format "{{.Names}}" 2>$null

if ($containerRunning -ne $ContainerName) {
    Write-Host "ERROR: Container '$ContainerName' is not running!" -ForegroundColor Red
    Write-Host "Start it with: docker-compose up -d" -ForegroundColor Yellow
    exit 1
}

# Создать имя файла с датой
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupFile = "$BackupDir\quiz-$timestamp.db"

Write-Host "Creating backup..." -ForegroundColor Green
Write-Host "  Container: $ContainerName" -ForegroundColor Cyan
Write-Host "  Source: $DbPath" -ForegroundColor Cyan
Write-Host "  Destination: $backupFile" -ForegroundColor Cyan

# Скопировать базу данных из контейнера
docker cp "${ContainerName}:${DbPath}" $backupFile 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to copy database from container!" -ForegroundColor Red
    exit 1
}

# Проверить что файл создан
if (Test-Path $backupFile) {
    $size = (Get-Item $backupFile).Length
    $sizeFormatted = "{0:N2} KB" -f ($size / 1KB)
    
    Write-Host "`n✓ Backup created successfully!" -ForegroundColor Green
    Write-Host "  File: $backupFile" -ForegroundColor Cyan
    Write-Host "  Size: $sizeFormatted" -ForegroundColor Cyan
    
    # Показать список всех бэкапов
    Write-Host "`nAll backups:" -ForegroundColor Yellow
    Get-ChildItem $BackupDir -Filter "quiz-*.db" | 
        Sort-Object LastWriteTime -Descending |
        Select-Object Name, @{Name="Size";Expression={"{0:N2} KB" -f ($_.Length / 1KB)}}, LastWriteTime |
        Format-Table -AutoSize
    
    # Предложить удалить старые бэкапы
    $oldBackups = Get-ChildItem $BackupDir -Filter "quiz-*.db" | 
        Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) }
    
    if ($oldBackups.Count -gt 0) {
        Write-Host "Found $($oldBackups.Count) backups older than 30 days." -ForegroundColor Yellow
        $confirm = Read-Host "Delete old backups? (y/n)"
        if ($confirm -eq 'y') {
            $oldBackups | Remove-Item -Force
            Write-Host "✓ Old backups deleted." -ForegroundColor Green
        }
    }
} else {
    Write-Host "✗ ERROR: Backup file was not created!" -ForegroundColor Red
    exit 1
}
