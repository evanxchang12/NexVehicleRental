# HTTP 路由與 MVC 合約：車輛租用系統

**功能分支**: `001-vehicle-rental-system`  
**日期**: 2026-04-27  
**輸入**: data-model.md + spec.md + research.md

---

## 路由總覽

| HTTP 方法 | 路由 | Controller | Action | 需登入 | 說明 |
|-----------|------|------------|--------|--------|------|
| GET | `/` | `HomeController` | `Index` | 否 | 首頁（導向車型清單或登入）|
| GET | `/Account/Register` | `AccountController` | `Register` | 否 | 顯示註冊表單 |
| POST | `/Account/Register` | `AccountController` | `Register` | 否 | 送出註冊資料 |
| GET | `/Account/Login` | `AccountController` | `Login` | 否 | 顯示登入表單 |
| POST | `/Account/Login` | `AccountController` | `Login` | 否 | 送出登入資料 |
| POST | `/Account/Logout` | `AccountController` | `Logout` | 是 | 登出並清除 Cookie |
| GET | `/Reservation` | `ReservationController` | `Index` | 是 | 車型清單頁 |
| GET | `/Reservation/Create/{vehicleTypeId}` | `ReservationController` | `Create` | 是 | 選擇租用時間與計算租金 |
| POST | `/Reservation/Create` | `ReservationController` | `Create` | 是 | 送出預約（建立預約）|
| GET | `/Reservation/Confirmation/{id}` | `ReservationController` | `Confirmation` | 是 | 預約確認頁（顯示預約編號）|
| GET | `/Reservation/My` | `ReservationController` | `My` | 是 | 我的預約清單（P2）|
| POST | `/Reservation/Cancel/{id}` | `ReservationController` | `Cancel` | 是 | 取消預約（P2）|
| GET | `/Reservation/CalculateCost` | `ReservationController` | `CalculateCost` | 是 | AJAX：計算租金（回傳 JSON）|

---

## AccountController 合約

### GET /Account/Register → View: `Account/Register.cshtml`

**輸出 ViewModel**：`RegisterViewModel`
```csharp
public class RegisterViewModel
{
    [Required(ErrorMessage = "請輸入姓名")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "姓名需為 2~100 字元")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入 Email")]
    [EmailAddress(ErrorMessage = "請輸入有效的 Email 格式")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼需至少 6 字元")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "密碼與確認密碼不符")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

### POST /Account/Register

**成功**：重導向 `GET /Account/Login`，帶 TempData 訊息「註冊成功，請登入」。  
**Email 重複**：ModelState 加入錯誤「此 Email 已被使用」，重新顯示表單。  
**驗證失敗**：重新顯示表單（ModelState 自動顯示錯誤）。

**Command 呼叫**：`mediator.Send(new RegisterCustomerCommand { FullName, Email, Password })`

---

### GET /Account/Login → View: `Account/Login.cshtml`

**輸出 ViewModel**：`LoginViewModel`
```csharp
public class LoginViewModel
{
    [Required(ErrorMessage = "請輸入 Email")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "請輸入密碼")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
```

### POST /Account/Login

**成功**：建立認證 Cookie，重導向 `ReturnUrl`（驗證後）或 `/Reservation`。  
**失敗**：ModelState 加入「帳號或密碼錯誤」，重新顯示表單。

**Command 呼叫**：`mediator.Send(new LoginCustomerCommand { Email, Password })`  
**回傳值**：`LoginResult { bool Success, int? CustomerId, string? FullName }`

---

## ReservationController 合約

### GET /Reservation → View: `Reservation/Index.cshtml`

**Query 呼叫**：`mediator.Send(new GetVehicleTypesQuery())`  
**輸出 ViewModel**：`VehicleTypeListViewModel`
```csharp
public class VehicleTypeListViewModel
{
    public IEnumerable<VehicleTypeDto> VehicleTypes { get; set; } = [];
}

public record VehicleTypeDto(int Id, string Name, string Description, decimal DailyRate, bool IsAvailable, string? ImageUrl);
```

---

### GET /Reservation/Create/{vehicleTypeId} → View: `Reservation/Create.cshtml`

**路由參數**：`vehicleTypeId` (int)  
**Query 呼叫**：載入指定車型資訊。  
**輸出 ViewModel**：`CreateReservationViewModel`
```csharp
public class CreateReservationViewModel
{
    public int VehicleTypeId { get; set; }
    public string VehicleTypeName { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }

    [Required(ErrorMessage = "請選擇起始日期")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "請選擇結束日期")]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; }

    public decimal? EstimatedCost { get; set; }    // 由 JS 呼叫 CalculateCost 填入
}
```

### POST /Reservation/Create

**成功**：重導向 `GET /Reservation/Confirmation/{id}`。  
**時段衝突**：ModelState 加入「此時段車輛已被預約，請選擇其他時間」。  
**驗證失敗**：重新顯示表單。

**Command 呼叫**：
```csharp
mediator.Send(new CreateReservationCommand
{
    CustomerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
    VehicleTypeId, StartDate, EndDate
})
```
**回傳值**：`CreateReservationResult { bool Success, int ReservationId, string? Error }`

---

### GET /Reservation/CalculateCost（AJAX）

**查詢字串**：`?vehicleTypeId=1&startDate=2026-05-01&endDate=2026-05-04`  
**回傳 JSON**：
```json
{ "totalCost": 5400.00, "days": 3, "dailyRate": 1800.00 }
```
**Query 呼叫**：`mediator.Send(new CalculateRentalCostQuery { VehicleTypeId, StartDate, EndDate })`

---

### GET /Reservation/My → View: `Reservation/My.cshtml`（P2）

**Query 呼叫**：`mediator.Send(new GetMyReservationsQuery { CustomerId })`  
**輸出 ViewModel**：`MyReservationsViewModel`
```csharp
public record ReservationSummaryDto(int Id, string ReservationNumber, string VehicleTypeName, DateOnly StartDate, DateOnly EndDate, decimal TotalCost, string Status, bool CanCancel);
```

---

### POST /Reservation/Cancel/{id}（P2）

**Command 呼叫**：`mediator.Send(new CancelReservationCommand { ReservationId, CustomerId })`  
**成功**：重導向 `/Reservation/My`，帶 TempData「預約已取消」。  
**無法取消**：重導向 `/Reservation/My`，帶 TempData 錯誤訊息。

---

## 認證 Claim 結構

登入成功後，Cookie 中的 Claims：

| Claim Type | 值 |
|------------|-----|
| `ClaimTypes.NameIdentifier` | Customer.Id（字串）|
| `ClaimTypes.Name` | Customer.FullName |
| `ClaimTypes.Email` | Customer.Email |

---

## 未登入導向行為

所有標記 `[Authorize]` 的 Action，未登入時自動重導向：
```
GET /Account/Login?ReturnUrl=%2FReservation
```
設定於 `Program.cs`：
```csharp
options.LoginPath = "/Account/Login";
options.AccessDeniedPath = "/Account/Login";
```
