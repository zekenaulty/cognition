param(
    [switch]$Purge
)

$ErrorActionPreference = 'Stop'

function Ensure-DockerInstalled {
    try { docker --version | Out-Null } catch { throw "Docker is not installed or not on PATH." }
}

Push-Location (Resolve-Path "$PSScriptRoot/..")
try {
    Ensure-DockerInstalled
    if ($Purge) {
        Write-Host "Stopping containers and removing volumes..." -ForegroundColor Yellow
        docker compose down -v
    } else {
        docker compose down
    }
} finally {
    Pop-Location
}
