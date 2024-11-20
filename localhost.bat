@echo off
setlocal enabledelayedexpansion

set HOSTS_FILE=%SystemRoot%\System32\drivers\etc\hosts

echo Updating hosts file...

rem Check if the entry for career.aiko already exists
findstr /c:"127.0.0.1 career.aiko" %HOSTS_FILE% >nul
if %errorlevel% neq 0 (
    echo 127.0.0.1 career.aiko >> %HOSTS_FILE%
    echo Added entry for career.aiko
) else (
    echo Entry for career.aiko already exists
)

rem Check if the entry for admin.career.aiko already exists
findstr /c:"127.0.0.1 admin.career.aiko" %HOSTS_FILE% >nul
if %errorlevel% neq 0 (
    echo 127.0.0.1 admin.career.aiko >> %HOSTS_FILE%
    echo Added entry for admin.career.aiko
) else (
    echo Entry for admin.career.aiko already exists
)

echo Hosts file update complete.
endlocal
pause