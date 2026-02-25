# Download-PostalDatabase.ps1
# 自動下載中華郵政 3+3 郵遞區號資料庫

[CmdletBinding()]
param(
    [string]$OutputPath = ".\temp\rall1.dbf",
    [string]$TempDir = ".\temp\download",
    [switch]$KeepTemp
)

$ErrorActionPreference = "Stop"

Write-Host "=== 中華郵政郵遞區號資料庫下載工具 ===" -ForegroundColor Cyan
Write-Host ""

# 檢查是否為 Windows
if (-not $IsWindows -and $PSVersionTable.PSVersion.Major -ge 6) {
    Write-Error "此工具僅支援 Windows 系統（安裝檔為 Windows 專用）"
    exit 1
}

# 檢查 7-Zip 是否安裝
$7zipPaths = @(
    "C:\Program Files\7-Zip\7z.exe",
    "C:\Program Files (x86)\7-Zip\7z.exe"
)

$7zip = $7zipPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $7zip) {
    Write-Error "找不到 7-Zip。請先安裝 7-Zip: https://www.7-zip.org/"
    exit 1
}

Write-Host "✓ 找到 7-Zip: $7zip" -ForegroundColor Green

# 建立暫存目錄
if (Test-Path $TempDir) {
    Write-Host "清理暫存目錄..." -ForegroundColor Yellow
    Remove-Item -Path $TempDir -Recurse -Force
}
New-Item -ItemType Directory -Path $TempDir -Force | Out-Null

try {
    # 步驟 1: 下載網頁，找出 .rar 檔案連結
    Write-Host ""
    Write-Host "步驟 1: 取得下載連結..." -ForegroundColor Cyan

    $pageUrl = "https://www.post.gov.tw/post/internet/Download/all_list.jsp?ID=2201"
    Write-Host "從中華郵政網站取得下載頁面..." -ForegroundColor Gray

    $response = Invoke-WebRequest -Uri $pageUrl -UseBasicParsing

    # 解析 .rar 檔案連結（只下載 Zip33U 相關檔案）
    $rarLinks = $response.Links | Where-Object {
        $_.href -match '\.rar$' -and $_.href -match 'Zip33U'
    } | Select-Object -ExpandProperty href -Unique

    if ($rarLinks.Count -eq 0) {
        Write-Error "找不到 .rar 下載連結"
        exit 1
    }

    Write-Host "找到 $($rarLinks.Count) 個 .rar 檔案" -ForegroundColor Green

    # 步驟 2: 下載 .rar 檔案
    Write-Host ""
    Write-Host "步驟 2: 下載 .rar 檔案..." -ForegroundColor Cyan

    $downloadedFiles = @()
    $baseUrl = "https://www.post.gov.tw"

    foreach ($link in $rarLinks) {
        $fullUrl = if ($link -notmatch '^https?://') {
            $baseUrl + $link
        } else {
            $link
        }

        $fileName = [System.IO.Path]::GetFileName($link)
        $outputFile = Join-Path $TempDir $fileName

        Write-Host "  下載: $fileName" -ForegroundColor Gray
        Invoke-WebRequest -Uri $fullUrl -OutFile $outputFile

        $downloadedFiles += $outputFile
        Write-Host "  ✓ 完成" -ForegroundColor Green
    }

    # 步驟 3: 解壓縮 .rar 檔案
    Write-Host ""
    Write-Host "步驟 3: 解壓縮檔案..." -ForegroundColor Cyan

    # 優先使用 part1 檔案（多分割檔案的第一個）
    $rarToExtract = $downloadedFiles | Where-Object { $_ -match 'part1' } | Select-Object -First 1

    if (-not $rarToExtract) {
        $rarToExtract = $downloadedFiles[0]
    }

    Write-Host "  解壓縮: $(Split-Path $rarToExtract -Leaf)" -ForegroundColor Gray

    $extractDir = Join-Path $TempDir "extracted"
    & $7zip x "$rarToExtract" "-o$extractDir" -y | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Error "解壓縮失敗"
        exit 1
    }

    Write-Host "  ✓ 解壓縮完成" -ForegroundColor Green

    # 步驟 4: 尋找安裝檔
    Write-Host ""
    Write-Host "步驟 4: 尋找安裝檔..." -ForegroundColor Cyan

    $installer = Get-ChildItem -Path $extractDir -Filter "*.exe" -Recurse | Select-Object -First 1

    if (-not $installer) {
        Write-Error "找不到安裝檔 (.exe)"
        exit 1
    }

    Write-Host "  找到: $($installer.Name)" -ForegroundColor Green

    # 步驟 5: 執行安裝
    Write-Host ""
    Write-Host "步驟 5: 安裝資料庫..." -ForegroundColor Cyan
    Write-Host "  安裝路徑: C:\Zip33U\" -ForegroundColor Gray

    # 如果已安裝，先清理
    if (Test-Path "C:\Zip33U") {
        Write-Host "  清理舊版本..." -ForegroundColor Yellow
        Remove-Item -Path "C:\Zip33U" -Recurse -Force
    }

    Write-Host "  執行安裝（靜默模式）..." -ForegroundColor Gray
    $installProcess = Start-Process -FilePath $installer.FullName -ArgumentList "-silent" -Wait -PassThru

    if ($installProcess.ExitCode -ne 0) {
        Write-Warning "安裝程式返回代碼: $($installProcess.ExitCode)"
    }

    # 等待檔案系統同步
    Start-Sleep -Seconds 2

    # 步驟 6: 複製 rall1.dbf
    Write-Host ""
    Write-Host "步驟 6: 複製 rall1.dbf..." -ForegroundColor Cyan

    $dbfSource = "C:\Zip33U\DBF\rall1.dbf"

    if (-not (Test-Path $dbfSource)) {
        Write-Error "找不到 rall1.dbf 於 $dbfSource"
        Write-Host "安裝目錄內容:" -ForegroundColor Yellow
        Get-ChildItem "C:\Zip33U" -Recurse | Select-Object FullName | Format-Table
        exit 1
    }

    # 取得檔案資訊
    $dbfInfo = Get-Item $dbfSource
    Write-Host "  檔案大小: $([math]::Round($dbfInfo.Length / 1MB, 2)) MB" -ForegroundColor Gray
    Write-Host "  修改時間: $($dbfInfo.LastWriteTime)" -ForegroundColor Gray

    # 確保輸出目錄存在
    $outputDir = Split-Path $OutputPath -Parent
    if ($outputDir -and -not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    }

    # 複製檔案
    Copy-Item -Path $dbfSource -Destination $OutputPath -Force
    Write-Host "  ✓ 已複製到: $OutputPath" -ForegroundColor Green

    # 完成
    Write-Host ""
    Write-Host "=== 下載完成！ ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "下一步：" -ForegroundColor Cyan
    Write-Host "  1. cd tools\Postal.Builder" -ForegroundColor White
    Write-Host "  2. dotnet run" -ForegroundColor White
    Write-Host "  3. cd ..\..\src\TaiwanUtilities" -ForegroundColor White
    Write-Host "  4. dotnet build" -ForegroundColor White

} finally {
    # 清理暫存檔案
    if (-not $KeepTemp) {
        Write-Host ""
        Write-Host "清理暫存檔案..." -ForegroundColor Yellow

        if (Test-Path $TempDir) {
            Remove-Item -Path $TempDir -Recurse -Force
        }

        # 可選：清理安裝目錄
        # if (Test-Path "C:\Zip33U") {
        #     Remove-Item -Path "C:\Zip33U" -Recurse -Force
        # }
    } else {
        Write-Host ""
        Write-Host "暫存檔案保留於: $TempDir" -ForegroundColor Yellow
    }
}
