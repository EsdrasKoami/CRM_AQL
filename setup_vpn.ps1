<#
.SYNOPSIS
Script d'automatisation de l'installation de Cisco Secure Client (TechinfoTR)

.DESCRIPTION
Ce script exécute le pipeline d'installation pour le réseau techinfotr.qc.ca.
Il installe le client en mode silencieux, importe le certificat racine de confiance,
et génère un profil XML respectant l'UI et les "credentials".

.NOTES
Exécutez cette console ou ce script en mode Administrateur (requis pour le CertStore et ProgramData).
#>

$ErrorActionPreference = "Stop"

$msiFile = "cisco-secure-client-win-5.1.11.388-core-vpn-predeploy-k9.msi"
$certFile = "TechinfoTR-RootCA.cer"

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "   PIPELINE D'INSTALLATION CISCO SECURE CLIENT (VPN) " -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

# 1. Vérification des assets locaux
if (-not (Test-Path $msiFile)) {
    Write-Host "[ERREUR] Fichier introuvable dans le dossier actuel : $msiFile" -ForegroundColor Red
    Write-Host "Veuillez placer l'installateur dans le même répertoire que ce script." -ForegroundColor Yellow
    exit 1
}
if (-not (Test-Path $certFile)) {
    Write-Host "[ERREUR] Fichier introuvable dans le dossier actuel : $certFile" -ForegroundColor Red
    Write-Host "Veuillez placer le certificat CA dans le même répertoire que ce script." -ForegroundColor Yellow
    exit 1
}

# 2. Déploiement MSI (Silent Mode)
Write-Host "`n[1/4] Installation du package MSI en mode silencieux..." -ForegroundColor Yellow
$msiArgs = "/i `"$msiFile`" /passive /norestart ACCEPT_EULA=1"
try {
    $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $msiArgs -Wait -PassThru
    if ($process.ExitCode -ne 0 -and $process.ExitCode -ne 3010) {
        Write-Host "[ERREUR] L'installation a échoué (Code = $($process.ExitCode))" -ForegroundColor Red
        exit 1
    }
    Write-Host "[SUCCÈS] Installation de Cisco Secure Client terminée." -ForegroundColor Green
}
catch {
    Write-Host "[ERREUR] Échec de l'exécution de msiexec.exe : $_" -ForegroundColor Red
    exit 1
}

# 3. Injection du Certificat
Write-Host "`n[2/4] Injection du certificat CA dans le magasin de l'ordinateur local..." -ForegroundColor Yellow
try {
    # Nécéssite l'élévation des privilèges pour "LocalMachine"
    $cert = Import-Certificate -FilePath $certFile -CertStoreLocation "Cert:\LocalMachine\Root"
    Write-Host "L'importation a réussi. (Empreinte: $($cert.Thumbprint))" -ForegroundColor Green
}
catch {
    Write-Host "[ERREUR] L'importation du certificat a échoué: $_" -ForegroundColor Red
    Write-Host "Assurez-vous d'avoir lancé PowerShell en tant qu'Administrateur." -ForegroundColor Yellow
    exit 1
}

# 4. Paramétrage & Tunneling (Génération Profil XML)
Write-Host "`n[3/4] Paramétrage du Endpoint et optimisation UI (Profil XML)..." -ForegroundColor Yellow

$profileDir = "$env:ProgramData\Cisco\Cisco Secure Client\VPN\Profile"
if (-not (Test-Path $profileDir)) {
    $null = New-Item -ItemType Directory -Path $profileDir -Force
}

# Le profil XML force le groupe, l'adresse, et l'option MinimizeOnConnect
$xmlProfile = @"
<?xml version="1.0" encoding="UTF-8"?>
<AnyConnectProfile xmlns="http://schemas.xmlsoap.org/encoding/">
  <ClientInitialization>
    <UseStartBeforeLogon UserControllable="false">false</UseStartBeforeLogon>
    <StrictCertificateTrust>false</StrictCertificateTrust>
    <MinimizeOnConnect>true</MinimizeOnConnect>
  </ClientInitialization>
  <ServerList>
    <HostEntry>
      <HostName>VPN TechinfoTR</HostName>
      <HostAddress>vpn.cegep3r.info:4443</HostAddress>
      <UserGroup>TECHINFOTR</UserGroup>
    </HostEntry>
  </ServerList>
</AnyConnectProfile>
"@

$profilePath = Join-Path $profileDir "TechinfoTR_Profile.xml"
Set-Content -Path $profilePath -Value $xmlProfile -Encoding UTF8
Write-Host "[SUCCÈS] Profil VPN injecté : vpn.cegep3r.info:4443 (Groupe TECHINFOTR)." -ForegroundColor Green

# 5. État Final Attendu / Démarrage
Write-Host "`n[4/4] Démarrage de l'interface Cisco Secure Client..." -ForegroundColor Yellow
$uiExe = "C:\Program Files (x86)\Cisco\Cisco Secure Client\vpnui.exe"
if (Test-Path $uiExe) {
    Start-Process -FilePath $uiExe
    Write-Host "[TERMINÉ] L'icône Cisco Secure Client devrait apparaitre dans le systray." -ForegroundColor Green
    Write-Host "          Vous n'avez plus qu'à cliquer sur 'Connecter' dans l'interface" -ForegroundColor Cyan
    Write-Host "          et utiliser vos identifiants (matricule) du labo SB0134." -ForegroundColor Cyan
}
else {
    Write-Host "[ERREUR] Impossible de trouver vpnui.exe, mais l'installation est terminée." -ForegroundColor Yellow
}
