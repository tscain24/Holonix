param(
    [string[]]$ChangedPaths,
    [switch]$StagedOnly
)

$ErrorActionPreference = "Stop"

function Normalize-RepoPath {
    param([string]$Path)

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    $normalized = $Path.Trim() -replace '\\', '/'
    if ($normalized.StartsWith('./')) {
        $normalized = $normalized.Substring(2)
    }

    return $normalized.Trim('/')
}

function Get-ChangedPathsFromGit {
    param([switch]$StagedOnly)

    $gitArgs = @('-c', 'safe.directory=C:/Users/tscai/source/repos', 'diff', '--name-only', '--diff-filter=ACMR')
    if ($StagedOnly) {
        $gitArgs += '--cached'
    }

    $output = & git @gitArgs 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Unable to read changed paths from git. Pass -ChangedPaths explicitly if needed."
    }

    $paths = @($output | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { Normalize-RepoPath $_ })

    if (-not $StagedOnly -and $paths.Count -eq 0) {
        $headOutput = & git -c safe.directory=C:/Users/tscai/source/repos diff --name-only --diff-filter=ACMR HEAD 2>$null
        if ($LASTEXITCODE -eq 0) {
            $paths = @($headOutput | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | ForEach-Object { Normalize-RepoPath $_ })
        }
    }

    return $paths
}

function Test-PathMatch {
    param(
        [string]$Path,
        [string[]]$Patterns
    )

    foreach ($pattern in $Patterns) {
        if ($Path -like $pattern) {
            return $true
        }
    }

    return $false
}

$rules = @(
    @{
        Name = 'Repo runtime scripts'
        CodePatterns = @('start-dev.ps1', 'stop-dev.ps1')
        RequiredContexts = @('AGENTS.md')
    },
    @{
        Name = 'Backend startup and runtime wiring'
        CodePatterns = @('Server/Program.cs')
        RequiredContexts = @('Server/AGENTS.md', 'context/API_CONTEXT.md', 'context/AUTH_CONTEXT.md')
    },
    @{
        Name = 'Auth backend'
        CodePatterns = @('Server/Controllers/AuthController.cs', 'Server/Application/Handlers/Auth/*', 'Server/Infrastructure/Services/TokenService.cs')
        RequiredContexts = @('context/API_CONTEXT.md', 'context/AUTH_CONTEXT.md')
    },
    @{
        Name = 'Business backend'
        CodePatterns = @('Server/Controllers/BusinessController.cs', 'Server/Contracts/Business/*')
        RequiredContexts = @('context/API_CONTEXT.md', 'context/BUSINESS_CONTEXT.md', 'context/server/BUSINESS_CONTEXT.md')
    },
    @{
        Name = 'Home backend'
        CodePatterns = @('Server/Controllers/HomeController.cs', 'Server/Contracts/Home/*')
        RequiredContexts = @('context/API_CONTEXT.md')
    },
    @{
        Name = 'Search and map backend'
        CodePatterns = @('Server/Controllers/ServiceSearchController.cs', 'Server/Controllers/MapboxController.cs', 'Server/Controllers/GeocodingController.cs', 'Server/Controllers/DevCategoryEmbeddingsController.cs')
        RequiredContexts = @('context/API_CONTEXT.md', 'context/client/FRONTEND_CONTEXT.md')
    },
    @{
        Name = 'Server data model'
        CodePatterns = @('Server/Infrastructure/Data/*', 'Server/Domain/Entities/*', 'Server/Migrations/*')
        RequiredContexts = @('Server/AGENTS.md', 'context/server/DATA_CONTEXT.md')
    },
    @{
        Name = 'Frontend routing'
        CodePatterns = @('ClientApp/src/app/app-routing.module.ts')
        RequiredContexts = @('ClientApp/AGENTS.md', 'context/client/FRONTEND_CONTEXT.md')
    },
    @{
        Name = 'Frontend auth services'
        CodePatterns = @('ClientApp/src/app/core/services/auth.service.ts', 'ClientApp/src/app/core/services/auth-session.service.ts', 'ClientApp/src/app/core/services/auth.interceptor.ts')
        RequiredContexts = @('context/API_CONTEXT.md', 'context/AUTH_CONTEXT.md', 'context/client/FRONTEND_CONTEXT.md')
    },
    @{
        Name = 'Frontend business service'
        CodePatterns = @('ClientApp/src/app/core/services/business.service.ts')
        RequiredContexts = @('context/API_CONTEXT.md', 'context/BUSINESS_CONTEXT.md', 'context/client/business/BUSINESS_CONTEXT.md')
    },
    @{
        Name = 'Frontend home service'
        CodePatterns = @('ClientApp/src/app/core/services/home.service.ts')
        RequiredContexts = @('context/API_CONTEXT.md', 'context/client/FRONTEND_CONTEXT.md')
    },
    @{
        Name = 'Frontend search and public-profile services'
        CodePatterns = @('ClientApp/src/app/core/services/service-search.service.ts', 'ClientApp/src/app/core/services/public-business.service.ts', 'ClientApp/src/app/core/services/mapbox-config.service.ts', 'ClientApp/src/app/core/services/search-origin.service.ts')
        RequiredContexts = @('context/API_CONTEXT.md', 'context/client/FRONTEND_CONTEXT.md')
    },
    @{
        Name = 'Frontend business features'
        CodePatterns = @('ClientApp/src/app/features/business/*')
        RequiredContexts = @('context/BUSINESS_CONTEXT.md', 'context/client/business/BUSINESS_CONTEXT.md')
    },
    @{
        Name = 'Frontend auth and profile features'
        CodePatterns = @('ClientApp/src/app/features/auth/*', 'ClientApp/src/app/features/profile/*')
        RequiredContexts = @('context/AUTH_CONTEXT.md', 'context/client/FRONTEND_CONTEXT.md')
    },
    @{
        Name = 'Frontend search features'
        CodePatterns = @('ClientApp/src/app/features/search/*')
        RequiredContexts = @('context/client/FRONTEND_CONTEXT.md', 'context/client/business/BUSINESS_CONTEXT.md', 'context/BUSINESS_CONTEXT.md')
    }
)

$normalizedChangedPaths = if ($ChangedPaths -and $ChangedPaths.Count -gt 0) {
    @($ChangedPaths | ForEach-Object { Normalize-RepoPath $_ } | Where-Object { $_ })
} else {
    Get-ChangedPathsFromGit -StagedOnly:$StagedOnly
}

if ($normalizedChangedPaths.Count -eq 0) {
    Write-Host "No changed paths found."
    exit 0
}

$changedContextFiles = @(
    $normalizedChangedPaths |
        Where-Object { $_ -like 'context/*' -or $_ -eq 'AGENTS.md' -or $_ -eq 'Server/AGENTS.md' -or $_ -eq 'ClientApp/AGENTS.md' } |
        Select-Object -Unique
)

$failures = @()

foreach ($rule in $rules) {
    $matchedCodePaths = @(
        $normalizedChangedPaths |
            Where-Object {
                ($_ -notlike 'context/*') -and
                $_ -ne 'AGENTS.md' -and
                $_ -ne 'Server/AGENTS.md' -and
                $_ -ne 'ClientApp/AGENTS.md' -and
                (Test-PathMatch -Path $_ -Patterns $rule.CodePatterns)
            } |
            Select-Object -Unique
    )

    if ($matchedCodePaths.Count -eq 0) {
        continue
    }

    $missingContexts = @(
        $rule.RequiredContexts |
            Where-Object { $_ -notin $changedContextFiles }
    )

    if ($missingContexts.Count -gt 0) {
        $failures += [pscustomobject]@{
            Name = $rule.Name
            CodePaths = $matchedCodePaths
            MissingContexts = $missingContexts
        }
    }
}

if ($failures.Count -eq 0) {
    Write-Host "Context check passed."
    Write-Host "Changed context files:"
    foreach ($path in $changedContextFiles) {
        Write-Host "  - $path"
    }
    exit 0
}

Write-Error @"
Context updates are missing for one or more changed code areas.

$(
    ($failures | ForEach-Object {
        $codePaths = ($_.CodePaths | ForEach-Object { "    - $_" }) -join [Environment]::NewLine
        $missing = ($_.MissingContexts | ForEach-Object { "    - $_" }) -join [Environment]::NewLine
        @"
Rule: $($_.Name)
  Changed code:
$codePaths
  Required context updates:
$missing
"@
    }) -join [Environment]::NewLine
)
"@
