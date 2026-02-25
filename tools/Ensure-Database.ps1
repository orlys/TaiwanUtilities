# Ensure-Database.ps1
# 確保 TaiwanUtilities 專案的資料庫存在，不存在時自動從 GitHub Artifacts 下載

[CmdletBinding()]
param(
    [string]$DatabasePath = "src\TaiwanUtilities\Postal\zipcode.db",
    [string]$Repository = "orlys/TaiwanUtilities",
    [string]$WorkflowName = "update-postal-database.yml"
)

$ErrorActionPreference = "Stop"

# 檢查資料庫是否已存在
if (Test-Path $DatabasePath) {
    $dbInfo = Get-Item $DatabasePath
    $dbSizeMB = [math]::Round($dbInfo.Length / 1MB, 2)
    Write-Host "✓ 資料庫已存在: $DatabasePath ($dbSizeMB MB)" -ForegroundColor Green
    exit 0
}

Write-Host "========================================" -ForegroundColor Yellow
Write-Host "資料庫不存在，開始自動下載..." -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Yellow
Write-Host ""

# 檢查是否安裝 gh CLI
$ghInstalled = $null -ne (Get-Command gh -ErrorAction SilentlyContinue)

if (-not $ghInstalled) {
    Write-Error @"
❌ 找不到 GitHub CLI (gh)

資料庫檔案不存在: $DatabasePath

請選擇以下方式之一取得資料庫：

方法 1: 安裝 GitHub CLI 後重新建置（推薦）
  1. 安裝 GitHub CLI: https://cli.github.com/
  2. 重新執行 dotnet build

方法 2: 手動從 GitHub Artifacts 下載
  1. 前往: https://github.com/$Repository/actions/workflows/$WorkflowName
  2. 點擊最新成功的 workflow run
  3. 下載 postal-database artifact
  4. 解壓縮並將 zipcode.db 放置於 $DatabasePath

方法 3: 執行完整建置流程
  1. 下載原始資料: .\tools\Download-PostalDatabase.ps1
  2. 建立資料庫: .\tools\Build-PostalDatabase.ps1
"@
    exit 1
}

try {
    Write-Host "使用 GitHub CLI 下載最新資料庫..." -ForegroundColor Cyan
    Write-Host ""

    # 列出最近的成功 workflow runs
    Write-Host "查詢最新的資料庫 artifact..." -ForegroundColor Gray

    $runs = gh run list `
        --repo $Repository `
        --workflow $WorkflowName `
        --status success `
        --limit 5 `
        --json databaseId,conclusion,createdAt,displayTitle `
        | ConvertFrom-Json

    if ($runs.Count -eq 0) {
        Write-Error "找不到成功的 workflow run。請手動建立資料庫或等待自動更新執行。"
        exit 1
    }

    # 嘗試從最近的 runs 下載 artifact
    $downloaded = $false
    foreach ($run in $runs) {
        Write-Host "  嘗試 run #$($run.databaseId) ($($run.createdAt))..." -ForegroundColor Gray

        try {
            # 列出該 run 的 artifacts
            $artifacts = gh run view $run.databaseId `
                --repo $Repository `
                --json artifacts `
                | ConvertFrom-Json `
                | Select-Object -ExpandProperty artifacts

            # 找到 postal-database artifact
            $dbArtifact = $artifacts | Where-Object { $_.name -match '^postal-database-' } | Select-Object -First 1

            if ($null -eq $dbArtifact) {
                Write-Host "    跳過（無資料庫 artifact）" -ForegroundColor Gray
                continue
            }

            Write-Host "  ✓ 找到 artifact: $($dbArtifact.name)" -ForegroundColor Green
            Write-Host "    下載中..." -ForegroundColor Gray

            # 下載 artifact 到臨時目錄
            $tempDir = Join-Path $env:TEMP "taiwanutilities-db-download"
            if (Test-Path $tempDir) {
                Remove-Item -Path $tempDir -Recurse -Force
            }
            New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

            gh run download $run.databaseId `
                --repo $Repository `
                --name $dbArtifact.name `
                --dir $tempDir

            # 檢查下載的檔案
            $downloadedDb = Get-ChildItem -Path $tempDir -Filter "zipcode.db" -Recurse | Select-Object -First 1

            if ($null -eq $downloadedDb) {
                Write-Warning "  下載的 artifact 中找不到 zipcode.db"
                continue
            }

            # 確保目標目錄存在
            $targetDir = Split-Path $DatabasePath -Parent
            if ($targetDir -and -not (Test-Path $targetDir)) {
                New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
            }

            # 複製檔案
            Copy-Item -Path $downloadedDb.FullName -Destination $DatabasePath -Force

            # 清理臨時目錄
            Remove-Item -Path $tempDir -Recurse -Force

            $dbInfo = Get-Item $DatabasePath
            $dbSizeMB = [math]::Round($dbInfo.Length / 1MB, 2)

            Write-Host ""
            Write-Host "========================================" -ForegroundColor Green
            Write-Host "✓ 資料庫下載完成！" -ForegroundColor Green
            Write-Host "========================================" -ForegroundColor Green
            Write-Host ""
            Write-Host "位置: $DatabasePath" -ForegroundColor White
            Write-Host "大小: $dbSizeMB MB" -ForegroundColor Gray
            Write-Host "來源: $($dbArtifact.name)" -ForegroundColor Gray
            Write-Host ""

            $downloaded = $true
            break

        } catch {
            Write-Host "    下載失敗: $_" -ForegroundColor Yellow
            continue
        }
    }

    if (-not $downloaded) {
        Write-Host ""
        Write-Host "Artifacts 中找不到資料庫，嘗試從 GitHub Release 下載..." -ForegroundColor Yellow
        Write-Host ""

        try {
            $releaseTag = "database-latest"

            Write-Host "  下載 Release: $releaseTag" -ForegroundColor Gray

            # 下載到臨時目錄
            $tempDir = Join-Path $env:TEMP "taiwanutilities-db-release"
            if (Test-Path $tempDir) {
                Remove-Item -Path $tempDir -Recurse -Force
            }
            New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

            # 使用 gh release download
            gh release download $releaseTag `
                --repo $Repository `
                --pattern "zipcode.db" `
                --dir $tempDir

            $downloadedDb = Get-ChildItem -Path $tempDir -Filter "zipcode.db" | Select-Object -First 1

            if ($null -eq $downloadedDb) {
                throw "下載的 Release 中找不到 zipcode.db"
            }

            # 確保目標目錄存在
            $targetDir = Split-Path $DatabasePath -Parent
            if ($targetDir -and -not (Test-Path $targetDir)) {
                New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
            }

            # 複製檔案
            Copy-Item -Path $downloadedDb.FullName -Destination $DatabasePath -Force

            # 清理臨時目錄
            Remove-Item -Path $tempDir -Recurse -Force

            $dbInfo = Get-Item $DatabasePath
            $dbSizeMB = [math]::Round($dbInfo.Length / 1MB, 2)

            Write-Host ""
            Write-Host "========================================" -ForegroundColor Green
            Write-Host "✓ 資料庫下載完成（從 Release）！" -ForegroundColor Green
            Write-Host "========================================" -ForegroundColor Green
            Write-Host ""
            Write-Host "位置: $DatabasePath" -ForegroundColor White
            Write-Host "大小: $dbSizeMB MB" -ForegroundColor Gray
            Write-Host "來源: GitHub Release ($releaseTag)" -ForegroundColor Gray
            Write-Host ""

            $downloaded = $true

        } catch {
            Write-Host "  Release 下載失敗: $_" -ForegroundColor Red
        }
    }

    if (-not $downloaded) {
        Write-Error @"
❌ 無法從 Artifacts 或 Release 下載資料庫

請手動建置資料庫：
1. 下載原始資料: .\tools\Download-PostalDatabase.ps1
2. 建立資料庫: .\tools\Build-PostalDatabase.ps1

或從 GitHub 手動下載：
https://github.com/$Repository/releases/tag/database-latest
"@
        exit 1
    }

} catch {
    Write-Error @"
❌ 下載資料庫時發生錯誤

錯誤訊息: $_

請嘗試手動下載：
https://github.com/$Repository/actions/workflows/$WorkflowName
"@
    exit 1
}
