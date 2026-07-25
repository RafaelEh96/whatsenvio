[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot 'deploy/compose/compose.yml'
$environmentFile = Join-Path $repositoryRoot 'deploy/compose/.env.example'
$expectedSdk = (Get-Content -Raw (Join-Path $repositoryRoot 'global.json') | ConvertFrom-Json).sdk.version
$expectedPostgresImage = 'postgres:18.4-alpine3.23'

function Write-Check {
    param(
        [Parameter(Mandatory)]
        [string] $Message
    )

    Write-Host "[OK] $Message" -ForegroundColor Green
}

Push-Location $repositoryRoot

try {
    $sdkVersion = (dotnet --version).Trim()
    if ($sdkVersion -ne $expectedSdk) {
        throw "Expected .NET SDK $expectedSdk, but dotnet selected $sdkVersion."
    }

    Write-Check ".NET SDK $sdkVersion"

    $dockerServerVersion = (docker version --format '{{.Server.Version}}').Trim()
    if ([string]::IsNullOrWhiteSpace($dockerServerVersion)) {
        throw 'Docker daemon did not return a server version.'
    }

    Write-Check "Docker daemon $dockerServerVersion"

    $composeVersion = (docker compose version --short).Trim()
    $normalizedComposeVersion = $composeVersion.TrimStart('v').Split('-')[0]
    if ([version]$normalizedComposeVersion -lt [version]'2.39.0') {
        throw "Docker Compose 2.39.0 or newer is required, but $composeVersion is installed."
    }

    Write-Check "Docker Compose $composeVersion"

    docker compose --env-file $environmentFile -f $composeFile config --quiet
    if ($LASTEXITCODE -ne 0) {
        throw 'Docker Compose configuration is invalid.'
    }

   $resolvedImages = @(docker compose --env-file $environmentFile -f $composeFile config --images)
if ($resolvedImages -notcontains $expectedPostgresImage) {
    throw "Expected image $expectedPostgresImage, but Compose resolved $($resolvedImages -join ', ')."
}

    Write-Check "Compose resolves $resolvedImage"

    $containerId = (docker compose --env-file $environmentFile -f $composeFile ps --quiet postgres).Trim()
    if ([string]::IsNullOrWhiteSpace($containerId)) {
        throw 'PostgreSQL container is not running. Start it with the command in deploy/compose/README.md.'
    }

    $health = (docker inspect --format '{{.State.Health.Status}}' $containerId).Trim()
    if ($health -ne 'healthy') {
        throw "PostgreSQL container health is $health."
    }

    Write-Check 'PostgreSQL container is healthy'
    Write-Host 'Development environment is ready for Milestone 02.' -ForegroundColor Cyan
}
finally {
    Pop-Location
}
