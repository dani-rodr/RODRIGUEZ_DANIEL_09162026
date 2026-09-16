@echo off
setlocal

cd /d "%~dp0"

where docker >nul 2>&1
if errorlevel 1 (
    echo Docker is required. Install Docker Desktop and try again.
    goto :failed
)

docker compose version >nul 2>&1
if errorlevel 1 (
    echo Docker Compose is required. Update Docker Desktop and try again.
    goto :failed
)

if not exist .env (
    copy /Y .env.example .env >nul
    echo Created .env from .env.example.
)

docker compose up --build --detach --wait
if errorlevel 1 goto :failed

for /f "tokens=2 delims==" %%P in ('findstr /B "API_PORT=" .env') do set "API_PORT=%%P"
if not defined API_PORT set "API_PORT=5274"

echo API is running at http://localhost:%API_PORT%
echo Swagger: http://localhost:%API_PORT%/swagger
echo Default API key: local-development-key
echo Use the API_KEY value from .env if you changed it.
echo.
pause
exit /b 0

:failed
echo.
echo Setup failed. Review the message above, then press any key to close this window.
pause
exit /b 1
