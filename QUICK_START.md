# ⚡ دليل البدء السريع - 5 دقائق

## 🎯 الهدف
تشغيل المشروع في **5 دقائق** فقط!

---

## ✅ Checklist سريع

```
[ ] تثبيت .NET 9.0 SDK
[ ] تثبيت SQL Server
[ ] Clone المشروع
[ ] تحديث Connection String
[ ] تطبيق Migration
[ ] تشغيل المشروع
```

---

## 🚀 الخطوات

### **1️⃣ التحقق من المتطلبات (30 ثانية)**

```bash
# تحقق من .NET
dotnet --version
# يجب أن يظهر: 9.0.x

# تحقق من SQL Server
sqlcmd -S localhost -Q "SELECT 1"
# يجب أن يظهر: (1 rows affected)
```

**إذا لم يكن مثبتاً:**
- .NET 9.0: https://dotnet.microsoft.com/download/dotnet/9.0
- SQL Server: https://www.microsoft.com/sql-server/sql-server-downloads

---

### **2️⃣ Clone المشروع (30 ثانية)**

```bash
git clone https://github.com/your-repo/mina-events.git
cd mina-events
```

---

### **3️⃣ تحديث Connection String (30 ثانية)**

افتح `RourtPPl01/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MinaEventsDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

**ملاحظة:** إذا كنت تستخدم SQL Server Express:
```json
"Server=.\\SQLEXPRESS;Database=MinaEventsDb;..."
```

---

### **4️⃣ تطبيق Migration (1 دقيقة)**

```bash
cd RouteDAl
dotnet ef database update --startup-project ../RourtPPl01
cd ..
```

**يجب أن يظهر:**
```
Applying migration '20250106_InitialMinaEventsSchema'.
Done.
```

---

### **5️⃣ تشغيل المشروع (30 ثانية)**

```bash
cd RourtPPl01
dotnet run
```

**يجب أن يظهر:**
```
Now listening on: https://localhost:5001
Now listening on: http://localhost:5000
```

---

### **6️⃣ فتح المتصفح (10 ثوانٍ)**

افتح المتصفح وانتقل إلى:
```
https://localhost:5001
```

---

## 🎭 إنشاء بيانات تجريبية (اختياري)

### **SQL Script سريع:**

```sql
USE MinaEventsDb;
GO

-- 1. إنشاء Organization
DECLARE @OrgId UNIQUEIDENTIFIER = NEWID();
INSERT INTO Organizations (OrganizationId, Name, Type, IsActive, CreatedAt)
VALUES (@OrgId, 'وزارة التعليم', 'Government', 1, GETDATE());

-- 2. إنشاء Admin
INSERT INTO Users (UserId, OrganizationId, FullName, Email, PasswordHash, Role, IsActive, CreatedAt)
VALUES (
    NEWID(),
    @OrgId,
    'مدير النظام',
    'admin@example.com',
    'AQAAAAEAACcQAAAAEJ...', -- Hash for 'Admin@123'
    'Admin',
    1,
    GETDATE()
);

-- 3. إنشاء User
INSERT INTO Users (UserId, OrganizationId, FullName, Email, PasswordHash, Role, IsActive, CreatedAt)
VALUES (
    NEWID(),
    @OrgId,
    'مستخدم تجريبي',
    'user@example.com',
    'AQAAAAEAACcQAAAAEJ...', -- Hash for 'User@123'
    'User',
    1,
    GETDATE()
);
```

---

## 🔑 تسجيل الدخول

```
Admin:
Email: admin@example.com
Password: Admin@123

User:
Email: user@example.com
Password: User@123
```

---

## 🧪 اختبار سريع

### **1. اختبار Admin (2 دقيقة):**

1. سجّل الدخول كـ Admin
2. اذهب إلى **الأحداث** → **إنشاء حدث جديد**
3. أدخل:
   - العنوان: "اجتماع تجريبي"
   - تاريخ البداية: اليوم
   - تاريخ النهاية: غداً
4. أضف بند واحد
5. أضف استبيان بسيط
6. احفظ

### **2. اختبار User (1 دقيقة):**

1. سجّل الدخول كـ User
2. اذهب إلى **أحداثي**
3. افتح الحدث التجريبي
4. أجب على الاستبيان
5. أرسل

### **3. عرض النتائج (30 ثانية):**

1. سجّل الدخول كـ Admin
2. اذهب إلى **الأحداث**
3. اضغط **النتائج** للحدث التجريبي
4. تحقق من الإحصائيات

---

## 🔍 استكشاف الأخطاء السريع

### **مشكلة: لا يمكن الاتصال بقاعدة البيانات**
```bash
# تحقق من SQL Server
sqlcmd -S localhost -Q "SELECT 1"

# إذا فشل، شغّل SQL Server Service
# Windows: Services → SQL Server → Start
```

### **مشكلة: Migration فشل**
```bash
# حذف قاعدة البيانات وإعادة المحاولة
dotnet ef database drop --startup-project ../RourtPPl01 --force
dotnet ef database update --startup-project ../RourtPPl01
```

### **مشكلة: Build فشل**
```bash
# تنظيف وإعادة البناء
dotnet clean
dotnet restore
dotnet build
```

### **مشكلة: Port مستخدم**
```bash
# غيّر Port في launchSettings.json
# RourtPPl01/Properties/launchSettings.json
"applicationUrl": "https://localhost:5002;http://localhost:5001"
```

---

## 📊 التحقق من النجاح

### **✅ Checklist النهائي:**

```
[ ] المشروع يعمل على https://localhost:5001
[ ] يمكن تسجيل الدخول كـ Admin
[ ] يمكن إنشاء حدث جديد
[ ] يمكن تسجيل الدخول كـ User
[ ] يمكن المشاركة في الحدث
[ ] يمكن عرض النتائج
```

---

## 🎉 مبروك!

إذا وصلت هنا، فالمشروع يعمل بنجاح! 🎊

### **الخطوات التالية:**

1. **اقرأ الوثائق:**
   - [README.md](README.md) - نظرة عامة
   - [GETTING_STARTED.md](GETTING_STARTED.md) - دليل مفصّل
   - [TECHNICAL_DOCUMENTATION.md](TECHNICAL_DOCUMENTATION.md) - توثيق فني

2. **استكشف المشروع:**
   - جرّب جميع الميزات
   - أنشئ أحداث متنوعة
   - اختبر الاستبيانات والنقاشات

3. **طوّر المشروع:**
   - أضف ميزات جديدة
   - حسّن التصميم
   - اكتب Tests

---

## 📞 الدعم

إذا واجهت أي مشكلة:
- **GitHub Issues**: https://github.com/your-repo/mina-events/issues
- **Email**: support@mina-events.com
- **Documentation**: راجع الملفات المُسلّمة

---

<div align="center">

**⏱️ الوقت المستغرق: 5 دقائق**  
**✅ الحالة: جاهز للاستخدام**

**صُنع بـ ❤️ واحترافية**

</div>

