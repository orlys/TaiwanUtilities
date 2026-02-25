# 發布流程說明

## CI/CD 流程

本專案使用 GitHub Actions 自動化建置、測試和發布流程。

### 自動觸發條件

1. **Pull Request** - 執行建置和測試
2. **推送到 master 分支** - 執行建置、測試並發布預覽版本到 NuGet
3. **推送標籤 (v*)** - 執行建置、測試並發布正式版本到 NuGet

## 版本管理

### 預覽版本（Preview）

推送到 master 分支會自動建立預覽版本：
```
版本格式: 1.1.0-preview.YYYYMMDD.{commit-sha}
範例: 1.1.0-preview.20260225.a1b2c3d
```

### 正式版本（Release）

創建標籤並推送到 GitHub 會發布正式版本：

```bash
# 1. 創建標籤
git tag v1.1.0

# 2. 推送標籤到 GitHub
git push origin v1.1.0
```

版本格式遵循 [Semantic Versioning](https://semver.org/)：
- `v1.0.0` - 主版本.次版本.修訂版本
- `v1.1.0` - 新增功能（向後相容）
- `v1.0.1` - 修復錯誤（向後相容）
- `v2.0.0` - 重大變更（可能不相容）

## 發布步驟

### 發布新版本

1. **更新版本號**（可選）
   ```bash
   # 編輯 src/TaiwanUtilities/TaiwanUtilities.csproj
   # 修改 <Version>1.1.0</Version>
   ```

2. **創建並推送標籤**
   ```bash
   git tag v1.1.0
   git push origin v1.1.0
   ```

3. **自動化流程**
   - GitHub Actions 自動執行
   - 建置專案（多平台：Ubuntu、Windows、macOS）
   - 執行所有測試（954 個）
   - 打包 NuGet 套件
   - 推送到 NuGet
   - 創建 GitHub Release

### 必要的 Secrets

在 GitHub Repository Settings > Secrets and variables > Actions 中配置：

- `NUGET_API_KEY` - NuGet 伺服器的 API 金鑰

## 手動發布（緊急情況）

```bash
# 1. 建置
dotnet build src/TaiwanUtilities/TaiwanUtilities.csproj --configuration Release

# 2. 打包
dotnet pack src/TaiwanUtilities/TaiwanUtilities.csproj --configuration Release --output ./artifacts

# 3. 推送到 NuGet
dotnet nuget push ./artifacts/*.nupkg \
  --source https://api.nuget.org/v3/index.json \
  --api-key YOUR_API_KEY
```

## 回滾版本

如果發現問題需要回滾：

1. **NuGet 套件** - 登入 NuGet，取消列出（Unlist）有問題的版本

2. **Git 標籤**
   ```bash
   git tag -d v1.1.0
   git push origin :refs/tags/v1.1.0
   ```
