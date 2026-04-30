[CmdletBinding()]
param(
    [ValidateSet('app', 'tests', 'unit-tests', 'e2e-tests')]
    [string] $Target = 'app'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSCommandPath
$appProjectPath = Join-Path $repoRoot 'src\LockPaper.Ui\LockPaper.Ui.csproj'
$testsRoot = Join-Path $repoRoot 'tests'
$windowsTargetFramework = 'net10.0-windows10.0.19041.0'
$appProcessName = 'LockPaper.Ui'

function Invoke-DotNetCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    Write-Host "dotnet $($Arguments -join ' ')" -ForegroundColor Cyan
    & dotnet @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code $LASTEXITCODE."
    }
}

function Get-TestProjects {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Mode
    )

    if (-not (Test-Path $testsRoot)) {
        return @()
    }

    $projects = Get-ChildItem -Path $testsRoot -Filter '*.csproj' -Recurse -File

    switch ($Mode) {
        'tests' { return $projects }
        'unit-tests' { return $projects | Where-Object { $_.Name -like '*.UnitTests.csproj' } }
        'e2e-tests' { return $projects | Where-Object { $_.Name -like '*.E2ETests.csproj' } }
        default { return @() }
    }
}

function Invoke-TestTarget {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Mode
    )

    $projects = @(Get-TestProjects -Mode $Mode)

    if ($projects.Count -eq 0) {
        Write-Host "No matching test projects were found for target '$Mode' under .\tests." -ForegroundColor Yellow
        return
    }

    foreach ($project in $projects) {
        Invoke-DotNetCommand -Arguments @('test', $project.FullName, '--nologo')
    }
}

function Get-AppExecutable {
    $outputRoot = Join-Path (Split-Path $appProjectPath -Parent) "bin\Debug\$windowsTargetFramework"

    if (-not (Test-Path $outputRoot)) {
        return $null
    }

    return Get-ChildItem -Path $outputRoot -Filter "$appProcessName.exe" -Recurse -File |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1
}

function Invoke-AppTarget {
    if (-not $IsWindows) {
        throw "The 'app' target is only supported on Windows because it launches the MAUI Windows app locally."
    }

    if (-not (Test-Path $appProjectPath)) {
        throw "App project not found at '$appProjectPath'."
    }

    Invoke-DotNetCommand -Arguments @('build', $appProjectPath, '--framework', $windowsTargetFramework, '--nologo')

    $existingProcess = Get-Process -Name $appProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $existingProcess) {
        Write-Host "$appProcessName is already running (PID $($existingProcess.Id))." -ForegroundColor Yellow
        return
    }

    $appExecutable = Get-AppExecutable
    if ($null -eq $appExecutable) {
        throw "Could not find '$appProcessName.exe' after building the app."
    }

    $process = Start-Process -FilePath $appExecutable.FullName -WorkingDirectory $appExecutable.DirectoryName -PassThru
    Write-Host "Started $appProcessName (PID $($process.Id))." -ForegroundColor Green
}

Push-Location $repoRoot

try {
    switch ($Target) {
        'app' { Invoke-AppTarget }
        'tests' { Invoke-TestTarget -Mode 'tests' }
        'unit-tests' { Invoke-TestTarget -Mode 'unit-tests' }
        'e2e-tests' { Invoke-TestTarget -Mode 'e2e-tests' }
        default { throw "Unsupported target '$Target'." }
    }
}
finally {
    Pop-Location
}
