@echo off
color 0B
title Console Interactive CRM - Ecoute et Envoi RabbitMQ
echo ====================================================================
echo   [SYSTEM] DEMARRAGE DE LA CONSOLE INTERACTIVE CRM
echo   Grace a cette fenetre, tu vas pouvoir envoyer des messages
echo   a EDI, ERP ou au Prof, et voir tout ce qu'on te repond !
echo ====================================================================
echo.
echo [SYSTEM] Recherche de ton projet CRM...

:: Test de presence du projet
if not exist "src\CRM.MessagingConsole\CRM.MessagingConsole.csproj" (
    color 0C
    echo [ERREUR] Projet introuvable dans src\CRM.MessagingConsole\
    pause
    exit
)

echo [SYSTEM] Lancement du service interactif...
echo.
dotnet run --project src\CRM.MessagingConsole\CRM.MessagingConsole.csproj

if %errorlevel% neq 0 (
    color 0C
    echo.
    echo [ERREUR] L'application s'est arretee avec une erreur.
)
pause
