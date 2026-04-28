# 快速入門與部署指南：車輛租用系統（Blazor WASM + GitHub Pages）

**功能分支**: `001-vehicle-rental-system`  
**日期更新**: 2026-04-28  
**架構**: Blazor WebAssembly .NET 10 → GitHub Pages（靜態部署）

---

## 先決條件

| 工具 | 版本需求 | 安裝方式 |
|------|---------|---------|
| .NET SDK | 10.0 以上 | https://dot.net/download |
| Visual Studio 2022 / VS Code | 最新版 | 含 Blazor WASM workload |
| Git | 任意版本 | https://git-scm.com |
| GitHub 帳號 | — | https://github.com/signup |

```powershell
dotnet --version
# 預期輸出：10.x.x
```

---

## Part A：本地開發

### A1. 從現有 MVC 專案轉換（架構遷移）

```powershell
cd c:\develop\NexVehicleRental

# 建立新的 Blazor WASM 前端（取代 VehicleRental.Web）
dotnet new blazorwasm -n VehicleRental.Wasm -o src/VehicleRental.Wasm
dotnet sln add src/VehicleRental.Wasm/VehicleRental.Wasm.csproj
```

### A2. 安裝 NuGet 套件

```powershell
# Application（CQRS）
cd src/VehicleRental.Application
dotnet add package MediatR --version 12.*
dotnet add reference ../VehicleRental.Domain/VehicleRental.Domain.csproj
cd ../..

# Infrastructure（移除 SqlServer，改用 InMemory）
cd src/VehicleRental.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore --version 10.*
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 10.*
dotnet add reference ../VehicleRental.Domain/VehicleRental.Domain.csproj
dotnet add reference ../VehicleRental.Application/VehicleRental.Application.csproj
cd ../..

# Blazor WASM 前端
cd src/VehicleRental.Wasm
dotnet add package MediatR --version 12.*
dotnet add package Microsoft.AspNetCore.Components.WebAssembly.Authentication --version 10.*
dotnet add reference ../VehicleRental.Application/VehicleRental.Application.csproj
dotnet add reference ../VehicleRental.Infrastructure/VehicleRental.Infrastructure.csproj
cd ../..

# Tests
cd tests/VehicleRental.Tests
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 10.*
dotnet add package Moq --version 4.*
dotnet add package bunit --version 1.*
dotnet add reference ../../src/VehicleRental.Application/VehicleRental.Application.csproj
cd ../..
```

### A3. 設定 Program.cs（Blazor WASM 入口）

`src/VehicleRental.Wasm/Program.cs`：

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(ApplicationAssemblyMarker).Assembly));

// Infrastructure（EF Core InMemory + localStorage 持久化）
builder.Services.AddInfrastructureWasm();

// 認證
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<BrowserAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<BrowserAuthenticationStateProvider>());

await builder.Build().RunAsync();
```

### A4. 設定 App.razor（路由守衛）

`src/VehicleRental.Wasm/App.razor`：

```razor
<Router AppAssembly="@typeof(App).Assembly">
    <Found Context="routeData">
        <AuthorizeRouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)">
            <NotAuthorized>
                <RedirectToLogin />
            </NotAuthorized>
        </AuthorizeRouteView>
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(MainLayout)">
            <p>找不到此頁面。</p>
        </LayoutView>
    </NotFound>
</Router>
```

### A5. 新增 SPA 路由解決方案

建立 `src/VehicleRental.Wasm/wwwroot/404.html`：

```html
<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8">
  <title>NexVehicleRental</title>
  <script type="text/javascript">
    var pathSegmentsToKeep = 1;
    var l = window.location;
    l.replace(
      l.protocol + '//' + l.hostname + (l.port ? ':' + l.port : '') +
      l.pathname.split('/').slice(0, 1 + pathSegmentsToKeep).join('/') + '/?/' +
      l.pathname.slice(1).split('/').slice(pathSegmentsToKeep).join('/').replace(/&/g, '~and~') +
      (l.search ? '&' + l.search.slice(1).replace(/&/g, '~and~') : '') +
      l.hash
    );
  </script>
</head>
<body>正在重新導向...</body>
</html>
```

在 `wwwroot/index.html` 的 `<head>` 最前面加入還原腳本：

```html
<script type="text/javascript">
  (function(l) {
    if (l.search[1] === '/' ) {
      var decoded = l.search.slice(1).split('&').map(function(s) {
        return s.replace(/~and~/g, '&')
      }).join('?');
      window.history.replaceState(null, null,
          l.pathname.slice(0, -1) + decoded + l.hash
      );
    }
  }(window.location))
</script>
```

建立 `.nojekyll`：

```powershell
New-Item -ItemType File -Path "src/VehicleRental.Wasm/wwwroot/.nojekyll" -Force
```

### A6. 本地執行

```powershell
cd src/VehicleRental.Wasm
dotnet run
```

開啟瀏覽器：`https://localhost:5001`

預期行為：
1. 首頁顯示電競風格 Landing Page
2. 點「立即租車」→ 導向登入頁（未登入保護）
3. 點「立即註冊」→ 完成帳號建立
4. 登入 → 車型清單（4 款 Seed 車型）
5. 選車 → 選日期 → 即時顯示租金 → 確認送出
6. 顯示預約確認頁，含預約編號

### A7. 執行測試

```powershell
dotnet test --verbosity normal
```

**預期通過的測試（MVP P1）**：
- `RegisterCustomerCommandHandler_Should_HashPassword_And_Save`
- `RegisterCustomerCommandHandler_Should_RejectDuplicateEmail`
- `CalculateRentalCostQuery_Should_Return_Correct_Cost`
- `CreateReservationCommandHandler_Should_Detect_TimeConflict`
- `LoginPage_Should_ShowError_WhenCredentialsInvalid` （bUnit）
- `VehicleTypesPage_Should_Redirect_WhenNotAuthenticated` （bUnit）

---

## Part B：部署至 GitHub Pages（完整教學）

> 本節針對完全不熟悉 GitHub Pages 的用戶，請逐步操作。

### B1. 在 GitHub 建立 Repository

1. 前往 https://github.com → 右上角「**+**」→「**New repository**」
2. 填寫：
   - **Repository name**：`NexVehicleRental`（記住此名稱，後面會用到）
   - **Visibility**：選 **`Public`**（免費 GitHub Pages 需要 Public）
   - ❌ 不要勾選「Add a README file」
3. 點擊「**Create repository**」
4. 複製頁面顯示的 repository URL，格式：  
   `https://github.com/YOUR_USERNAME/NexVehicleRental.git`

### B2. 推送本地程式碼至 GitHub

```powershell
cd c:\develop\NexVehicleRental

# 若 Git 尚未初始化
git init
git add .
git commit -m "feat: Blazor WASM vehicle rental system"

# 連結至 GitHub（替換 YOUR_USERNAME）
git remote add origin https://github.com/YOUR_USERNAME/NexVehicleRental.git
git branch -M main
git push -u origin main
```

### B3. 修改 `<base href>`（關鍵步驟）

⚠️ **這是最常被遺忘、導致部署失敗的步驟。**

GitHub Pages 的 URL 格式為：`https://YOUR_USERNAME.github.io/NexVehicleRental/`  
因此 `<base href>` 必須是 `/NexVehicleRental/`（斜線都要有）。

編輯 `src/VehicleRental.Wasm/wwwroot/index.html`，將：
```html
<base href="/" />
```
改為（把 `NexVehicleRental` 替換為您的 repository 名稱）：
```html
<base href="/NexVehicleRental/" />
```

提交並推送：
```powershell
git add src/VehicleRental.Wasm/wwwroot/index.html
git commit -m "fix: set base href for GitHub Pages deployment"
git push
```

### B4. 建立 GitHub Actions 部署工作流程

建立檔案 `.github/workflows/deploy.yml`：

```yaml
name: Deploy Blazor WASM to GitHub Pages

on:
  push:
    branches: [main]
  workflow_dispatch:

permissions:
  contents: read
  pages: write
  id-token: write

concurrency:
  group: "pages"
  cancel-in-progress: false

jobs:
  build-and-deploy:
    environment:
      name: github-pages
      url: ${{ steps.deployment.outputs.page_url }}
    runs-on: ubuntu-latest

    steps:
      - name: Checkout repository
        uses: actions/checkout@v4

      - name: Setup .NET 10 SDK
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore NuGet packages
        run: dotnet restore

      - name: Publish Blazor WASM (Release mode)
        run: |
          dotnet publish src/VehicleRental.Wasm/VehicleRental.Wasm.csproj \
            -c Release \
            -o publish \
            --nologo

      - name: Ensure .nojekyll file exists
        run: touch publish/wwwroot/.nojekyll

      - name: Upload GitHub Pages artifact
        uses: actions/upload-pages-artifact@v3
        with:
          path: publish/wwwroot

      - name: Deploy to GitHub Pages
        id: deployment
        uses: actions/deploy-pages@v4
```

提交：
```powershell
git add .github/workflows/deploy.yml
git commit -m "ci: GitHub Pages deployment workflow"
git push
```

### B5. 在 GitHub 上啟用 Pages（一次性設定）

1. 前往您的 GitHub repository 頁面
2. 點擊上方選單「**Settings**」（齒輪圖示）
3. 在左側側欄滾動找到「**Pages**」→ 點擊
4. 在「**Build and deployment**」區塊：
   - **Source**：選擇「**GitHub Actions**」
5. 設定會自動儲存

> 選「GitHub Actions」是關鍵，不是「Deploy from a branch」。

### B6. 觀察自動部署流程

1. 回到 repository 首頁
2. 點擊上方「**Actions**」標籤（火箭圖示）
3. 您會看到「Deploy Blazor WASM to GitHub Pages」工作流程正在執行
4. 點進去查看每個步驟：
   - ✅ Checkout repository
   - ✅ Setup .NET 10 SDK
   - ✅ Restore NuGet packages
   - ✅ Publish Blazor WASM
   - ✅ Deploy to GitHub Pages
5. 全部綠色 ✅ 後，點「Deploy to GitHub Pages」步驟查看網站 URL

### B7. 驗證部署結果

開啟 `https://YOUR_USERNAME.github.io/NexVehicleRental/`

✅ 應看到：
- 電競風格首頁（深黑背景 + 螢光綠）
- 路由守衛正常（點租車按鈕跳至登入）
- 可完整走完：註冊 → 登入 → 選車 → 預約 → 確認頁

### B8. 日後更新流程

每次推送到 `main` 分支，GitHub Actions 自動重新部署，約 2–3 分鐘完成：

```powershell
git add .
git commit -m "feat: 更新描述"
git push    # 推送後自動觸發部署，無需手動操作
```

---

## 部署架構示意圖

```
本地開發
┌────────────────────┐
│  dotnet run        │
│  localhost:5001    │
└────────┬───────────┘
         │ git push
         ▼
GitHub Repository (main)
┌────────────────────────────────┐
│  .github/workflows/deploy.yml  │
│  觸發 GitHub Actions Runner    │
│                                │
│  1. dotnet publish →           │
│     publish/wwwroot/           │
│     (index.html, *.wasm, *.js) │
│  2. upload-pages-artifact      │
│  3. deploy-pages               │
└────────────────────────────────┘
         │
         ▼
GitHub Pages CDN
┌────────────────────────────────┐
│  https://USER.github.io/       │
│  NexVehicleRental/             │
│  （全靜態，永久免費）           │
└────────────────────────────────┘
         │ 使用者開啟網頁
         ▼
使用者瀏覽器
┌────────────────────────────────┐
│  .NET WASM 執行環境載入        │
│  C# 程式碼在瀏覽器中執行       │
│  資料存於 localStorage         │
│  無伺服器依賴                  │
└────────────────────────────────┘
```

---

## 常見問題排解

| 問題 | 原因 | 解法 |
|------|------|------|
| 空白頁面，Console 有 404 | `<base href>` 錯誤 | 確認 `<base href="/NexVehicleRental/" />` 與 repo 名稱一致 |
| 直接輸入路由顯示 404 | GitHub Pages 不支援 SPA 路由 | 確認 `wwwroot/404.html` 存在（詳見 A5）|
| GitHub Actions 失敗「Permission denied」| 未啟用 Pages | 依 B5 在 Settings → Pages 選「GitHub Actions」|
| CSS/WASM MIME 錯誤 | Jekyll 處理了靜態檔 | 確認 `.nojekyll` 檔案存在（`deploy.yml` 步驟自動建立）|
| 登入後資料不見 | 預期行為：localStorage 各設備獨立 | 此為本地沙盒 Demo 模式，無跨裝置同步 |


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
