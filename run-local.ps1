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

function Get-AppPackageManifest {
    $appOutputRoot = Join-Path (Split-Path $appProjectPath -Parent) "bin\Debug\$windowsTargetFramework"
    $manifestPath = Join-Path $appOutputRoot 'win-x64\AppX\AppxManifest.xml'

    if (-not (Test-Path $manifestPath)) {
        return $null
    }

    return Get-Item -Path $manifestPath
}

function Register-AppPackage {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo] $Manifest
    )

    [xml] $manifestXml = Get-Content -Path $Manifest.FullName
    $packageName = $manifestXml.Package.Identity.Name
    $appId = $manifestXml.Package.Applications.Application.Id

    Write-Host "Registering Windows app package '$packageName'." -ForegroundColor Cyan
    Add-AppxPackage -Register $Manifest.FullName -DisableDevelopmentMode

    $package = Get-AppxPackage -Name $packageName |
        Where-Object { $_.InstallLocation -ieq $Manifest.DirectoryName } |
        Select-Object -First 1

    if ($null -eq $package) {
        throw "Could not find registered app package '$packageName' at '$($Manifest.DirectoryName)'."
    }

    return [PSCustomObject] @{
        AppId             = $appId
        PackageFamilyName = $package.PackageFamilyName
    }
}

function Get-RunningAppProcess {
    return Get-Process -Name $appProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
}

function Start-PackagedApp {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PackageFamilyName,

        [Parameter(Mandatory = $true)]
        [string] $AppId
    )

    $appUserModelId = "$PackageFamilyName!$AppId"
    Start-Process -FilePath "shell:AppsFolder\$appUserModelId"

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        Start-Sleep -Milliseconds 500

        $process = Get-RunningAppProcess
        if ($null -ne $process) {
            Write-Host "Started $appProcessName (PID $($process.Id))." -ForegroundColor Green
            return
        }
    }

    throw "Launched '$appUserModelId', but no '$appProcessName' process stayed running."
}

function Invoke-AppTarget {
    if (-not $IsWindows) {
        throw "The 'app' target is only supported on Windows because it launches the MAUI Windows app locally."
    }

    if (-not (Test-Path $appProjectPath)) {
        throw "App project not found at '$appProjectPath'."
    }

    $existingProcess = Get-RunningAppProcess
    if ($null -ne $existingProcess) {
        Write-Host "$appProcessName is already running (PID $($existingProcess.Id))." -ForegroundColor Yellow
        return
    }

    Invoke-DotNetCommand -Arguments @('build', $appProjectPath, '--framework', $windowsTargetFramework, '--nologo')

    $appPackageManifest = Get-AppPackageManifest
    if ($null -eq $appPackageManifest) {
        throw "Could not find the Windows app package manifest after building the app."
    }

    $appPackage = Register-AppPackage -Manifest $appPackageManifest
    Start-PackagedApp -PackageFamilyName $appPackage.PackageFamilyName -AppId $appPackage.AppId
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
