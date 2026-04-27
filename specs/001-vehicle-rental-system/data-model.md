# 資料模型：車輛租用系統

**功能分支**: `001-vehicle-rental-system`  
**日期**: 2026-04-27  
**輸入**: research.md + spec.md

---

## 實體定義

### 1. Customer（租車用戶）

**說明**：系統中的租車用戶帳戶。

| 欄位 | 型別 | 說明 | 驗證規則 |
|------|------|------|----------|
| `Id` | `int` (PK) | 自動遞增主鍵 | - |
| `FullName` | `string` | 用戶全名 | 必填；長度 2–100 字元 |
| `Email` | `string` | Email（登入帳號）| 必填；符合 Email 格式；系統唯一 |
| `PasswordHash` | `string` | 密碼雜湊（PBKDF2）| 必填；不明文儲存 |
| `CreatedAt` | `DateTimeOffset` | 帳戶建立時間 | 系統自動設定 |

**關聯**：一個 Customer 可有多筆 Reservation（1:N）。

**EF Core 設定**：
```csharp
entity.HasIndex(e => e.Email).IsUnique();
entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
entity.Property(e => e.PasswordHash).IsRequired();
```

---

### 2. VehicleType（車型）

**說明**：可供租用的車輛類型。

| 欄位 | 型別 | 說明 | 驗證規則 |
|------|------|------|----------|
| `Id` | `int` (PK) | 自動遞增主鍵 | - |
| `Name` | `string` | 車型名稱（如「Toyota Camry」）| 必填；長度 2–100 字元 |
| `Description` | `string` | 車型描述 | 可選；最長 500 字元 |
| `DailyRate` | `decimal(18,2)` | 每日租金（新台幣）| 必填；>0 |
| `IsAvailable` | `bool` | 是否可租用 | 必填；預設 `true` |
| `ImageUrl` | `string?` | 車型圖片 URL | 可選 |

**關聯**：一個 VehicleType 可有多筆 Reservation（1:N）。

**EF Core 設定**：
```csharp
entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
entity.Property(e => e.Description).HasMaxLength(500);
entity.Property(e => e.DailyRate).HasColumnType("decimal(18,2)").IsRequired();
```

**Seed Data（初始車型）**：
```csharp
new VehicleType { Id=1, Name="Toyota Camry 2024", Description="舒適四門轎車，適合商務出行", DailyRate=1800, IsAvailable=true },
new VehicleType { Id=2, Name="Mazda CX-5 2024",  Description="時尚休旅車，空間寬敞",        DailyRate=2200, IsAvailable=true },
new VehicleType { Id=3, Name="BMW 3 Series 2024", Description="豪華性能轎車，駕駛快感十足",  DailyRate=3500, IsAvailable=true },
new VehicleType { Id=4, Name="Toyota Hiace Van",  Description="商用廂型車，載貨載人皆宜",   DailyRate=2500, IsAvailable=true },
```

---

### 3. Reservation（預約）

**說明**：租車預約記錄，連結用戶與車型，記錄租用時間與費用。

| 欄位 | 型別 | 說明 | 驗證規則 |
|------|------|------|----------|
| `Id` | `int` (PK) | 自動遞增主鍵 | - |
| `ReservationNumber` | `string` | 唯一預約編號（如 `RES-20260427-0001`）| 系統自動產生；唯一 |
| `CustomerId` | `int` (FK) | 租車用戶 ID | 必填 |
| `VehicleTypeId` | `int` (FK) | 車型 ID | 必填 |
| `StartDate` | `DateOnly` | 租用起始日期 | 必填；≥ 今日 |
| `EndDate` | `DateOnly` | 租用結束日期 | 必填；> StartDate |
| `TotalCost` | `decimal(18,2)` | 總租金（DailyRate × 天數）| 系統計算；>0 |
| `Status` | `ReservationStatus` (enum) | 預約狀態 | 必填；預設 `Confirmed` |
| `CreatedAt` | `DateTimeOffset` | 建立時間 | 系統自動設定 |

**預約狀態枚舉（ReservationStatus）**：
```csharp
public enum ReservationStatus
{
    Confirmed = 1,   // 已確認（預設）
    Cancelled = 2    // 已取消
}
```

**關聯**：
```csharp
entity.HasOne(e => e.Customer)
      .WithMany(c => c.Reservations)
      .HasForeignKey(e => e.CustomerId)
      .OnDelete(DeleteBehavior.Restrict);

entity.HasOne(e => e.VehicleType)
      .WithMany(v => v.Reservations)
      .HasForeignKey(e => e.VehicleTypeId)
      .OnDelete(DeleteBehavior.Restrict);

entity.HasIndex(e => e.ReservationNumber).IsUnique();
```

**業務規則**：
- `EndDate` 必須嚴格大於 `StartDate`（至少 1 天）。
- 建立預約前必須檢查：同一 `VehicleTypeId` 在 `[StartDate, EndDate)` 區間內不存在其他 `Confirmed` 狀態的預約（時段衝突）。
- `TotalCost = DailyRate × (EndDate - StartDate).Days`
- `ReservationNumber` 格式：`RES-{yyyyMMdd}-{序號4碼}`（序號為當天的自增數）。

---

## 狀態轉換

```
Confirmed ──[取消]──> Cancelled
```
- 只有 `StartDate > DateOnly.FromDateTime(DateTime.UtcNow.Date)` 的預約可以被取消。

---

## ERD（文字格式）

```
Customer (1) ──── (N) Reservation (N) ──── (1) VehicleType
```

---

## 資料庫 DbContext

**名稱**：`AppDbContext : DbContext`

| DbSet 屬性 | 對應實體 |
|------------|---------|
| `Customers` | `Customer` |
| `VehicleTypes` | `VehicleType` |
| `Reservations` | `Reservation` |
