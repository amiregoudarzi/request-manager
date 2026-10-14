# سامانه مدیریت درخواست‌های سازمانی

پیاده‌سازی محدود به صورت آزمون: ثبت درخواست برای **واحد مقصد خدمت**، مشاهده با کنترل دسترسی، تأیید/رد و تاریخچه اتمیک. بدون UI، اعلان، ثبت‌نام، API مدیریت کاربران، ویرایش، لغو، حذف، بازگشایی یا idempotency. فایل یا پروژه تست وجود ندارد.

## ساختار

- `Domain`: مدل‌ها، کلاس پایه `Entity` با `Id` و `CreatedDateUtc` و قواعد تغییر وضعیت.
- `Application`: قراردادهای دیتابیس، هویت، احراز هویت، repository و قفل.
- `Infrastructure`: EF Core، SQL Server، configurationها در همان `AppDbContext`، repository تراکنشی و Identity.
- `Web`: FastEndpoints؛ هر عملیات در `Modules/...` دارای `Endpoint.cs` و `Command.cs` یا `Query.cs` است. handler، DTO و validator در فایل همان عملیات قرار دارند.

یک Solution با چهار پروژه؛ Domain مستقل است. Web برای اجرای query از قرارداد `IAppDbContext` استفاده می‌کند و به پیاده‌سازی دیتابیس وابسته نیست. composition root در `Program.cs` زیرساخت را ثبت می‌کند.

## پیش‌نیاز و اتصال

.NET SDK 10 و SQL Server اجراشده در Docker لازم است. Docker و دیتابیس موجود شما را خودکار تغییر نمی‌دهیم.

از ریشه repository، اطلاعات اتصال را با user-secrets تنظیم کنید؛ رمز واقعی را در فایل‌های tracked قرار ندهید:

```sh
dotnet user-secrets set 'ConnectionStrings:SqlServer' 'Server=localhost,1433;Database=OrganizationRequests;User Id=sa;Password=<YOUR_SQL_PASSWORD>;Encrypt=True;TrustServerCertificate=True' --project src/OrganizationRequests.Web
```

`TrustServerCertificate=True` فقط برای SQL محلی با گواهی خودامضاست. در production از گواهی معتبر و حساب دیتابیس با حداقل مجوز استفاده کنید. تنظیمات production از environment یا secret provider تأمین شوند؛ برای اتصال نام متغیر `ConnectionStrings__SqlServer` است.

## ایجاد schema و داده اولیه

```sh
dotnet tool restore
dotnet build
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update --project src/OrganizationRequests.Infrastructure --startup-project src/OrganizationRequests.Web
```

داده نمونه فقط با دستور صریح زیر و در محیط Development ایجاد می‌شود. یک واحد `IT`، یک کاربر `employee` و یک کاربر `reviewer` ایجاد می‌شوند. رمز را خودتان با حداقل ۱۲ کاراکتر و حروف بزرگ/کوچک، عدد و علامت تعیین کنید:

```sh
dotnet user-secrets set 'Seed:Password' '<YOUR_DEVELOPMENT_PASSWORD>' --project src/OrganizationRequests.Web
dotnet run --project src/OrganizationRequests.Web --launch-profile https -- --seed
dotnet run --project src/OrganizationRequests.Web --launch-profile https
```

Seed حساب‌های موجود را دوباره ایجاد نمی‌کند و رمز آن‌ها را تغییر نمی‌دهد. هیچ حساب یا رمز پیش‌فرضی در production ایجاد نمی‌شود.

## Debug در JetBrains Rider

Solution را باز کنید، build configuration را روی `Debug` و run configuration را روی `OrganizationRequests.Web (Debug HTTPS)` بگذارید و Debug را اجرا کنید. پیکربندی مشترک در `.run/OrganizationRequests.Web.run.xml` ذخیره شده است؛ از پروژه Web، فریم‌ورک `net10.0` و پروفایل `https` استفاده می‌کند و قبل از اجرا build می‌کند. پروفایل محلی قدیمی `http` دیگر در `launchSettings.json` وجود ندارد و نباید انتخاب شود. اطلاعات اتصال همان user-secrets بالاست؛ رمز در run configuration ذخیره نمی‌شود.

## احراز هویت و مجوز

`AuthenticationService` از ASP.NET Core Identity برای بررسی رمز و lockout استفاده می‌کند. نشست با Cookie از نوع HttpOnly، Secure و SameSite=Strict برقرار می‌شود؛ JWT استفاده نشده است.

همه کاربران فعال نقش Employee دارند. کاربران دارای assignment در `UnitReviewers` نقش Reviewer نیز می‌گیرند. اعتبار حساب و نقش‌ها در هر درخواست بازبینی می‌شوند. کارمند درخواست‌های خودش را می‌بیند؛ مسئول علاوه بر آن، درخواست‌های واحدهای تحت مسئولیتش را می‌بیند. تصمیم‌گیری همیشه assignment همان واحد مقصد را بررسی می‌کند.

طبق صورت آزمون، مسئول مجاز می‌تواند درخواست خودش را نیز بررسی کند؛ ممنوعیت جداگانه‌ای اضافه نشده است. هر واحد می‌تواند چند مسئول داشته باشد.

## APIها

| متد | مسیر | کاربرد |
|---|---|---|
| GET | `/api/auth/csrf` | دریافت توکن CSRF و Cookie مربوطه |
| POST | `/api/auth/login` | ورود با `username` و `password` |
| POST | `/api/auth/logout` | خروج از نشست جاری |
| GET | `/api/units` | واحدهای فعال مقصد با صفحه‌بندی |
| POST | `/api/requests` | ثبت درخواست |
| GET | `/api/requests` | فهرست مجاز با فیلتر و صفحه‌بندی |
| GET | `/api/requests/{id}` | جزئیات مجاز |
| GET | `/api/requests/{id}/history` | تاریخچه مجاز با صفحه‌بندی |
| POST | `/api/requests/{id}/approve` | تأیید؛ توضیح اختیاری |
| POST | `/api/requests/{id}/reject` | رد؛ `reason` اجباری |

برای تمام POSTها، از جمله login/logout، ابتدا `/api/auth/csrf` را فراخوانی کنید؛ Cookieها را نگه دارید و مقدار `token` را در هدر `X-CSRF-TOKEN` بفرستید. **پس از ورود یا خروج دوباره توکن بگیرید**، چون هویت نشست تغییر کرده است. این endpoint برای حفاظت Cookie ضروری است و قابلیت کسب‌وکاری جدیدی اضافه نمی‌کند.

نمونه بدنه ثبت درخواست:

```json
{
  "title": "درخواست مانیتور",
  "description": "برای انجام کارهای روزانه",
  "type": "Equipment",
  "organizationalUnitId": "<destination-unit-guid>"
}
```

نوع‌ها: `Equipment`، `InternalService`، `TechnicalIssue`. وضعیت‌ها: `Pending`، `Approved`، `Rejected`. شناسه ثبت‌کننده و actor از نشست گرفته می‌شوند، نه بدنه API.

پارامترهای صفحه‌بندی: `page` از ۱ و `pageSize` از ۱ تا ۱۰۰؛ پیش‌فرض ۲۰. فهرست درخواست‌ها فیلترهای اختیاری `status`، `type` و `organizationalUnitId` دارد. محدودیت عنوان ۲۰۰، شرح ۴۰۰۰ و توضیح تصمیم ۱۰۰۰ کاراکتر است.

پاسخ‌ها: `201` ثبت، `200` خواندن/تصمیم/ورود/خروج، `400` ورودی یا CSRF نامعتبر، `401` ورود لازم یا اعتبارنامه نامعتبر، `403` مجوز تصمیم ناکافی، `404` منبع ناموجود یا خارج از دسترسی مشاهده، `409` تصمیم نهایی‌شده یا تعارض هم‌زمانی. خطاهای تصمیم و زیرساخت به ProblemDetails بدون جزئیات داخلی تبدیل می‌شوند.

## چند instance و تراکنش

تمام instanceها به یک دیتابیس متصل می‌شوند. برای تصمیم، repository ابتدا تراکنش باز می‌کند، سپس قفل `sp_getapplock` با کلید شناسه درخواست می‌گیرد، وضعیت و مجوز را بررسی و تصمیم و تاریخچه را در یک SaveChanges ذخیره می‌کند. قفل با commit/rollback آزاد می‌شود؛ timeout پنج ثانیه است. `RowVersion` نیز جلوی overwrite هم‌زمان را می‌گیرد. قفل Redis یا قفل حافظه محلی وجود ندارد.

تاریخچه ایجاد و تنها تصمیم نهایی ثبت می‌شود. تاریخچه از برنامه قابل ویرایش/حذف نیست؛ FKهای داده کسب‌وکار حذف آبشاری ندارند. تمام نام ستون‌ها صریح و تاریخ‌ها UTC با نوع SQL Server `datetime2` هستند.

کلیدهای Data Protection در جدول مشترک SQL ذخیره می‌شوند تا Cookie و CSRF بین instanceها معتبر باشند. در production باید گواهی رمزگذاری مشترک کلیدها را تأمین کنید:

- `DataProtection__CertificatePath`: مسیر گواهی PFX با private key.
- `DataProtection__CertificatePassword`: رمز از secret provider.

در Development گواهی رمزگذاری الزامی نیست. SQL schema شامل چهار جدول کسب‌وکار، `Users`، سه جدول داخلی user store در Identity و `DataProtectionKeys` است؛ جدول مدیریت نقش اضافه نشده است.

Migrationها را یک‌بار در مرحله انتشار اجرا کنید، نه هنگام startup تمام instanceها. برنامه schema را خودکار تغییر نمی‌دهد. اجرای محلی به HTTPS و گواهی توسعه .NET نیاز دارد؛ URL پروفایل HTTPS در `launchSettings.json` مشخص است. مقدار `AllowedHosts` در production باید با hostname واقعی سرویس تنظیم شود.

## وضعیت بررسی

Build بدون خطا و هشدار انجام شد؛ migration و SQL متناظر تولید شدند. برنامه ۱۰ endpoint را ثبت کرد. بررسی دستی درخواست بدون ورود، `401` و login بدون CSRF، `400` برگرداند. هیچ فایل یا پروژه تستی ایجاد نشده است.

هنگام آماده‌سازی، SQL Server فعال در Docker موجود نبود؛ migration روی دیتابیس اجرا نشده و ورود، ذخیره درخواست، rollback و هم‌زمانی واقعی هنوز در برابر SQL Server بررسی نشده‌اند. قبل از استفاده واقعی، اتصال، schema و این مسیرهای عملیاتی باید روی دیتابیس شما بررسی شوند. کاربران و assignmentهای production باید از مسیر provisioning مورد اعتماد سازمان تأمین شوند؛ API مدیریتی در محدوده این پروژه نیست.
