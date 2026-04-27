# 研究報告：車輛租用系統技術選型

**功能分支**: `001-vehicle-rental-system`  
**日期**: 2026-04-27  
**研究範疇**: ASP.NET Core 10 MVC + CQRS + 認證 + UI 框架 + 測試策略

---

## 1. CQRS 實作方式

**決策**：使用 **MediatR 12**（via `MediatR` NuGet）作為 CQRS 的命令/查詢分發機制。

**理由**：
- MediatR 是 .NET 生態中最成熟的中介者模式實作，廣泛用於 ASP.NET Core CQRS 架構。
- 控制器只需注入 `IMediator`，呼叫 `Send(command)` 或 `Send(query)`，完全與應用層解耦。
- 不需要手寫 ICommandBus / IQueryBus 介面，減少樣板程式碼，符合 YAGNI 原則。
- 支援 Pipeline Behaviors（可於 MVP 後加入 validation / logging pipeline，不影響現有程式碼）。

**已評估的替代方案**：
- 手寫 `ICommandHandler<T,R>` 介面：結構清晰但需要更多樣板；MediatR 已提供等效功能。
- 直接注入 Application Service 介面：違反 CQRS 分離原則，混合讀寫語意。

**MVP 命令/查詢清單**：

| 類型 | 名稱 | 觸發時機 |
|------|------|----------|
| Command | `RegisterCustomerCommand` | 用戶送出註冊表單 |
| Command | `CreateReservationCommand` | 用戶確認預約送出 |
| Command | `CancelReservationCommand` | 用戶點擊取消預約（P2）|
| Query | `GetVehicleTypesQuery` | 載入車型清單頁 |
| Query | `GetMyReservationsQuery` | 載入我的預約頁（P2）|
| Query | `CalculateRentalCostQuery` | 用戶選擇日期後即時計算（前端 JS fetch 呼叫）|

---

## 2. 使用者認證策略

**決策**：使用 **ASP.NET Core Cookie Authentication**（手動實作），不引入 ASP.NET Core Identity 完整套件。

**理由**：
- ASP.NET Core Identity 包含 Role、Claims、Token、外部登入等大量功能，MVP 不需要。
- 手動 Cookie Auth 僅需：`AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)` + `AddCookie()`，程式碼量極少。
- 密碼雜湊使用 `PasswordHasher<T>`（ASP.NET Core 內建，不需額外套件）或 `BCrypt.Net-Next`，符合安全標準。
- `[Authorize]` attribute 仍然可用，行為與 Identity 完全相同。

**已評估的替代方案**：
- ASP.NET Core Identity：功能過多（Entity scaffolding、Razor Identity UI），增加複雜度，違反 MVP 原則。
- JWT：用於 API 場景，MVC View-based 應用不需要。

---

## 3. 資料存取（ORM）

**決策**：使用 **Entity Framework Core 10**（`Microsoft.EntityFrameworkCore.SqlServer` + `Microsoft.EntityFrameworkCore.Tools`）。

**理由**：
- EF Core 10 與 .NET 10 同步發布，原生支援。
- Code-First + Migrations 可快速建立資料庫結構，方便本地開發與 CI 測試（可使用 SQLite 或 In-Memory provider 進行測試）。
- DbContext 可在整合測試中替換為 `UseInMemoryDatabase`，不需 Mock Repository 層。

**開發環境資料庫**：SQL Server LocalDB（`(localdb)\MSSQLLocalDB`）；測試環境使用 `UseInMemoryDatabase`。

**已評估的替代方案**：
- Dapper：靈活但需手寫 SQL；MVP 階段 EF Core 更快速。
- Repository Pattern + Unit of Work：對此規模過度設計；直接注入 `DbContext` 至 Application Service 即可。

---

## 4. UI 框架與電競風格

**決策**：使用 **Bootstrap 5.1**（CDN 方式引入）+ **自訂 CSS 電競主題**（`wwwroot/css/esports.css`）。

**電競主題設計規範**：
- 主色：`#0d0d0d`（深黑背景）
- 強調色 1：`#00ff99`（螢光綠）
- 強調色 2：`#7b2fff`（電競紫）
- 文字色：`#e0e0e0`（淺灰）
- 字型：`Rajdhani`（Google Fonts）+ 系統 sans-serif fallback
- 按鈕：border-based 霓虹邊框效果 + hover glow (`box-shadow`)
- 卡片：半透明黑底 + 螢光邊框
- 表單輸入框：深色背景 + 螢光焦點邊框

**Bootstrap 用途**：RWD Grid System、表單基本結構、Navbar；不使用 Bootstrap 預設配色。

**已評估的替代方案**：
- Tailwind CSS：需要 CLI build pipeline，增加設定複雜度。
- 純自訂 CSS：放棄 Bootstrap Grid RWD 支援。

---

## 5. 測試策略

**決策**：使用 **xUnit** + **Moq** + **ASP.NET Core `WebApplicationFactory<T>`**。

| 測試類型 | 工具 | 覆蓋範圍 |
|---------|------|---------|
| 單元測試 | xUnit + Moq | 租金計算邏輯、Command Handler 業務規則 |
| 整合測試 | xUnit + WebApplicationFactory + EF InMemory | Controller 行為、認證流程、預約建立/衝突 |

**已評估的替代方案**：
- NUnit：xUnit 在 ASP.NET Core 生態更為主流。
- TestServer 手動設定：`WebApplicationFactory<T>` 已封裝此功能，更簡潔。

---

## 6. 已解析的所有 NEEDS CLARIFICATION

| 原問題 | 決策 |
|--------|------|
| 密碼儲存方式 | `PasswordHasher<T>`（PBKDF2-HMACSHA512）|
| 認證機制 | Cookie Authentication（非 Identity）|
| ORM | Entity Framework Core 10 + SQL Server LocalDB |
| 測試框架 | xUnit + Moq + WebApplicationFactory |
| UI 框架 | Bootstrap 5.1 CDN + 自訂電競主題 CSS |
| CQRS 分發 | MediatR 12 |
| 初始車型資料 | EF Core `HasData` Seed Data |
| RWD | Bootstrap 5.1 Grid（桌面優先）|
