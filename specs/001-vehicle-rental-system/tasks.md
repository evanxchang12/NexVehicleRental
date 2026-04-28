# Tasks: 車輛租用系統（Blazor WASM + GitHub Pages）

**Input**: Design documents from `specs/001-vehicle-rental-system/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/component-contracts.md, quickstart.md

**Architecture**: Blazor WebAssembly .NET 10 + EF Core InMemory + localStorage 持久化 + GitHub Pages 部署

**Tests**: P1 使用者故事必須包含測試（Constitution Compliance 明確規定）。測試任務需在對應實作任務之前完成且確認失敗後再進行實作。

**Organization**: 任務依使用者故事組織，每個故事可獨立實作、測試、交付。

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: 可平行執行（不同檔案，無未完成的依賴）
- **[Story]**: 對應使用者故事（US1~US4）
- 每個描述需含明確檔案路徑

## Path Conventions

- Domain 實體：`src/VehicleRental.Domain/`
- Application CQRS：`src/VehicleRental.Application/`
- Infrastructure（WASM 版）：`src/VehicleRental.Infrastructure/`
- Blazor WASM 前端：`src/VehicleRental.Wasm/`
- 測試：`tests/VehicleRental.Tests/`

---

## Phase 1：Setup（方案初始化與架構遷移）

**目的**：建立 Blazor WASM 專案、更新 NuGet 套件、設定 CI/CD 工作流程

- [ ] T001 建立 Blazor WASM 專案並加入方案：執行 `dotnet new blazorwasm -n VehicleRental.Wasm -o src/VehicleRental.Wasm` 及 `dotnet sln add`，對應 quickstart.md A1 節
- [ ] T002 更新 `src/VehicleRental.Infrastructure/VehicleRental.Infrastructure.csproj`：移除 `Microsoft.EntityFrameworkCore.SqlServer`，加入 `Microsoft.EntityFrameworkCore.InMemory 10.*`
- [ ] T003 更新 `src/VehicleRental.Wasm/VehicleRental.Wasm.csproj`：加入 MediatR 12.*、Microsoft.AspNetCore.Components.WebAssembly.Authentication 10.*，並設定專案參照至 Application 和 Infrastructure
- [ ] T004 更新 `tests/VehicleRental.Tests/VehicleRental.Tests.csproj`：移除 Microsoft.AspNetCore.Mvc.Testing，加入 `bunit 1.*`，保留 Moq 4.* 和 EF Core InMemory
- [ ] T005 [P] 建立 `.github/workflows/deploy.yml` GitHub Actions 自動部署工作流程（setup-dotnet@v4、dotnet publish、upload-pages-artifact@v3、deploy-pages@v4），內容參照 quickstart.md B4 節

**Checkpoint**：方案可 `dotnet build` 成功

---

## Phase 2：Foundational（基礎設施，所有使用者故事的前置條件）

**目的**：EF Core InMemory 設定、localStorage 持久化服務、認證提供者、Blazor WASM 入口與共用 Layout

**⚠️ CRITICAL**：所有使用者故事實作皆依賴此 Phase 完成

- [ ] T006 修改 `src/VehicleRental.Infrastructure/Data/AppDbContext.cs`：移除 SQL Server 設定與 Migration 相關程式碼，確認 `OnModelCreating` 保留所有 Entity 設定（HasIndex、HasMaxLength 等），**不**加入 HasData seed（改由 DbInitializer 注入）
- [ ] T007 建立 `src/VehicleRental.Infrastructure/Data/DbInitializer.cs`：實作 `IDbInitializer` 介面，於 `InitializeAsync()` 中檢查 VehicleTypes 是否為空，若空則加入 4 筆 Seed Data（Toyota Camry/Mazda CX-5/BMW 3 Series/Toyota Hiace Van）
- [ ] T008 建立 `src/VehicleRental.Application/Interfaces/IPersistenceService.cs`（`Task SaveAsync()` 與 `Task RestoreAsync()`）
- [ ] T009 建立 `src/VehicleRental.Infrastructure/Services/LocalStoragePersistenceService.cs`：注入 `IAppDbContext` 與 `IJSRuntime`，`SaveAsync()` 序列化 Customers/VehicleTypes/Reservations 至 localStorage key `vehiclerental-data`，`RestoreAsync()` 從 localStorage 讀取並 Add/Update EF InMemory 資料集
- [ ] T010 修改 `src/VehicleRental.Infrastructure/DependencyInjection.cs`：新增 `AddInfrastructureWasm()` 擴充方法，註冊 `UseInMemoryDatabase("VehicleRentalDb")`、`IAppDbContext`、`IPersistenceService`（LocalStoragePersistenceService）、`IDbInitializer`（DbInitializer）
- [ ] T011 建立 `src/VehicleRental.Wasm/Auth/BrowserAuthenticationStateProvider.cs`：繼承 `AuthenticationStateProvider`，以 `sessionStorage`（預設）或 `localStorage`（RememberMe）儲存 `{customerId, fullName}` JSON；`LoginAsync()` 以 `Rfc2898DeriveBytes.Pbkdf2` 驗證密碼並呼叫 `NotifyAuthenticationStateChanged`；`LogoutAsync()` 清除 storage
- [ ] T012 建立/修改 `src/VehicleRental.Wasm/Program.cs`：設定 `AddMediatR`（ApplicationAssembly）、`AddInfrastructureWasm()`、`AddAuthorizationCore()`、`BrowserAuthenticationStateProvider` 的 DI 註冊；在 `RunAsync` 前呼叫 `IDbInitializer.InitializeAsync()` 與 `IPersistenceService.RestoreAsync()`
- [ ] T013 建立/修改 `src/VehicleRental.Wasm/App.razor`：設定 `AuthorizeRouteView` + `DefaultLayout`；`<NotAuthorized>` 使用 `RedirectToLogin` 元件
- [ ] T014 [P] 建立 `src/VehicleRental.Wasm/Shared/RedirectToLogin.razor`：在 `OnInitialized` 中以 `NavigationManager.NavigateTo("/login", forceLoad: false)` 重導向
- [ ] T015 [P] 建立 `src/VehicleRental.Wasm/Shared/AlertMessage.razor`：接受 `Type`（success/warning/danger）和 `Message` 參數，呈現對應 Bootstrap alert div，帶電競主題色
- [ ] T016 [P] 建立 `src/VehicleRental.Wasm/Shared/MainLayout.razor`：含 NavMenu 插槽、`@Body` 區域、電競主題背景色套用
- [ ] T017 [P] 建立 `src/VehicleRental.Wasm/Shared/NavMenu.razor`：已登入顯示「首頁、車型清單、我的預約」及用戶姓名與登出按鈕；未登入顯示「登入、註冊」連結；使用 `AuthorizeView` 切換顯示內容
- [ ] T018 [P] 建立/修改 `src/VehicleRental.Wasm/wwwroot/index.html`：在 `<head>` 加入 SPA 路由還原腳本（參照 quickstart.md A5 節），設定 `<base href="/" />`（本地），加入 Bootstrap 5.3 CDN、Google Fonts Rajdhani、esports.css 引用
- [ ] T019 [P] 建立 `src/VehicleRental.Wasm/wwwroot/404.html`：SPA 路由重定向腳本（參照 quickstart.md A5 節）
- [ ] T020 [P] 建立 `src/VehicleRental.Wasm/wwwroot/.nojekyll`（空檔案）及 `src/VehicleRental.Wasm/wwwroot/css/esports.css`（電競主題：深黑 #0d0d0d、螢光綠 #00ff99、電競紫 #7b2fff、Rajdhani 字型、按鈕 neon glow、卡片半透明深框）

**Checkpoint**：Foundation ready — `dotnet run` 可啟動 Blazor WASM，顯示基礎 Layout

---

## Phase 3：使用者故事 1 — 帳戶註冊與登入（優先級：P1）🎯 MVP

**目標**：用戶可自行建立帳戶並以帳號密碼登入；未登入用戶被阻擋於受保護頁面外

**獨立測試**：建立測試帳號並登入，驗證 NavMenu 顯示用戶姓名，且可成功進入 `/vehicles`

### 使用者故事 1 測試（REQUIRED for P1/MVP — 先寫測試確認 FAIL 後再實作）

- [ ] T021 [P] [US1] 建立 `tests/VehicleRental.Tests/Unit/Commands/RegisterCustomerCommandHandlerTests.cs`：`Handle_Should_HashPassword_And_SaveCustomer` 測試（驗證 PasswordHash 不等於明文）；`Handle_Should_Reject_DuplicateEmail` 測試（同 Email 第二次拋例外或回傳 Failure）
- [ ] T022 [P] [US1] 建立 `tests/VehicleRental.Tests/Unit/Commands/LoginCustomerCommandHandlerTests.cs`：`Handle_Should_ReturnSuccess_WhenCredentialsValid` 測試；`Handle_Should_ReturnFailure_WhenWrongPassword` 測試；`Handle_Should_ReturnFailure_WhenEmailNotFound` 測試

### 使用者故事 1 實作

- [ ] T023 [P] [US1] 修改/確認 `src/VehicleRental.Application/Commands/RegisterCustomer/RegisterCustomerCommand.cs` 及 `RegisterCustomerCommandHandler.cs`：使用 `Rfc2898DeriveBytes.Pbkdf2` 取代 `PasswordHasher<T>`（相容 WASM），以 `PBKDF2:v1:{saltHex}:{hashHex}` 格式儲存；重複 Email 回傳 `RegisterResult { bool Success, string? Error }`
- [ ] T024 [P] [US1] 修改/確認 `src/VehicleRental.Application/Commands/LoginCustomer/LoginCustomerCommand.cs` 及 `LoginCustomerCommandHandler.cs`：以 `Rfc2898DeriveBytes.Pbkdf2` 驗證密碼；成功回傳 `LoginResult { bool Success, int? CustomerId, string? FullName }`；完成後呼叫（或通知呼叫端呼叫）`IPersistenceService.SaveAsync()`
- [ ] T025 [US1] 建立 `src/VehicleRental.Wasm/Pages/Register.razor`：`@page "/register"`；EditForm + DataAnnotationsValidator 綁定 `RegisterModel`；送出呼叫 `RegisterCustomerCommand`；成功後 `NavigationManager.NavigateTo("/login?registered=true")`；重複 Email 顯示 AlertMessage type=danger；顯示 ValidationSummary 及個別 ValidationMessage；電競主題表單樣式
- [ ] T026 [US1] 建立 `src/VehicleRental.Wasm/Pages/Login.razor`：`@page "/login"`；EditForm 綁定 `LoginModel`；送出呼叫 `BrowserAuthenticationStateProvider.LoginAsync()`；成功後 NavigateTo "/vehicles"；失敗顯示「帳號或密碼錯誤」AlertMessage；若 URL 帶 `?registered=true` 則顯示「註冊成功，請登入」綠色提示；電競主題樣式

**Checkpoint**：使用者故事 1 可獨立展示：可完成完整註冊 → 登入 → NavMenu 顯示姓名流程

---

## Phase 4：使用者故事 2 — 車型瀏覽與選擇（優先級：P1）

**目標**：已登入用戶可瀏覽車型清單，並點選一款車輛進入預約流程

**獨立測試**：登入後進入 `/vehicles`，顯示 4 款 Seed 車型，點「選擇此車」可跳至 `/reservation/create/{id}`

### 使用者故事 2 測試（REQUIRED for P1）

- [ ] T027 [P] [US2] 建立 `tests/VehicleRental.Tests/Unit/Queries/GetVehicleTypesQueryHandlerTests.cs`：`Handle_Should_ReturnAllVehicleTypes` 測試（注入 EF InMemory，預先加入 4 筆，驗證回傳 4 筆 VehicleTypeDto）；`Handle_Should_ReturnEmpty_WhenNoVehicles` 測試

### 使用者故事 2 實作

- [ ] T028 [P] [US2] 修改/確認 `src/VehicleRental.Application/Queries/GetVehicleTypes/GetVehicleTypesQuery.cs` 及 `GetVehicleTypesQueryHandler.cs`：查詢 AppDbContext.VehicleTypes.ToListAsync()，投影至 `IEnumerable<VehicleTypeDto>`（Id, Name, Description, DailyRate, IsAvailable, ImageUrl）
- [ ] T029 [US2] 建立 `src/VehicleRental.Wasm/Pages/VehicleTypes.razor`：`@page "/vehicles"`；`@attribute [Authorize]`；`OnInitializedAsync` 呼叫 `GetVehicleTypesQuery`；以電競風格卡片 Grid 顯示車型（名稱/描述/每日租金/neon 框）；可用車型顯示「選擇此車」按鈕 → `NavigateTo($"/reservation/create/{v.Id}")`；不可用顯示灰色停用按鈕；無車型時顯示「目前無可用車輛」AlertMessage
- [ ] T030 [US2] 建立 `src/VehicleRental.Wasm/Pages/Home.razor`：`@page "/"`；電競風格 Landing Page（Hero 區塊含系統標題、副標題說明、「立即租車」CTA 按鈕 → NavigateTo "/vehicles"）；未登入 CTA 按鈕 → NavigateTo "/login"；使用 AuthorizeView 切換 CTA 目標

**Checkpoint**：使用者故事 2 可獨立展示：登入後可瀏覽完整車型清單並選擇

---

## Phase 5：使用者故事 3 — 租用時間選擇與租金計算（優先級：P1）

**目標**：選定車型後設定租用日期，系統即時計算租金，確認後建立預約並顯示預約編號

**獨立測試**：選定車型，選 3 天日期後畫面即時顯示 DailyRate × 3，送出後顯示預約確認頁含 `RES-` 開頭編號

### 使用者故事 3 測試（REQUIRED for P1）

- [ ] T031 [P] [US3] 建立 `tests/VehicleRental.Tests/Unit/Queries/CalculateRentalCostQueryHandlerTests.cs`：`Handle_Should_Return_CorrectCost`（1800 × 3 天 = 5400）；`Handle_Should_Return_ZeroDays_WhenSameDayStartEnd`；`Handle_Should_Return_IsValid_False_WhenEndBeforeStart`
- [ ] T032 [P] [US3] 建立 `tests/VehicleRental.Tests/Unit/Commands/CreateReservationCommandHandlerTests.cs`：`Handle_Should_CreateReservation_WithCorrectTotalCost`；`Handle_Should_Reject_WhenTimeConflict`（同車型相同日期區間已有 Confirmed 預約）；`Handle_Should_Reject_WhenEndDateNotAfterStartDate`；`Handle_Should_GenerateReservationNumber_WithRESPrefix`

### 使用者故事 3 實作

- [ ] T033 [P] [US3] 修改/確認 `src/VehicleRental.Application/Queries/CalculateRentalCost/CalculateRentalCostQuery.cs` 及 `CalculateRentalCostQueryHandler.cs`：查詢 VehicleType.DailyRate，計算 `TotalCost = DailyRate × (EndDate - StartDate).Days`；EndDate ≤ StartDate 時回傳 `IsValid = false`；回傳 `CalculateRentalCostResult { decimal TotalCost, int Days, decimal DailyRate, bool IsValid, string? ErrorMessage }`
- [ ] T034 [US3] 修改/確認 `src/VehicleRental.Application/Commands/CreateReservation/CreateReservationCommand.cs` 及 `CreateReservationCommandHandler.cs`：驗證 EndDate > StartDate；時段衝突檢查（同 VehicleTypeId 在 [StartDate, EndDate) 內無其他 Confirmed 預約）；生成 `ReservationNumber = $"RES-{DateTime.UtcNow:yyyyMMdd}-{count+1:D4}"`；計算 TotalCost；儲存 Reservation；呼叫 IPersistenceService.SaveAsync()；回傳 `CreateReservationResult { bool Success, string? ReservationNumber, string? Error }`
- [ ] T035 [P] [US3] 建立 `src/VehicleRental.Wasm/Components/DateRangePicker.razor`：接受 `StartDate`/`EndDate`（DateOnly）雙向綁定 EventCallback；內含兩個 `<input type="date">`；任一改變時觸發父元件 `OnDateChanged`
- [ ] T036 [US3] 建立 `src/VehicleRental.Wasm/Pages/CreateReservation.razor`：`@page "/reservation/create/{VehicleTypeId:int}"`；`@attribute [Authorize]`；`OnInitializedAsync` 查詢車型資訊；嵌入 DateRangePicker；日期改變時呼叫 `CalculateRentalCostQuery` 並即時更新「預估租金」顯示；EditForm 送出時呼叫 `CreateReservationCommand`；成功 → `NavigateTo($"/reservation/confirmation/{reservationNumber}")`；衝突錯誤 → 顯示「此時段車輛已被預約」AlertMessage；日期驗證失敗 → ValidationMessage；電競主題卡片樣式
- [ ] T037 [US3] 建立 `src/VehicleRental.Wasm/Pages/ReservationConfirmation.razor`：`@page "/reservation/confirmation/{ReservationNumber}"`；`@attribute [Authorize]`；`OnInitializedAsync` 以 ReservationNumber 從 IPersistenceService 還原的 EF InMemory 查詢預約詳情；顯示預約編號（大字螢光綠）、車型名稱、日期範圍、總租金；「查看我的預約」按鈕 → NavigateTo "/reservation/my"；「返回首頁」按鈕

**Checkpoint**：使用者故事 1+2+3 完整可展示，MVP 功能完整

---

## Phase 6：使用者故事 4 — 預約管理（優先級：P2）

**目標**：用戶可查看自己的歷史預約清單，並取消尚未開始的預約

**獨立測試**：登入後進入 `/reservation/my`，顯示已建立的預約；「取消」功能更新狀態為已取消

### 使用者故事 4 測試

- [ ] T038 [P] [US4] 建立 `tests/VehicleRental.Tests/Unit/Queries/GetMyReservationsQueryHandlerTests.cs`：`Handle_Should_Return_OnlyCurrentUserReservations`；`Handle_Should_Set_CanCancel_True_WhenStartDateInFuture`；`Handle_Should_Set_CanCancel_False_WhenStartDateTodayOrPast`
- [ ] T039 [P] [US4] 建立 `tests/VehicleRental.Tests/Unit/Commands/CancelReservationCommandHandlerTests.cs`：`Handle_Should_SetStatus_Cancelled`；`Handle_Should_Reject_WhenStartDateAlreadyPassed`；`Handle_Should_Reject_WhenReservationBelongsToDifferentUser`

### 使用者故事 4 實作

- [ ] T040 [P] [US4] 修改/確認 `src/VehicleRental.Application/Queries/GetMyReservations/GetMyReservationsQuery.cs` 及 `GetMyReservationsQueryHandler.cs`：依 CustomerId 查詢 Reservation，Include VehicleType，設定 `CanCancel = r.StartDate > DateOnly.FromDateTime(DateTime.UtcNow.Date) && r.Status == ReservationStatus.Confirmed`；回傳 `IEnumerable<ReservationSummaryDto>`
- [ ] T041 [P] [US4] 修改/確認 `src/VehicleRental.Application/Commands/CancelReservation/CancelReservationCommand.cs` 及 `CancelReservationCommandHandler.cs`：驗證預約屬於 CustomerId；驗證 StartDate > 今日；更新 Status = Cancelled；呼叫 IPersistenceService.SaveAsync()；回傳 `CancelReservationResult { bool Success, string? Error }`
- [ ] T042 [US4] 建立 `src/VehicleRental.Wasm/Pages/MyReservations.razor`：`@page "/reservation/my"`；`@attribute [Authorize]`；`OnInitializedAsync` 呼叫 `GetMyReservationsQuery`；顯示預約清單（電競風格表格：編號/車型/日期/金額/狀態標籤）；CanCancel = true 時顯示「取消預約」紅色按鈕；點擊後確認對話框（`IJSRuntime.InvokeAsync<bool>("confirm", "確定取消？")`），確認後呼叫 `CancelReservationCommand` 並重新載入列表

**Checkpoint**：所有使用者故事均可獨立展示與測試

---

## Phase 7：Polish 與 GitHub Pages 部署設定

**目的**：確認部署設定正確、所有測試通過、GitHub Actions 可正常運行

- [ ] T043 [P] 修改 `src/VehicleRental.Wasm/wwwroot/index.html`：更新 `<base href>` 說明（以 `<!-- LOCAL: <base href="/" /> -->` 和 `<!-- GITHUB PAGES: <base href="/NexVehicleRental/" /> -->` 雙行註解指引）；確認 SPA 路由還原腳本在 `<head>` 開頭位置正確
- [ ] T044 [P] 確認 `src/VehicleRental.Wasm/VehicleRental.Wasm.csproj`：加入 `<PublishTrimmed>true</PublishTrimmed>` 以縮小 WASM 輸出體積；確認 `<WasmBuildNative>false</WasmBuildNative>`（MVP 不需 AOT）
- [ ] T045 執行 `dotnet test --verbosity normal`，確認所有單元測試通過（目標：T021/T022/T027/T031/T032 及其他 US4 測試全數通過）
- [ ] T046 執行本地 `dotnet publish src/VehicleRental.Wasm/VehicleRental.Wasm.csproj -c Release -o publish` 並驗證 `publish/wwwroot/` 目錄含 `index.html`、`_framework/`、`.nojekyll`、`404.html`、`css/esports.css`
- [ ] T047 [P] 在 `src/VehicleRental.Wasm/wwwroot/` 建立 `security-notice.html`（可選）：簡短說明此為 Demo 用途的本地沙盒認證，提醒用戶資料僅存於本機瀏覽器
- [ ] T048 依照 quickstart.md Part B 完整步驟（B1-B7）執行 GitHub Pages 部署，確認 `https://USER.github.io/NexVehicleRental/` 可正常訪問

---

## Dependencies（依賴關係圖）

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundation)
    ↓
┌───────────────────────────────┐
│  US1  │  US2  │  US3  │  US4 │  ← 可平行（由不同開發者），但建議 US1→US2→US3→US4 序列
└───────────────────────────────┘
    ↓
Phase 7 (Polish & Deploy)
```

### 使用者故事內部依賴

- **US1**：T021/T022（測試）→ T023/T024（Handler）→ T025/T026（Pages）
- **US2**：T027（測試）→ T028（Handler）→ T029/T030（Pages）
- **US3**：T031/T032（測試）→ T033/T034（Handlers）→ T035（DateRangePicker）→ T036/T037（Pages）
- **US4**：T038/T039（測試）→ T040/T041（Handlers）→ T042（Page）

### Phase 2 內部依賴

- T008（IPersistenceService 介面）→ T009（LocalStoragePersistenceService 實作）→ T010（DI 設定）
- T010（DI 設定）→ T011（AuthStateProvider）→ T012（Program.cs）→ T013（App.razor）

---

## 平行執行範例

### 故事 1 平行機會（3 位開發者）

```
開發者 A: T021 → T023 → T025
開發者 B: T022 → T024 → T026
（測試先由 A/B 各自寫，Handler 確認後再分別實作 Page）
```

### Phase 2 平行機會

```
開發者 A: T006 → T007 → T008 → T009 → T010 → T011 → T012 → T013
開發者 B: T014 → T015 → T016 → T017  （共用元件，無依賴）
開發者 C: T018 → T019 → T020          （wwwroot 靜態檔案）
```

---

## 實作策略

### MVP 優先範圍

建議第一個可展示版本僅完成：

1. Phase 1（全部）
2. Phase 2（全部）
3. Phase 3 US1（帳戶系統）
4. Phase 4 US2（車型清單）
5. Phase 5 US3（預約核心）

這樣即可完整走過「首頁 → 登入 → 選車 → 預約 → 確認頁」主流程。

**US4（我的預約/取消）可在 MVP 展示後再補實作。**

---

## 格式驗證確認

所有任務均遵循以下格式：
- ✅ 以 `- [ ]` 開始（Markdown checkbox）
- ✅ 含 Task ID（T001-T048）
- ✅ 平行可執行任務標記 `[P]`
- ✅ 使用者故事 Phase 任務標記 `[USx]`
- ✅ 每個描述含明確檔案路徑
