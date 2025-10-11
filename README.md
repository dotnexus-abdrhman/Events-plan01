# 🎯 مينا لإدارة الأحداث | Mina Events Management System

<div align="center">

![Build Status](https://img.shields.io/badge/build-passing-brightgreen)
![.NET](https://img.shields.io/badge/.NET-9.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![RTL](https://img.shields.io/badge/RTL-Arabic-orange)

**نظام متكامل لإدارة الأحداث المؤسسية باللغة العربية**

[المميزات](#-المميزات) • [التثبيت](#-التثبيت) • [الاستخدام](#-الاستخدام) • [البنية](#-البنية-المعمارية) • [المساهمة](#-المساهمة)

</div>

---

## 📖 نظرة عامة

**مينا لإدارة الأحداث** هو نظام MVC شامل مبني على ASP.NET Core 9.0 لإدارة الأحداث المؤسسية مع دعم كامل للبنود، القرارات، الاستبيانات، النقاشات، الجداول، المرفقات، والتوقيعات الإلكترونية.

### 🎯 الهدف

توفير منصة موحدة لإدارة الأحداث المؤسسية مع:
- ✅ واجهة عربية كاملة (RTL)
- ✅ تصميم احترافي وهادئ
- ✅ دعم كامل للموبايل والكمبيوتر
- ✅ أمان على مستوى المؤسسة

---

## ✨ المميزات

### 👨‍💼 للأدمن

- 📝 **إدارة الأحداث**: إنشاء، تعديل، حذف، وعرض الأحداث
- 📋 **البنود والقرارات**: إضافة بنود متعددة مع قرارات وعناصر قرار
- 📊 **الاستبيانات**: إنشاء استبيانات مع أسئلة (Single/Multiple Choice)
- 💬 **النقاشات**: إضافة نقاشات للحدث
- 📑 **الجداول**: إنشاء جداول بصيغة JSON
- 📎 **المرفقات**: رفع صور وملفات PDF
- 📈 **النتائج**: عرض نتائج تفصيلية مع إحصائيات
- 🖥️ **لوحة التحكم**: إحصائيات شاملة عن الأحداث

### 👤 للمستخدم

- 📅 **أحداثي**: عرض أحداث الجهة فقط
- 👁️ **عرض التفاصيل**: قراءة البنود والقرارات
- ✍️ **المشاركة**: الإجابة على الاستبيانات والنقاشات
- 📊 **الجداول**: عرض الجداول (Read-only)
- 🖼️ **المرفقات**: عرض الصور وملفات PDF
- ✒️ **التوقيع**: توقيع إلكتروني باستخدام HTML5 Canvas
- 📤 **الإرسال**: إرسال موحد لجميع الإجابات

---

## 🏗️ البنية المعمارية

```
RourtMvc/
├── RouteDAl/              # Data Access Layer
│   ├── Models/
│   │   └── Mina/         # 13 Domain Models
│   ├── Data/
│   │   └── Contexts/     # AppDbContext
│   └── Repositories/     # Generic Repository
│
├── RoutePLLe/            # Business Logic Layer
│   ├── Services/         # 8 Services
│   ├── Dto/              # Data Transfer Objects
│   └── Mapping/          # AutoMapper Profiles
│
└── RourtPPl01/           # Presentation Layer
    ├── Areas/
    │   ├── Admin/        # Admin Area
    │   │   ├── Controllers/
    │   │   ├── Views/
    │   │   └── ViewModels/
    │   └── UserPortal/   # User Area
    │       ├── Controllers/
    │       ├── Views/
    │       └── ViewModels/
    ├── wwwroot/
    │   ├── css/          # Custom CSS
    │   └── js/           # JavaScript Files
    └── Views/
        └── Shared/       # Shared Layout
```

---

## 🚀 التثبيت

### المتطلبات

- .NET 9.0 SDK
- SQL Server 2019+
- Visual Studio 2022 أو VS Code

### خطوات التثبيت

1. **Clone المشروع:**
```bash
git clone https://github.com/your-repo/mina-events.git
cd mina-events
```

2. **تحديث Connection String:**
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MinaEventsDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

3. **تطبيق Migration:**
```bash
cd RouteDAl
dotnet ef database update --startup-project ../RourtPPl01
```

4. **تشغيل المشروع:**
```bash
cd ../RourtPPl01
dotnet run
```

5. **فتح المتصفح:**
```
https://localhost:5001
```

---

## 💻 الاستخدام

### تسجيل الدخول

```
Admin:
Username: admin@example.com
Password: Admin@123

User:
Username: user@example.com
Password: User@123
```

### إنشاء حدث جديد

1. انتقل إلى **Admin** → **الأحداث** → **إنشاء حدث جديد**
2. أدخل المعلومات الأساسية (العنوان، الوصف، التواريخ)
3. أضف البنود والقرارات
4. أضف المكونات (استبيانات، نقاشات، جداول، مرفقات)
5. احفظ الحدث

### مشاركة المستخدم

1. انتقل إلى **أحداثي**
2. اختر الحدث
3. اقرأ البنود والقرارات
4. أجب على الاستبيانات
5. شارك في النقاشات
6. وقّع (إذا مطلوب)
7. أرسل الإجابات

---

## 🛠️ التقنيات

### Backend
- **ASP.NET Core MVC 9.0**
- **Entity Framework Core 9.0**
- **SQL Server**
- **AutoMapper 12.0.1**
- **Cookie Authentication**

### Frontend
- **Bootstrap 5.3 RTL**
- **Font Awesome 6.4**
- **jQuery 3.7**
- **HTML5 Canvas**
- **Vanilla JavaScript**

---

## 📊 قاعدة البيانات

### الجداول الرئيسية

| الجدول | الوصف |
|--------|-------|
| `Events` | الأحداث الرئيسية |
| `Sections` | البنود |
| `Decisions` | القرارات |
| `DecisionItems` | عناصر القرار |
| `Surveys` | الاستبيانات |
| `SurveyQuestions` | أسئلة الاستبيان |
| `SurveyOptions` | خيارات الأسئلة |
| `SurveyAnswers` | إجابات المستخدمين |
| `Discussions` | النقاشات |
| `DiscussionReplies` | ردود النقاش |
| `TableBlocks` | الجداول (JSON) |
| `Attachments` | المرفقات |
| `UserSignatures` | التوقيعات |

---

## 🔐 الأمان

- ✅ **Cookie Authentication**
- ✅ **Role-based Authorization** (Admin/User)
- ✅ **Organization-level Access Control**
- ✅ **Anti-Forgery Tokens**
- ✅ **Input Validation**
- ✅ **SQL Injection Protection** (EF Core)

---

## 📝 API Endpoints

### Admin

```
GET  /Admin/Dashboard              # لوحة التحكم
GET  /Admin/Events                 # قائمة الأحداث
GET  /Admin/Events/Create          # إنشاء حدث
POST /Admin/Events/Create          # حفظ حدث جديد
GET  /Admin/Events/Edit/{id}       # تعديل حدث
POST /Admin/Events/Edit/{id}       # حفظ التعديلات
GET  /Admin/Events/Details/{id}    # تفاصيل الحدث
POST /Admin/Events/Delete/{id}     # حذف حدث
GET  /Admin/EventResults/Summary/{id}  # النتائج
```

### User Portal

```
GET  /UserPortal/MyEvents                      # أحداثي
GET  /UserPortal/EventParticipation/Details/{id}  # تفاصيل الحدث
POST /UserPortal/EventParticipation/SubmitResponses  # إرسال الإجابات
```

---

## 🎨 التصميم

### الألوان

```css
--primary: #4A90E2;
--primary-dark: #357ABD;
--success: #5CB85C;
--danger: #D9534F;
--warning: #F0AD4E;
--info: #5BC0DE;
```

### الخطوط

- **Primary**: Segoe UI
- **Fallback**: Tahoma, Geneva, Verdana, sans-serif

---

## 🤝 المساهمة

نرحب بالمساهمات! يرجى اتباع الخطوات التالية:

1. Fork المشروع
2. إنشاء Branch جديد (`git checkout -b feature/AmazingFeature`)
3. Commit التغييرات (`git commit -m 'Add some AmazingFeature'`)
4. Push إلى Branch (`git push origin feature/AmazingFeature`)
5. فتح Pull Request

---

## 📄 الترخيص

هذا المشروع مرخص تحت **MIT License** - انظر ملف [LICENSE](LICENSE) للتفاصيل.

---

## 📞 التواصل

- **Email**: support@mina-events.com
- **Website**: https://mina-events.com
- **GitHub**: https://github.com/your-repo/mina-events

---

## 🙏 شكر وتقدير

- **Bootstrap Team** - للإطار الرائع
- **Font Awesome** - للأيقونات الجميلة
- **Microsoft** - لـ ASP.NET Core و EF Core

---

<div align="center">

**صُنع بـ ❤️ في السعودية**

⭐ إذا أعجبك المشروع، لا تنسَ إعطائه نجمة!

</div>

