# Восстановить SQLite базу данных в Docker контейнер

param(
    [Parameter(Mandatory=$false)]
    [string]$BackupFile
)

$BackupDir = ".\backups"
$ContainerName = "homecenter"
$DbPath = "/app/data/quiz.db"

# Если файл не указан, найти последний бэкап
if ([string]::IsNullOrWhiteSpace($BackupFile)) {
    $latestBackup = Get-ChildItem $BackupDir -Filter "quiz-*.db" -ErrorAction SilentlyContinue | 
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 1
    
    if ($null -eq $latestBackup) {
        Write-Host "ERROR: No backup files found in $BackupDir" -ForegroundColor Red
        Write-Host "Create a backup first with: .\scripts\backup.ps1" -ForegroundColor Yellow
        exit 1
    }
    
    $BackupFile = $latestBackup.FullName
}

# Проверить что файл существует
if (-not (Test-Path $BackupFile)) {
    Write-Host "ERROR: Backup file not found: $BackupFile" -ForegroundColor Red
    exit 1
}

Write-Host "WARNING: This will replace the current database!" -ForegroundColor Red
Write-Host "  Backup file: $BackupFile" -ForegroundColor Yellow
Write-Host "  Container: $ContainerName" -ForegroundColor Yellow
Write-Host "  Destination: $DbPath" -ForegroundColor Yellow
Write-Host ""

$confirm = Read-Host "Are you sure? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "Cancelled." -ForegroundColor Yellow
    exit 0
}

# Проверить что контейнер запущен
$containerRunning = docker ps --filter "name=$ContainerName" --format "{{.Names}}" 2>$null

if ($containerRunning -ne $ContainerName) {
    Write-Host "ERROR: Container '$ContainerName' is not running!" -ForegroundColor Red
    Write-Host "Start it with: docker-compose up -d" -ForegroundColor Yellow
    exit 1
}

Write-Host "`nStep 1: Creating backup of current database..." -ForegroundColor Green
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$currentBackup = "$BackupDir\quiz-before-restore-$timestamp.db"
docker cp "${ContainerName}:${DbPath}" $currentBackup 2>$null

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Current database backed up to: $currentBackup" -ForegroundColor Cyan
} else {
    Write-Host "⚠ Warning: Could not backup current database (maybe it doesn't exist yet)" -ForegroundColor Yellow
}

Write-Host "`nStep 2: Stopping container..." -ForegroundColor Green
docker-compose stop 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to stop container!" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Container stopped" -ForegroundColor Cyan

Write-Host "`nStep 3: Copying backup file to container..." -ForegroundColor Green
docker cp $BackupFile "${ContainerName}:${DbPath}" 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to copy backup file!" -ForegroundColor Red
    Write-Host "Starting container back..." -ForegroundColor Yellow
    docker-compose start 2>$null
    exit 1
}

Write-Host "✓ Backup file copied" -ForegroundColor Cyan

Write-Host "`nStep 4: Starting container..." -ForegroundColor Green
docker-compose start 2>$null

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to start container!" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Container started" -ForegroundColor Cyan

# Подождать пока контейнер запустится
Write-Host "`nWaiting for container to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# Проверить что контейнер работает
$containerRunning = docker ps --filter "name=$ContainerName" --format "{{.Names}}" 2>$null

if ($containerRunning -eq $ContainerName) {
    Write-Host "`n✓ Database restored successfully!" -ForegroundColor Green
    Write-Host "  From: $BackupFile" -ForegroundColor Cyan
    Write-Host "  Application is running at: http://localhost:8080" -ForegroundColor Cyan
} else {
    Write-Host "`n✗ ERROR: Container is not running!" -ForegroundColor Red
    Write-Host "Check logs with: docker-compose logs" -ForegroundColor Yellow
}
