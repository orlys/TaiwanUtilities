# TaiwanUtilities 文件

完整專案說明請參閱 [根目錄 README.md](../README.md)。

## 快速連結

- **[快速入門指南](QUICKSTART.md)** — 5 分鐘上手
- **[Postal API 文件](API.md)** — 郵遞區號模組完整 API
- **[地址生成器](POSTAL_ADDRESS_GENERATOR_API.md)** — 測試地址生成

## 技術文件

- [內嵌資源說明](EMBEDDED_RESOURCE.md) — 資料庫嵌入技術細節
- [Expression Tree 決策樹](EXPRESSION_TREE_IMPLEMENTATION.md) — PostalRulesEngine 編譯機制
- [授權詳細說明](LICENSING.md) — 雙授權模式（MIT + OGDL-Taiwan-1.0）

## 開發指引

- [工具腳本](../tools/README.md) — PowerShell 工具與 Postal.Builder
- [GitHub Workflows](../.github/workflows/README.md) — 自動化工作流程

## 測試

- **總測試數：1004 個**
- **測試覆蓋：** 地址解析、正規化、驗證、郵遞區號查詢、規則匹配、地址生成、中文數字、中文髒話偵測、民國曆、身分證驗證
- **執行測試：** `dotnet test test/TaiwanUtilities.UnitTests/`
