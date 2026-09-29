# ssh-via-proxy.ps1
# SSH vers une cible a travers le proxy HTTP d'entreprise (CONNECT).
#
# Usage:
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\ssh-via-proxy.ps1
#
# Le script compile SshProxyTunnel.exe au besoin (csc .NET Framework),
# puis lance OpenSSH avec ProxyCommand = tunnel CONNECT.

param(
    [string]$Target = "34.251.28.124",   # -Target <ip> pour changer la cible
    [Parameter(ValueFromRemainingArguments = $true)][string[]]$SshCommand
)

$ErrorActionPreference = "Stop"

# --- Proxy d'entreprise ---
$ProxyHost = "proxy-web.cm-cic.fr"
$ProxyPort = 8080
$ProxyUser = "vannesju"
$ProxyPass = "goyo4657"

# --- Cible SSH ---
$TargetUser = "student"
$TargetHost = $Target
$TargetPort = 22
$KeyFile    = Join-Path $PSScriptRoot "lfd459-julien-vannesson.pem"

# --- Compilation du tunnel si besoin ---
$csFile = Join-Path $PSScriptRoot "SshProxyTunnel.cs"
$exe    = Join-Path $PSScriptRoot "SshProxyTunnel.exe"
$needBuild = -not (Test-Path $exe) -or -not (Test-Path $csFile) -or
    ((Get-Item $csFile).LastWriteTime -gt (Get-Item $exe).LastWriteTime)

if ($needBuild) {
    $csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
    Write-Host "Compilation du tunnel CONNECT (SshProxyTunnel.exe)..." -ForegroundColor Yellow
    & $csc /nologo /optimize /target:exe /out:$exe $csFile
    if ($LASTEXITCODE -ne 0) { throw "Compilation csc echouee ($LASTEXITCODE)" }
}

# --- Lancement SSH via le proxy ---
$exeUnix = $exe.Replace("\", "/")   # slashes : compatibles ssh Windows et ssh Git Bash
$sshArgs = @(
    "-i", $KeyFile
    "-o", "StrictHostKeyChecking=accept-new"
    "-o", "ProxyCommand=`"$exeUnix`" $ProxyHost $ProxyPort $ProxyUser $ProxyPass %h %p"
    "${TargetUser}@${TargetHost}"
)
if ($SshCommand) { $sshArgs += $SshCommand }

Write-Host "SSH via proxy ${ProxyHost}:${ProxyPort} -> ${TargetUser}@${TargetHost}:${TargetPort}" -ForegroundColor Cyan
& ssh.exe @sshArgs
exit $LASTEXITCODE
