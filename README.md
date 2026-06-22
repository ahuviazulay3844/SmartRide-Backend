# CityCar — מערכת השכרת רכבים

מערכת API לניהול שירות השכרת רכבים עירוני, מבוססת .NET 9 ו-ASP.NET Core.  
הפרויקט מדמה שירות השכרה לפי דקה/שעה (Car2Go / Gett Car): משתמש מזמין רכב קרוב, פותח אותו מהאפליקציה, נוסע, מדווח מצב ומסיים — והמערכת מחשבת מחיר, מנהלת קונפליקטים ומעדכנת את הצי ברקע.

---

## מהות הפרויקט

**למשתמשים (`user`):** הרשמה עם אימות מייל, התחברות, הזמנת רכב לפי מיקום וזמינות, פתיחה/נעילה, בדיקת מצב לפני נסיעה, מעקב קילומטראז', תדלוק, סיום, משוב, קופונים והארכת הזמנה.

**למנהלים (`admin`):** CRUD מלא על משתמשים, רכבים, אזורים וקופונים; דוחות הכנסות והזמנות; תחזוקת צי; חסימת משתמשים.

**תרחישים מורכבים שהמערכת מטפלת בהם:** חפיפת הזמנות (משתמש ורכב), איחור בהחזרה, הצעת רכב חלופי, בuffer הכנה בין נסיעות, חישוב מחיר דינמי, דירוג והנחות, הגבלת שבת, הצפנת נתונים רגישים, וסימולציית נסיעה ב-Worker.

---

## ארכיטקטורה

הפרויקט בנוי ב-**Layered Architecture** — כל שכבה אחראית על תחום אחד, והתקשורת זורמת מלמעלה למטה:

| # | שכבה | אחריות |
|---|------|--------|
| 1 | `Project` | קבלת בקשות HTTP, Middleware, JWT, CORS, Swagger |
| 2 | `Service` | לוגיקה עסקית — אימותים, חישובים, תהליכים |
| 3 | `Repository` | גישה לנתונים — CRUD על DbSet |
| 4 | `DataContext` | DbContext, יחסי ישויות, מיגרציות |
| 5 | `Common` | DTOs — הפרדה בין API לישויות DB |

```
Client (Frontend / Swagger)
        │
        ▼
   Project (API) ──► Controllers דקים
        │
        ▼
   Service ──► UserService, CarService, OrderService ...
        │
        ▼
   Repository ──► IRepository<T>
        │
        ▼
   DataContext (CityCarDb) ──► SQL Server

CityCar.Worker ──► OrderTrackingService (ללא HTTP, כל 60 שנ')
```

**עקרון מרכזי:** Controller מקבל DTO, קורא ל-Service, ומחזיר תשובת HTTP. Entities לא יוצאות החוצה — AutoMapper ממיר בין Entity ל-DTO.

---

## טכנולוגיות

- ASP.NET Core Web API (.NET 9)
- Entity Framework Core 9 + SQL Server
- JWT Bearer Authentication
- Swagger / OpenAPI
- AutoMapper
- BCrypt (סיסמאות)
- AES — `EncryptionService` (רישיון, דרכון, אשראי)
- SMTP / Gmail — `EmailService`
- `MemoryCache` — קודי הרשמה ואיפוס סיסמה
- `BackgroundService` — `CityCar.Worker`

---

## מבנה ה-Solution

```
CityCar-Project/
├── Project/          ← Web API (נקודת כניסה)
├── Service/          ← לוגיקה עסקית
├── Repository/       ← Repositories + Entities
├── DataContext/      ← DbContext + Migrations
├── Common/           ← DTOs
└── Project.sln

CityCar.Worker/       ← עובד רקע (תיקייה אחות, מחוץ ל-repo)
```

| פרויקט | תפקיד |
|--------|--------|
| `Project` | Controllers, `Program.cs`, DI, Swagger |
| `Service` | שירותי דומיין, Email, Encryption, Mapper |
| `Repository` | `IRepository<T>` + מחלקות Entity |
| `DataContext` | `CityCarDb`, מיגרציות EF |
| `Common` | DTOs |
| `CityCar.Worker` | הרצת `OrderTrackingService` ברקע |

---

## מודל דומיין

### ישויות

| ישות | שדות / לוגיקה עיקרית |
|------|----------------------|
| **User** | פרטים אישיים, רישיון/דרכון (מוצפן), אשראי (מוצפן), `UserType`, `UserRank`, `AccountBalance`, `IsBlocked`, קוד איפוס סיסמה |
| **Car** | מודל, רישוי, `CarCategory`, `CarStatus`, GPS, תמחור (שעה/יום/ק"מ), דלק, ק"מ, נעילה, תחזוקה, `IsPopular` (>20 הזמנות) |
| **Order** | זמנים, מחיר (בסיס/איחור/סה"כ), סטטוס, ק"מ, ביטוח, קונפлיקט + רכב מוצע, קופון, `IsReassigned` |
| **Region** | שם, מרכז גיאוגרафי |
| **Coupon** | קוד, סכום/אחוז, תוקף, שיוך למשתמש |
| **CarFeedback** | דירוג 1–5, הערה, דיווח תקלה — קשור להזמנה (1:1) |
| **CarInspection** | ניקיון, מיזוג, נזקים, צמיג — לפני תחילת נסיעה |

### Enums

| Enum | ערכים |
|------|--------|
| `UserType` | `user`, `admin` |
| `UserRank` | `Regular` → `Bronze` (10) → `Silver` (20) → `Gold` (30) → `PurpleBadge` (50) |
| `CarStatus` | `Available`, `PartiallyBooked`, `Occupied`, `Maintenance` |
| `CarCategory` | `Mini`, `Family`, `Large`, `Commercial`, `Luxury` |
| `OrderStatus` | `Pending`, `Active`, `Completed`, `Canceled` |
| `PricingType` | `ByHour`, `ByDay` |
| `DiscountType` | `Amount`, `Percentage` |

---

## אימות והרשאות

- **JWT** — Header: `Authorization: Bearer {token}`. Claims: `NameIdentifier` (UserId), `Role`.
- **Login** מחזיר קוד סטטוס: 200 (הצלחה), 404 (משתמש לא קיים), 401 (סיסמה שגויה), 403 (חסום).
- **Swagger** — Bearer Security Scheme; ב-Development נגיש ב-root (`RoutePrefix = ""`).
- **CORS** — פתוח לכל Origin (Development).
- משתמש רואה רק הזמנות שלו; Admin רואה הכל.

---

## לוגיקה עסקית — סקירה

### מחזור חיי הזמנה

```
Pending ──► Active ──► Completed
   │            │
   └─ Canceled  └─ איחור: LateFee מצטבר, ההזמנה נשארת Active (לא נסגרת אוטומטית)
```

1. **יצירה** — ולידציות: רכב פנוי, אין חפיפה למשתמש/רכב, לא בשבת, משתמש לא חסום, רכב לא בתחזוקה → חישוב `BasePrice` → `Pending` → מייל אישור → רכב עובר ל-`PartiallyBooked`.
2. **הפעלה** — Worker מעביר ל-`Active` כש-`StartTime` הגיע (אם אין קונפליקט/תחזוקה).
3. **נסיעה** — `UnlockCar` / `LockCar`, עדכון ק"מ (API או Worker), `ReportRefuel`, `submit-start-report` (CarInspection).
4. **סיום** — `FinishOrder`: חישוב סופי, ניכוי מיתרה, עדכון דירוג, רכב חוזר ל-`Available`.

### Buffer של 15 דקות

בכל המערכת (CarService, OrderService, OrderTrackingService) מוגדר **buffer של 15 דקות** אחרי כל הזמנה — זמן הכנה/ניקוי בין נהג לנהג.  
בדיקות זמינות, חפיפות וחיפוש רכב חלופי מתחשבות ב-`ExpectedEndTime + 15min` (ובנסיעה פעילה באיחור — ב-`DateTime.Now`).

### חישוב מחיר

| שלב | חישוב |
|-----|--------|
| **בסיס (יצירה)** | `TotalDays × PricePerDay` + `TotalHours × PricePerHour`. אם שעות עולות על מחיר יום — חיוב יומי. ברירת מחדל: שעה אחת. |
| **ביטוח** | +50₪/יום או +3₪/שעה (`WantsInsuranceUpgrade`) |
| **בסיום** | +1.5₪/ק"מ, +1₪/דקת איחור, +50₪ לנהג חדש (גיל < 24) |
| **הנחות** | Gold 10%, PurpleBadge 15%, קופון, `DiscountAmount`, יתרה ב-`AccountBalance` |
| **תדלוק** | בונוס עד 2× `PricePerHour` ליתרה |

### זמינות רכב (`CarService`)

- **Haversine** — חישוב מרחק אווירי (ק"מ) בין משתמש לרכב; מיון לפי קרבה ב-`GetAllClosest`.
- **GetDetailedAvailabilityStatus** — בודק חפיפות ביומן (+ buffer), מחזיר `Available` / `PartiallyBooked` / `Occupied` / `Maintenance`.
- **PartiallyBooked** — יש חלון פנוי בתחילת/סוף טווח המבוקש (פער ≥ 60 דק' בהתחלה).
- **IsCarFitForRoad** — רכב כשיר: לא בתחזוקה, דלק ≥ 15%.
- **אזור זמן** — המרות ל-`Israel Standard Time` בחיפוש לפי תאריך.

### קונפליקטים ורכב חלופי (`OrderService`)

**מתי נוצר קונפליקט:** נהג קודם מחזיר באיחור (`Active` + `ExpectedEndTime` עבר) והזמנה `Pending` הבאה ממתינה על אותו רכב.

**`ProcessLateCustomerConflict`:**
1. מסמן `HasConflict = true`, `ConflictReason = "LateDriver"`.
2. בונה רשימת רכבים תפוסים ביומן (עם buffer).
3. מחפש רכב חלופי: לא אותו רכב, לא בתחזוקה, ≥ מקומות, פנוי ביומן, **≤ 10 ק"מ** (Haversine).
4. שומר הצעה: `SuggestedReplacementCarId`, מודל, מיקום, מקומות, דלק, `DiscountAmount = PricePerHour` (שעה חינם).

**`ConfirmReplacement`:**
- **מקבל** — מבטל הזמנה ישנה (חינם), יוצר הזמנה חדשה על הרכב החלופי (`Active` מיידית), מחיל הנחה.
- **דוחה** — מבטל + 20₪ פיצוי ל-`AccountBalance`.

**`UnlockCar`** — חוסם פתיחה אם נהג קודם עדיין `Active` על אותו רכב.

### הגבלת שבת

`IsTimeInShabbat`: שישי מ-16:00, שבת עד 20:00 — יצירת הזמנה נדחית.

### דירוג משתמשים

| דירוג | הזמנות שהושלמו | הטבה |
|--------|----------------|------|
| Bronze | 10+ | — |
| Silver | 20+ | — |
| Gold | 30+ | 10% הנחה |
| PurpleBadge | 50+ | 15% הנחה |

---

## Project — שכבת API

### `Program.cs`

- רישום Controllers עם JSON **camelCase**.
- DI: `IContext` → `CityCarDb`, `AddServices()`, `OrderTrackingService`, `MemoryCache`.
- JWT (Issuer, Audience, Key), Authentication + Authorization.
- Swagger עם Bearer; CORS פתוח.
- Pipeline: HTTPS → CORS → Auth → Controllers.

### Controllers

| Controller | לוגיקה / Endpoints |
|------------|-------------------|
| **UsersController** | `login`, `register`, CRUD (admin), `change-password`, `toggle-block`, `current`, `forgot-password`, `reset-password`, `request-registration-code`, `verify-registration-code`, חיפוש לפי אימייל |
| **CarsController** | CRUD (admin), `closest` (lat/lng + טווח), `available`, `check-suitability`, `popular`, `needs-fuel`, עדכון דלק/ק"מ, `is-fit`, סטטוס, תחזוקה, `toggle-lock`, לפי אזור, `extended-status` |
| **OrdersController** | CRUD, `unlock`/`lock`, `finish`, `update-progress`, דוחות (count/revenue/active), ביטול/תשלום, חיפושים, `submit-start-report`, `check-user-overlap`, `extend`, `confirm-replacement`, `report-refuel` |
| **CouponsController** | CRUD, `apply-discount`, `validate`, `redeem`, קופונים לפי משתמש, `expiring-soon` |
| **RegionsController** | CRUD, חיפוש לפי שם, `count`, עדכון מרכז |
| **CarFeedbacksController** | CRUD, לפי רכב / משתמש |
| **WeatherForecastController** | Controller דוגמה (תבנית ASP.NET) |

Base route: `api/{controller}`

---

## Service — לוגיקה עסקית

### Interfaces (`Service/Interfaces/`)

`IUserService`, `ICarService`, `IOrderService`, `ICouponService`, `IRegionService`, `ICarFeedbackService`, `IEmailService`, `IService<T>`, `IsExist`.

### Services (`Service/Services/`)

| קובץ | לוגיקה |
|------|--------|
| **UserService** | רישום (BCrypt), Login + JWT (`GenerateToken`), עדכון פרופיל, הצפנת רישיון/דרכון/אשראי (AES), שינוי/איפוס סיסמה (קוד במייל + MemoryCache), חסימה, `GetCurrentUser`, קוד אימות הרשמה |
| **CarService** | CRUD, Haversine + זמינות לפי יומן, buffer 15 דק', סטטוס מפורט, תחזוקה, דלק/ק"מ, נעילה, רכבים פופולריים / דורשי תדלוק |
| **OrderService** | יצירה/עדכון/ביטול, `IsCarBusy`, `IsUserOverlap`, תמחור, `FinishOrder`, Lock/Unlock, `UpdateTripProgress` (סימולציית ק"מ), קונפליקטים, `ConfirmReplacement`, הארכה, דוחות הכנסות, `ReportStartCondition` → CarInspection |
| **CouponService** | CRUD, ולידציה, מימוש, בדיקת תוקף |
| **RegionService** | CRUD אזורים |
| **CarFeedbackService** | משוב והערות על רכב/הזמנה |
| **EmailService** | SMTP — אישור הזמנה, איפוס סיסמה, קוד הרשמה (HTML RTL) |
| **OrderTrackingService** | לולאת Worker: הפעלת Pending, סימולציית ק"מ, קונפליקטים, התראת איחור |
| **EncryptionService** | AES — הצפנה/פענוח שדות רגישים |
| **MapperProfile** | AutoMapper Entity ↔ DTO |
| **ExtensionService** | `AddServices()` — רישום DI מרוכז לכל השירותים וה-Repositories |

---

## Repository — גישה לנתונים

### `IRepository<T>`

`GetAll`, `GetById`, `Add`, `Update`, `Delete`, `Exists` — ממשק CRUD גנרי לכל ישות.

### Repositories

| Repository | ישות |
|------------|------|
| `UserRepository` | User |
| `CarRepository` | Car |
| `OrderRepository` | Order |
| `RegionRepository` | Region |
| `CouponRepository` | Coupon |
| `FeedbackRepository` | CarFeedback |
| `CarInspectionRepository` | CarInspection |

**Entities** — `Repository/Entities/` (User, Car, Order, Region, Coupon, CarFeedback, CarInspection).

**`ExtensionsRepository`** — `AddRepository()` לרישום כל ה-Repositories ב-DI.

---

## DataContext

- **`CityCarDb`** — DbSets: Users, Cars, Orders, Regions, Coupons, Feedbacks, CarInspections.
- **`OnConfiguring`** — Connection String ל-SQL Server (ברירת מחדל אם לא הוגדר מבחוץ).
- **`OnModelCreating`** — דיוק decimal (18,2); קשר 1:1 Order↔Feedback; `DeleteBehavior.Restrict` על Inspections.
- **`Migrations/`** — מיגרציות EF (שדות קונפליקט, בדיקות, אשראי, שבת ועוד).

---

## Common — DTOs

| DTO | שימוש |
|-----|--------|
| `UserDto` | הרשמה, עדכון, תצוגה |
| `LoginDto` | התחברות |
| `CarDto` | CRUD + מרחק, סטטוס זמינות |
| `OrderDto` | יצירה, עדכון, תצוגה מפורטת |
| `CouponDto` | קופונים |
| `RegionDto` | אזורים |
| `CarFeedbackDto` | משוב |
| `CarInspectionDto` | בדיקת מצב לפני נסיעה |

---

## CityCar.Worker — עובד רקע

פרויקט נפרד (`BackgroundService`), רץ כ-Windows Service / Console, **כל 60 שניות**:

| שלב | מה קורה |
|-----|---------|
| `UpdatePendingOrders` | Pending → Active כשהגיע `StartTime`; אם הרכב עדיין תפוס/בתחזוקה — `ProcessLateCustomerConflict` |
| `UpdateActiveTripsProgress` | סימולציית ק"מ לנסיעות Active |
| `HandleBufferingAndConflicts` | זיהוי איחור (2+ דק' אחרי `ExpectedEndTime`) + הצעת רכב חלופי (פעם אחת — `HasConflict`) |
| `AutoFinishExpiredOrders` | לוג על הזמנות באיחור — **לא** סוגר אוטומטית (המשתמש רואה באיחור ב-UI) |

Worker משתמש באותם Services ו-DbContext כמו ה-API.

---

## API — Endpoints (תמצית)

### Users — `/api/Users`
`POST login`, `register`, `forgot-password`, `reset-password`, `request-registration-code`, `verify-registration-code` | `GET /`, `{id}`, `email/{email}`, `current`, `count-user` | `PUT {id}` | `PATCH change-password`, `toggle-block/{userId}` | `DELETE {id}`

### Cars — `/api/Cars`
`GET /`, `{id}`, `closest`, `available`, `popular`, `needs-fuel`, `status/{status}`, `available/by-region/{regionId}`, `{id}/check-suitability`, `{id}/is-fit`, `{id}/extended-status` | `POST`, `PUT`, `DELETE` (admin) | `PATCH {id}/fuel`, `mileage`, `status`, `toggle-lock`, תחזוקה

### Orders — `/api/Orders`
`POST /` (user), `{id}/unlock`, `{id}/lock`, `{id}/finish`, `{id}/update-progress`, `{id}/submit-start-report`, `extend/{id}`, `{id}/confirm-replacement`, `{id}/report-refuel` | `GET active`, `revenue`, `by-date`, `range`, חיפושים | `PATCH cancel`, `mark-as-paid`

### Coupons, Regions, CarFeedbacks
CRUD + endpoints ייעודיים (validate, redeem, expiring-soon, center וכו').

---

## הפעלה

### דרישות
- .NET 9 SDK
- SQL Server
- Visual Studio 2022 / VS Code
- (אופציונלי) Gmail App Password

### שלבים

1. Clone את `CityCar-Project` **ו-** `CityCar.Worker` (תיקייה אחות — נדרש ל-Solution).
2. עדכן Connection String ב-`DataContext/CityCarDb.cs` או ב-`CityCar.Worker/appsettings.json`.
3. מיגרציות:
   ```bash
   cd DataContext
   dotnet ef database update --startup-project ../Project
   ```
4. הרצה:
   ```bash
   dotnet restore
   dotnet run --project Project
   ```
5. Swagger: `https://localhost:7034` / `http://localhost:5014`
6. Worker (אופציונלי):
   ```bash
   dotnet run --project ../CityCar.Worker/CityCar.Worker
   ```

---

## תצורה

| מפתח | תיאור |
|------|--------|
| `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key` | JWT (Key חובה, 32+ תווים) |
| `EmailSettings` | SMTP — Server, Port, SenderEmail, SenderPassword |
| `EncryptionKey` | מפתח AES (32 תווים) |
| `ConnectionStrings:DefaultConnection` | Worker → SQL Server |

מומלץ User Secrets לסודות — לא ב-Git.

---

## סיכום

CityCar הוא Backend מלא לשירות השכרת רכבים: **API** שמקבל בקשות, **Service** שמבצע לוגיקה עסקית (תמחור, קונפליקטים, אימות), **Repository** שמנהל נתונים, **DataContext** שמגדיר סכימה, ו-**Worker** שמסנכרן מחזור חיים ברקע.  
כל יחידת קוד ממוקדת באחריות אחת — עבודה ממוסדת לניהול השכרה, משתמשים, צי רכבים והזמנות.
