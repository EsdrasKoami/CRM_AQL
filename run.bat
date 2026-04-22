@echo off
color 0A
title Console SAGA CRM - Moteur Automatique
echo ====================================================================
echo   [SYSTEM] DEMARRAGE DU MOTEUR CRM (SAGA ORCHESTRATOR)
echo   Cette console prend en charge les exigences du scenario CR1.
echo   Le CRM va reagir tout seul aux messages (ContratValid, Certificats)
echo   et emettre les factures (810) sans aucune aide manuelle !
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
