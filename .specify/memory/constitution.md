<!--
Sync Impact Report

Version change: none -> 0.1.0
Modified principles:
- [PRINCIPLE_1_NAME] -> 一、MVP 優先 (Minimal Viable Product)
- [PRINCIPLE_2_NAME] -> 二、可測試設計 (Testable)
- [PRINCIPLE_3_NAME] -> 三、測試優先 (Test-First)
- [PRINCIPLE_4_NAME] -> 四、簡潔可維護 (Simplicity & Maintainability)
- [PRINCIPLE_5_NAME] -> 五、版本與治理 (Versioning & Governance)
Added sections:
- 無新增主要章節（僅填入範本位置內容）
Removed sections:
- 無
Templates requiring review: ✅ .specify/templates/constitution-template.md (updated)
						   ⚠ .specify/templates/plan-template.md (pending review)
						   ⚠ .specify/templates/spec-template.md (pending review)
						   ⚠ .specify/templates/tasks-template.md (pending review)
Follow-up TODOs:
- 檢查並同義化上游 templates 是否需同步調整（plan/spec/tasks）
- 若需要，為 `RATIFICATION_DATE` 註記正式採納日期
-->

# VehicleRental 章程

## 核心原則

### 一、MVP 優先
所有新功能以最小可行產品為目標；先交付可驗證、可運行的最小版本，再逐步擴充。任何超出 MVP 必要性的設計皆視為過度設計（overdesign），需有明確商業或技術理由並經審查通過。

測試準則（可驗證）: 提交範例或使用情境的端到端驗證腳本，能在 CI 環境中成功執行。

### 二、可測試設計
所有程式碼必須設計為可被自動化測試覆蓋：單元、整合或合約測試之一或多種。功能接口應清晰、最小化外部狀態依賴，方便在隔離環境中模擬與斷言。

測試準則（可驗證）: 每個主要功能具備至少一個自動化測試案例，且 CI 在 Pull Request 時必須通過這些測試。

### 三、測試優先（Test-First）
對於核心行為與契約，採用先寫測試再實作的工作流程（TDD 或至少先建立失敗的驗證用例）。測試為設計驅動的驗證工具，而非事後檢查清單。

測試準則（可驗證）: 新增功能的 PR 必須包含對應測試，且 reviewer 應確認測試能覆蓋核心行為。

### 四、簡潔與可維護
系統設計與程式碼應追求簡潔：YAGNI（You Aren't Gonna Need It）、單一職責、明確接口。避免複雜抽象與超前優化；若引入複雜性，必須以文件與測試具體說明理由。

測試準則（可驗證）: 代碼審查需檢查是否存在未被使用或可移除的複雜性；CI 可加入簡單靜態分析檢查以輔助判定。

### 五、版本與治理
採用語義化版本（MAJOR.MINOR.PATCH）。破壞性變更必須提升 MAJOR，並附帶遷移說明與相容性策略。任何治理或流程的重大修改（原則或質量門檻）需經過文件化的提案與審查流程。

測試準則（可驗證）: 版本更新需包含 CHANGELOG 條目與必要的遷移測試或兼容性檢查。

## 約束與合規
技術選擇以最小必要為原則。工程流程需支援自動化測試與 CI。機密與個人資料處理請遵守適用法律與公司安全政策（在專案未包含具體法規時，請依公司標準實作）。

可驗證項目: CI 能在乾淨環境中執行測試套件且回傳綠燈/紅燈結果。

## 開發流程
- Pull Request: 所有變更需以 PR 方式提交，至少一名非作者的 reviewer 批准。
- 測試門檻: PR 必須通過 CI 所有測試與基本靜態檢查（lint、格式化）。
- 合併策略: 以 rebase 或 squash 合併為優先，保留清楚的變更敘述。

可驗證項目: PR 在合併前 CI 綠燈，Reviewer 在 PR 中確認測試與基本設計考量。

## 治理
章程為專案的運作基礎；變更章程需提出 PR 並說明變更理由、測試與遷移計畫。版本號採語義化版本；小修（語意、措辭）使用 patch、原則新增或移除使用 minor/major 視影響而定。

變更流程（可驗證）:
- 提交 PR，標註 `constitution` 標籤。
- 至少一位專案維護者批准。
- 若為破壞性變更，須提供遷移指南與至少一個回歸測試。

**Version**: 0.1.0 | **Ratified**: 2026-04-27 | **Last Amended**: 2026-04-27
