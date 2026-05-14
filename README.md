# CityCar-Project

מערכת API לניהול שירות השכרת רכבים מבוססת .NET 9 ו-ASP.NET Core.

## מבנה פרויקט

- `Project/` - Web API ראשי, תצורת שירותים, Controllers.
- `Service/` - לוגיקה עסקית, אימותים, חישובים ותהליכים.
- `Repository/` - גישה לנתונים ויישום CRUD.
- `DataContext/` - חיבור מסד נתונים, DbContext ויחסי ישויות.
- `Common/` - מחלקות DTO להעברת נתונים בין השכבות.
- `CityCar.Worker/` - פרויקט עובד ברקע.

## ארכיטקטורה

השכבות המרכזיות:

1. `Project` - קבלת בקשות HTTP, הגדרת Middleware, אימות והרשאות.
2. `Service` - יישום הלוגיקה העסקית עבור משתמשים, רכבים, הזמנות וקופונים.
3. `Repository` - גישה לדאטה, קריאות לערכי DbSet ושמירת שינויים.
4. `DataContext` - הגדרת DbSet ומודלים, קשרי ישויות והתנהגות מחיקה.
5. `Common` - DTOs שמפרידים בין ייצוג הנתונים בבקר לבין הישויות במסד.

טכנולוגיות עיקריות:

- ASP.NET Core Web API
- Entity Framework Core עם SQL Server
- JWT Authentication
- Swagger/OpenAPI
- AutoMapper
- BCrypt
- MemoryCache

## Project

`Program.cs`

- רישום שירותים ב-Dependency Injection.
- הגדרת JWT, CORS, Swagger, Authentication ו-Authorization.
- רישום `CityCarDb` ו-Services מה-`Service` Layer.

`Controllers/`

- `UsersController.cs` - ניהול משתמשים: רישום, התחברות, שינוי סיסמה, איפוס סיסמה, חסימה, קבלת משתמש נוכחי, חיפוש משתמש לפי אימייל.
- `CarsController.cs` - ניהול רכבים: CRUD, חיפוש לפי מיקום, זמינות ותאריך, בדיקת התאמה, ניהול דלק וקילומטראז', סטטוס תחזוקה, נעילה, דוחות סטטוס.
- `OrdersController.cs` - ניהול הזמנות: יצירה, עדכון, ביטול, דיווח תחילת נסיעה, נעילה/פתיחה של רכב, סיום הזמנה, עדכון התקדמות, דוחות הכנסות והזמנות.
- `CouponsController.cs` - ניהול קופונים ותוקפים: CRUD וקבלת קופונים.
- `RegionsController.cs` - ניהול אזורים: CRUD אזורים.
- `CarFeedbacksController.cs` - ניהול משוב על רכבים והזמנות.
- `WeatherForecastController.cs` - Controller סטנדרטי דוגמה.

## Service

`Service/Interfaces/`

- ממשקים עבור כל שירותי הדומיין (`IUserService`, `ICarService`, `IOrderService`, `ICouponService`, `IRegionService`, `ICarFeedbackService`, `IEmailService` ועוד).

`Service/Services/`

- `UserService.cs` - לוגיקה למשתמשים: הרשאה, יצירת JWT, אימות סיסמאות, רישום, עדכון פרופיל, חסימה, איפוס סיסמה, קבלת משתמש מחובר.
- `CarService.cs` - לוגיקה לרכבים: CRUD, זמינות, חיפוש קרוב, התאמת טווחי זמן, חישוב סטטוס, תחזוקה, עדכון דלק וקילומטראז'.
- `OrderService.cs` - לוגיקה להזמנות: יצירה, עדכון, ביטול, סיום, נעילה ופתיחה של רכבים, דיווח מצב נסיעה, חישוב הכנסות ודוחות.
- `CouponService.cs` - לוגיקה לקופונים: יצירה, עדכון, מחיקה ובדיקת תקינות.
- `RegionService.cs` - ניהול אזורים: יצירה, עדכון, מחיקה ושליפת אזורים.
- `CarFeedbackService.cs` - ניהול משוב והערות על רכבים והזמנות.
- `EmailService.cs` - שליחת מיילים למשתמשים, כולל בריכת שירותי דואר.
- `OrderTrackingService.cs` - שירות עזר למעקב אחר סטטוס הזמנות.
- `MapperProfile.cs` - מיפוי AutoMapper בין שכבת DTO לשכבת הישויות.
- `ExtensionService.cs` - הרחבת `IServiceCollection` לרישום שירותי אב-טיפוס.

## Repository

`Repository/Interfaces/`

- `IRepository<T>` - ממשק CRUD בסיסי.

`Repository/Repositories/`

- `CarRepository.cs` - ניהול רכבים ב-DbContext.
- `OrderRepository.cs` - ניהול הזמנות ב-DbContext.
- `UserRepository.cs` - ניהול משתמשים ב-DbContext.
- `CouponRepository.cs` - ניהול קופונים ב-DbContext.
- `RegionRepository.cs` - ניהול אזורים ב-DbContext.
- `FeedbackRepository.cs` - ניהול משובים ב-DbContext.
- `CarInspectionRepository.cs` - ניהול בדיקות רכב ב-DbContext.
- `ExtensionsRepository.cs` - הרחבות וסיוע לשאילתות מיוחדות.

## DataContext

- `CityCarDb.cs` - הגדרת `DbContext` עם DbSets עבור Users, Cars, Orders, Regions, Coupons, Feedbacks, CarInspections.
- `OnConfiguring` - חיבור ברירת מחדל ל-SQL Server במידה ולא מוגדר מבחוץ.
- `OnModelCreating` - הגדרת יחסי גומלין בין ישויות, התנהגות מחיקה ודיוק שדות מספריים.
- `Migrations/` - קבצי מיגרציה לשינויים בסכמת מסד הנתונים.

## Common

`Common/Dto/`

- `UserDto.cs`
- `CarDto.cs`
- `OrderDto.cs`
- `CouponDto.cs`
- `RegionDto.cs`
- `CarFeedbackDto.cs`
- `CarInspectionDto.cs`
- `LoginDto.cs`

## הפעלה

1. ודא התקנת `dotnet 9.0` ו-SQL Server.
2. פתח `Project.sln` ב-Visual Studio או VS Code.
3. עדכן חיבור מסד נתונים ב-`Project/appsettings.json` או ב-`DataContext/CityCarDb.cs`.
4. הרץ `dotnet restore` לכל הפרויקטים.
5. הרץ את פרויקט `Project` כ-Web API.
6. ב-development Swagger נגיש בכתובת `http://localhost:<port>/`.

## תצורה נדרשת

- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Key`
- `EmailSettings`

## סיכום

הפרויקט מחולק לשכבות ברורות: `API` שמקבל בקשות, `Service` שמבצע לוגיקה עסקית, `Repository` שמנהל נתונים ו-`DataContext` שמגדיר את סכימת מסד הנתונים. כל יחידת קוד ממוקדת באחריות ספציפית לעבודה ממוסדת וניהול תהליכי השכרה, משתמשים והזמנות.
