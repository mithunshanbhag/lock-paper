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

function Resolve-AppPackageManifestFile {
    $appProjectRoot = Split-Path $appProjectPath -Parent
    $appOutputRoot = Join-Path $appProjectRoot "bin\Debug\$windowsTargetFramework"
    $appIntermediateRoot = Join-Path $appProjectRoot "obj\Debug\$windowsTargetFramework"
    $candidateManifestPaths = @(
        (Join-Path $appOutputRoot 'win-x64\AppX\AppxManifest.xml'),
        (Join-Path $appOutputRoot 'win-x64\AppxManifest.xml'),
        (Join-Path $appIntermediateRoot 'win-x64\MsixContent\AppxManifest.xml')
    )

    foreach ($manifestPath in $candidateManifestPaths) {
        if (Test-Path $manifestPath) {
            return Get-Item -Path $manifestPath
        }
    }

    return $null
}

function Get-AppManifestRegistrationInfo {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo] $Manifest
    )

    [xml] $manifestXml = Get-Content -Path $Manifest.FullName
    $identityNode = $manifestXml.SelectSingleNode("/*[local-name()='Package']/*[local-name()='Identity']")
    $applicationNode = $manifestXml.SelectSingleNode("/*[local-name()='Package']/*[local-name()='Applications']/*[local-name()='Application']")

    $packageName = $null
    if ($null -ne $identityNode -and $null -ne $identityNode.Attributes['Name']) {
        $packageName = $identityNode.Attributes['Name'].Value
    }

    $appId = $null
    if ($null -ne $applicationNode -and $null -ne $applicationNode.Attributes['Id']) {
        $appId = $applicationNode.Attributes['Id'].Value
    }

    if ([string]::IsNullOrWhiteSpace($packageName)) {
        throw "Could not read the package identity name from '$($Manifest.FullName)'."
    }

    if ([string]::IsNullOrWhiteSpace($appId)) {
        throw "Could not read the application Id from '$($Manifest.FullName)'."
    }

    return [PSCustomObject] @{
        AppId       = $appId
        PackageName = $packageName
    }
}

function Register-AppPackage {
    param(
        [Parameter(Mandatory = $true)]
        [System.IO.FileInfo] $Manifest
    )

    $manifestInfo = Get-AppManifestRegistrationInfo -Manifest $Manifest
    $packageName = $manifestInfo.PackageName
    $appId = $manifestInfo.AppId

    Write-Host "Registering Windows app package '$packageName'." -ForegroundColor Cyan
    Add-AppxPackage -Register $Manifest.FullName -DisableDevelopmentMode

    return Get-RegisteredAppUserModelId -PackageName $packageName -AppId $appId
}

function Get-RegisteredAppUserModelId {
    param(
        [Parameter(Mandatory = $true)]
        [string] $PackageName,

        [Parameter(Mandatory = $true)]
        [string] $AppId
    )

    $appIdPattern = '^' + [regex]::Escape($PackageName) + '_.+!' + [regex]::Escape($AppId) + '$'

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        $startApp = Get-StartApps |
            Where-Object { $_.AppId -match $appIdPattern } |
            Select-Object -First 1

        if ($null -ne $startApp) {
            return $startApp.AppId
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Could not resolve the registered app user model id for package '$PackageName' and app '$AppId'."
}

function Get-RunningAppProcess {
    return Get-Process -Name $appProcessName -ErrorAction SilentlyContinue | Select-Object -First 1
}

function Start-PackagedApp {
    param(
        [Parameter(Mandatory = $true)]
        [string] $AppUserModelId
    )

    Start-Process -FilePath "shell:AppsFolder\$AppUserModelId"

    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        Start-Sleep -Milliseconds 500

        $process = Get-RunningAppProcess
        if ($null -ne $process) {
            Write-Host "Started $appProcessName (PID $($process.Id))." -ForegroundColor Green
            return
        }
    }

    throw "Launched '$AppUserModelId', but no '$appProcessName' process stayed running."
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

    $appPackageManifest = Resolve-AppPackageManifestFile
    if ($null -eq $appPackageManifest) {
        throw "Could not find the Windows app package manifest after building the app."
    }

    $appUserModelId = Register-AppPackage -Manifest $appPackageManifest
    Start-PackagedApp -AppUserModelId $appUserModelId
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
