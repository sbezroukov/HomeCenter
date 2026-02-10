# Запустите этот скрипт от имени администратора:
# ПКМ по файлу -> "Запуск с помощью PowerShell" (или откройте PowerShell от администратора и выполните: .\setup-firewall.ps1)

# Правило для QuizApp на порту 8080
New-NetFirewallRule -DisplayName "QuizApp HTTP 8080" -Direction Inbound -LocalPort 8080 -Protocol TCP -Action Allow -ErrorAction SilentlyContinue

# Разрешить входящий ping (ICMP)
New-NetFirewallRule -DisplayName "Allow ICMPv4 In" -Protocol ICMPv4 -IcmpType 8 -Direction Inbound -Action Allow -ErrorAction SilentlyContinue

Write-Host "Готово. Правила брандмауэра добавлены." -ForegroundColor Green
