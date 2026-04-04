param(
    [string]$MsvcRoot = "",
    [string]$MiniAudioExSource = "",
    [string]$Configuration = "Release",
    [switch]$SkipMiniAudioEx,
    [switch]$SkipTolk
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RequiredPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PathValue,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if ([string]::IsNullOrWhiteSpace($PathValue)) {
        throw "$Name is required."
    }

    $resolved = Resolve-Path -LiteralPath $PathValue -ErrorAction Stop
    return $resolved.ProviderPath
}

function Invoke-MsvcCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SetupBatch,
        [Parameter(Mandatory = $true)]
        [string]$CommandLine
    )

    $full = "call `"$SetupBatch`" && $CommandLine"
    Write-Host $full
    cmd /c $full
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $CommandLine"
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$msvcRootResolved = Resolve-RequiredPath -PathValue $MsvcRoot -Name "MsvcRoot"
$setupBatch = Join-Path $msvcRootResolved "setup_x64.bat"
if (!(Test-Path -LiteralPath $setupBatch)) {
    throw "MSVC setup batch was not found: $setupBatch"
}

if (!$SkipMiniAudioEx) {
    $miniAudioExSourceResolved = Resolve-RequiredPath -PathValue $MiniAudioExSource -Name "MiniAudioExSource"
    $miniAudioExBuildDir = Join-Path $miniAudioExSourceResolved "build-win64-static"
    $miniAudioExOutput = Join-Path $miniAudioExBuildDir "miniaudioex.dll"
    $miniAudioExTarget = Join-Path $repoRoot "MiniAudioExNET\runtimes\win-x64\native\miniaudioex.dll"

    if (Test-Path -LiteralPath $miniAudioExBuildDir) {
        Remove-Item -LiteralPath $miniAudioExBuildDir -Recurse -Force
    }

    Invoke-MsvcCommand -SetupBatch $setupBatch -CommandLine "cmake -S `"$miniAudioExSourceResolved`" -B `"$miniAudioExBuildDir`" -G Ninja -DCMAKE_BUILD_TYPE=$Configuration -DMINIAUDIOEX_BUILD_SHARED=ON -DMINIAUDIOEX_MSVC_STATIC_RUNTIME=ON -DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded"
    Invoke-MsvcCommand -SetupBatch $setupBatch -CommandLine "cmake --build `"$miniAudioExBuildDir`" --config $Configuration"

    if (!(Test-Path -LiteralPath $miniAudioExOutput)) {
        throw "MiniAudioEx output was not produced: $miniAudioExOutput"
    }

    Copy-Item -LiteralPath $miniAudioExOutput -Destination $miniAudioExTarget -Force
}

if (!$SkipTolk) {
    $tolkSource = Join-Path $repoRoot "tolk"
    $tolkBuildDir = Join-Path $tolkSource "build-win64-static"
    $tolkOutput = Join-Path $tolkBuildDir "Tolk.dll"
    $tolkTarget = Join-Path $repoRoot "CrossSpeak\lib\windows\Tolk.dll"

    if (Test-Path -LiteralPath $tolkBuildDir) {
        Remove-Item -LiteralPath $tolkBuildDir -Recurse -Force
    }

    Invoke-MsvcCommand -SetupBatch $setupBatch -CommandLine "cmake -S `"$tolkSource`" -B `"$tolkBuildDir`" -G Ninja -DCMAKE_BUILD_TYPE=$Configuration -DTOLK_MULTITHREADED=ON -DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded"
    Invoke-MsvcCommand -SetupBatch $setupBatch -CommandLine "cmake --build `"$tolkBuildDir`" --config $Configuration"

    if (!(Test-Path -LiteralPath $tolkOutput)) {
        throw "Tolk output was not produced: $tolkOutput"
    }

    Copy-Item -LiteralPath $tolkOutput -Destination $tolkTarget -Force
}

Write-Host "Windows native dependencies rebuilt and staged."
