@echo off
chcp 65001 >nul
echo ═══════════════════════════════════════════════════════════════
echo   УДАЛЕНИЕ ДУБЛИКАТОВ ПОЛЬЗОВАТЕЛЕЙ
echo ═══════════════════════════════════════════════════════════════
echo.

if "%1"=="" (
    echo Использование: delete-duplicate-user.bat email@example.com
    echo.
    echo Пример:
    echo   delete-duplicate-user.bat novozhilov.vv@dvfu.ru
    echo.
    pause
    exit /b 1
)

echo Поиск дубликатов для: %1
echo.

cd ChatApp.Server
dotnet run delete-duplicate %1

echo.
echo ═══════════════════════════════════════════════════════════════
echo   ГОТОВО
echo ═══════════════════════════════════════════════════════════════
echo.
pause
