@echo off
color 0A
title CRM Mastermind - JIT Service
echo [SYSTEM] Verification de l'environnement...

:: Test de presence du projet
if not exist "src\CRM.MessagingConsole\CRM.MessagingConsole.csproj" (
    color 0C
    echo [ERREUR] Projet introuvable dans src\CRM.MessagingConsole\
    dir /s /b *.csproj
    pause
    exit
)

echo [SYSTEM] Lancement du service sur le reseau departemental...
dotnet run --project src\CRM.MessagingConsole\CRM.MessagingConsole.csproj

if %errorlevel% neq 0 (
    color 0C
    echo [ERREUR] Le service a crash ou la compilation a echoue.
)
pause
