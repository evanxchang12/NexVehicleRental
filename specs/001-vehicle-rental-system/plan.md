# Implementation Plan: 車輛租用系統（Blazor WASM + GitHub Pages）

**Branch**: `001-vehicle-rental-system` | **Date**: 2026-04-28 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/001-vehicle-rental-system/spec.md`  
**Architecture Update**: ASP.NET Core MVC → Blazor WebAssembly + EF Core InMemory + GitHub Pages

## Summary

將現有 ASP.NET Core MVC 租車系統完全轉換為 **Blazor WebAssembly**，使 C# 程式碼在瀏覽器端執行（WASM），搭配 **EF Core InMemory + localStorage 持久化**取代伺服器端資料庫，並透過 **GitHub Actions 自動部署至 GitHub Pages**（永久免費靜態托管）。

Domain 實體（`Customer`、`VehicleType`、`Reservation`）與 CQRS Application Layer（MediatR Handlers）保持不變，僅替換最外層的 Host（Web → Wasm）與資料庫 Provider（SqlServer → InMemory）。

## Technical Context

**Language/Version**: C# 13 / .NET 10  
**Primary Dependencies**: Blazor WebAssembly 10、MediatR 12、EF Core 10 InMemory、Bootstrap 5.3（CDN）  
**Storage**: EF Core InMemory（執行期）+ localStorage JSON（跨 Session 持久化）  
**Testing**: xUnit + Moq + bUnit（Blazor 元件測試）  
**Target Platform**: 瀏覽器 WASM（GitHub Pages 靜態托管）  
**Project Type**: SPA Web Application（Client-side Rendered）  
**Performance Goals**: 首頁載入 < 5 秒（含 WASM 初始化）；租金計算 < 100ms（純記憶體運算）  
**Constraints**: 無伺服器依賴；純靜態輸出；每用戶資料存於本機瀏覽器  
**Scale/Scope**: Portfolio/Demo 用途；單一瀏覽器沙盒資料模型

## Constitution Check

*GATE: 通過後方可進入實作階段。*

| 原則 | 狀態 | 說明 |
|------|------|------|
| 一、MVP 優先 | ✅ 通過 | P2 功能（取消預約、我的預約）明確標記延後，不在 Phase 1 實作 |
| 二、可測試設計 | ✅ 通過 | CQRS Handlers 可獨立單元測試；Blazor 元件用 bUnit 測試 |
| 三、測試優先 | ✅ 通過 | plan.md 明確列出每個 P1 故事對應的測試案例 |
| 四、簡潔可維護 | ✅ 通過 | 無 Repository Pattern；直接注入 IAppDbContext；localStorage 持久化單一服務 |
| 五、版本治理 | ✅ 通過 | 架構重大變更記錄於 research.md；tasks.md 追蹤實作進度 |

**P1 測試覆蓋映射**：
- 使用者故事 1（帳戶註冊/登入）→ 單元測試：`RegisterCustomerCommandHandler`、`LoginCustomerCommandHandler`；bUnit：`LoginPage` 表單驗證
- 使用者故事 2（車型選擇）→ 單元測試：`GetVehicleTypesQueryHandler`；bUnit：`VehicleTypesPage` 路由守衛
- 使用者故事 3（租金計算/預約）→ 單元測試：`CalculateRentalCostQueryHandler`、`CreateReservationCommandHandler`（衝突檢查）

## Project Structure

### Documentation (this feature)

```text
specs/001-vehicle-rental-system/
├── plan.md                     # 本文件
├── research.md                 # 架構決策研究（已更新為 WASM 版）
├── data-model.md               # 資料模型 + localStorage 持久化策略
├── quickstart.md               # 本地開發 + GitHub Pages 部署教學
├── contracts/
│   ├── http-routes.md          # 舊版 MVC 路由合約（保留作為參考）
│   └── component-contracts.md # 新版 Blazor 元件合約（取代 http-routes）
└── tasks.md                    # Phase 2 輸出（/speckit.tasks 命令產生）
```

### Source Code（轉換後目標結構）

```text
src/
├── VehicleRental.Domain/              # ✅ 保持不變（純業務實體）
│   ├── Entities/
│   │   ├── Customer.cs
│   │   ├── VehicleType.cs
│   │   └── Reservation.cs
│   └── Enums/
│       └── ReservationStatus.cs
│
├── VehicleRental.Application/         # ✅ 保持不變（CQRS Handlers）
│   ├── Commands/
│   │   ├── RegisterCustomer/
│   │   ├── LoginCustomer/
│   │   ├── CreateReservation/
│   │   └── CancelReservation/
│   ├── Queries/
│   │   ├── GetVehicleTypes/
│   │   ├── GetMyReservations/
│   │   └── CalculateRentalCost/
│   ├── DTOs/
│   └── Interfaces/
│       └── IAppDbContext.cs
│
├── VehicleRental.Infrastructure/      # 🔄 修改：移除 SqlServer，改用 InMemory
│   ├── Data/
│   │   ├── AppDbContext.cs            # UseInMemoryDatabase
│   │   └── DbInitializer.cs          # 載入 Seed Data
│   ├── Services/
│   │   └── LocalStoragePersistenceService.cs  # 新增：localStorage 持久化
│   └── DependencyInjection.cs        # 新增 AddInfrastructureWasm()
│
├── VehicleRental.Wasm/                # 🆕 新建：Blazor WASM 前端（取代 Web）
│   ├── Program.cs
│   ├── App.razor
│   ├── Pages/
│   │   ├── Home.razor
│   │   ├── Login.razor
│   │   ├── Register.razor
│   │   ├── VehicleTypes.razor
│   │   ├── CreateReservation.razor
│   │   ├── ReservationConfirmation.razor
│   │   └── MyReservations.razor       # P2
│   ├── Shared/
│   │   ├── MainLayout.razor
│   │   ├── NavMenu.razor
│   │   ├── RedirectToLogin.razor
│   │   └── AlertMessage.razor
│   ├── Components/
│   │   └── DateRangePicker.razor
│   ├── Auth/
│   │   └── BrowserAuthenticationStateProvider.cs
│   └── wwwroot/
│       ├── index.html                 # 含 SPA 路由還原腳本
│       ├── 404.html                   # SPA 路由重定向
│       ├── .nojekyll                  # 禁用 Jekyll 處理
│       └── css/
│           └── esports.css            # 電競主題樣式
│
└── VehicleRental.Web/                 # 🔒 保留（歷史參考，不再使用）

.github/
└── workflows/
    └── deploy.yml                     # 🆕 GitHub Actions 自動部署

tests/
└── VehicleRental.Tests/               # 🔄 修改：移除 WebApplicationFactory，加入 bUnit
    ├── Unit/
    │   ├── Commands/
    │   └── Queries/
    └── Component/                     # 🆕 新增：Blazor bUnit 測試
```

## Complexity Tracking

| 違反原則 | 原因 | 更簡單替代方案被棄用原因 |
|---------|------|----------------------|
| InMemory + localStorage 雙層持久化 | WASM 無法直接使用 SQLite 原生二進位（MVP 範圍）| 純 localStorage JSON 失去 EF Core LINQ 查詢語意；純 SQLite OPFS 需要 COOP/COEP Headers，GitHub Pages 不原生支援 |
| 用戶端「認證」 | 無伺服器可驗證身分，為 Demo 用途的合理取捨 | 任何真正安全的認證都需要伺服器，與 GitHub Pages 靜態部署目標衝突 |

---

## Phase 0 Research Summary

所有研究已完成，詳見 [research.md](research.md)。關鍵決策：

| 問題 | 決策 |
|------|------|
| 資料庫 | EF Core InMemory + localStorage JSON 持久化 |
| 認證 | `BrowserAuthenticationStateProvider`（用戶端沙盒）|
| 密碼雜湊 | `Rfc2898DeriveBytes.Pbkdf2`（.NET WASM 標準庫）|
| SPA 路由 | 404.html 重定向腳本（GitHub Pages 標準解法）|
| 部署 | GitHub Actions `actions/deploy-pages@v4` |

---

## Phase 1 Design Artifacts

| 文件 | 狀態 | 說明 |
|------|------|------|
| [data-model.md](data-model.md) | ✅ 完成 | 實體定義 + localStorage 持久化策略 |
| [contracts/component-contracts.md](contracts/component-contracts.md) | ✅ 完成 | Blazor 元件路由、Model、送出流程 |
| [quickstart.md](quickstart.md) | ✅ 完成 | 本地開發 + GitHub Pages 完整部署教學 |

---

## 下一步

執行 `/speckit.tasks` 命令產生 `tasks.md`，將本計畫拆解為可執行的實作任務清單。
