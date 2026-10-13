[CmdletBinding()]
param(
    [string]$ProjectDir = $PSScriptRoot,
    [string]$OutputDir = "",
    [string]$Runtime = 'win-x64',
    [string]$Configuration = 'Release',
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$msg) {
    Write-Host "`n=== $msg ===" -ForegroundColor Cyan
}

function Assert-SafeOutputDir([string]$path) {
    if (-not $path) { throw 'OutputDir is required, for example: -OutputDir "D:\dist\TGVoiceMessagesEditText"' }
    $full = [System.IO.Path]::GetFullPath($path)
    $protected = @(
        [System.IO.Path]::GetPathRoot($full)
        $env:USERPROFILE
        $env:SystemRoot
        $env:ProgramFiles
        ${env:ProgramFiles(x86)}
        $ProjectDir
    ) | Where-Object { $_ }

    foreach ($candidate in $protected) {
        if ($full.TrimEnd('\') -ieq [System.IO.Path]::GetFullPath($candidate).TrimEnd('\')) {
            throw "Refusing to use -OutputDir '$full': the script wipes that directory before copying. Pick a dedicated folder."
        }
    }
    return $full
}

$CsprojPath = Join-Path $ProjectDir 'TGVoiceMessagesEditText.csproj'
$PublishDir = Join-Path $ProjectDir "bin\$Configuration\net9.0\$Runtime\publish"
$PreservedEntries = @('settings.json', 'data', 'models', 'cuda')

if (-not (Test-Path $CsprojPath)) {
    throw "Project not found: $CsprojPath"
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet SDK not found in PATH'
}

$OutputDir = Assert-SafeOutputDir $OutputDir

Write-Step 'Publishing single-file exe'
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }
& dotnet publish $CsprojPath `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=false `
    -p:IncludeAllContentForSelfExtract=false `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None -p:DebugSymbols=false `
    -p:NoWarn=MSB3246
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

Write-Step "Copying to $OutputDir"
Get-Process -Name 'TGVoiceMessagesEditText' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -like "$OutputDir*" } |
    Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500
if (Test-Path $OutputDir) {
    Get-ChildItem $OutputDir -Force |
        Where-Object { $PreservedEntries -notcontains $_.Name } |
        Remove-Item -Recurse -Force
}
else {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}
Copy-Item (Join-Path $PublishDir '*') $OutputDir -Recurse -Force

Write-Host "`nDone. Artifacts in: $OutputDir" -ForegroundColor Green
Write-Host "Preserved between deployments: $($PreservedEntries -join ', ')" -ForegroundColor Green

if ($Launch) {
    Start-Process -FilePath (Join-Path $OutputDir 'TGVoiceMessagesEditText.exe') -WorkingDirectory $OutputDir
}
