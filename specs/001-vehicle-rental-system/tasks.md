# Tasks: 車輛租用系統（線上預約租車）

**Input**: Design documents from `specs/001-vehicle-rental-system/`
**Prerequisites**: plan.md ✅ spec.md ✅ research.md ✅ data-model.md ✅ contracts/ ✅ quickstart.md ✅

**Tests**: P1 使用者故事均要求測試（由章程與規格中的 Constitution Compliance 章節明確規定）。測試任務必須在對應實作任務之前完成且確認失敗，再進行實作。

**Organization**: 任務依使用者故事分組，每個故事可獨立實作與測試。

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: 可平行執行（不同檔案、無未完成依賴）
- **[Story]**: 對應使用者故事（US1–US4）
- 每個描述均包含明確檔案路徑

## Path Conventions

- Domain 實體：`src/VehicleRental.Domain/`
- Application CQRS：`src/VehicleRental.Application/`
- Infrastructure：`src/VehicleRental.Infrastructure/`
- Web 控制器/View：`src/VehicleRental.Web/`
- 測試：`tests/VehicleRental.Tests/`

---

## Phase 1：Setup（專案初始化）

**目的**：建立方案結構、安裝套件、設定基本環境

- [ ] T001 建立方案與五個專案：執行 quickstart.md 第 1 節指令，產生 `VehicleRental.sln` 及 `src/`、`tests/` 目錄結構
- [ ] T002 安裝所有 NuGet 套件：執行 quickstart.md 第 2 節指令（MediatR 12、EF Core 10、xUnit、Moq、WebApplicationFactory 等）
- [ ] T003 [P] 設定 `appsettings.json` 連線字串至 `src/VehicleRental.Web/appsettings.Development.json`（LocalDB）
- [ ] T004 [P] 新增 `src/VehicleRental.Web/VehicleRental.Web.csproj` 的 `<UserSecretsId>` 與 `<Nullable>enable</Nullable>` 設定
- [ ] T005 [P] 新增測試專案 `<IsPackable>false</IsPackable>` 與 `<Nullable>enable</Nullable>` 設定至 `tests/VehicleRental.Tests/VehicleRental.Tests.csproj`

---

## Phase 2：Foundational（所有使用者故事的共用基礎）

**目的**：Domain 實體、DbContext、DI 設定、Layout — 必須在任何使用者故事開始前完成

**⚠️ CRITICAL**：所有使用者故事的實作均依賴此 Phase 完成

- [ ] T006 建立 `src/VehicleRental.Domain/Enums/ReservationStatus.cs`（`Confirmed = 1`, `Cancelled = 2`）
- [ ] T007 [P] 建立 `src/VehicleRental.Domain/Entities/Customer.cs`（Id, FullName, Email, PasswordHash, CreatedAt, Reservations 導覽屬性）
- [ ] T008 [P] 建立 `src/VehicleRental.Domain/Entities/VehicleType.cs`（Id, Name, Description, DailyRate, IsAvailable, ImageUrl, Reservations 導覽屬性）
- [ ] T009 建立 `src/VehicleRental.Domain/Entities/Reservation.cs`（Id, ReservationNumber, CustomerId, VehicleTypeId, StartDate, EndDate, TotalCost, Status, CreatedAt, Customer/VehicleType 導覽屬性）
- [ ] T010 建立 `src/VehicleRental.Infrastructure/Data/AppDbContext.cs`（AppDbContext : DbContext，含 Customers/VehicleTypes/Reservations DbSet、Entity 設定、Email UniqueIndex、VehicleType Seed Data 4 筆）
- [ ] T011 建立 `src/VehicleRental.Infrastructure/DependencyInjection.cs`（AddInfrastructure 擴充方法，註冊 AppDbContext + SqlServer）
- [ ] T012 建立 `src/VehicleRental.Web/Program.cs`（完整設定：AddControllersWithViews、AddMediatR、AddInfrastructure、AddAuthentication(Cookie)、UseAuthentication、UseAuthorization；LoginPath="/Account/Login"）
- [ ] T013 建立 EF Core 初始 Migration 並套用資料庫：執行 quickstart.md 第 4 節指令（`InitialCreate`，含 Seed Data）
- [ ] T014 [P] 建立 `src/VehicleRental.Web/wwwroot/css/esports.css`（電競主題：深黑背景 #0d0d0d、螢光綠 #00ff99、電競紫 #7b2fff、Rajdhani Google Font、按鈕 glow 效果、卡片半透明深色、深色表單輸入框）
- [ ] T015 建立 `src/VehicleRental.Web/Views/Shared/_Layout.cshtml`（Bootstrap 5.1 CDN link、esports.css link、Rajdhani Google Font import、電競風格 Navbar 含品牌 logo/登入狀態/登出按鈕、@RenderBody()、@RenderSection scripts）

**Checkpoint**：Foundation ready — 使用者故事實作可開始

---

## Phase 3：使用者故事 1 — 帳戶註冊與登入（優先級：P1）🎯 MVP

**目標**：用戶可自行註冊帳戶並登入系統，未登入時受保護頁面自動導向登入頁

**獨立測試**：建立測試帳號並登入，驗證 Cookie 存在且可進入 `/Reservation` 即通過

### 使用者故事 1 測試（REQUIRED — P1/MVP，測試先寫，確認失敗後再實作）

> **注意**：先建立以下測試並執行確認 FAIL，再進行實作

- [ ] T016 [P] [US1] 新增 `tests/VehicleRental.Tests/Integration/AccountControllerTests.cs`：`Register_Post_Should_HashPassword_And_RedirectToLogin` 測試（WebApplicationFactory + InMemory DB）
- [ ] T017 [P] [US1] 新增 `AccountControllerTests.cs`：`Register_Post_Should_Reject_DuplicateEmail` 測試（相同 Email 第二次註冊應回傳 ModelState 錯誤）
- [ ] T018 [P] [US1] 新增 `AccountControllerTests.cs`：`Login_Post_Should_SetAuthCookie_OnSuccess` 測試（正確帳密登入後 Response.Headers 含 Set-Cookie）
- [ ] T019 [P] [US1] 新增 `AccountControllerTests.cs`：`Login_Post_Should_ReturnError_OnWrongPassword` 測試
- [ ] T020 [P] [US1] 新增 `AccountControllerTests.cs`：`ReservationIndex_Should_RedirectToLogin_WhenUnauthenticated` 測試（未登入 GET /Reservation 應回 302 導向 /Account/Login）

### 使用者故事 1 實作

- [ ] T021 [P] [US1] 建立 `src/VehicleRental.Application/Commands/RegisterCustomer/RegisterCustomerCommand.cs`（record：FullName, Email, Password）與 `RegisterCustomerCommandHandler.cs`（注入 AppDbContext，驗證 Email 唯一性，PasswordHasher<Customer> 雜湊，儲存 Customer）
- [ ] T022 [P] [US1] 建立 `src/VehicleRental.Application/Commands/LoginCustomer/LoginCustomerCommand.cs` 與 `LoginCustomerCommandHandler.cs`（依 Email 查找 Customer，PasswordHasher.VerifyHashedPassword 驗證，回傳 `LoginResult { bool Success, int? CustomerId, string? FullName }`）
- [ ] T023 [P] [US1] 建立 `src/VehicleRental.Application/DTOs/Results/LoginResult.cs`
- [ ] T024 [US1] 建立 `src/VehicleRental.Web/Models/ViewModels/RegisterViewModel.cs`（FullName, Email, Password, ConfirmPassword + DataAnnotations 驗證屬性）
- [ ] T025 [US1] 建立 `src/VehicleRental.Web/Models/ViewModels/LoginViewModel.cs`（Email, Password, ReturnUrl + DataAnnotations）
- [ ] T026 [US1] 建立 `src/VehicleRental.Web/Controllers/AccountController.cs`（注入 IMediator；Register GET/POST、Login GET/POST、Logout POST；POST Register 呼叫 RegisterCustomerCommand；POST Login 呼叫 LoginCustomerCommand 並以 ClaimsPrincipal 建立 Cookie；Logout 呼叫 HttpContext.SignOutAsync）
- [ ] T027 [US1] 建立 `src/VehicleRental.Web/Views/Account/Register.cshtml`（電競風格表單：深色卡片、螢光邊框輸入框、glow 送出按鈕；Bootstrap form-control；顯示 ValidationMessage）
- [ ] T028 [US1] 建立 `src/VehicleRental.Web/Views/Account/Login.cshtml`（同電競風格；含「還沒帳號？立即註冊」連結；TempData 顯示「註冊成功」訊息）
- [ ] T029 [US1] 建立 `src/VehicleRental.Web/Controllers/HomeController.cs` 與 `src/VehicleRental.Web/Views/Home/Index.cshtml`（電競風格 Landing Page：Hero 區塊含「立即租車」CTA 按鈕、系統名稱、背景效果）

**Checkpoint**：使用者故事 1 可獨立功能測試與展示

---

## Phase 4：使用者故事 2 — 車型瀏覽與選擇（優先級：P1）

**目標**：已登入用戶可看到車型清單並選擇車輛進入預約流程

**獨立測試**：登入後開啟 `/Reservation`，顯示 4 款 Seed 車型卡片，點擊任一車型「選擇此車」可進入下一頁

### 使用者故事 2 測試（REQUIRED — P1）

- [ ] T030 [P] [US2] 新增 `tests/VehicleRental.Tests/Integration/ReservationControllerTests.cs`：`Index_Should_Return_VehicleList_WhenAuthenticated` 測試（登入後 GET /Reservation，確認 ViewData 含 4 筆車型）
- [ ] T031 [P] [US2] 新增 `ReservationControllerTests.cs`：`Index_Should_RedirectToLogin_WhenUnauthenticated` 測試（未登入 GET /Reservation 回傳 302）

### 使用者故事 2 實作

- [ ] T032 [P] [US2] 建立 `src/VehicleRental.Application/Queries/GetVehicleTypes/GetVehicleTypesQuery.cs`（空 record）與 `GetVehicleTypesQueryHandler.cs`（查詢 AppDbContext.VehicleTypes，回傳 `IEnumerable<VehicleTypeDto>`）
- [ ] T033 [P] [US2] 建立 `src/VehicleRental.Application/DTOs/VehicleTypeDto.cs`（record：Id, Name, Description, DailyRate, IsAvailable, ImageUrl）
- [ ] T034 [US2] 建立 `src/VehicleRental.Web/Models/ViewModels/VehicleTypeListViewModel.cs`（`IEnumerable<VehicleTypeDto> VehicleTypes`）
- [ ] T035 [US2] 新增 `src/VehicleRental.Web/Controllers/ReservationController.cs`（`[Authorize]`；注入 IMediator；Index GET 呼叫 GetVehicleTypesQuery，回傳 VehicleTypeListViewModel）
- [ ] T036 [US2] 建立 `src/VehicleRental.Web/Views/Reservation/Index.cshtml`（電競風格車型卡片 Grid：Bootstrap 卡片 + 螢光邊框 + hover glow；顯示車型圖片/名稱/描述/每日租金；「選擇此車」按鈕連結至 `/Reservation/Create/{id}`；車型不可用時顯示停用狀態）

**Checkpoint**：使用者故事 2 可獨立功能測試與展示

---

## Phase 5：使用者故事 3 — 租用時間選擇與租金計算（優先級：P1）

**目標**：用戶選定車型後可設定日期、即時看到租金、送出預約並取得預約編號

**獨立測試**：選定車型後，設定 3 天租期，頁面顯示正確租金，送出後看到預約確認頁含預約編號

### 使用者故事 3 測試（REQUIRED — P1）

- [ ] T037 [P] [US3] 新增 `tests/VehicleRental.Tests/Unit/CalculateRentalCostQueryHandlerTests.cs`：`Handle_Should_Return_CorrectCost`（3 天 × 1800 = 5400）與 `Handle_Should_Return_Zero_WhenSameDayStartEnd` 測試
- [ ] T038 [P] [US3] 新增 `ReservationControllerTests.cs`：`Create_Post_Should_CreateReservation_AndRedirectToConfirmation` 測試（合法日期送出後建立預約）
- [ ] T039 [P] [US3] 新增 `ReservationControllerTests.cs`：`Create_Post_Should_ReturnError_WhenTimeConflict` 測試（時段衝突應在 ModelState 中加入錯誤）
- [ ] T040 [P] [US3] 新增 `ReservationControllerTests.cs`：`Create_Post_Should_ReturnError_WhenEndDateBeforeStartDate` 測試

### 使用者故事 3 實作

- [ ] T041 [P] [US3] 建立 `src/VehicleRental.Application/Queries/CalculateRentalCost/CalculateRentalCostQuery.cs`（VehicleTypeId, StartDate, EndDate）與 `CalculateRentalCostQueryHandler.cs`（查詢 DailyRate，計算 TotalCost = DailyRate × (EndDate - StartDate).Days，回傳 `CalculateRentalCostResult { decimal TotalCost, int Days, decimal DailyRate }`）
- [ ] T042 [P] [US3] 建立 `src/VehicleRental.Application/Queries/CalculateRentalCost/CalculateRentalCostResult.cs`
- [ ] T043 [P] [US3] 建立 `src/VehicleRental.Application/Commands/CreateReservation/CreateReservationCommand.cs`（CustomerId, VehicleTypeId, StartDate, EndDate）與 `CreateReservationCommandHandler.cs`（驗證：EndDate > StartDate；時段衝突檢查；生成 ReservationNumber `RES-{yyyyMMdd}-{序號:D4}`；計算 TotalCost；儲存 Reservation；回傳 `CreateReservationResult { bool Success, int ReservationId, string? Error }`）
- [ ] T044 [P] [US3] 建立 `src/VehicleRental.Application/DTOs/Results/CreateReservationResult.cs`
- [ ] T045 [US3] 建立 `src/VehicleRental.Web/Models/ViewModels/CreateReservationViewModel.cs`（VehicleTypeId, VehicleTypeName, DailyRate, StartDate, EndDate, EstimatedCost + DataAnnotations）
- [ ] T046 [US3] 在 `ReservationController.cs` 新增：Create GET（依 vehicleTypeId 載入車型資訊，初始化 ViewModel）、Create POST（呼叫 CreateReservationCommand，成功重導向 Confirmation，失敗回 View）、CalculateCost GET（呼叫 CalculateRentalCostQuery，回傳 JSON）
- [ ] T047 [US3] 建立 `src/VehicleRental.Web/Views/Reservation/Create.cshtml`（電競風格日期選擇表單；顯示已選車型資訊與每日租金；JS 呼叫 `/Reservation/CalculateCost?vehicleTypeId=&startDate=&endDate=` AJAX，即時更新「預估租金」顯示區塊；Bootstrap datepicker；驗證訊息）
- [ ] T048 [US3] 建立 `src/VehicleRental.Web/Views/Reservation/Confirmation.cshtml`（電競風格確認頁；顯示預約編號（螢光綠大字）、車型名稱、租用期間、總租金；「查看我的預約」與「返回首頁」按鈕）

**Checkpoint**：使用者故事 1+2+3 均可獨立功能測試，MVP 完整可展示

---

## Phase 6：使用者故事 4 — 預約管理（優先級：P2）

**目標**：用戶可查看自己的預約清單，並取消尚未開始的預約

**獨立測試**：登入後進入 `/Reservation/My`，顯示先前建立的預約，點擊「取消」後狀態更新為已取消

### 使用者故事 4 實作（P2 — MVP 後實作）

- [ ] T049 [P] [US4] 建立 `src/VehicleRental.Application/Queries/GetMyReservations/GetMyReservationsQuery.cs`（CustomerId）與 `GetMyReservationsQueryHandler.cs`（查詢該用戶所有 Reservation，包含 VehicleType 名稱，回傳 `IEnumerable<ReservationSummaryDto>`）
- [ ] T050 [P] [US4] 建立 `src/VehicleRental.Application/DTOs/ReservationSummaryDto.cs`（record：Id, ReservationNumber, VehicleTypeName, StartDate, EndDate, TotalCost, Status, CanCancel）
- [ ] T051 [P] [US4] 建立 `src/VehicleRental.Application/Commands/CancelReservation/CancelReservationCommand.cs`（ReservationId, CustomerId）與 `CancelReservationCommandHandler.cs`（驗證預約屬於該用戶且 StartDate > 今日；更新狀態為 Cancelled）
- [ ] T052 [US4] 建立 `src/VehicleRental.Web/Models/ViewModels/MyReservationsViewModel.cs`（`IEnumerable<ReservationSummaryDto> Reservations`）
- [ ] T053 [US4] 在 `ReservationController.cs` 新增：My GET（呼叫 GetMyReservationsQuery，回傳 MyReservationsViewModel）、Cancel POST（呼叫 CancelReservationCommand，重導向 My 頁帶 TempData 訊息）
- [ ] T054 [US4] 建立 `src/VehicleRental.Web/Views/Reservation/My.cshtml`（電競風格預約清單表格；顯示預約編號/車型/日期/金額/狀態；可取消的預約顯示「取消預約」按鈕（螢光紅邊框）；不可取消者顯示灰色狀態標籤）

**Checkpoint**：全部四個使用者故事均可獨立功能測試

---

## Phase N：Polish 與跨切面優化

**目的**：提升跨故事品質的改善項目

- [ ] T055 [P] 更新 `src/VehicleRental.Web/Views/Shared/_Layout.cshtml` Navbar：已登入時顯示「我的預約」連結（P2 頁面加入後）與用戶姓名
- [ ] T056 [P] 新增 `src/VehicleRental.Web/Views/Shared/_ValidationScriptsPartial.cshtml`（Bootstrap + jQuery Unobtrusive Validation CDN）並在所有表單 View 中引用
- [ ] T057 執行完整測試套件：`dotnet test --verbosity normal`，確認所有測試通過（預計 8+ 個測試全部綠燈）
- [ ] T058 執行 quickstart.md 第 7 節驗收腳本，確認端點行為符合規格
- [ ] T059 [P] 新增 `src/VehicleRental.Web/Views/Shared/Error.cshtml`（電競風格錯誤頁；顯示錯誤碼與返回首頁按鈕）

---

## Dependencies & Execution Order

### Phase 依賴關係

- **Setup (Phase 1)**：無依賴，立即開始
- **Foundational (Phase 2)**：依賴 Setup 完成 — **封鎖所有使用者故事**
- **使用者故事（Phase 3–6）**：依賴 Foundational 完成
  - US1、US2、US3 可在 Foundational 完成後平行進行（如有多位開發者）
  - 或依優先序執行：US1 → US2 → US3 → US4
- **Polish (Phase N)**：依賴所有目標使用者故事完成

### 使用者故事依賴關係

- **US1（P1）**：Foundational 完成後即可開始，無其他故事依賴
- **US2（P1）**：Foundational 完成後即可開始；Controller 框架與 ReservationController 骨架需先建立
- **US3（P1）**：依賴 US2 中已建立的 `ReservationController.cs` 基礎；US1 的 Cookie Auth 設定必須就位
- **US4（P2）**：依賴 US3 已建立的 `Reservation` 實體與 `ReservationController.cs`

### 每個使用者故事內部順序

1. **測試任務（REQUIRED for P1）→ 確認失敗 → 實作**
2. Application 層（Command/Query Handler）
3. ViewModel / DTO
4. Controller Action
5. View（.cshtml）

### 平行機會

- T007、T008 可與 T006 平行（不同 Domain 實體檔案）
- T014（esports.css）可與所有 Phase 2 後端任務平行
- 所有標記 `[P]` 的測試任務可同時撰寫
- US1 測試（T016–T020）可與 US2 測試（T030–T031）平行

---

## Parallel Example：使用者故事 1

```text
# 同時建立所有 US1 測試：
Task T016: Register_Post_Should_HashPassword_And_RedirectToLogin
Task T017: Register_Post_Should_Reject_DuplicateEmail
Task T018: Login_Post_Should_SetAuthCookie_OnSuccess
Task T019: Login_Post_Should_ReturnError_OnWrongPassword
Task T020: ReservationIndex_Should_RedirectToLogin_WhenUnauthenticated

# 確認全部 FAIL 後，同時建立 Application 層（不同檔案）：
Task T021: RegisterCustomerCommandHandler.cs
Task T022: LoginCustomerCommandHandler.cs
```

---

## Implementation Strategy

### MVP 優先（US1 + US2 + US3）

1. Phase 1：Setup
2. Phase 2：Foundational（**必須完成，封鎖後續**）
3. Phase 3：US1（帳戶） → 測試通過 → 展示
4. Phase 4：US2（車型清單） → 測試通過 → 展示
5. Phase 5：US3（預約建立） → 測試通過 → **MVP 完整可展示**
6. **STOP and VALIDATE**：執行 `dotnet test`，確認 P1 測試全通過；執行驗收腳本

### 增量交付

1. Setup + Foundational → 基礎就緒
2. +US1 → 帳戶功能可獨立展示
3. +US2 → 車型清單可展示（需已登入）
4. +US3 → MVP 完整流程：從首頁到預約確認
5. +US4（P2）→ 自助預約管理功能

### 平行團隊策略

多位開發者時：
1. 全員共同完成 Setup + Foundational
2. Foundational 完成後：
   - 開發者 A：US1（帳戶）
   - 開發者 B：US2（車型）
   - 開發者 C：US3（預約） — 需等 US1 Cookie Auth 就位後才能完整測試

---

## Notes

- `[P]` 任務 = 不同檔案、無未完成依賴，可平行執行
- `[Story]` 標籤追蹤每個任務對應的使用者故事
- **P1 測試為必要（非選項）** — 由章程與規格 Constitution Compliance 要求
- 驗證測試先失敗後再實作（Red-Green-Refactor）
- 每個任務或邏輯群組完成後提交 commit
- 在每個 Checkpoint 停下來獨立驗證該故事功能
- 避免：模糊任務描述、同一檔案的衝突修改、破壞故事獨立性的跨故事依賴
