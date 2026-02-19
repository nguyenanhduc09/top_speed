$ErrorActionPreference = "Stop"

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupDir = Join-Path $env:USERPROFILE "Desktop\VSRegistryBackup_$timestamp"
New-Item -Path $BackupDir -ItemType Directory -Force | Out-Null

$removedKeys = New-Object System.Collections.Generic.List[string]
$removedValues = New-Object System.Collections.Generic.List[string]
$errors = New-Object System.Collections.Generic.List[string]

function Convert-ToRegPath {
    param([string]$Path)
    $p = $Path -replace "^Microsoft\.PowerShell\.Core\\Registry::", ""
    $p = $p -replace "^HKLM:", "HKLM"
    $p = $p -replace "^HKCU:", "HKCU"
    return $p
}

function Get-KeyHash {
    param([string]$Text)
    $sha1 = [System.Security.Cryptography.SHA1]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($Text)
        $hash = $sha1.ComputeHash($bytes)
        return ([BitConverter]::ToString($hash)).Replace("-", "").Substring(0, 12)
    } finally {
        $sha1.Dispose()
    }
}

function Backup-Key {
    param([string]$PsPath)
    try {
        if (-not (Test-Path -LiteralPath $PsPath)) { return }
        $regPath = Convert-ToRegPath $PsPath
        $leaf = Split-Path -Path $regPath -Leaf
        if ([string]::IsNullOrWhiteSpace($leaf)) { $leaf = "root" }
        $safeLeaf = ($leaf -replace '[^A-Za-z0-9_.-]', "_")
        $hash = Get-KeyHash $regPath
        $backupFile = Join-Path $BackupDir "${safeLeaf}_${hash}.reg"
        if (-not (Test-Path -LiteralPath $backupFile)) {
            & reg.exe export $regPath $backupFile /y *> $null
        }
    } catch {
        $errors.Add("Backup failed: $PsPath :: $($_.Exception.Message)")
    }
}

function Remove-KeySafe {
    param([string]$PsPath)
    try {
        if (-not (Test-Path -LiteralPath $PsPath)) { return }
        Backup-Key $PsPath
        Remove-Item -LiteralPath $PsPath -Recurse -Force
        $removedKeys.Add($PsPath)
    } catch {
        $errors.Add("Remove key failed: $PsPath :: $($_.Exception.Message)")
    }
}

function Remove-ValueSafe {
    param(
        [string]$PsPath,
        [string]$Name
    )
    try {
        if (-not (Test-Path -LiteralPath $PsPath)) { return }
        Backup-Key $PsPath
        Remove-ItemProperty -LiteralPath $PsPath -Name $Name -Force
        $removedValues.Add("$PsPath::$Name")
    } catch {
        $errors.Add("Remove value failed: $PsPath::$Name :: $($_.Exception.Message)")
    }
}

function Remove-DefaultValueSafe {
    param([string]$PsPath)
    try {
        if (-not (Test-Path -LiteralPath $PsPath)) { return }
        Backup-Key $PsPath
        $regPath = Convert-ToRegPath $PsPath
        & reg.exe delete $regPath /ve /f *> $null
        if ($LASTEXITCODE -eq 0) {
            $removedValues.Add("$PsPath::(Default)")
        }
    } catch {
        $errors.Add("Remove default failed: $PsPath :: $($_.Exception.Message)")
    }
}

$progIdRegex = "^(VisualStudio(\.|$)|VisualStudio\.Launcher(\.|$))"
$valueTextRegex = "(?i)(devenv\.exe|vslauncher\.exe|visual studio\\|visualstudio\.)"

$classRoots = @(
    "HKCU:\Software\Classes",
    "HKLM:\SOFTWARE\Classes",
    "HKLM:\SOFTWARE\WOW6432Node\Classes"
)

# 1) Remove top-level Visual Studio ProgID/protocol keys.
foreach ($root in $classRoots) {
    if (-not (Test-Path -LiteralPath $root)) { continue }
    Get-ChildItem -LiteralPath $root -ErrorAction SilentlyContinue | ForEach-Object {
        $name = $_.PSChildName
        if ($name -match $progIdRegex -or $name -match "^vsweb\+") {
            Remove-KeySafe $_.PSPath
        }
    }
}

# 2) Clean extension keys that still point to Visual Studio ProgIDs.
foreach ($root in $classRoots) {
    if (-not (Test-Path -LiteralPath $root)) { continue }
    Get-ChildItem -LiteralPath $root -ErrorAction SilentlyContinue | Where-Object {
        $_.PSChildName.StartsWith(".")
    } | ForEach-Object {
        $extKey = $_.PSPath
        try {
            $item = Get-Item -LiteralPath $extKey -ErrorAction SilentlyContinue
            if ($null -ne $item) {
                $defaultValue = [string]$item.GetValue("")
                if ($defaultValue -match $progIdRegex) {
                    Remove-DefaultValueSafe $extKey
                }
            }
        } catch {
            $errors.Add("Read extension key failed: $extKey :: $($_.Exception.Message)")
        }

        $owp = Join-Path $extKey "OpenWithProgids"
        if (Test-Path -LiteralPath $owp) {
            try {
                $props = (Get-ItemProperty -LiteralPath $owp -ErrorAction SilentlyContinue).PSObject.Properties |
                    Where-Object { $_.Name -notmatch "^PS" -and $_.Name -match $progIdRegex }
                foreach ($p in $props) {
                    Remove-ValueSafe $owp $p.Name
                }
                $remaining = (Get-ItemProperty -LiteralPath $owp -ErrorAction SilentlyContinue).PSObject.Properties |
                    Where-Object { $_.Name -notmatch "^PS" }
                if (-not $remaining) {
                    Remove-KeySafe $owp
                }
            } catch {
                $errors.Add("Clean OpenWithProgids failed: $owp :: $($_.Exception.Message)")
            }
        }
    }
}

# 3) Clean Explorer Open With and UserChoice data.
$fileExtRoot = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts"
if (Test-Path -LiteralPath $fileExtRoot) {
    Get-ChildItem -LiteralPath $fileExtRoot -ErrorAction SilentlyContinue | ForEach-Object {
        $extBase = $_.PSPath

        $owl = Join-Path $extBase "OpenWithList"
        if (Test-Path -LiteralPath $owl) {
            try {
                $props = (Get-ItemProperty -LiteralPath $owl -ErrorAction SilentlyContinue).PSObject.Properties |
                    Where-Object { $_.Name -notmatch "^PS" -and ([string]$_.Value -match "(?i)(devenv\.exe|vslauncher\.exe)") }
                if ($props.Count -gt 0) {
                    Remove-KeySafe $owl
                }
            } catch {
                $errors.Add("Clean OpenWithList failed: $owl :: $($_.Exception.Message)")
            }
        }

        $owp = Join-Path $extBase "OpenWithProgids"
        if (Test-Path -LiteralPath $owp) {
            try {
                $props = (Get-ItemProperty -LiteralPath $owp -ErrorAction SilentlyContinue).PSObject.Properties |
                    Where-Object { $_.Name -notmatch "^PS" -and $_.Name -match $progIdRegex }
                foreach ($p in $props) {
                    Remove-ValueSafe $owp $p.Name
                }
                $remaining = (Get-ItemProperty -LiteralPath $owp -ErrorAction SilentlyContinue).PSObject.Properties |
                    Where-Object { $_.Name -notmatch "^PS" }
                if (-not $remaining) {
                    Remove-KeySafe $owp
                }
            } catch {
                $errors.Add("Clean FileExts OpenWithProgids failed: $owp :: $($_.Exception.Message)")
            }
        }

        $userChoice = Join-Path $extBase "UserChoice"
        if (Test-Path -LiteralPath $userChoice) {
            try {
                $uc = Get-ItemProperty -LiteralPath $userChoice -ErrorAction SilentlyContinue
                if ([string]$uc.ProgId -match $progIdRegex) {
                    Remove-KeySafe $userChoice
                }
            } catch {
                $errors.Add("Clean UserChoice failed: $userChoice :: $($_.Exception.Message)")
            }
        }
    }

    $dde = Join-Path $fileExtRoot "DDECache"
    if (Test-Path -LiteralPath $dde) {
        Get-ChildItem -LiteralPath $dde -ErrorAction SilentlyContinue | ForEach-Object {
            if ($_.PSChildName -match "^VisualStudio(\.|$)") {
                Remove-KeySafe $_.PSPath
            }
        }
    }
}

# 4) Clean shell/context menu entries pointing to devenv/vslauncher.
$shellRoots = @(
    "HKCU:\Software\Classes\*\shell",
    "HKCU:\Software\Classes\Directory\shell",
    "HKCU:\Software\Classes\Directory\Background\shell",
    "HKLM:\SOFTWARE\Classes\*\shell",
    "HKLM:\SOFTWARE\Classes\Directory\shell",
    "HKLM:\SOFTWARE\Classes\Directory\Background\shell",
    "HKLM:\SOFTWARE\WOW6432Node\Classes\*\shell",
    "HKLM:\SOFTWARE\WOW6432Node\Classes\Directory\shell",
    "HKLM:\SOFTWARE\WOW6432Node\Classes\Directory\Background\shell"
)

foreach ($root in $shellRoots) {
    if (-not (Test-Path -LiteralPath $root)) { continue }
    Get-ChildItem -LiteralPath $root -ErrorAction SilentlyContinue | ForEach-Object {
        $k = $_.PSPath
        $name = $_.PSChildName
        $hit = $false
        try {
            $item = Get-Item -LiteralPath $k -ErrorAction SilentlyContinue
            $selfDefault = [string]$item.GetValue("")
            if ($name -match "(?i)(visual studio|devenv|vslauncher)" -or $selfDefault -match $valueTextRegex) {
                $hit = $true
            }

            $cmdKey = Join-Path $k "command"
            if (-not $hit -and (Test-Path -LiteralPath $cmdKey)) {
                $cmdItem = Get-Item -LiteralPath $cmdKey -ErrorAction SilentlyContinue
                $cmdDefault = [string]$cmdItem.GetValue("")
                if ($cmdDefault -match $valueTextRegex) {
                    $hit = $true
                }
            }
        } catch {
            $errors.Add("Scan shell key failed: $k :: $($_.Exception.Message)")
        }
        if ($hit) {
            Remove-KeySafe $k
        }
    }
}

# 5) Remove app paths / app registration leftovers.
$explicitKeys = @(
    "HKCU:\Software\Classes\AppID\devenv.exe",
    "HKCU:\Software\Classes\Applications\devenv.exe",
    "HKLM:\SOFTWARE\Classes\AppID\devenv.exe",
    "HKLM:\SOFTWARE\WOW6432Node\Classes\AppID\devenv.exe",
    "HKLM:\SOFTWARE\Classes\Applications\devenv.exe",
    "HKLM:\SOFTWARE\WOW6432Node\Classes\Applications\devenv.exe",
    "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\devenv.exe",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\devenv.exe",
    "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\vslauncher.exe",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\vslauncher.exe",
    "HKCU:\Software\Microsoft\VisualStudio",
    "HKCU:\Software\Microsoft\VSCommon",
    "HKLM:\SOFTWARE\Microsoft\VisualStudio",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\VisualStudio",
    "HKLM:\SOFTWARE\Microsoft\VSCommon",
    "HKLM:\SOFTWARE\WOW6432Node\Microsoft\VSCommon"
)

foreach ($k in $explicitKeys) {
    Remove-KeySafe $k
}

# 6) Clean MuiCache values that still reference Visual Studio executables/installer.
$muiKey = "HKCU:\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\MuiCache"
if (Test-Path -LiteralPath $muiKey) {
    try {
        $props = (Get-ItemProperty -LiteralPath $muiKey -ErrorAction SilentlyContinue).PSObject.Properties |
            Where-Object {
                $_.Name -notmatch "^PS" -and
                ($_.Name -match "(?i)(devenv\.exe|vslauncher\.exe|visualstudiosetup\.exe|\\Microsoft Visual Studio\\)")
            }
        foreach ($p in $props) {
            Remove-ValueSafe $muiKey $p.Name
        }
    } catch {
        $errors.Add("Clean MuiCache failed: $($_.Exception.Message)")
    }
}

# 7) Remove CLSID entries whose backing file under Visual Studio path no longer exists.
$clsidRoots = @(
    "HKLM:\SOFTWARE\Classes\CLSID",
    "HKLM:\SOFTWARE\WOW6432Node\Classes\CLSID"
)
foreach ($cr in $clsidRoots) {
    if (-not (Test-Path -LiteralPath $cr)) { continue }
    Get-ChildItem -LiteralPath $cr -ErrorAction SilentlyContinue | ForEach-Object {
        $inproc = Join-Path $_.PSPath "InprocServer32"
        if (-not (Test-Path -LiteralPath $inproc)) { return }
        try {
            $item = Get-Item -LiteralPath $inproc -ErrorAction SilentlyContinue
            $dll = [string]$item.GetValue("")
            if ($dll -match "(?i)\\Microsoft\\VisualStudio\\|\\ProgramData\\Microsoft\\VisualStudio\\" -and -not (Test-Path -LiteralPath $dll)) {
                Remove-KeySafe $_.PSPath
            }
        } catch {
            $errors.Add("Scan CLSID failed: $($_.PSPath) :: $($_.Exception.Message)")
        }
    }
}

# Summary output for terminal and log file.
$summary = [PSCustomObject]@{
    BackupDir = $BackupDir
    RemovedKeyCount = $removedKeys.Count
    RemovedValueCount = $removedValues.Count
    ErrorCount = $errors.Count
}

$summary | Format-List | Out-String | Write-Host
"---- Removed Keys ----" | Write-Host
$removedKeys | Sort-Object -Unique | ForEach-Object { $_ | Write-Host }
"---- Removed Values ----" | Write-Host
$removedValues | Sort-Object -Unique | ForEach-Object { $_ | Write-Host }
if ($errors.Count -gt 0) {
    "---- Errors ----" | Write-Host
    $errors | ForEach-Object { $_ | Write-Host }
}

$logPath = Join-Path $BackupDir "cleanup_summary.txt"
@(
    ($summary | Format-List | Out-String)
    "---- Removed Keys ----"
    ($removedKeys | Sort-Object -Unique)
    "---- Removed Values ----"
    ($removedValues | Sort-Object -Unique)
    "---- Errors ----"
    ($errors)
) | Out-File -LiteralPath $logPath -Encoding utf8

Write-Host "Summary log: $logPath"
