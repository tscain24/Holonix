$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$serverDir = Join-Path $repoRoot "Server"
$clientDir = Join-Path $repoRoot "ClientApp"
$runtimeDir = Join-Path $repoRoot ".dev-runtime"
$logDir = Join-Path $runtimeDir "logs"
$clientCertDir = Join-Path $runtimeDir "certs"
$serverPidFile = Join-Path $runtimeDir "server.pid"
$clientPidFile = Join-Path $runtimeDir "client.pid"
$latestLogsFile = Join-Path $runtimeDir "latest-logs.txt"
$clientSslCertPath = Join-Path $clientCertDir "localhost.crt"
$clientSslKeyPath = Join-Path $clientCertDir "localhost.key"

$serverHttpUrl = "http://localhost:5237"
$serverSwaggerUrl = "$serverHttpUrl/swagger"
$serverHttpsUrl = "https://localhost:7241"
$clientUrl = "https://localhost:4200"

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$serverOut = Join-Path $logDir "server-$timestamp.log"
$serverErr = Join-Path $logDir "server-$timestamp.err.log"
$clientOut = Join-Path $logDir "client-$timestamp.log"
$clientErr = Join-Path $logDir "client-$timestamp.err.log"

New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null
New-Item -ItemType Directory -Path $logDir -Force | Out-Null
New-Item -ItemType Directory -Path $clientCertDir -Force | Out-Null

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

    # CIM process inspection can be denied on locked-down Windows environments.
    # The executable path is sufficient to find orphaned backend processes that
    # were launched from this repository, including the former staged runtime.
    Get-Process -Name "Holonix.Server" -ErrorAction SilentlyContinue |
        Where-Object { $_.Id -ne $PID -and $_.Path -and $_.Path.StartsWith($RepoRoot, [StringComparison]::OrdinalIgnoreCase) } |
        ForEach-Object {
            Write-Host "Stopping repo backend process $($_.Id) ($($_.Path))"
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
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
            $response = Invoke-WebRequestWithOptionalTlsBypass -Url $Url -TimeoutSec 5
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

function Invoke-WebRequestWithOptionalTlsBypass {
    param(
        [string]$Url,
        [int]$TimeoutSec = 5
    )

    $uri = [Uri]$Url
    $isLocalHttps = $uri.Scheme -eq "https" -and ($uri.Host -eq "localhost" -or $uri.Host -eq "127.0.0.1")
    if (-not $isLocalHttps) {
        return Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec $TimeoutSec
    }

    $invokeCommand = Get-Command Invoke-WebRequest -ErrorAction SilentlyContinue
    if ($null -ne $invokeCommand -and $invokeCommand.Parameters.ContainsKey("SkipCertificateCheck")) {
        return Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec $TimeoutSec -SkipCertificateCheck
    }

    $previousCallback = [System.Net.ServicePointManager]::ServerCertificateValidationCallback
    try {
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
        return Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec $TimeoutSec
    }
    finally {
        [System.Net.ServicePointManager]::ServerCertificateValidationCallback = $previousCallback
    }
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

function Should-BuildBackend {
    param(
        [string]$ProjectDirectory,
        [string]$AssemblyPath
    )

    if (-not (Test-Path $AssemblyPath)) {
        return $true
    }

    $assemblyTime = (Get-Item $AssemblyPath).LastWriteTimeUtc
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

    return $newestSource.LastWriteTimeUtc -gt $assemblyTime
}

function Get-BackendListenerProcess {
    param([int[]]$Ports)

    $listeners = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue |
        Where-Object { $_.LocalPort -in $Ports }
    $processIds = @($listeners | Select-Object -ExpandProperty OwningProcess -Unique)

    if ($processIds.Count -eq 0) {
        # Some restricted Windows sessions can query the healthy endpoint but
        # cannot enumerate TCP ownership. Fall back to the single repo backend.
        $repoBackendProcesses = @(Get-Process -Name "Holonix.Server" -ErrorAction SilentlyContinue |
            Where-Object { $_.Path -and $_.Path.StartsWith($serverDir, [StringComparison]::OrdinalIgnoreCase) })
        if ($repoBackendProcesses.Count -eq 1) {
            return $repoBackendProcesses[0]
        }
    }

    if ($processIds.Count -ne 1) {
        throw "Expected one backend process to own ports $($Ports -join ', '), but found $($processIds.Count)."
    }

    $ownedPorts = @($listeners |
        Where-Object { $_.OwningProcess -eq $processIds[0] } |
        Select-Object -ExpandProperty LocalPort -Unique)
    foreach ($port in $Ports) {
        if ($port -notin $ownedPorts) {
            throw "Backend process $($processIds[0]) does not own expected port $port."
        }
    }

    return Get-Process -Id $processIds[0] -ErrorAction Stop
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

function Export-ClientHttpsCertificateFiles {
    param(
        [string]$CertificatePath,
        [string]$KeyPath
    )

    New-Item -ItemType Directory -Path (Split-Path -Parent $CertificatePath) -Force | Out-Null

    $certificatePathEscaped = $CertificatePath.Replace("'", "''")
    $keyPathEscaped = $KeyPath.Replace("'", "''")

    $exportScript = @"
`$cert = Get-ChildItem Cert:\CurrentUser\My |
    Where-Object {
        `$_.Subject -eq 'CN=localhost' -and
        `$_.HasPrivateKey -and
        `$_.NotAfter -gt [DateTime]::UtcNow
    } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if (`$null -eq `$cert) {
    throw 'No exportable localhost development certificate was found in Cert:\CurrentUser\My.'
}

`$rsa = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey(`$cert)
if (`$null -eq `$rsa) {
    throw 'The localhost development certificate does not expose an RSA private key.'
}

`$certBytes = `$cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
`$keyBytes = `$rsa.ExportPkcs8PrivateKey()
`$certPem = ([System.Security.Cryptography.PemEncoding]::Write('CERTIFICATE', `$certBytes) -join '')
`$keyPem = ([System.Security.Cryptography.PemEncoding]::Write('PRIVATE KEY', `$keyBytes) -join '')

[System.IO.File]::WriteAllText('$certificatePathEscaped', `$certPem)
[System.IO.File]::WriteAllText('$keyPathEscaped', `$keyPem)
"@

    $null = $exportScript | pwsh -NoProfile -Command -

    if (-not (Test-Path $CertificatePath) -or -not (Test-Path $KeyPath)) {
        throw "Frontend HTTPS certificate files were not created."
    }
}

Stop-RecordedProcess -PidPath $serverPidFile -Name "backend"
Stop-RecordedProcess -PidPath $clientPidFile -Name "frontend"
Stop-PortListeners -Ports @(4200, 5237, 7241)
Stop-RepoProcesses -RepoRoot $repoRoot
if (Test-Path (Join-Path $runtimeDir "server-run")) {
    Remove-Item (Join-Path $runtimeDir "server-run") -Recurse -Force
}
Remove-LogFiles

Assert-AspNetCoreRuntimeInstalled -RequiredMajorMinor "10.0"

$serverAssembly = Join-Path $serverDir "bin\Debug\net10.0\Holonix.Server.dll"

if (Should-BuildBackend -ProjectDirectory $serverDir -AssemblyPath $serverAssembly) {
    Write-Host "Building backend (source is newer than executable)"
    dotnet build $serverDir\Holonix.Server.csproj

    if (-not (Test-Path $serverAssembly)) {
        throw "Backend assembly was not produced at $serverAssembly"
    }
}

Export-ClientHttpsCertificateFiles -CertificatePath $clientSslCertPath -KeyPath $clientSslKeyPath

$postgresConnectionString = Get-PostgresConnectionString
$escapedConnectionString = $postgresConnectionString.Replace("'", "''")

$serverCommand = @"
`$env:ASPNETCORE_ENVIRONMENT = 'Development'
`$env:ConnectionStrings__DefaultConnection = '$escapedConnectionString'
Set-Location '$repoRoot'
dotnet run --project '$serverDir\Holonix.Server.csproj' --no-build --launch-profile https
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

$backendProcess = Get-BackendListenerProcess -Ports @(5237, 7241)
$backendProcess.Id | Set-Content $serverPidFile

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
Write-Host "Backend PID: $($backendProcess.Id)"
Write-Host "Frontend PID: $($clientProcess.Id)"
Write-Host "Swagger: $serverSwaggerUrl"
Write-Host "Backend HTTP: $serverHttpUrl"
Write-Host "Backend HTTPS: $serverHttpsUrl"
Write-Host "Frontend: $clientUrl"
Write-Host "Stop command: .\stop-dev.ps1"
