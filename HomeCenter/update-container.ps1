# Обновление приложения HomeCenter в контейнере.
# Запускайте из папки HomeCenter (где лежит docker-compose.yml).
# Подробнее: DOCKER-UPDATE.md

param(
    [switch]$Down,      # Сначала выполнить docker compose down
    [switch]$NoCache    # Пересборка без кэша (docker compose build --no-cache)
)

$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

if ($Down) {
    Write-Host "Останавливаем контейнер..." -ForegroundColor Yellow
    docker compose down
}

if ($NoCache) {
    Write-Host "Пересборка образа без кэша..." -ForegroundColor Yellow
    docker compose build --no-cache
    Write-Host "Запуск контейнера..." -ForegroundColor Yellow
    docker compose up -d
} else {
    Write-Host "Пересборка и запуск контейнера..." -ForegroundColor Yellow
    docker compose up -d --build
}

Write-Host "Готово. Приложение: http://localhost:8080" -ForegroundColor Green
