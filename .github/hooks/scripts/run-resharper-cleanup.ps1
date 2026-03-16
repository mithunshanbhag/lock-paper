[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$workspaceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path

function Invoke-CleanupCode {
    param(
        [Parameter(Mandatory = $true)]
        [string] $SolutionPath,

        [Parameter(Mandatory = $true)]
        [string] $SolutionName
    )

    if (Get-Command jb -ErrorAction SilentlyContinue) {
        Write-Host ("jb cleanupcode {0}" -f $SolutionName)
        & jb cleanupcode $SolutionPath
        return
    }

    Write-Host ("dotnet tool run jb -- cleanupcode {0}" -f $SolutionName)
    & dotnet tool run jb -- cleanupcode $SolutionPath
}

Push-Location $workspaceRoot

try {
    $solutions = @(Get-ChildItem -Path $workspaceRoot -Filter *.slnx -File | Sort-Object -Property FullName)

    if ($solutions.Count -eq 0) {
        Write-Host 'No .slnx files found. Skipping ReSharper cleanup.'
        exit 0
    }

    $hasGlobalJb = $null -ne (Get-Command jb -ErrorAction SilentlyContinue)
    $hasLocalJb = $false

    try {
        & dotnet tool run jb -- help *> $null
        $hasLocalJb = ($LASTEXITCODE -eq 0)
    }
    catch {
        $hasLocalJb = $false
    }

    if (-not $hasGlobalJb -and -not $hasLocalJb) {
        Write-Warning 'Skipping ReSharper cleanup because the jb tool is not available. Run "dotnet tool restore" to install repository-local tools or install JetBrains.ReSharper.GlobalTools globally.'
        exit 0
    }

    Write-Host 'Running ReSharper CleanupCode for solution files in the workspace.'
    Write-Host 'CleanupCode resolves best after a successful build of the target solution.'

    foreach ($solution in $solutions) {
        Invoke-CleanupCode -SolutionPath $solution.FullName -SolutionName $solution.Name

        if ($LASTEXITCODE -ne 0) {
            throw ("CleanupCode failed for {0} with exit code {1}." -f $solution.FullName, $LASTEXITCODE)
        }
    }
}
finally {
    Pop-Location
}
