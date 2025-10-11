# 🚀 دليل البدء السريع - مينا لإدارة الأحداث

## 📋 المحتويات

1. [المتطلبات](#-المتطلبات)
2. [التثبيت](#-التثبيت)
3. [إعداد قاعدة البيانات](#-إعداد-قاعدة-البيانات)
4. [تشغيل المشروع](#-تشغيل-المشروع)
5. [إنشاء بيانات تجريبية](#-إنشاء-بيانات-تجريبية)
6. [استكشاف الأخطاء](#-استكشاف-الأخطاء)

---

## 🔧 المتطلبات

### البرامج المطلوبة:

- ✅ **.NET 9.0 SDK** - [تحميل](https://dotnet.microsoft.com/download/dotnet/9.0)
- ✅ **SQL Server 2019+** - [تحميل](https://www.microsoft.com/sql-server/sql-server-downloads)
- ✅ **Visual Studio 2022** أو **VS Code** - [تحميل](https://visualstudio.microsoft.com/)

### التحقق من التثبيت:

```bash
# التحقق من .NET SDK
dotnet --version
# يجب أن يظهر: 9.0.x

# التحقق من SQL Server
sqlcmd -S localhost -Q "SELECT @@VERSION"
```

---

## 📦 التثبيت

### 1. Clone المشروع

```bash
# Clone من GitHub
git clone https://github.com/your-repo/mina-events.git

# الانتقال إلى مجلد المشروع
cd mina-events
```

### 2. استعادة الحزم (Restore Packages)

```bash
# استعادة حزم NuGet
dotnet restore
```

---

## 🗄️ إعداد قاعدة البيانات

### 1. تحديث Connection String

افتح ملف `RourtPPl01/appsettings.json` وعدّل Connection String:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MinaEventsDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

**ملاحظة:** إذا كنت تستخدم SQL Server Express:
```json
"Server=.\\SQLEXPRESS;Database=MinaEventsDb;Trusted_Connection=True;TrustServerCertificate=True"
```

### 2. تطبيق Migration

```bash
# الانتقال إلى مجلد DAL
cd RouteDAl

# تطبيق Migration
dotnet ef database update --startup-project ../RourtPPl01

# العودة إلى المجلد الرئيسي
cd ..
```

### 3. التحقق من قاعدة البيانات

```sql
-- فتح SQL Server Management Studio (SSMS)
-- الاتصال بـ Server: localhost أو .
-- التحقق من وجود Database: MinaEventsDb
-- التحقق من الجداول:

USE MinaEventsDb;
GO

SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
```

يجب أن تظهر الجداول التالية:
- `Events`
- `Sections`
- `Decisions`
- `DecisionItems`
- `Surveys`
- `SurveyQuestions`
- `SurveyOptions`
- `SurveyAnswers`
- `Discussions`
- `DiscussionReplies`
- `TableBlocks`
- `Attachments`
- `UserSignatures`
- `Organizations`
- `Users`

---

## ▶️ تشغيل المشروع

### 1. البناء (Build)

```bash
# الانتقال إلى مجلد Presentation Layer
cd RourtPPl01

# بناء المشروع
dotnet build
```

يجب أن يظهر:
```
Build succeeded.
    0 Error(s)
    14 Warning(s) (nullable only)
```

### 2. التشغيل (Run)

```bash
# تشغيل المشروع
dotnet run
```

يجب أن يظهر:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### 3. فتح المتصفح

افتح المتصفح وانتقل إلى:
```
https://localhost:5001
```

---

## 🎭 إنشاء بيانات تجريبية

### 1. إنشاء Organization

```sql
USE MinaEventsDb;
GO

INSERT INTO Organizations (OrganizationId, Name, Type, IsActive, CreatedAt)
VALUES (NEWID(), 'وزارة التعليم', 'Government', 1, GETDATE());
```

### 2. إنشاء Admin User

```sql
DECLARE @OrgId UNIQUEIDENTIFIER = (SELECT TOP 1 OrganizationId FROM Organizations);

INSERT INTO Users (UserId, OrganizationId, FullName, Email, PasswordHash, Role, IsActive, CreatedAt)
VALUES (
    NEWID(),
    @OrgId,
    'مدير النظام',
    'admin@example.com',
    'AQAAAAEAACcQAAAAEJ...', -- Hashed password for 'Admin@123'
    'Admin',
    1,
    GETDATE()
);
```

### 3. إنشاء Regular User

```sql
DECLARE @OrgId UNIQUEIDENTIFIER = (SELECT TOP 1 OrganizationId FROM Organizations);

INSERT INTO Users (UserId, OrganizationId, FullName, Email, PasswordHash, Role, IsActive, CreatedAt)
VALUES (
    NEWID(),
    @OrgId,
    'مستخدم تجريبي',
    'user@example.com',
    'AQAAAAEAACcQAAAAEJ...', -- Hashed password for 'User@123'
    'User',
    1,
    GETDATE()
);
```

### 4. تسجيل الدخول

```
Admin:
Email: admin@example.com
Password: Admin@123

User:
Email: user@example.com
Password: User@123
```

---

## 🧪 اختبار المشروع

### 1. اختبار Admin

1. سجّل الدخول كـ Admin
2. انتقل إلى **لوحة التحكم**
3. انتقل إلى **الأحداث** → **إنشاء حدث جديد**
4. أدخل المعلومات:
   - العنوان: "اجتماع تجريبي"
   - الوصف: "اجتماع لاختبار النظام"
   - تاريخ البداية: اليوم
   - تاريخ النهاية: غداً
5. أضف بند:
   - العنوان: "البند الأول"
   - المحتوى: "محتوى البند"
6. أضف قرار:
   - العنوان: "القرار الأول"
7. أضف استبيان:
   - العنوان: "استبيان تجريبي"
   - أضف سؤال: "ما رأيك في النظام؟"
   - أضف خيارات: "ممتاز", "جيد", "مقبول"
8. احفظ الحدث

### 2. اختبار User

1. سجّل الدخول كـ User
2. انتقل إلى **أحداثي**
3. افتح الحدث التجريبي
4. اقرأ البنود والقرارات
5. أجب على الاستبيان
6. وقّع (إذا مطلوب)
7. أرسل الإجابات

### 3. عرض النتائج

1. سجّل الدخول كـ Admin
2. انتقل إلى **الأحداث**
3. اضغط على **النتائج** للحدث التجريبي
4. تحقق من الإحصائيات
5. تحقق من إجابات المستخدمين

---

## 🔍 استكشاف الأخطاء

### مشكلة: لا يمكن الاتصال بقاعدة البيانات

**الحل:**
```bash
# تحقق من SQL Server
sqlcmd -S localhost -Q "SELECT 1"

# إذا فشل، تأكد من تشغيل SQL Server Service
# Windows: Services → SQL Server (MSSQLSERVER) → Start
```

### مشكلة: Migration فشل

**الحل:**
```bash
# حذف قاعدة البيانات
dotnet ef database drop --startup-project ../RourtPPl01 --force

# إعادة تطبيق Migration
dotnet ef database update --startup-project ../RourtPPl01
```

### مشكلة: Build فشل

**الحل:**
```bash
# تنظيف المشروع
dotnet clean

# استعادة الحزم
dotnet restore

# إعادة البناء
dotnet build
```

### مشكلة: Port مستخدم

**الحل:**
```bash
# تغيير Port في launchSettings.json
# RourtPPl01/Properties/launchSettings.json

"applicationUrl": "https://localhost:5002;http://localhost:5001"
```

### مشكلة: CSS/JS لا يعمل

**الحل:**
```bash
# تأكد من وجود الملفات في wwwroot
ls RourtPPl01/wwwroot/css/
ls RourtPPl01/wwwroot/js/

# إعادة بناء المشروع
dotnet build
```

---

## 📚 موارد إضافية

### الوثائق:
- [ASP.NET Core MVC](https://docs.microsoft.com/aspnet/core/mvc)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [Bootstrap RTL](https://getbootstrap.com/docs/5.3/getting-started/rtl/)

### الدعم:
- **Email**: support@mina-events.com
- **GitHub Issues**: https://github.com/your-repo/mina-events/issues

---

## ✅ Checklist

- [ ] تثبيت .NET 9.0 SDK
- [ ] تثبيت SQL Server
- [ ] Clone المشروع
- [ ] تحديث Connection String
- [ ] تطبيق Migration
- [ ] إنشاء Organization
- [ ] إنشاء Admin User
- [ ] إنشاء Regular User
- [ ] تشغيل المشروع
- [ ] اختبار Admin
- [ ] اختبار User
- [ ] عرض النتائج

---

**🎉 مبروك! المشروع جاهز للاستخدام!**

