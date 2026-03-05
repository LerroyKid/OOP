@echo off
chcp 65001 >nul
echo ═══════════════════════════════════════════════════════════════
echo   ОСТАНОВКА КЛИЕНТА ChatApp
echo ═══════════════════════════════════════════════════════════════
echo.

echo Поиск процесса ChatApp.Client.exe...
echo.

set found=0
for /f "tokens=2" %%a in ('tasklist ^| findstr /I "ChatApp.Client.exe"') do (
    set found=1
    echo Найден процесс: %%a
    taskkill /PID %%a /F >nul 2>&1
    if errorlevel 1 (
        echo ❌ Не удалось остановить процесс %%a
    ) else (
        echo ✓ Процесс %%a успешно остановлен
    )
)

if %found%==0 (
    echo ℹ️  Клиент не запущен
)

echo.
echo ═══════════════════════════════════════════════════════════════
echo   ГОТОВО
echo ═══════════════════════════════════════════════════════════════
echo.
pause
