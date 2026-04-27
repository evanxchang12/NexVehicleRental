# 快速入門指南：車輛租用系統

**功能分支**: `001-vehicle-rental-system`  
**日期**: 2026-04-27

---

## 先決條件

| 工具 | 版本需求 |
|------|---------|
| .NET SDK | 10.0 以上 |
| SQL Server LocalDB | 隨 Visual Studio 2022 安裝，或 `mssql-tools` |
| Visual Studio 2022 / VS Code | 最新版 |
| Git | 任意版本 |

確認 .NET SDK 版本：
```powershell
dotnet --version
# 預期輸出：10.x.x
```

---

## 1. 建立方案結構

```powershell
cd c:\develop\VehicleRental

# 建立解決方案
dotnet new sln -n VehicleRental

# 建立各專案
dotnet new mvc      -n VehicleRental.Web            -o src/VehicleRental.Web
dotnet new classlib -n VehicleRental.Application    -o src/VehicleRental.Application
dotnet new classlib -n VehicleRental.Domain         -o src/VehicleRental.Domain
dotnet new classlib -n VehicleRental.Infrastructure -o src/VehicleRental.Infrastructure
dotnet new xunit    -n VehicleRental.Tests          -o tests/VehicleRental.Tests

# 加入至解決方案
dotnet sln add src/VehicleRental.Web/VehicleRental.Web.csproj
dotnet sln add src/VehicleRental.Application/VehicleRental.Application.csproj
dotnet sln add src/VehicleRental.Domain/VehicleRental.Domain.csproj
dotnet sln add src/VehicleRental.Infrastructure/VehicleRental.Infrastructure.csproj
dotnet sln add tests/VehicleRental.Tests/VehicleRental.Tests.csproj
```

---

## 2. 安裝 NuGet 套件

```powershell
# Domain 無外部依賴

# Application
cd src/VehicleRental.Application
dotnet add package MediatR --version 12.*
dotnet add reference ../VehicleRental.Domain/VehicleRental.Domain.csproj

# Infrastructure
cd ../VehicleRental.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 10.*
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 10.*
dotnet add reference ../VehicleRental.Domain/VehicleRental.Domain.csproj
dotnet add reference ../VehicleRental.Application/VehicleRental.Application.csproj

# Web
cd ../VehicleRental.Web
dotnet add package MediatR --version 12.*
dotnet add reference ../VehicleRental.Application/VehicleRental.Application.csproj
dotnet add reference ../VehicleRental.Infrastructure/VehicleRental.Infrastructure.csproj

# Tests
cd ../../tests/VehicleRental.Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 10.*
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 10.*
dotnet add package Moq --version 4.*
dotnet add reference ../../src/VehicleRental.Web/VehicleRental.Web.csproj
dotnet add reference ../../src/VehicleRental.Application/VehicleRental.Application.csproj

cd ../..
```

---

## 3. 設定連線字串

編輯 `src/VehicleRental.Web/appsettings.Development.json`：
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=VehicleRentalDev;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

---

## 4. 執行資料庫 Migration

```powershell
cd src/VehicleRental.Web

# 建立第一次 Migration（含 Seed Data）
dotnet ef migrations add InitialCreate --project ../VehicleRental.Infrastructure --startup-project .

# 套用 Migration（建立資料庫 + Seed Data）
dotnet ef database update --project ../VehicleRental.Infrastructure --startup-project .

cd ../..
```

---

## 5. 執行應用程式

```powershell
cd src/VehicleRental.Web
dotnet run
```

開啟瀏覽器：`https://localhost:5001`

預期行為：
1. 首頁顯示電競風格 Landing Page，含「立即租車」按鈕。
2. 點擊「立即租車」→ 導向登入頁（因為未登入）。
3. 點擊「立即註冊」→ 顯示電競風格註冊表單。
4. 完成註冊並登入 → 顯示車型清單（4 款 Seed 車型）。
5. 選擇車型 → 顯示日期選擇頁，選擇日期後即時顯示租金。
6. 確認預約 → 顯示預約確認頁（含預約編號）。

---

## 6. 執行測試

```powershell
dotnet test --verbosity normal
```

**預期通過的測試（MVP P1）**：
- `RegisterCustomerCommandHandler_Should_HashPassword_And_Save`
- `RegisterCustomerCommandHandler_Should_RejectDuplicateEmail`
- `CalculateRentalCostQuery_Should_Return_Correct_Cost`
- `CreateReservationCommandHandler_Should_Detect_TimeConflict`
- `AccountController_Register_Post_Should_RedirectToLogin_OnSuccess`
- `AccountController_Login_Post_Should_SetAuthCookie_OnSuccess`
- `ReservationController_Index_Should_Return_VehicleList_WhenLoggedIn`
- `ReservationController_Index_Should_RedirectToLogin_WhenNotAuthenticated`

---

## 7. 驗收情境快速驗證腳本

以下 PowerShell 指令可在 CI 中快速確認核心端點回應（需先啟動應用程式）：

```powershell
# 未登入存取受保護頁面應回傳 302 導向登入
$response = Invoke-WebRequest -Uri "https://localhost:5001/Reservation" -MaximumRedirection 0 -ErrorAction SilentlyContinue
if ($response.StatusCode -eq 302) { Write-Host "✅ 未授權導向正常" } else { Write-Host "❌ FAIL" }
```

---

## 架構圖（簡化）

```
瀏覽器
  │  HTTP Request
  ▼
[AccountController / ReservationController]  ← 注入 IMediator
  │  mediator.Send(Command/Query)
  ▼
[MediatR Pipeline]
  │  Handler dispatch
  ▼
[Command Handler / Query Handler]            ← 在 Application 層
  │  使用 DbContext / Repositories
  ▼
[AppDbContext (EF Core 10)]
  │
  ▼
[SQL Server LocalDB]
```
