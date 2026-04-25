$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverDir = Join-Path $repoRoot "Server"
$clientDir = Join-Path $repoRoot "ClientApp"
$runtimeDir = Join-Path $repoRoot ".dev-runtime"
$logDir = Join-Path $runtimeDir "logs"
$serverBuildDir = Join-Path $serverDir "bin\Debug\net10.0"
$serverRuntimeDir = Join-Path $runtimeDir "server-run"
$serverPidFile = Join-Path $runtimeDir "server.pid"
$clientPidFile = Join-Path $runtimeDir "client.pid"
$serverExecutable = Join-Path $serverBuildDir "Holonix.Server.exe"
$serverRuntimeExecutable = Join-Path $serverRuntimeDir "Holonix.Server.exe"
$latestLogsFile = Join-Path $runtimeDir "latest-logs.txt"

$serverHttpUrl = "http://localhost:5237"
$serverSwaggerUrl = "$serverHttpUrl/swagger"
$serverHttpsUrl = "https://localhost:7241"
$clientUrl = "http://localhost:4200"

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$serverOut = Join-Path $logDir "server-$timestamp.log"
$serverErr = Join-Path $logDir "server-$timestamp.err.log"
$clientOut = Join-Path $logDir "client-$timestamp.log"
$clientErr = Join-Path $logDir "client-$timestamp.err.log"

New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null
New-Item -ItemType Directory -Path $logDir -Force | Out-Null

function Remove-LogFiles {
    if (Test-Path $latestLogsFile) {
        Remove-Item $latestLogsFile -Force -ErrorAction SilentlyContinue
    }
}

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
        Write-Host "Stopping existing $Name process $pidValue"
        Stop-Process -Id $pidValue -Force
        Start-Sleep -Milliseconds 500
    }

    Remove-Item $PidPath -Force -ErrorAction SilentlyContinue
}

function Stop-PortListeners {
    param(
        [int[]]$Ports,
        [int[]]$ExcludePids = @()
    )

    $processIds = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
        Where-Object { $_.LocalPort -in $Ports } |
        Select-Object -ExpandProperty OwningProcess -Unique

    foreach ($processId in $processIds) {
        if ($processId -in $ExcludePids) {
            continue
        }

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

function Start-BackgroundShell {
    param(
        [string]$WorkingDirectory,
        [string]$Command,
        [string]$StdOutPath,
        [string]$StdErrPath
    )

    return Start-Process powershell `
        -ArgumentList "-NoProfile", "-Command", $Command `
        -WorkingDirectory $WorkingDirectory `
        -RedirectStandardOutput $StdOutPath `
        -RedirectStandardError $StdErrPath `
        -PassThru
}

function Assert-AspNetCoreRuntimeInstalled {
    param(
        [string]$RequiredMajorMinor = "8.0"
    )

    $runtimes = @()
    try {
        $runtimes = & dotnet --list-runtimes 2>$null
    }
    catch {
        throw "dotnet is required but could not be executed. Ensure the .NET SDK is installed and available on PATH."
    }

    $requiredMajorMinorEscaped = [Regex]::Escape($RequiredMajorMinor)
    $hasAspNet = $runtimes | Where-Object { $_ -match "^Microsoft\.AspNetCore\.App\s+$requiredMajorMinorEscaped\." } | Select-Object -First 1
    if ($null -ne $hasAspNet) {
        return
    }

    Write-Host ""
    Write-Host "Missing required ASP.NET Core shared framework Microsoft.AspNetCore.App $RequiredMajorMinor.x."
    Write-Host "The backend targets net$RequiredMajorMinor and cannot start without it."
    Write-Host ""
    Write-Host "Install one of the following, then re-run .\\start-dev.ps1:"
    Write-Host "  - .NET SDK $RequiredMajorMinor (includes the ASP.NET Core runtime)"
    Write-Host "  - ASP.NET Core Runtime $RequiredMajorMinor"
    Write-Host ""

    throw "Microsoft.AspNetCore.App $RequiredMajorMinor.x is not installed."
}

function Wait-ForUrl {
    param(
        [string]$Url,
        [string]$Name,
        [int]$TimeoutSeconds = 120,
        [System.Diagnostics.Process]$Process = $null
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        if ($null -ne $Process -and $Process.HasExited) {
            $exitCode = $null
            try { $exitCode = $Process.ExitCode } catch { }
            if ($null -ne $exitCode) {
                throw "$Name process exited (exit code $exitCode) before becoming ready at $Url."
            }

            throw "$Name process exited before becoming ready at $Url."
        }

        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                Write-Host "$Name is ready at $Url"
                return
            }
        }
        catch {
        }

        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)

    throw "$Name did not become ready at $Url within $TimeoutSeconds seconds."
}

function Show-RecentLogs {
    param(
        [string]$Title,
        [string]$Path
    )

    if (-not (Test-Path $Path)) {
        return
    }

    Write-Host ""
    Write-Host $Title
    Get-Content $Path -Tail 40
}

function Stage-ServerBuildOutput {
    param(
        [string]$SourceDirectory,
        [string]$DestinationDirectory
    )

    if (-not (Test-Path $SourceDirectory)) {
        throw "Backend build output directory was not found at $SourceDirectory"
    }

    if (Test-Path $DestinationDirectory) {
        Remove-Item $DestinationDirectory -Recurse -Force
    }

    New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null

    Get-ChildItem -Path $SourceDirectory -File | ForEach-Object {
        Copy-Item -Path $_.FullName -Destination (Join-Path $DestinationDirectory $_.Name) -Force
    }

    foreach ($directoryName in @("wwwroot", "runtimes")) {
        $sourcePath = Join-Path $SourceDirectory $directoryName
        if (Test-Path $sourcePath) {
            Copy-Item -Path $sourcePath -Destination (Join-Path $DestinationDirectory $directoryName) -Recurse -Force
        }
    }
}

function Should-BuildBackend {
    param(
        [string]$ProjectDirectory,
        [string]$ExecutablePath
    )

    if (-not (Test-Path $ExecutablePath)) {
        return $true
    }

    $exeTime = (Get-Item $ExecutablePath).LastWriteTimeUtc
    $newestSource = Get-ChildItem -Path $ProjectDirectory -Recurse -File `
        -Include *.cs, *.csproj, *.json `
        -ErrorAction SilentlyContinue |
        Where-Object {
            $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
        } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $newestSource) {
        return $false
    }

    return $newestSource.LastWriteTimeUtc -gt $exeTime
}

function Get-PostgresConnectionString {
    $connectionFile = Join-Path $runtimeDir "postgres-connection-string.txt"

    $existing = $env:ConnectionStrings__DefaultConnection
    if (-not [string]::IsNullOrWhiteSpace($existing)) {
        $trimmed = $existing.Trim()
        if (-not (Test-Path $connectionFile)) {
            $trimmed | Set-Content -Path $connectionFile
            Write-Host "Saved connection string to $connectionFile"
        }
        return $trimmed
    }

    if (Test-Path $connectionFile) {
        $saved = (Get-Content $connectionFile -ErrorAction SilentlyContinue | Select-Object -First 1).Trim()
        if (-not [string]::IsNullOrWhiteSpace($saved)) {
            return $saved
        }
    }

    Write-Host ""
    Write-Host "Postgres connection is not configured via ConnectionStrings__DefaultConnection."
    Write-Host "Enter your Azure Postgres Flexible Server connection details."
    Write-Host ""

    $defaultHost = "holonix.postgres.database.azure.com"
    $hostValue = Read-Host "Host [$defaultHost]"
    if ([string]::IsNullOrWhiteSpace($hostValue)) { $hostValue = $defaultHost }

    $defaultDb = "postgres"
    $dbValue = Read-Host "Database [$defaultDb]"
    if ([string]::IsNullOrWhiteSpace($dbValue)) { $dbValue = $defaultDb }

    $defaultUser = ""
    $userValue = Read-Host "Username (server admin or app user)"
    if ([string]::IsNullOrWhiteSpace($userValue)) {
        throw "Username is required."
    }

    $passwordSecure = Read-Host "Password" -AsSecureString
    $passwordValue = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($passwordSecure)
    )
    if ([string]::IsNullOrWhiteSpace($passwordValue)) {
        throw "Password is required."
    }

    $connectionString = "Host=$hostValue;Port=5432;Database=$dbValue;Username=$userValue;Password=$passwordValue;Ssl Mode=Require;Include Error Detail=true"
    $connectionString | Set-Content -Path $connectionFile
    Write-Host "Saved connection string to $connectionFile"
    return $connectionString
}

Stop-RecordedProcess -PidPath $serverPidFile -Name "backend"
Stop-RecordedProcess -PidPath $clientPidFile -Name "frontend"
Stop-PortListeners -Ports @(4200, 5237, 7241)
Stop-RepoProcesses -RepoRoot $repoRoot
Remove-LogFiles

Assert-AspNetCoreRuntimeInstalled -RequiredMajorMinor "10.0"

if (-not (Test-Path $serverExecutable)) {
    # Legacy fallback: handled by Should-BuildBackend.
}

if (Should-BuildBackend -ProjectDirectory $serverDir -ExecutablePath $serverExecutable) {
    Write-Host "Building backend (source is newer than executable)"
    dotnet build $serverDir\Holonix.Server.csproj

    if (-not (Test-Path $serverExecutable)) {
        throw "Backend executable was not produced at $serverExecutable"
    }
}

Stage-ServerBuildOutput -SourceDirectory $serverBuildDir -DestinationDirectory $serverRuntimeDir

$postgresConnectionString = Get-PostgresConnectionString
$escapedConnectionString = $postgresConnectionString.Replace("'", "''")

$serverCommand = @"
`$env:ASPNETCORE_ENVIRONMENT = 'Development'
`$env:ASPNETCORE_URLS = '$serverHttpsUrl;$serverHttpUrl'
`$env:ConnectionStrings__DefaultConnection = '$escapedConnectionString'
Set-Location '$serverRuntimeDir'
& '$serverRuntimeExecutable'
"@

$clientCommand = @"
Set-Location '$clientDir'
npm run start
"@

$serverProcess = Start-BackgroundShell `
    -WorkingDirectory $serverDir `
    -Command $serverCommand `
    -StdOutPath $serverOut `
    -StdErrPath $serverErr

$serverProcess.Id | Set-Content $serverPidFile
"server stdout: $serverOut" | Set-Content $latestLogsFile
"server stderr: $serverErr" | Add-Content $latestLogsFile
"client stdout: $clientOut" | Add-Content $latestLogsFile
"client stderr: $clientErr" | Add-Content $latestLogsFile

try {
    Wait-ForUrl -Url $serverSwaggerUrl -Name "Backend" -Process $serverProcess
}
catch {
    if (-not $serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue
    }

    Remove-Item $serverPidFile -Force -ErrorAction SilentlyContinue
    Show-RecentLogs -Title "Recent backend stdout:" -Path $serverOut
    Show-RecentLogs -Title "Recent backend stderr:" -Path $serverErr
    throw
}

$clientProcess = Start-BackgroundShell `
    -WorkingDirectory $clientDir `
    -Command $clientCommand `
    -StdOutPath $clientOut `
    -StdErrPath $clientErr

$clientProcess.Id | Set-Content $clientPidFile

try {
    Wait-ForUrl -Url $clientUrl -Name "Frontend" -Process $clientProcess
}
catch {
    if (-not $clientProcess.HasExited) {
        Stop-Process -Id $clientProcess.Id -Force -ErrorAction SilentlyContinue
    }

    Remove-Item $clientPidFile -Force -ErrorAction SilentlyContinue
    Show-RecentLogs -Title "Recent frontend stdout:" -Path $clientOut
    Show-RecentLogs -Title "Recent frontend stderr:" -Path $clientErr
    throw
}

Write-Host ""
Write-Host "Backend PID: $($serverProcess.Id)"
Write-Host "Frontend PID: $($clientProcess.Id)"
Write-Host "Swagger: $serverSwaggerUrl"
Write-Host "Backend HTTP: $serverHttpUrl"
Write-Host "Backend HTTPS: $serverHttpsUrl"
Write-Host "Frontend: $clientUrl"
Write-Host "Stop command: .\stop-dev.ps1"
