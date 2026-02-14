# Настроить автоматический бэкап через Windows Task Scheduler

$ScriptPath = Join-Path $PSScriptRoot "backup.ps1"
$WorkingDir = Split-Path $PSScriptRoot -Parent

Write-Host "=== Setup Automatic Backup ===" -ForegroundColor Green
Write-Host ""
Write-Host "This will create a Windows Task Scheduler task to backup your database daily at 2:00 AM" -ForegroundColor Yellow
Write-Host ""
Write-Host "Script: $ScriptPath" -ForegroundColor Cyan
Write-Host "Working Directory: $WorkingDir" -ForegroundColor Cyan
Write-Host ""

$confirm = Read-Host "Continue? (y/n)"
if ($confirm -ne 'y') {
    Write-Host "Cancelled." -ForegroundColor Yellow
    exit 0
}

# Проверить права администратора
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "ERROR: This script requires Administrator privileges!" -ForegroundColor Red
    Write-Host "Please run PowerShell as Administrator and try again." -ForegroundColor Yellow
    exit 1
}

# Создать задачу в Task Scheduler
$taskName = "HomeCenter Database Backup"
$taskDescription = "Automatic backup of HomeCenter SQLite database"

# Удалить существующую задачу если есть
$existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($existingTask) {
    Write-Host "Removing existing task..." -ForegroundColor Yellow
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
}

# Создать действие
$action = New-ScheduledTaskAction `
    -Execute "PowerShell.exe" `
    -Argument "-NoProfile -ExecutionPolicy Bypass -File `"$ScriptPath`"" `
    -WorkingDirectory $WorkingDir

# Создать триггер (каждый день в 2:00)
$trigger = New-ScheduledTaskTrigger -Daily -At 2:00AM

# Создать настройки
$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -RunOnlyIfNetworkAvailable:$false

# Создать principal (запуск от текущего пользователя)
$principal = New-ScheduledTaskPrincipal `
    -UserId $env:USERNAME `
    -LogonType S4U `
    -RunLevel Limited

# Зарегистрировать задачу
Register-ScheduledTask `
    -TaskName $taskName `
    -Description $taskDescription `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Principal $principal | Out-Null

Write-Host "`n✓ Automatic backup configured successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Task Details:" -ForegroundColor Cyan
Write-Host "  Name: $taskName" -ForegroundColor Gray
Write-Host "  Schedule: Daily at 2:00 AM" -ForegroundColor Gray
Write-Host "  Script: $ScriptPath" -ForegroundColor Gray
Write-Host ""
Write-Host "To manage the task:" -ForegroundColor Yellow
Write-Host "  1. Open Task Scheduler (taskschd.msc)" -ForegroundColor Gray
Write-Host "  2. Find '$taskName' in the task list" -ForegroundColor Gray
Write-Host "  3. Right-click to Run, Disable, or Delete" -ForegroundColor Gray
Write-Host ""
Write-Host "To test the backup now:" -ForegroundColor Yellow
Write-Host "  .\scripts\backup.ps1" -ForegroundColor Cyan
