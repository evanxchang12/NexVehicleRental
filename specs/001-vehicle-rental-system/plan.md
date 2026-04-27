# Implementation Plan: 車輛租用系統（線上預約租車）

**Branch**: `001-vehicle-rental-system` | **Date**: 2026-04-27 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `specs/001-vehicle-rental-system/spec.md`

## Summary

以 ASP.NET Core 10 MVC 建構線上車輛租用系統，提供租車用戶帳戶註冊/登入、車型瀏覽與選擇、租用時間設定與租金計算、預約建立等功能。Controller 透過 MediatR 12 呼叫 Application Services 中的 CQRS Commands/Queries。UI 採用 Bootstrap 5.1 RWD + 電競主題自訂 CSS。

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: ASP.NET Core 10 MVC、MediatR 12、Entity Framework Core 10、Bootstrap 5.1 (CDN)  
**Storage**: SQL Server LocalDB（開發）/ SQL Server（生產）；測試使用 EF Core InMemory  
**Testing**: xUnit + Moq + WebApplicationFactory（整合測試）  
**Target Platform**: 桌面瀏覽器（Web 應用程式，Windows/Linux 伺服器）  
**Project Type**: web-service（MVC Server-rendered）  
**Performance Goals**: 頁面回應 < 500ms p95（MVP 本地開發目標）；租金計算 AJAX < 200ms  
**Constraints**: 密碼不得明文儲存；`[Authorize]` 保護所有預約相關頁面  
**Scale/Scope**: MVP — 單一租車平台，小型用戶規模（< 1000 用戶，< 10000 預約）

## Constitution Check

*GATE: Phase 0 研究前完成；Phase 1 設計後重新確認。*

| 原則 | 狀態 | 說明 |
|------|------|------|
| 一、MVP 優先 | ✅ 通過 | P2（預約管理）明確延後；付款、管理後台排除於 MVP 外 |
| 二、可測試設計 | ✅ 通過 | Controller 僅依賴 IMediator；Handler 僅依賴 DbContext（可替換 InMemory）|
| 三、測試優先 | ✅ 通過 | 每個 P1 使用者故事皆有對應測試任務，需先撰寫再實作 |
| 四、簡潔可維護 | ✅ 通過 | 無 Repository 抽象層（YAGNI）；直接使用 DbContext；無不必要的抽象 |
| 五、版本與治理 | ✅ 通過 | 初始版本 1.0.0；EF Migrations 追蹤 schema 變更 |

**P1 測試覆蓋確認**：
- 使用者故事 1 → `AccountController` 整合測試（Register POST / Login POST）
- 使用者故事 2 → `ReservationController.Index` 整合測試（已登入 / 未登入）
- 使用者故事 3 → `CalculateRentalCostQueryHandler` 單元測試 + `CreateReservationCommandHandler` 整合測試（含衝突檢查）

## Project Structure

### 文件（本功能）

```text
specs/001-vehicle-rental-system/
├── plan.md              ← 本文件
├── research.md          ← Phase 0 輸出
├── data-model.md        ← Phase 1 輸出
├── quickstart.md        ← Phase 1 輸出
├── contracts/
│   └── http-routes.md   ← Phase 1 輸出
└── tasks.md             ← Phase 2 輸出（/speckit.tasks 指令產生）
```

### 原始碼（儲存庫根目錄）

```text
src/
├── VehicleRental.Domain/
│   ├── Entities/
│   │   ├── Customer.cs
│   │   ├── VehicleType.cs
│   │   └── Reservation.cs
│   └── Enums/
│       └── ReservationStatus.cs
│
├── VehicleRental.Application/
│   ├── Commands/
│   │   ├── RegisterCustomer/
│   │   │   ├── RegisterCustomerCommand.cs
│   │   │   └── RegisterCustomerCommandHandler.cs
│   │   ├── LoginCustomer/
│   │   │   ├── LoginCustomerCommand.cs
│   │   │   └── LoginCustomerCommandHandler.cs
│   │   ├── CreateReservation/
│   │   │   ├── CreateReservationCommand.cs
│   │   │   └── CreateReservationCommandHandler.cs
│   │   └── CancelReservation/           ← P2
│   │       ├── CancelReservationCommand.cs
│   │       └── CancelReservationCommandHandler.cs
│   ├── Queries/
│   │   ├── GetVehicleTypes/
│   │   │   ├── GetVehicleTypesQuery.cs
│   │   │   └── GetVehicleTypesQueryHandler.cs
│   │   ├── CalculateRentalCost/
│   │   │   ├── CalculateRentalCostQuery.cs
│   │   │   └── CalculateRentalCostQueryHandler.cs
│   │   └── GetMyReservations/           ← P2
│   │       ├── GetMyReservationsQuery.cs
│   │       └── GetMyReservationsQueryHandler.cs
│   └── DTOs/
│       ├── VehicleTypeDto.cs
│       ├── ReservationSummaryDto.cs
│       └── Results/
│           ├── LoginResult.cs
│           └── CreateReservationResult.cs
│
├── VehicleRental.Infrastructure/
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── Migrations/
│   └── DependencyInjection.cs
│
└── VehicleRental.Web/
    ├── Controllers/
    │   ├── AccountController.cs
    │   ├── ReservationController.cs
    │   └── HomeController.cs
    ├── Models/ViewModels/
    │   ├── RegisterViewModel.cs
    │   ├── LoginViewModel.cs
    │   ├── VehicleTypeListViewModel.cs
    │   ├── CreateReservationViewModel.cs
    │   └── MyReservationsViewModel.cs      ← P2
    ├── Views/
    │   ├── Account/
    │   │   ├── Register.cshtml
    │   │   └── Login.cshtml
    │   ├── Reservation/
    │   │   ├── Index.cshtml
    │   │   ├── Create.cshtml
    │   │   ├── Confirmation.cshtml
    │   │   └── My.cshtml                   ← P2
    │   ├── Home/
    │   │   └── Index.cshtml
    │   └── Shared/
    │       ├── _Layout.cshtml
    │       └── _ValidationScriptsPartial.cshtml
    ├── wwwroot/
    │   └── css/
    │       └── esports.css
    ├── Program.cs
    └── appsettings.json

tests/
└── VehicleRental.Tests/
    ├── Unit/
    │   └── CalculateRentalCostQueryHandlerTests.cs
    └── Integration/
        ├── AccountControllerTests.cs
        └── ReservationControllerTests.cs
```

**Structure Decision**：採用四層清潔架構（Domain / Application / Infrastructure / Web）+ 測試專案。選擇此結構因 CQRS Handlers 需獨立於 Web 層以利單元測試；未引入 Repository 抽象層（直接注入 DbContext，符合 YAGNI）。

## Complexity Tracking

> 僅在章程檢查有違反項時填寫

| 違反項目 | 理由 | 拒絕更簡單方案的原因 |
|---------|------|------------------|
| 4 個專案（非 1 個）| CQRS Handlers 需獨立測試；Domain 實體無外部依賴 | 單一 MVC 專案會混合 Domain、Application、Infrastructure 程式碼，使 Handler 難以在不啟動 Web 應用的情況下進行單元測試 |
