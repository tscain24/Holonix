$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$runtimeDir = Join-Path $repoRoot ".dev-runtime"
$serverPidFile = Join-Path $runtimeDir "server.pid"
$clientPidFile = Join-Path $runtimeDir "client.pid"

function Read-PidFile {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return $null
    }

    $content = (Get-Content $Path -ErrorAction SilentlyContinue | Select-Object -First 1).Trim()
    if ([string]::IsNullOrWhiteSpace($content)) {
        Remove-Item $Path -Force -ErrorAction SilentlyContinue
        return $null
    }

    try {
        return [int]$content
    }
    catch {
        Remove-Item $Path -Force -ErrorAction SilentlyContinue
        return $null
    }
}

function Stop-RecordedProcess {
    param(
        [string]$PidPath,
        [string]$Name
    )

    $pidValue = Read-PidFile -Path $PidPath
    if ($null -eq $pidValue) {
        return
    }

    $process = Get-Process -Id $pidValue -ErrorAction SilentlyContinue
    if ($null -ne $process) {
        Write-Host "Stopping $Name process $pidValue"
        Stop-Process -Id $pidValue -Force
    }

    Remove-Item $PidPath -Force -ErrorAction SilentlyContinue
}

function Stop-PortListeners {
    param([int[]]$Ports)

    $processIds = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
        Where-Object { $_.LocalPort -in $Ports } |
        Select-Object -ExpandProperty OwningProcess -Unique

    foreach ($processId in $processIds) {
        $process = Get-Process -Id $processId -ErrorAction SilentlyContinue
        if ($null -eq $process) {
            continue
        }

        $portsForProcess = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
            Where-Object { $_.OwningProcess -eq $processId } |
            Select-Object -ExpandProperty LocalPort -Unique |
            Sort-Object

        $portText = ($portsForProcess -join ", ")
        Write-Host "Stopping process $($process.Id) listening on port(s) $portText ($($process.ProcessName))"
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
}

function Stop-RepoProcesses {
    param([string]$RepoRoot)

    try {
        $escapedRepoRoot = [Regex]::Escape($RepoRoot)
        $currentPid = $PID
        $processes = Get-CimInstance Win32_Process -ErrorAction Stop |
            Where-Object {
                $_.ProcessId -ne $currentPid -and
                $_.CommandLine -and
                $_.CommandLine -match $escapedRepoRoot -and
                $_.Name -in @("powershell.exe", "dotnet.exe", "node.exe", "Holonix.Server.exe")
            }

        foreach ($processInfo in $processes) {
            $process = Get-Process -Id $processInfo.ProcessId -ErrorAction SilentlyContinue
            if ($null -eq $process) {
                continue
            }

            Write-Host "Stopping repo process $($process.Id) ($($process.ProcessName))"
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
    }
    catch {
    }
}

Stop-RecordedProcess -PidPath $serverPidFile -Name "backend"
Stop-RecordedProcess -PidPath $clientPidFile -Name "frontend"
Stop-PortListeners -Ports @(4200, 5237, 7241)
Stop-RepoProcesses -RepoRoot $repoRoot

Write-Host "Development processes stopped."
