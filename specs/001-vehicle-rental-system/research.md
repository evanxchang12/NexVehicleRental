# 研究報告：車輛租用系統技術選型（架構更新：Blazor WASM + GitHub Pages）

**功能分支**: `001-vehicle-rental-system`  
**日期更新**: 2026-04-28  
**架構修訂**: ASP.NET Core MVC → Blazor WebAssembly + SQLite-WASM + GitHub Pages  
**研究範疇**: Blazor WASM + SQLite-WASM + 用戶端認證 + GitHub Pages 部署 + 測試策略

---

## 架構轉換動機

原架構（ASP.NET Core MVC）需要伺服器常駐運行，無法部署至 GitHub Pages 等純靜態服務。  
新架構目標：**100% 靜態檔案輸出**，可直接部署至 GitHub Pages（永久免費）。

| 面向 | 原架構 | 新架構 |
|------|--------|--------|
| 執行環境 | 伺服器（Kestrel/IIS）| 使用者瀏覽器（WASM）|
| 資料庫 | SQL Server LocalDB | SQLite-WASM（瀏覽器端）|
| 認證 | Cookie Session（伺服器端）| 用戶端 AuthenticationStateProvider |
| 部署 | 需要主機/雲端 | GitHub Pages（靜態 HTML/JS/WASM）|
| 費用 | 需付費主機 | 完全免費 |

---

---

## 1. 前端框架：Blazor WebAssembly

**決策**：使用 **Blazor WebAssembly (.NET 10)** 取代原有 ASP.NET Core MVC。

**理由**：
- C# 程式碼直接編譯為 WebAssembly，在使用者瀏覽器中執行，不需伺服器。
- 與現有 `VehicleRental.Domain`、`VehicleRental.Application`（MediatR）完全相容——只需換掉最外層的 Host。
- Blazor Router 取代 MVC Route，`[Authorize]` attribute 對 Blazor 元件仍然有效（通過 `AuthorizeView` / `AuthorizeRouteView`）。
- 編譯輸出為純靜態檔案（`index.html` + `*.wasm` + `*.js` + `*.css`），直接上傳 GitHub Pages。

**限制**：
- 首次載入較慢（需下載 .NET WASM 執行環境，約 3–7 MB；啟用 Brotli 壓縮後可降至 ~1.5 MB）。
- 所有邏輯均在瀏覽器執行，原始碼（IL）仍可被工具反編譯——不適合存放商業機密。

**已評估的替代方案**：
- Blazor Server：仍需伺服器，不符合 GitHub Pages 需求。
- Vue/React + C# API：放棄全 C# 堆疊優勢，增加複雜度。
- Next.js 靜態匯出：需改用 JavaScript，不符合用戶偏好。

---

## 2. 資料庫：SQLite-WASM（瀏覽器端）

### 2a. 方案選擇

| 方案 | 套件 | 持久化 | 複雜度 | 選用 |
|------|------|--------|--------|------|
| EF Core + SQLite in-memory + localStorage JSON | `Microsoft.EntityFrameworkCore.Sqlite` | 頁面刷新後清空（除 localStorage 備份外）| 低 | ✅ **採用** |
| EF Core + SQLite OPFS（瀏覽器沙盒檔案）| `SQLitePCLRaw.bundle_e_sqlite3_wasm` | 跨 Session 持久 | 高（需 COOP/COEP headers）| 可選升級 |
| Blazor.IndexedDB / localForage JS 互通 | JS 互通套件 | 跨 Session 持久 | 中 | 備用 |
| 純 localStorage JSON | 無 ORM | Session 內持久 | 最低 | 棄用（無 SQL 語意）|

**決策（MVP 採用）**：

```
EF Core 10 + UseInMemoryDatabase（執行期）
     + localStorage JSON 序列化（用於跨 Session 持久化用戶與預約資料）
```

**理由**：
- `Microsoft.EntityFrameworkCore.InMemory` 套件在 WASM 中完全支援，無需任何 WASM 原生二進位。
- 業務邏輯（CQRS Handlers、`IAppDbContext`）無需修改——僅替換 Provider 設定。
- localStorage 容量限制約 5–10 MB，對 MVP 租車資料（用戶 + 預約 + 車型 seed）已綽綽有餘。
- 保留升級路徑：日後可換為 `SQLitePCLRaw.bundle_e_sqlite3_wasm` + OPFS，只需修改 `Program.cs` 的 DI 設定。

**持久化策略（localStorage）**：

```
App 啟動 → 讀 localStorage("vehiclerental-data")
         → 若存在：JSON 反序列化並重建 EF InMemory 資料集
         → 若不存在：載入 Seed Data（4 款車型）

資料異動（RegisterCustomer / CreateReservation / CancelReservation）
         → 完成後呼叫 PersistenceService.SaveAsync()
         → 序列化 { customers[], vehicleTypes[], reservations[] } → localStorage
```

### 2b. NuGet 套件清單

```xml
<!-- VehicleRental.Infrastructure.Wasm.csproj -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.*" />

<!-- VehicleRental.Wasm.csproj (前端) -->
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.*" />
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Authentication" Version="10.0.*" />
<PackageReference Include="MediatR" Version="12.*" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.*" />
```

---

## 3. 用戶端認證策略

**⚠️ 安全性聲明**：此為「本地沙盒模擬認證」，適用於 Demo / Portfolio 用途。  
所有資料存在使用者自己的瀏覽器中，無跨裝置同步，無伺服器端驗證。  
請勿在真實生產環境中使用此模式儲存敏感個資。

**決策**：自訂 `BrowserAuthenticationStateProvider : AuthenticationStateProvider`

**實作流程**：

```
登入流程：
  1. 用戶輸入 Email + 密碼
  2. 從 EF InMemory 查詢 Customer（依 Email）
  3. 驗證密碼：使用 System.Security.Cryptography.SHA256（.NET WASM 支援）
  4. 驗證成功 → 呼叫 NotifyAuthenticationStateChanged
  5. 將 { customerId, fullName } 存入 sessionStorage（關分頁即失效）
  6. 可選 "記住我"：存入 localStorage（直到主動登出）

[Authorize] 保護：
  - App.razor 使用 AuthorizeRouteView
  - 未登入訪問保護路由 → 自動導向 /login
```

**密碼雜湊**：
```csharp
// 使用 PBKDF2（System.Security.Cryptography.Rfc2898DeriveBytes）
// .NET WASM 完整支援 System.Security.Cryptography
byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
    password: Encoding.UTF8.GetBytes(plainPassword),
    salt: Convert.FromHexString(saltHex),
    iterations: 100_000,
    hashAlgorithm: HashAlgorithmName.SHA256,
    outputLength: 32);
```

**已評估的替代方案**：
- `Microsoft.AspNetCore.Identity`：需要伺服器，不適用 WASM。
- BCrypt.Net-Next：在 WASM 中相容，但 `Rfc2898DeriveBytes` 已是標準庫，不需額外套件。
- JavaScript WebCrypto API：JS 互通增加複雜度，.NET 加密庫已足夠。

---

## 4. CQRS / MediatR

**決策**：**保留 MediatR 12**，無需修改。

**理由**：
- MediatR 是純 .NET 套件，與執行環境無關（伺服器 / WASM 均可）。
- `VehicleRental.Application` 中所有 Command/Query Handler 維持不變。
- 只需在 Blazor WASM 的 `Program.cs` 中重新進行 `builder.Services.AddMediatR(...)` 注冊。

**CQRS 命令/查詢清單（維持不變）**：

| 類型 | 名稱 | 觸發元件 |
|------|------|----------|
| Command | `RegisterCustomerCommand` | `RegisterPage.razor` 送出 |
| Command | `LoginCustomerCommand` | `LoginPage.razor` 送出 |
| Command | `CreateReservationCommand` | `CreateReservationPage.razor` 確認 |
| Command | `CancelReservationCommand` | `MyReservationsPage.razor` 取消（P2）|
| Query | `GetVehicleTypesQuery` | `VehicleTypesPage.razor` 初始化 |
| Query | `GetMyReservationsQuery` | `MyReservationsPage.razor` 初始化（P2）|
| Query | `CalculateRentalCostQuery` | 日期選擇器 `OnChange` 事件（即時計算）|

---

## 5. UI 框架與電競風格（Blazor 版）

**決策**：**Bootstrap 5.3**（CDN）+ **自訂電競主題 CSS**（`wwwroot/css/esports.css`）

電競主題規範（沿用原設計）：
- 主色：`#0d0d0d`（深黑背景）
- 強調色 1：`#00ff99`（螢光綠）
- 強調色 2：`#7b2fff`（電競紫）
- 文字色：`#e0e0e0`（淺灰）
- 字型：Rajdhani（Google Fonts CDN）

Blazor 特有差異：
- MVC `_Layout.cshtml` → `MainLayout.razor`（含 NavMenu）
- MVC PartialView → Blazor 子元件（`@code` block）
- `ModelState` 驗證 → `EditForm` + `DataAnnotationsValidator`
- `TempData` 訊息 → Blazor `IToastService` 或 `IJSRuntime.InvokeVoidAsync("alert", ...)`

---

## 6. GitHub Pages 部署策略

**決策**：使用 **GitHub Actions + `actions/deploy-pages`** 自動部署。

### 關鍵設定

| 設定項目 | 值 | 說明 |
|---------|-----|------|
| `<base href>` | `/NexVehicleRental/` | 對應 GitHub repo 名稱 |
| `404.html` | SPA 重定向腳本 | 解決 Blazor 路由在 GitHub Pages 的 404 問題 |
| `.nojekyll` | 空檔案 | 告知 GitHub Pages 不要套用 Jekyll 處理 |
| `index.html` 腳本 | 路由解碼腳本 | 配合 404.html 還原 Blazor 路由路徑 |

**GitHub Actions Workflow**（`.github/workflows/deploy.yml`）：
```yaml
name: Deploy to GitHub Pages
on:
  push:
    branches: [main]
permissions:
  pages: write
  id-token: write
  contents: read
jobs:
  build-and-deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Publish Blazor WASM
        run: dotnet publish src/VehicleRental.Wasm/VehicleRental.Wasm.csproj \
             -c Release -o publish
      - uses: actions/upload-pages-artifact@v3
        with:
          path: publish/wwwroot
      - id: deployment
        uses: actions/deploy-pages@v4
```

**SPA 路由 404 解決方案**：

GitHub Pages 對所有非根目錄路由回傳 404，需以下解決方案：

1. `wwwroot/404.html`：將路由路徑編碼到 URL query 並重定向至 `index.html`
2. `index.html` 中加入解碼腳本：將 query 路徑還原為正確路由

---

## 7. 測試策略（調整）

**決策**：**xUnit + bUnit（Blazor 元件測試）+ Moq**

| 測試類型 | 工具 | 覆蓋範圍 |
|---------|------|---------|
| 單元測試 | xUnit + Moq | 租金計算邏輯、Command Handler 業務規則、密碼雜湊 |
| 元件測試 | bUnit | 表單驗證行為、路由守衛（[Authorize]）|
| 整合測試 | xUnit + EF InMemory | 預約衝突檢查、CQRS Handler 端對端 |

**移除**：`WebApplicationFactory<T>`（已無 ASP.NET Core Host，改用 bUnit TestContext）

**新增**：`bUnit`（`Bunit` NuGet 套件，Blazor 元件的 xUnit 擴充）

---

## 8. 已解析的所有技術決策

| 問題 | 決策 |
|------|------|
| 部署平台 | GitHub Pages（靜態檔案，永久免費）|
| 前端框架 | Blazor WebAssembly .NET 10 |
| 資料庫 | EF Core InMemory + localStorage JSON 持久化 |
| 認證機制 | 用戶端 `AuthenticationStateProvider`（BrowserAuthenticationStateProvider）|
| 密碼雜湊 | PBKDF2 via `Rfc2898DeriveBytes`（.NET 標準庫）|
| CQRS 分發 | MediatR 12（不變）|
| 初始車型資料 | C# 靜態 Seed Data（啟動時注入 EF InMemory）|
| UI 框架 | Bootstrap 5.3 CDN + 自訂電競主題 CSS |
| 測試框架 | xUnit + Moq + bUnit |
| SPA 路由 | 404.html 重定向腳本（GitHub Pages 標準解法）|


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
