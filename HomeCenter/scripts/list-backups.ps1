# Показать список всех бэкапов

$BackupDir = ".\backups"

if (-not (Test-Path $BackupDir)) {
    Write-Host "No backups directory found." -ForegroundColor Yellow
    Write-Host "Create your first backup with: .\scripts\backup.ps1" -ForegroundColor Cyan
    exit 0
}

$backups = Get-ChildItem $BackupDir -Filter "quiz-*.db" -ErrorAction SilentlyContinue

if ($backups.Count -eq 0) {
    Write-Host "No backups found in $BackupDir" -ForegroundColor Yellow
    Write-Host "Create your first backup with: .\scripts\backup.ps1" -ForegroundColor Cyan
    exit 0
}

Write-Host "`n=== Available Backups ===" -ForegroundColor Green
Write-Host ""

$backups | 
    Sort-Object LastWriteTime -Descending |
    Select-Object @{Name="Backup File";Expression={$_.Name}}, 
                  @{Name="Size";Expression={"{0:N2} KB" -f ($_.Length / 1KB)}}, 
                  @{Name="Created";Expression={$_.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")}},
                  @{Name="Age";Expression={
                      $age = (Get-Date) - $_.LastWriteTime
                      if ($age.TotalDays -ge 1) {
                          "{0:N0} days ago" -f $age.TotalDays
                      } elseif ($age.TotalHours -ge 1) {
                          "{0:N0} hours ago" -f $age.TotalHours
                      } else {
                          "{0:N0} minutes ago" -f $age.TotalMinutes
                      }
                  }} |
    Format-Table -AutoSize

# Статистика
$totalSize = ($backups | Measure-Object -Property Length -Sum).Sum
$totalSizeFormatted = "{0:N2} MB" -f ($totalSize / 1MB)
$oldestBackup = ($backups | Sort-Object LastWriteTime | Select-Object -First 1).LastWriteTime
$newestBackup = ($backups | Sort-Object LastWriteTime -Descending | Select-Object -First 1).LastWriteTime

Write-Host "Total backups: $($backups.Count)" -ForegroundColor Cyan
Write-Host "Total size: $totalSizeFormatted" -ForegroundColor Cyan
Write-Host "Oldest backup: $($oldestBackup.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Cyan
Write-Host "Newest backup: $($newestBackup.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor Cyan

# Предупреждение о старых бэкапах
$oldBackups = $backups | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) }
if ($oldBackups.Count -gt 0) {
    Write-Host "`n⚠ Warning: $($oldBackups.Count) backups are older than 30 days" -ForegroundColor Yellow
    Write-Host "Consider deleting them to free up space." -ForegroundColor Yellow
}

Write-Host "`nTo restore from a backup, run:" -ForegroundColor Green
Write-Host "  .\scripts\restore.ps1" -ForegroundColor Cyan
Write-Host "  or" -ForegroundColor Gray
Write-Host "  .\scripts\restore.ps1 -BackupFile .\backups\quiz-YYYYMMDD-HHMMSS.db" -ForegroundColor Cyan
