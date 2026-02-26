#!/usr/bin/env pwsh
<#
.SYNOPSIS
    建立資料庫 GitHub Release

.DESCRIPTION
    從本地 zipcode.db 建立或更新 database-latest release

.EXAMPLE
    .\Create-DatabaseRelease.ps1
#>

[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

Write-Host "=== 建立資料庫 Release ===" -ForegroundColor Cyan
Write-Host ""

# 檢查 gh CLI
if (!(Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "找不到 gh CLI。請先安裝: https://cli.github.com/"
    exit 1
}

# 檢查資料庫檔案
$dbPath = Join-Path $PSScriptRoot ".." ".." "src" "TaiwanUtilities" "Postal" "zipcode.db"
if (!(Test-Path $dbPath)) {
    Write-Error "找不到資料庫檔案: $dbPath"
    Write-Host ""
    Write-Host "請先執行以下命令建立資料庫：" -ForegroundColor Yellow
    Write-Host "  pwsh tools/postal/Download-PostalDatabase.ps1" -ForegroundColor White
    Write-Host "  pwsh tools/postal/Build-PostalDatabase.ps1" -ForegroundColor White
    exit 1
}

$dbSize = (Get-Item $dbPath).Length / 1MB
Write-Host "✓ 找到資料庫檔案: $([Math]::Round($dbSize, 2)) MB" -ForegroundColor Green
Write-Host ""

# 檢查是否已存在 database-latest release
Write-Host "檢查現有 release..." -ForegroundColor Yellow
$existingRelease = gh release view database-latest --json name 2>$null

if ($existingRelease) {
    Write-Host "✓ database-latest release 已存在，將更新..." -ForegroundColor Yellow

    # 刪除舊的 zipcode.db asset（如果存在）
    Write-Host "刪除舊的 zipcode.db asset..." -ForegroundColor Yellow
    gh release delete-asset database-latest zipcode.db --yes 2>$null

    # 上傳新的 zipcode.db
    Write-Host "上傳新的 zipcode.db..." -ForegroundColor Yellow
    gh release upload database-latest $dbPath --clobber

    Write-Host ""
    Write-Host "✓ Release 更新成功！" -ForegroundColor Green
} else {
    Write-Host "建立新的 database-latest release..." -ForegroundColor Yellow

    # 建立新 release
    gh release create database-latest `
        --title "郵遞區號資料庫 (最新版)" `
        --notes "此 release 包含最新的台灣郵遞區號資料庫。`n`n由建置流程自動更新。" `
        $dbPath

    Write-Host ""
    Write-Host "✓ Release 建立成功！" -ForegroundColor Green
}

Write-Host ""
Write-Host "下載 URL:" -ForegroundColor Cyan
Write-Host "  https://github.com/orlys/TaiwanUtilities/releases/download/database-latest/zipcode.db" -ForegroundColor White
Write-Host ""
Write-Host "或使用 gh CLI:" -ForegroundColor Cyan
Write-Host "  gh release download database-latest --pattern 'zipcode.db' --repo orlys/TaiwanUtilities" -ForegroundColor White
