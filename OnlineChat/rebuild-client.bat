@echo off
chcp 65001 >nul
echo ═══════════════════════════════════════════════════════════════
echo   ПЕРЕСБОРКА КЛИЕНТА ChatApp
echo ═══════════════════════════════════════════════════════════════
echo.

echo [1/2] Остановка запущенного клиента...
for /f "tokens=2" %%a in ('tasklist ^| findstr /I "ChatApp.Client.exe"') do (
    echo Остановка процесса %%a...
    taskkill /PID %%a /F >nul 2>&1
)

echo.
echo [2/2] Сборка проекта...
cd ChatApp.Client
dotnet build

echo.
echo ═══════════════════════════════════════════════════════════════
echo   ГОТОВО
echo ═══════════════════════════════════════════════════════════════
echo.
echo Теперь можно запустить клиент:
echo   cd ChatApp.Client
echo   dotnet run
echo.
pause
