@echo off
title CRM Messaging Console - Demo Saga
echo ======================================================
echo  VALIDATION PRELIMINAIRE DES TESTS UNITAIRES
echo ======================================================
dotnet test src\CRM.Tests\CRM.Tests.csproj --nologo
if %errorlevel% neq 0 (
    echo [ERREUR] Les tests unitaires ont echoue. Verifiez le code avant de lancer la console.
    pause
    exit /b %errorlevel%
)

echo.
echo ======================================================
echo  LANCEMENT DU MODULE CRM (PANNEAU DE CONTROLE)
echo ======================================================
dotnet run --project src\CRM.MessagingConsole\CRM.MessagingConsole.csproj
pause
