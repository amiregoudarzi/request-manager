# سامانه مدیریت درخواست‌های سازمانی

پیاده‌سازی صورت آزمون: ثبت درخواست برای **واحد مقصد خدمت**، مشاهده با کنترل دسترسی، تأیید/رد و تاریخچه اتمیک. طبق درخواست تکمیلی، ثبت‌نام و Swagger نیز اضافه شده‌اند. بدون UI کاربری، اعلان، API مدیریت کاربران، ویرایش، لغو، حذف، بازگشایی یا idempotency. فایل یا پروژه تست وجود ندارد.

## ساختار

- `Domain`: مدل‌ها، کلاس پایه `Entity` با `Id` و `CreatedDateUtc` و قواعد تغییر وضعیت.
- `Application`: هر interface در فایل مستقل داخل `Abstractions`؛ سرویس `RequestService` در `Services` مسئول اجرای ثبت و بررسی درخواست، مجوز واحد و خطاهای کاربردی است.
- `Infrastructure`: EF Core، SQL Server، configurationها در همان `AppDbContext`، repository برای query، ذخیره‌سازی، قفل و تراکنش و Identity. مدل حساب، سرویس احراز هویت و ساخت Claim هرکدام فایل مستقل دارند.
- `Web`: FastEndpoints؛ هر عملیات در `Modules/...` دارای `Endpoint.cs` و `Command.cs` یا `Query.cs` است. handler، DTO و validator در فایل همان عملیات قرار دارند.

یک Solution با چهار پروژه؛ Domain مستقل است. Web برای اجرای query از قرارداد `IAppDbContext` استفاده می‌کند و به پیاده‌سازی دیتابیس وابسته نیست. composition root در `Program.cs` زیرساخت را ثبت می‌کند.

Commandهای ثبت و تصمیم فقط سرویس Application را فراخوانی می‌کنند. سرویس به قرارداد `IRequestRepository` وابسته است و Infrastructure این قرارداد را پیاده می‌کند. Repository از طریق `ExecuteInTransactionAsync` عملیات سرویس را در تراکنش اجرا می‌کند؛ تنها پس از موفقیت عملیات، SaveChanges و commit انجام می‌شوند. قفل بررسی، پیش از خواندن درخواست گرفته می‌شود و تا پایان همان تراکنش باقی می‌ماند. Generic Repository یا UnitOfWork جدا اضافه نشده است. Queryهای ساده با EF و قراردادهای Application در فایل همان عملیات باقی مانده‌اند؛ این انتخاب عملی برای کوچک نگه‌داشتن پروژه است.

## پیش‌نیاز و اتصال

.NET SDK 10 و Docker لازم است. فایل `compose.yaml` یک SQL Server 2022 Developer محلی با volume پایدار ایجاد می‌کند و پورت را فقط روی `127.0.0.1:1433` منتشر می‌کند:

```sh
docker compose up -d sqlserver
docker compose ps
```

رمز محلی در `.env` با کلید `MSSQL_SA_PASSWORD` قرار دارد؛ این فایل وارد Git نمی‌شود. هنگام آماده‌سازی این محیط، رمز تصادفی تولید و connection string کامل در user-secrets ذخیره شد. برای clone جدید، `.env` را با رمز انتخابی خود بسازید و همان رمز را در دستور user-secrets زیر استفاده کنید. `docker compose down` داده را در volume نگه می‌دارد؛ گزینه `-v` داده را حذف می‌کند.

برای Mac با CPU ARM، Compose از `linux/amd64` و emulation استفاده می‌کند. این تنظیم برای توسعه محلی است؛ [پلتفرم پشتیبانی‌شده SQL Server](https://learn.microsoft.com/en-us/sql/linux/sql-server-linux-docker-container-deployment) برای استقرار واقعی Linux x86-64 است.

تنظیم اتصال بدون رمز در `appsettings.Development.json` قرار دارد. مقدار کامل و دارای رمز از user-secrets خوانده می‌شود و اولویت بالاتری دارد. از ریشه repository، برای تنظیم یا تغییر اتصال:

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

## Swagger

در Development، Swagger UI در `https://localhost:7130/swagger` و سند OpenAPI در `/swagger/v1/swagger.json` در دسترس است. پروفایل HTTPS در Rider صفحه Swagger را باز می‌کند. از `/api/auth/login` وارد شوید؛ مرورگر Cookie را نگه می‌دارد و Swagger برای هر عملیات نوشتن، توکن CSRF تازه را خودکار دریافت و در هدر قرار می‌دهد. JWT یا وارد کردن دستی Cookie در Authorize لازم نیست. Swagger در production فعال نیست.

## احراز هویت و مجوز

`AuthenticationService` از ASP.NET Core Identity برای بررسی رمز و lockout استفاده می‌کند. نشست با Cookie از نوع HttpOnly، Secure و SameSite=Strict برقرار می‌شود؛ JWT استفاده نشده است.

هر کارمند در جدول `Employees` به یک حساب و یک واحد سازمانی مرتبط است. همه اعضای یک واحد، درخواست‌های مقصد همان واحد را می‌بینند؛ ثبت‌کننده نیز درخواست‌های خودش را برای پیگیری می‌بیند. با فیلتر `organizationalUnitId` می‌توان ورودی‌های واحد را فهرست کرد؛ این فیلتر دسترسی به اطلاعات واحدهای دیگر ایجاد نمی‌کند.

همه حساب‌های فعال نقش Employee دارند، اما ثبت درخواست نیازمند عضویت سازمانی در `Employees` است. عضویت توسط سازمان تعیین می‌شود و در signup عمومی قابل انتخاب نیست. نقش حساب در ستون `Users.role` با مقدار `Employee` یا `UnitReviewer` ذخیره می‌شود. حساب با نقش UnitReviewer فقط وقتی عضو واحد مقصد در Employees باشد، درخواست آن واحد را تأیید یا رد می‌کند. Cookie و نقش‌ها در هر درخواست بازبینی می‌شوند؛ عضویت و مجوز واحد از دیتابیس کنترل می‌شوند.

مسئول واحد هر دو نقش `Employee` و `UnitReviewer` را دارد؛ بنابراین همه عملیات کارمند را نیز انجام می‌دهد. این سلسله‌مراتب با تخصیص هر دو Claim پیاده شده و ASP.NET Core نقش‌ها را خودکار از یکدیگر به ارث نمی‌برد. Repository در یک query، نقش فعلی حساب و عضویت او در واحد مقصد را بررسی می‌کند؛ نقش مسئول به‌تنهایی برای تصمیم در واحد دیگر کافی نیست.

نام نقش‌ها در enum `UserRole` در Domain تعریف شده و در بررسی Endpoint و ساخت Claim از `nameof(UserRole.Employee)` و `nameof(UserRole.UnitReviewer)` استفاده می‌شود. `normalized_user_name` فقط برای جست‌وجوی حساب هنگام ورود است و نقش را تعیین نمی‌کند.

طبق صورت آزمون، مسئول مجاز می‌تواند درخواست خودش را نیز بررسی کند؛ ممنوعیت جداگانه‌ای اضافه نشده است. هر واحد می‌تواند چند مسئول داشته باشد.

## APIها

| متد | مسیر | کاربرد |
|---|---|---|
| GET | `/api/auth/csrf` | دریافت توکن CSRF و Cookie مربوطه |
| POST | `/api/auth/signup` | ثبت حساب Employee با `username`، `password` و `displayName` |
| POST | `/api/auth/login` | ورود با `username` و `password` |
| POST | `/api/auth/logout` | خروج از نشست جاری |
| GET | `/api/units` | واحدهای فعال مقصد با صفحه‌بندی |
| POST | `/api/requests` | ثبت درخواست |
| GET | `/api/requests` | فهرست مجاز با فیلتر و صفحه‌بندی |
| GET | `/api/requests/{id}` | جزئیات مجاز |
| GET | `/api/requests/{id}/history` | تاریخچه مجاز با صفحه‌بندی |
| POST | `/api/requests/{id}/approve` | تأیید؛ توضیح اختیاری |
| POST | `/api/requests/{id}/reject` | رد؛ `reason` اجباری |

برای تمام POSTها، از جمله signup/login/logout، ابتدا `/api/auth/csrf` را فراخوانی کنید؛ Cookieها را نگه دارید و مقدار `token` را در هدر `X-CSRF-TOKEN` بفرستید. **پس از ورود یا خروج دوباره توکن بگیرید**، چون هویت نشست تغییر کرده است. Swagger این مراحل را خودکار انجام می‌دهد.

ثبت‌نام فقط حساب Employee می‌سازد و ورود خودکار انجام نمی‌دهد؛ سپس login کنید. رمز حداقل ۱۲ کاراکتر، حروف بزرگ و کوچک، عدد و علامت لازم دارد. نام کاربری تکراری `409` برمی‌گرداند. برای ثبت درخواست، سازمان باید کارمند را در جدول `Employees` به واحدش مرتبط کند؛ UnitReviewer شدن نیز نیازمند تعیین `Users.role` توسط سازمان است؛ signup اجازه تعیین نقش نمی‌دهد.

نمونه بدنه ثبت درخواست:

```json
{
  "title": "درخواست مانیتور",
  "description": "برای انجام کارهای روزانه",
  "type": "Equipment",
  "organizationalUnitId": "<destination-unit-guid>",
  "receiverEmployeeId": null
}
```

نوع‌ها: `Equipment`، `InternalService`، `TechnicalIssue`. وضعیت‌ها: `Pending`، `Approved`، `Rejected`. شناسه ثبت‌کننده و actor از نشست گرفته می‌شوند، نه بدنه API.

گیرنده اختیاری است؛ می‌توان `receiverEmployeeId` را حذف یا null ارسال کرد. اگر مشخص شود، باید شناسه کارمند از جدول `Employees` و عضو واحد مقصد باشد؛ این رابطه با FK مرکب نیز در دیتابیس کنترل می‌شود. تعیین گیرنده، مجوز تغییر وضعیت ایجاد نمی‌کند. API فهرست گیرندگان فعلاً اضافه نشده و برای مرحله بعد است.

پارامترهای صفحه‌بندی: `page` از ۱ و `pageSize` از ۱ تا ۱۰۰؛ پیش‌فرض ۲۰. فهرست درخواست‌ها فیلترهای اختیاری `status`، `type` و `organizationalUnitId` دارد. محدودیت عنوان ۲۰۰، شرح ۴۰۰۰ و توضیح تصمیم ۱۰۰۰ کاراکتر است.

خواندن‌ها با AsNoTracking و projection انجام می‌شوند؛ درخواستِ بررسی‌شونده برای ذخیره تغییر وضعیت tracked می‌ماند. بررسی عضویت ثبت‌کننده، فعال‌بودن مقصد و گیرنده اختیاری در یک query انجام می‌شود. بررسی دسترسی تاریخچه و شمارش آن نیز یک query مشترک دارند و صفحه آیتم‌ها با query دوم خوانده می‌شود. فهرست خالی پس از Count، query اضافی برای آیتم‌ها اجرا نمی‌کند. هیچ query به‌ازای هر آیتم فهرست اجرا نمی‌شود؛ subqueryهای EXISTS و COUNT داخل همان SQL هستند و N+1 ایجاد نمی‌کنند. تعداد ذکرشده مربوط به عملیات کاربردی است؛ بررسی Cookie و حساب، queryهای امنیتی جدا دارد.

پاسخ‌ها: `201` ثبت درخواست یا ثبت‌نام، `200` خواندن/تصمیم/ورود/خروج، `400` ورودی یا CSRF نامعتبر، `401` ورود لازم یا اعتبارنامه نامعتبر، `403` مجوز تصمیم ناکافی، `404` منبع ناموجود یا خارج از دسترسی مشاهده، `409` نام کاربری تکراری، تصمیم نهایی‌شده یا تعارض هم‌زمانی. خطاهای تصمیم و زیرساخت به ProblemDetails بدون جزئیات داخلی تبدیل می‌شوند.

مدیریت خطا با middleware داخلی ASP.NET Core و `ApiExceptionHandler` در همه محیط‌ها و پیش از authentication، CSRF و endpointها فعال است. خطاهای ناشناخته پاسخ عمومی `500` با `traceId` می‌گیرند؛ جزئیات exception فقط در لاگ ثبت می‌شود. خطاهای HTTP نامعتبر کد مناسب خود را حفظ می‌کنند و پاسخ خطا `no-store` است. خطای یک درخواست، به‌تنهایی host را متوقف نمی‌کند. خطای راه‌اندازی یا اجرای host با سطح Critical ثبت و با کد خروج `1` پایان می‌یابد؛ برنامه با تنظیمات نامعتبر به کار ادامه نمی‌دهد. پس از شروع ارسال پاسخ، امکان جایگزینی آن با JSON خطا وجود ندارد و سرور اتصال را مدیریت می‌کند. خاتمه اجباری پردازش و خطاهای حیاتی سیستم با middleware قابل تضمین نیستند.

## چند instance و تراکنش

تمام instanceها به یک دیتابیس متصل می‌شوند. برای تصمیم، repository ابتدا تراکنش باز می‌کند، سپس قفل `sp_getapplock` با کلید شناسه درخواست می‌گیرد، وضعیت و مجوز را بررسی و تصمیم و تاریخچه را در یک SaveChanges ذخیره می‌کند. قفل با commit/rollback آزاد می‌شود؛ timeout پنج ثانیه است. `RowVersion` نیز جلوی overwrite هم‌زمان را می‌گیرد. قفل Redis یا قفل حافظه محلی وجود ندارد.

تاریخچه ایجاد و تنها تصمیم نهایی ثبت می‌شود. تاریخچه از برنامه قابل ویرایش/حذف نیست؛ FKهای داده کسب‌وکار حذف آبشاری ندارند. تمام نام ستون‌ها صریح و تاریخ‌ها UTC با نوع SQL Server `datetime2` هستند.

کلیدهای Data Protection در جدول مشترک SQL ذخیره می‌شوند تا Cookie و CSRF بین instanceها معتبر باشند. در production باید گواهی رمزگذاری مشترک کلیدها را تأمین کنید:

- `DataProtection__CertificatePath`: مسیر گواهی PFX با private key.
- `DataProtection__CertificatePassword`: رمز از secret provider.

در Development گواهی رمزگذاری الزامی نیست. SQL schema شش جدول اصلی دارد: `Users`، `Employees`، `OrganizationalUnits`، `Requests`، `RequestStatusHistories` و `DataProtectionKeys`. نقش در Users ذخیره می‌شود؛ جدول UnitReviewers حذف شده است. Migration جدید نقش مسئولان قبلی را پیش از حذف جدول به Users منتقل می‌کند. جدول‌های UserClaims، UserLogins و UserTokens فقط در صورت خالی‌بودن حذف می‌شوند. Identity از AccountStore استفاده می‌کند که ذخیره‌سازی حساب، رمز، SecurityStamp و lockout را به EF store استاندارد واگذار می‌کند و قابلیت‌های بلااستفاده claims ذخیره‌شده، ورود خارجی و توکن ذخیره‌شده را ارائه نمی‌دهد. Claimهای کوکی همچنان از حساب و نقش آن ساخته می‌شوند؛ برای آن‌ها جدول UserClaims لازم نیست.

Migrationها را یک‌بار در مرحله انتشار اجرا کنید، نه هنگام startup تمام instanceها. برنامه schema را خودکار تغییر نمی‌دهد. اجرای محلی به HTTPS و گواهی توسعه .NET نیاز دارد؛ URL پروفایل HTTPS در `launchSettings.json` مشخص است. مقدار `AllowedHosts` در production باید با hostname واقعی سرویس تنظیم شود.

## وضعیت بررسی

Build بدون خطا و هشدار انجام شد؛ migration روی SQL Server کانتینری اجرا شد. برنامه ۱۱ endpoint دارد و Swagger نیز در دسترس است. بررسی مستقیم HTTP شامل ثبت‌نام، نام کاربری تکراری، login/logout، CSRF، ثبت درخواست، مجوز تصمیم، دلیل رد اجباری، تاریخچه و جلوگیری از تصمیم مجدد موفق بود. داده‌های موقت بررسی حذف شدند؛ فایل یا پروژه تستی ایجاد نشده است.

پس از اصلاح جریان واحدها، مشاهده درخواست توسط عضو دیگر همان واحد، عدم افشای درخواست به واحد دیگر، رد تصمیم مسئول واحد دیگر، گیرنده null، گیرنده نامعتبر برای واحد مقصد و نیاز به عضویت برای ثبت درخواست بررسی شدند. Migration و بررسی‌ها درخواست‌های موجود را حفظ کردند. حساب‌های نمونه employee و reviewer هر دو عضو IT هستند؛ reviewer مسئول همان واحد است.

دو instance روی پورت‌های جدا اجرا شدند: Cookie و CSRF صادرشده از یکی در دیگری معتبر بودند. تأیید و رد هم‌زمان از دو instance، دقیقاً یک پاسخ موفق و یک `409` ایجاد کرد؛ تنها یک تصمیم نهایی و رکورد تاریخچه مربوطه ذخیره شد. instanceهای موقت پس از بررسی متوقف شدند تا پورت debugger آزاد باشد.

کانتینر محلی SQL Server healthy است. داده اولیه شامل واحد IT و حساب‌های `employee` و `reviewer` ایجاد شده؛ رمز آن‌ها در user-secrets با کلید `Seed:Password` قرار دارد و وارد Git نشده است. کاربران جدید می‌توانند با signup ثبت شوند. عضویت واحد و نقش مسئولان باید از مسیر مورد اعتماد سازمان تأمین شود؛ API مدیریتی در محدوده این پروژه نیست. بازیابی بکاپ، خرابی شبکه و استقرار production در این بررسی پوشش داده نشده‌اند.
