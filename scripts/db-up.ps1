param(
    [switch]$Recreate
)

$ErrorActionPreference = 'Stop'

function Ensure-DockerInstalled {
    try { docker --version | Out-Null }
    catch { throw "Docker is not installed or not on PATH. Please install Docker Desktop." }
}

function Ensure-DockerRunning {
    try {
        docker info | Out-Null
    } catch {
        throw "Docker daemon is not running. Please start Docker Desktop and retry."
    }
}

function Ensure-EnvFile {
    if (-not (Test-Path ".env") -and (Test-Path ".env.example")) {
        Write-Host "Creating .env from .env.example" -ForegroundColor Yellow
        Copy-Item .env.example .env
    }
}

function Up-Compose {
    $svcName = 'postgres'
    $containerName = 'cognition-postgres'

    if ($Recreate) {
        Write-Host "Recreating container..." -ForegroundColor Yellow
        docker compose rm -fs $svcName | Out-Null
    }

    # If container missing or stopped, bring it up
    $exists = docker ps -a --format '{{.Names}}' | Select-String -SimpleMatch $containerName
    if (-not $exists) {
        Write-Host "Creating Postgres container ($containerName)..." -ForegroundColor Green
        docker compose up -d $svcName
    } else {
        $running = docker ps --format '{{.Names}}' | Select-String -SimpleMatch $containerName
        if (-not $running) {
            Write-Host "Starting Postgres container ($containerName)..." -ForegroundColor Green
            docker start $containerName | Out-Null
        } else {
            Write-Host "Postgres container already running ($containerName)." -ForegroundColor Cyan
        }
    }
}

Push-Location (Resolve-Path "$PSScriptRoot/..")
try {
    Ensure-DockerInstalled
    Ensure-DockerRunning
    Ensure-EnvFile
    Up-Compose

    $containerName = 'cognition-postgres'
    $running = docker ps --format '{{.Names}}' | Select-String -SimpleMatch $containerName
    if ($running) {
        $port = ((Get-Content .env -ErrorAction SilentlyContinue | Select-String 'POSTGRES_PORT' | ForEach-Object {($_ -split '=')[1]}) -join '' -replace '\s','')
        if (-not $port) { $port = '5432' }
        Write-Host "Postgres is running on port $port" -ForegroundColor Green
    } else {
        throw "Postgres container failed to start. Check 'docker compose logs postgres'."
    }
} finally {
    Pop-Location
}
