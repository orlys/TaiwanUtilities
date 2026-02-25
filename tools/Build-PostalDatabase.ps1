# Build-PostalDatabase.ps1
# 從 rall1.dbf 建立 SQLite 資料庫

[CmdletBinding()]
param(
    [string]$DbfPath = "..\temp\rall1.dbf",
    [string]$OutputPath = "..\src\TaiwanUtilities\Postal\zipcode.db",
    [switch]$Validate,
    [switch]$ShowStats
)

$ErrorActionPreference = "Stop"

Write-Host "=== TaiwanUtilities 郵遞區號資料庫建立工具 ===" -ForegroundColor Cyan
Write-Host ""

# 取得腳本所在目錄
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BuilderDir = Join-Path $ScriptDir "Peak.Builder"

# 檢查 Peak.Builder 專案是否存在
if (-not (Test-Path (Join-Path $BuilderDir "Peak.Builder.csproj"))) {
    Write-Error "找不到 Peak.Builder 專案於 $BuilderDir"
    exit 1
}

# 解析相對路徑
if (-not [System.IO.Path]::IsPathRooted($DbfPath)) {
    $DbfPath = Join-Path $ScriptDir $DbfPath
}

if (-not [System.IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath = Join-Path $ScriptDir $OutputPath
}

# 檢查輸入檔案
if (-not (Test-Path $DbfPath)) {
    Write-Error "找不到 DBF 檔案: $DbfPath"
    exit 1
}

Write-Host "輸入: $DbfPath" -ForegroundColor Gray
Write-Host "輸出: $OutputPath" -ForegroundColor Gray
Write-Host ""

# 建立命令參數
$builderArgs = @("run", "--project", $BuilderDir, "--configuration", "Release", "--")

if ($Validate) {
    Write-Host "執行驗證模式..." -ForegroundColor Cyan
    $builderArgs += "validate"
    $builderArgs += $DbfPath
} elseif ($ShowStats) {
    Write-Host "顯示資料庫統計..." -ForegroundColor Cyan
    $builderArgs += "stats"
    $builderArgs += $OutputPath
} else {
    Write-Host "建立資料庫..." -ForegroundColor Cyan
    $builderArgs += "build"
    $builderArgs += $DbfPath
    $builderArgs += $OutputPath
}

# 執行建立
try {
    & dotnet @builderArgs

    if ($LASTEXITCODE -ne 0) {
        Write-Error "建立失敗，退出代碼: $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    if (-not $Validate -and -not $ShowStats) {
        # 檢查輸出檔案
        if (Test-Path $OutputPath) {
            $dbInfo = Get-Item $OutputPath
            $dbSizeMB = [math]::Round($dbInfo.Length / 1MB, 2)

            Write-Host ""
            Write-Host "=== 建立完成！ ===" -ForegroundColor Green
            Write-Host ""
            Write-Host "資料庫位置: $OutputPath" -ForegroundColor White
            Write-Host "檔案大小: $dbSizeMB MB" -ForegroundColor Gray
            Write-Host ""
            Write-Host "下一步：" -ForegroundColor Cyan
            Write-Host "  1. 重新建置 TaiwanUtilities 專案以內嵌新資料庫" -ForegroundColor White
            Write-Host "  2. cd ..\src\TaiwanUtilities" -ForegroundColor Gray
            Write-Host "  3. dotnet build" -ForegroundColor Gray
        } else {
            Write-Error "建立失敗：找不到輸出檔案 $OutputPath"
            exit 1
        }
    }
} catch {
    Write-Error "執行失敗: $_"
    exit 1
}
