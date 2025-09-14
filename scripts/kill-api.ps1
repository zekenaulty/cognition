#requires -Version 5.1
<#
 .SYNOPSIS
  Kills any running Cognition.Api processes to release file locks.

 .DESCRIPTION
  Finds processes whose name or command line references Cognition.Api and terminates them.
  Useful when builds fail due to locked binaries from a previous run.

 .PARAMETER ListOnly
  If specified, only lists matching processes without killing them.

 .EXAMPLE
  ./scripts/kill-api.ps1

 .EXAMPLE
  ./scripts/kill-api.ps1 -ListOnly
#>

param(
  [switch]$ListOnly
)

Write-Host "Scanning for Cognition.Api-related processes..." -ForegroundColor Cyan

try {
  $procs = Get-CimInstance Win32_Process |
    Where-Object {
      $_.Name -match '^Cognition\.Api(\.exe)?$' -or
      ($_.CommandLine -and (
         $_.CommandLine -match 'Cognition\.Api(\.dll)?' -or
         $_.CommandLine -match 'src\\Cognition\.Api'
      ))
    }

  if (-not $procs) {
    Write-Host "No Cognition.Api processes found." -ForegroundColor Yellow
    exit 0
  }

  $procs | Select-Object ProcessId, Name, CommandLine | Format-Table -AutoSize

  if ($ListOnly) {
    Write-Host "ListOnly specified. No processes were terminated." -ForegroundColor Yellow
    exit 0
  }

  foreach ($p in $procs) {
    try {
      Stop-Process -Id $p.ProcessId -Force -ErrorAction Stop
      Write-Host ("Killed PID {0} {1}" -f $p.ProcessId, $p.Name) -ForegroundColor Green
    }
    catch {
      Write-Warning ("Failed to kill PID {0}: {1}" -f $p.ProcessId, $_.Exception.Message)
    }
  }
}
catch {
  Write-Error $_
  exit 1
}

exit 0

