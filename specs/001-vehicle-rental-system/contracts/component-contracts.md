# Blazor 元件合約：車輛租用系統（WASM 版）

**功能分支**: `001-vehicle-rental-system`  
**日期**: 2026-04-28  
**架構**: Blazor WebAssembly（取代原 MVC HTTP 路由合約）  
**輸入**: data-model.md + spec.md + research.md

> **注意**：本文件取代原 `http-routes.md`。  
> Blazor WASM 無 HTTP Controller，改由 Blazor Router 處理頁面導航，表單提交由 `EditForm` + CQRS Command 處理。

---

## 路由總覽

| 路由路徑 | 元件 | 需登入 | 說明 |
|---------|------|--------|------|
| `/` | `Pages/Home.razor` | 否 | 首頁（Landing page，含「立即租車」CTA）|
| `/register` | `Pages/Register.razor` | 否 | 帳戶註冊頁 |
| `/login` | `Pages/Login.razor` | 否 | 帳戶登入頁 |
| `/vehicles` | `Pages/VehicleTypes.razor` | ✅ | 車型清單頁 |
| `/reservation/create/{vehicleTypeId:int}` | `Pages/CreateReservation.razor` | ✅ | 租用時間選擇 + 租金計算 |
| `/reservation/confirmation/{reservationNumber}` | `Pages/ReservationConfirmation.razor` | ✅ | 預約確認頁（含預約編號）|
| `/reservation/my` | `Pages/MyReservations.razor` | ✅ | 我的預約清單（P2）|

**路由守衛設定（`App.razor`）**：
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

---

## 元件合約

### `Pages/Register.razor`

**路由**：`@page "/register"`  
**需要認證**：否  
**Model**：`RegisterModel`（`EditForm` 的綁定物件）

```csharp
public class RegisterModel
{
    [Required(ErrorMessage = "請輸入姓名")]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入 Email")]
    [EmailAddress(ErrorMessage = "請輸入有效的 Email 格式")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Compare(nameof(Password), ErrorMessage = "密碼與確認密碼不符")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

**送出流程**：
1. `EditForm` 觸發 `OnValidSubmit`
2. 呼叫 `mediator.Send(new RegisterCustomerCommand { FullName, Email, Password })`
3. 成功 → `NavigationManager.NavigateTo("/login?registered=true")`
4. Email 重複 → 顯示驗證訊息「此 Email 已被使用」
5. 顯示 `<ValidationSummary />` 及個別欄位 `<ValidationMessage />`

---

### `Pages/Login.razor`

**路由**：`@page "/login"`  
**需要認證**：否  
**Model**：`LoginModel`

```csharp
public class LoginModel
{
    [Required(ErrorMessage = "請輸入 Email")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;
}
```

**送出流程**：
1. 呼叫 `BrowserAuthenticationStateProvider.LoginAsync(email, password, rememberMe)`
2. 成功 → `NavigationManager.NavigateTo("/vehicles")`
3. 失敗 → 顯示警告「帳號或密碼錯誤」（不揭露哪項錯誤，符合 spec AC-3）
4. 若帶有 `?registered=true` query 參數 → 顯示綠色成功訊息「註冊成功，請登入」

---

### `Pages/VehicleTypes.razor`

**路由**：`@page "/vehicles"`  
**需要認證**：✅ `@attribute [Authorize]`  
**資料來源**：`mediator.Send(new GetVehicleTypesQuery())`

**輸出格式**：

```csharp
// GetVehicleTypesQuery → IEnumerable<VehicleTypeDto>
public class VehicleTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal DailyRate { get; set; }
    public bool IsAvailable { get; set; }
    public string? ImageUrl { get; set; }
}
```

**UI 行為**：
- 可用車型：顯示「選擇此車」按鈕 → `NavigationManager.NavigateTo($"/reservation/create/{vehicle.Id}")`
- 不可用車型：顯示停用的「目前無可用」按鈕
- 無車型：顯示「目前無可用車輛，請稍後再試」

---

### `Pages/CreateReservation.razor`

**路由**：`@page "/reservation/create/{VehicleTypeId:int}"`  
**需要認證**：✅ `@attribute [Authorize]`  
**Route Parameter**：`[Parameter] public int VehicleTypeId { get; set; }`  
**Model**：`CreateReservationModel`

```csharp
public class CreateReservationModel
{
    [Required]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
}
```

**即時租金計算**：
- `StartDate` 或 `EndDate` 任一變更時，觸發 `OnDateChanged()`
- 呼叫 `mediator.Send(new CalculateRentalCostQuery { VehicleTypeId, StartDate, EndDate })`
- 回傳 `CalculateRentalCostResult { decimal TotalCost, int Days, bool IsValid, string? ErrorMessage }`
- 於表單中即時顯示費用（無頁面重整）

**送出流程**：
1. 驗證通過後，呼叫 `mediator.Send(new CreateReservationCommand { ... })`
2. 成功 → `NavigationManager.NavigateTo($"/reservation/confirmation/{reservationNumber}")`
3. 時段衝突 → 顯示錯誤「此時段車輛已被預約，請選擇其他日期」
4. 日期驗證失敗 → `<ValidationMessage />` 顯示對應錯誤

---

### `Pages/ReservationConfirmation.razor`

**路由**：`@page "/reservation/confirmation/{ReservationNumber}"`  
**需要認證**：✅ `@attribute [Authorize]`  
**Route Parameter**：`[Parameter] public string ReservationNumber { get; set; } = string.Empty;`

**顯示內容**（從 `GetMyReservationsQuery` 或快取取得）：
- 預約編號（如 `RES-20260428-0001`）
- 車型名稱
- 租用日期範圍
- 總租金
- 「查看我的預約」按鈕

---

### `Pages/MyReservations.razor`（P2）

**路由**：`@page "/reservation/my"`  
**需要認證**：✅ `@attribute [Authorize]`  
**資料來源**：`mediator.Send(new GetMyReservationsQuery { CustomerId = currentUser.Id })`

```csharp
// GetMyReservationsQuery → IEnumerable<ReservationSummaryDto>
public class ReservationSummaryDto
{
    public int Id { get; set; }
    public string ReservationNumber { get; set; } = string.Empty;
    public string VehicleTypeName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal TotalCost { get; set; }
    public ReservationStatus Status { get; set; }
    public bool CanCancel { get; set; }  // StartDate > Today
}
```

**取消流程**：
1. 點擊「取消預約」→ 顯示確認對話框
2. 確認後 → `mediator.Send(new CancelReservationCommand { ReservationId, CustomerId })`
3. 成功 → 列表重新整理，顯示「預約已取消」

---

## 共用元件

| 元件 | 說明 |
|------|------|
| `Shared/MainLayout.razor` | 頁面主框架（含 NavMenu 與電競主題 CSS）|
| `Shared/NavMenu.razor` | 導航列（已登入顯示用戶名；未登入顯示登入/註冊連結）|
| `Shared/RedirectToLogin.razor` | 自動重導向未登入用戶至 `/login` |
| `Shared/AlertMessage.razor` | 顯示成功/警告/錯誤訊息的通用元件 |
| `Components/DateRangePicker.razor` | 日期範圍選擇器（含即時租金計算觸發）|

---

## 認證狀態 API（BrowserAuthenticationStateProvider）

```csharp
public interface IBrowserAuthService
{
    Task<bool> LoginAsync(string email, string password, bool rememberMe);
    Task LogoutAsync();
    Task<ClaimsPrincipal> GetCurrentUserAsync();
    bool IsAuthenticated { get; }
    int? CurrentCustomerId { get; }
    string? CurrentCustomerName { get; }
}
```

**Claim 結構**（登入成功後建立）：
```csharp
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString()),
    new Claim(ClaimTypes.Name, customer.FullName),
    new Claim(ClaimTypes.Email, customer.Email),
};
```
