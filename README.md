# CityCar — מערכת השכרת רכבים

מערכת Backend מלאה לניהול שירות השכרת רכבים עירוני (מודל דומה ל-Car2Go / Gett Car).  
הפרויקט בנוי ב-.NET 9, ASP.NET Core Web API, Entity Framework Core ו-SQL Server, עם הפרדה לשכבות (Layered Architecture), אימות JWT, ועובד רקע (Background Worker) לניהול מחזור חיי ההזמנות.

---

## מהות הפרויקט

CityCar מאפשרת:

- **למשתמשים (`user`)** — הרשמה, התחברות, הזמנת רכב לפי מיקום וזמינות, פתיחה/נעילה של רכב, דיווח מצב לפני נסיעה, מעקב נסיעה, סיום הזמנה, משוב, שימוש בקופונים והארכת הזמנה.
- **למנהלים (`admin`)** — ניהול מלא של משתמשים, רכבים, אזורים, קופונים, דוחות הכנסות והזמנות, תחזוקת צי וחסימת משתמשים.

המערכת מטפלת בתרחישים עסקיים מורכבים: חפיפת הזמנות, איחור בהחזרת רכב, הצעת רכב חלופי, חישוב מחיר דינמי, דירוג משתמשים, הגבלת הזמנות בשבת, וסימולציית נסיעה ברקע.

---

## טכנולוגיות

| טכנולוגיה | שימוש |
|-----------|--------|
| ASP.NET Core Web API (.NET 9) | שכבת API |
| Entity Framework Core 9 | ORM ומיגרציות |
| SQL Server | מסד נתונים |
| JWT Bearer Authentication | אימות והרשאות |
| Swagger / OpenAPI | תיעוד ו-UI לבדיקות |
| AutoMapper | מיפוי Entity ↔ DTO |
| BCrypt | הצפנת סיסמאות |
| AES (`EncryptionService`) | הצפנת פרטי אשראי |
| SMTP (Gmail) | שליחת מיילים (אישור הזמנה, איפוס סיסמה, קוד הרשמה) |
| `BackgroundService` | עובד רקע לעדכון סטטוס הזמנות |

---

## מבנה ה-Solution

```
CityCar-Project/
├── Project/          ← Web API (נקודת הכניסה)
├── Service/          ← לוגיקה עסקית
├── Repository/       ← גישה לנתונים + ישויות (Entities)
├── DataContext/      ← DbContext, מיגרציות EF
├── Common/           ← DTOs
└── Project.sln

CityCar.Worker/       ← פרויקט עובד רקע (תיקייה אחות, מחוץ ל-repo)
└── CityCar.Worker/
```

| פרויקט | תלות | תפקיד |
|--------|------|--------|
| `Project` | Service, Repository, DataContext, Common, CityCar.Worker | Controllers, Middleware, DI, Swagger |
| `Service` | Repository, Common | שירותי דומיין, AutoMapper, Email, Encryption |
| `Repository` | — | Repositories + Entity classes |
| `DataContext` | Repository | `CityCarDb`, מיגרציות |
| `Common` | — | DTOs להעברת נתונים בין שכבות |
| `CityCar.Worker` | Service, DataContext, Repository | הרצת `OrderTrackingService` כל 60 שניות |

---

## ארכיטקטורה

```
Client (Frontend / Swagger)
        │
        ▼
┌───────────────────┐
│  Project (API)    │  Controllers, JWT, CORS
└─────────┬─────────┘
          │
          ▼
┌───────────────────┐
│  Service          │  UserService, CarService, OrderService, ...
└─────────┬─────────┘
          │
          ▼
┌───────────────────┐
│  Repository       │  IRepository<T> → UserRepository, CarRepository, ...
└─────────┬─────────┘
          │
          ▼
┌───────────────────┐
│  DataContext      │  CityCarDb (EF Core) → SQL Server
└───────────────────┘

CityCar.Worker ──► OrderTrackingService (מחזור רקע, ללא HTTP)
```

**עקרון:** Controllers דקים — מקבלים בקשות, קוראים ל-Service, ומחזירים תשובות HTTP.  
Entities נשארות ב-`Repository.Entities`; DTOs ב-`Common.Dto` מפרידים בין API לבין מסד הנתונים.

---

## מודל דומיין

### ישויות עיקריות

| ישות | תיאור |
|------|--------|
| `User` | משתמש — פרטים אישיים, רישיון/דרכון, אשראי מוצפן, דירוג, יתרה, חסימה |
| `Car` | רכב — מודל, מספר רישוי, קטגוריה, סטטוס, מיקום GPS, תמחור, דלק, ק"מ |
| `Order` | הזמנה — זמנים, מחיר, סטטוס, קילומטראז', קונפליקטים, קופון |
| `Region` | אזור גיאוגרפי — שם ומרכז (lat/lng) |
| `Coupon` | קופון — קוד, סוג הנחה (סכום/אחוז), תוקף, שיוך למשתמש |
| `CarFeedback` | משוב — דירוג 1–5, הערה, דיווח תקלה |
| `CarInspection` | בדיקת מצב רכב — ניקיון, מיזוג, נזקים, צמיג |

### Enums מרכזיים

| Enum | ערכים |
|------|--------|
| `UserType` | `user`, `admin` |
| `UserRank` | `Regular`, `Bronze`, `Silver`, `Gold`, `PurpleBadge` |
| `CarStatus` | `Available`, `PartiallyBooked`, `Occupied`, `Maintenance` |
| `CarCategory` | `Mini`, `Family`, `Large`, `Commercial`, `Luxury` |
| `OrderStatus` | `Pending`, `Active`, `Completed`, `Canceled` |
| `PricingType` | `ByHour`, `ByDay` |
| `DiscountType` | `Amount`, `Percentage` |

### קשרים בין ישויות

- `User` ←→ `Order` (1:N)
- `Car` ←→ `Order` (1:N)
- `Region` ←→ `Car` (1:N)
- `Coupon` ←→ `Order` (1:N, אופציונלי)
- `Order` ←→ `CarFeedback` (1:1)
- `Order` ←→ `CarInspection` (1:1)

---

## אימות והרשאות

- **JWT Bearer** — לאחר התחברות מוצג Token ב-Header: `Authorization: Bearer {token}`.
- **Claims:** `NameIdentifier` (UserId), `Role` (`user` / `admin`).
- **Swagger** — מוגדר עם Security Scheme מסוג Bearer לבדיקות נוחות.
- **CORS** — פתוח לכל Origin (Development).

Endpoints מוגנים לפי `[Authorize]` ו-`[Authorize(Roles = "admin")]` / `[Authorize(Roles = "user")]`.  
משתמש רגיל יכול לצפות רק בהזמנות שלו; Admin רואה הכל.

---

## תהליכים עסקיים מרכזיים

### מחזור חיי הזמנה

```
Pending ──► Active ──► Completed
   │            │
   └─ Canceled  └─ (איחור → LateFee, הזמנה נשארת Active)
```

1. **יצירה (`Pending`)** — בדיקת זמינות רכב, חפיפה עם הזמנות אחרות, חסימת שבת, חישוב מחיר בסיס, שליחת מייל אישור.
2. **הפעלה (`Active`)** — אוטומטית על ידי Worker כשמגיע `StartTime`, או ידנית דרך API. הרכב עובר ל-`Occupied`.
3. **נסיעה** — עדכון קילומטראז' (API או Worker), פתיחה/נעילה, דיווח תדלוק.
4. **סיום (`Completed`)** — חישוב מחיר סופי (ק"מ, איחור, ביטוח, דירוג), ניכוי מיתרה, עדכון דירוג משתמש.

### חישוב מחיר

- **בסיס:** `TotalDays × PricePerDay` + `TotalHours × PricePerHour` (אם שעות עולות על מחיר יום — חיוב יומי).
- **ביטוח:** +50₪/יום או +3₪/שעה (`WantsInsuranceUpgrade`).
- **בסיום:** +1.5₪/ק"מ, +1₪/דקת איחור, +50₪ לנהג חדש (גיל < 24).
- **הנחות:** דירוג Gold (10%), PurpleBadge (15%), קופון, יתרה בחשבון.
- **בונוס תדלוק:** עד 2× `PricePerHour` ליתרת המשתמש.

### ניהול קונפליקטים (רכב חלופי)

כשמשתמש מחזיר רכב באיחור והזמנה הבאה ממתינה:

- `ProcessLateCustomerConflict` מחפש רכב חלופי זמין (אותה קטגוריה, מספיק מקומות).
- ההזמנה הממתינה מקבלת `HasConflict = true` + פרטי הרכב המוצע.
- המשתמש מאשר/דוחה דרך `confirm-replacement`.
- Worker מריץ את הלוגיקה כל 60 שניות.

### הגבלת שבת

הזמנות עם `StartTime` או `ExpectedEndTime` ביום שישי מ-16:00 או בשבת עד 20:00 — נדחות.

### דירוג משתמשים

| דירוג | תנאי | הטבה |
|--------|------|------|
| Bronze | 10+ הזמנות שהושלמו | — |
| Silver | 20+ | — |
| Gold | 30+ | 10% הנחה |
| PurpleBadge | 50+ | 15% הנחה |

### עובד רקע (`CityCar.Worker`)

רץ כ-Windows Service / Console, כל **60 שניות**:

| שלב | פעולה |
|-----|--------|
| `UpdatePendingOrders` | מעבר Pending → Active; טיפול בקונפליקט/תחזוקה |
| `UpdateActiveTripsProgress` | סימולציית קילומטראז' לנסיעות פעילות |
| `HandleBufferingAndConflicts` | זיהוי איחורים והפעלת הצעת רכב חלופי |
| `AutoFinishExpiredOrders` | התראה על הזמנות שעבר זמנן (ללא סגירה אוטומטית) |

---

## API — Controllers ו-Endpoints

Base route: `api/{controller}`

### Users (`/api/Users`)

| Method | Route | הרשאה | תיאור |
|--------|-------|--------|--------|
| POST | `login` | ציבורי | התחברות → JWT |
| POST | `register` | ציבורי | הרשמה |
| GET | `/` | admin | כל המשתמשים |
| GET | `{id}` | admin | משתמש לפי ID |
| PUT | `{id}` | user | עדכון פרופיל |
| DELETE | `{id}` | admin | מחיקה |
| GET | `email/{email}` | admin | חיפוש לפי אימייל |
| PATCH | `change-password` | מחובר | שינוי סיסמה |
| PATCH | `toggle-block/{userId}` | admin | חסימה/שחרור |
| GET | `current` | מחובר | משתמש נוכחי |
| GET | `count-user` | admin | ספירת משתמשים |
| POST | `forgot-password` | ציבורי | שליחת קוד איפוס |
| POST | `reset-password` | ציבורי | איפוס סיסמה |
| POST | `request-registration-code` | ציבורי | קוד אימות הרשמה |
| POST | `verify-registration-code` | ציבורי | אימות קוד |

### Cars (`/api/Cars`)

| Method | Route | הרשאה | תיאור |
|--------|-------|--------|--------|
| GET | `/`, `{id}` | ציבורי/מחובר | רשימה / פרטי רכב |
| POST, PUT, DELETE | `/`, `{id}` | admin | CRUD |
| GET | `closest` | — | רכבים קרובים (lat, lng, start, end) |
| GET | `available` | — | רכבים פנויים |
| GET | `{id}/check-suitability` | — | בדיקת התאמה לטווח זמן |
| GET | `popular`, `needs-fuel` | — | רכבים פופולריים / דורשים תדלוק |
| PATCH | `{id}/fuel`, `{id}/mileage` | — | עדכון דלק / ק"מ |
| GET | `{id}/is-fit` | — | כשירות לנסיעה |
| GET | `status/{status}` | — | רכבים לפי סטטוס |
| PATCH | `{id}/status` | admin | שינוי סטטוס |
| PATCH | `{carId}/send-to-maintenance`, `release-from-maintenance` | admin | תחזוקה |
| GET | `available/by-region/{regionId}` | — | לפי אזור |
| GET | `{id}/extended-status` | — | סטטוס מורחב |
| PATCH | `{id}/toggle-lock` | — | נעילה/פתיחה |

### Orders (`/api/Orders`)

| Method | Route | הרשאה | תיאור |
|--------|-------|--------|--------|
| GET | `/`, `{id}` | admin / מחובר | הזמנות |
| POST | `/` | user | יצירת הזמנה |
| PUT, DELETE | `{id}` | user | עדכון / ביטול |
| POST | `{id}/unlock`, PUT `{id}/lock` | user | פתיחה / נעילת רכב |
| PATCH | `{id}/finish` | user | סיום נסיעה |
| POST | `{id}/update-progress` | user | עדכון ק"מ |
| GET | `count`, `revenue`, `active` | admin / מחובר | דוחות |
| GET | `by-date/{date}`, `range` | admin | לפי תאריך |
| PATCH | `mark-as-paid/{orderId}`, `cancel/{orderId}` | מחובר | תשלום / ביטול |
| GET | `by-email/{email}`, `by-car/{carNumber}`, `user/{userId}` | admin / מחובר | חיפושים |
| POST | `{id}/submit-start-report` | מחובר | דיווח מצב לפני נסיעה |
| GET | `check-user-overlap` | — | בדיקת חפיפה |
| POST | `extend/{id}` | user | הארכת הזמנה |
| POST | `{id}/confirm-replacement` | user | אישור/דחיית רכב חלופי |
| POST | `{id}/report-refuel` | user | דיווח תדלוק |

### Coupons (`/api/Coupons`)

CRUD (admin), `apply-discount`, `validate`, `redeem`, `user/{userId}/unused`, `expiring-soon`.

### Regions (`/api/Regions`)

CRUD (admin), `name/{name}`, `count`, PATCH `{id}/center`.

### CarFeedbacks (`/api/CarFeedbacks`)

CRUD, `car/{carId}`, `user/{userId}`.

---

## שכבת Service — שירותים

| שירות | אחריות |
|--------|---------|
| `UserService` | הרשמה, Login + JWT, BCrypt, איפוס סיסמה, חסימה, הצפנת אשראי |
| `CarService` | CRUD, זמינות, חיפוש גיאוגרפי, סטטוס, תחזוקה, נעילה |
| `OrderService` | מחזור הזמנה, תמחור, קונפליקטים, הארכה, סיום, דוחות |
| `CouponService` | CRUD, ולידציה, מימוש |
| `RegionService` | CRUD אזורים |
| `CarFeedbackService` | משוב והערות |
| `EmailService` | SMTP — אישור הזמנה, איפוס סיסמה, קוד הרשמה |
| `OrderTrackingService` | לוגיקת Worker — הפעלה, ק"מ, קונפליקטים |
| `EncryptionService` | AES לפרטי כרטיס אשראי |
| `MapperProfile` | AutoMapper Entity ↔ DTO |
| `ExtensionService` | `AddServices()` — רישום DI מרוכז |

---

## שכבת Repository

- **`IRepository<T>`** — CRUD גנרי: `GetAll`, `GetById`, `Add`, `Update`, `Delete`, `Exists`.
- **`IContext`** — ממשק ל-`CityCarDb` (Save).
- **Repositories:** `UserRepository`, `CarRepository`, `OrderRepository`, `RegionRepository`, `CouponRepository`, `FeedbackRepository`, `CarInspectionRepository`.
- **Entities** — מוגדרות ב-`Repository/Entities/`.

---

## DataContext

- **`CityCarDb`** — DbContext עם DbSets לכל הישויות.
- **`OnModelCreating`** — דיוק עשרוני (18,2), קשר 1:1 Order↔Feedback, `DeleteBehavior.Restrict`.
- **`Migrations/`** — מיגרציות EF Core (שינויי סכמה לאורך פיתוח הפרויקט).

---

## Common — DTOs

| DTO | שימוש |
|-----|--------|
| `UserDto` | הרשמה, עדכון, תצוגה |
| `LoginDto` | התחברות |
| `CarDto` | CRUD רכב |
| `OrderDto` | יצירה ועדכון הזמנה |
| `CouponDto` | קופונים |
| `RegionDto` | אזורים |
| `CarFeedbackDto` | משוב |
| `CarInspectionDto` | בדיקת מצב רכב |

---

## הפעלה

### דרישות

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB / Express / Full)
- Visual Studio 2022 / VS Code / Rider
- (אופציונלי) Gmail App Password לשליחת מיילים

### שלבים

1. **Clone** את `CityCar-Project` ואת `CityCar.Worker` (תיקייה אחות — נדרש לפתיחת `Project.sln`).

2. **חיבור DB** — עדכן Connection String:
   - `DataContext/CityCarDb.cs` → `OnConfiguring` (ברירת מחדל)
   - או `CityCar.Worker/appsettings.json` → `ConnectionStrings:DefaultConnection`

3. **מיגרציות:**
   ```bash
   cd DataContext
   dotnet ef database update --startup-project ../Project
   ```

4. **Restore & Run API:**
   ```bash
   dotnet restore
   dotnet run --project Project
   ```

5. **Swagger** (Development):  
   `https://localhost:7034` או `http://localhost:5014`

6. **Worker** (אופציונלי, לתהליכי רקע):
   ```bash
   dotnet run --project ../CityCar.Worker/CityCar.Worker
   ```

---

## תצורה (`appsettings.json` / User Secrets)

| מפתח | תיאור |
|------|--------|
| `Jwt:Issuer` | Issuer של ה-Token |
| `Jwt:Audience` | Audience של ה-Token |
| `Jwt:Key` | מפתח סימטרי (32+ תווים) — **חובה** |
| `EmailSettings:SmtpServer` | שרת SMTP (ברירת מחדל: Gmail) |
| `EmailSettings:Port` | פורט SMTP |
| `EmailSettings:SenderEmail` | כתובת שולח |
| `EmailSettings:SenderPassword` | App Password |
| `EncryptionKey` | מפתח AES (32 תווים) — הצפנת אשראי |
| `ConnectionStrings:DefaultConnection` | (Worker) חיבור SQL Server |

מומלץ לשמור סודות (`Jwt:Key`, סיסמת מייל) ב-**User Secrets** ולא ב-Git.

---

## סיכום

CityCar הוא פרויקט Backend מלא לשירות השכרת רכבים, עם הפרדת אחריות ברורה בין שכבות, מודל דומיין עשיר, ותהליכים עסקיים שמדמים מערכת השכרה אמיתית — מהרשמת משתמש ועד ניהול קונפליקטים, תמחור דינמי ועדכון אוטומטי ברקע.
