# 📘 التوثيق الفني - مينا لإدارة الأحداث

## 📋 المحتويات

1. [البنية المعمارية](#-البنية-المعمارية)
2. [قاعدة البيانات](#-قاعدة-البيانات)
3. [طبقة الأعمال](#-طبقة-الأعمال)
4. [طبقة العرض](#-طبقة-العرض)
5. [الأمان](#-الأمان)
6. [API Reference](#-api-reference)

---

## 🏗️ البنية المعمارية

### Three-Layer Architecture

```
┌─────────────────────────────────────────┐
│   Presentation Layer (RourtPPl01)      │
│   - Controllers (Areas)                 │
│   - Views (Razor)                       │
│   - ViewModels                          │
│   - wwwroot (Static Files)              │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│   Business Logic Layer (RoutePLLe)     │
│   - Services (8 Services)               │
│   - DTOs (Data Transfer Objects)        │
│   - AutoMapper Profiles                 │
│   - Interfaces                          │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│   Data Access Layer (RouteDAl)         │
│   - Models (13 Domain Models)           │
│   - DbContext (EF Core)                 │
│   - Repositories (Generic Pattern)      │
│   - Migrations                          │
└─────────────────────────────────────────┘
```

---

## 🗄️ قاعدة البيانات

### Schema Overview

#### **1. Events Table**
```sql
CREATE TABLE Events (
    EventId UNIQUEIDENTIFIER PRIMARY KEY,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    StartAt DATETIME2 NOT NULL,
    EndAt DATETIME2 NOT NULL,
    RequireSignature BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2,
    FOREIGN KEY (OrganizationId) REFERENCES Organizations(OrganizationId)
);

CREATE INDEX IX_Events_OrganizationId ON Events(OrganizationId);
CREATE INDEX IX_Events_StartAt ON Events(StartAt);
```

#### **2. Sections Table**
```sql
CREATE TABLE Sections (
    SectionId UNIQUEIDENTIFIER PRIMARY KEY,
    EventId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX),
    OrderIndex INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (EventId) REFERENCES Events(EventId) ON DELETE CASCADE
);

CREATE INDEX IX_Sections_EventId ON Sections(EventId);
```

#### **3. Decisions Table**
```sql
CREATE TABLE Decisions (
    DecisionId UNIQUEIDENTIFIER PRIMARY KEY,
    SectionId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    OrderIndex INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (SectionId) REFERENCES Sections(SectionId) ON DELETE CASCADE
);

CREATE INDEX IX_Decisions_SectionId ON Decisions(SectionId);
```

#### **4. DecisionItems Table**
```sql
CREATE TABLE DecisionItems (
    DecisionItemId UNIQUEIDENTIFIER PRIMARY KEY,
    DecisionId UNIQUEIDENTIFIER NOT NULL,
    Content NVARCHAR(500) NOT NULL,
    OrderIndex INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (DecisionId) REFERENCES Decisions(DecisionId) ON DELETE CASCADE
);

CREATE INDEX IX_DecisionItems_DecisionId ON DecisionItems(DecisionId);
```

#### **5. Surveys Table**
```sql
CREATE TABLE Surveys (
    SurveyId UNIQUEIDENTIFIER PRIMARY KEY,
    EventId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    OrderIndex INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (EventId) REFERENCES Events(EventId) ON DELETE CASCADE
);

CREATE INDEX IX_Surveys_EventId ON Surveys(EventId);
```

#### **6. SurveyQuestions Table**
```sql
CREATE TABLE SurveyQuestions (
    SurveyQuestionId UNIQUEIDENTIFIER PRIMARY KEY,
    SurveyId UNIQUEIDENTIFIER NOT NULL,
    QuestionText NVARCHAR(500) NOT NULL,
    Type NVARCHAR(50) NOT NULL, -- 'Single' or 'Multiple'
    OrderIndex INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (SurveyId) REFERENCES Surveys(SurveyId) ON DELETE CASCADE
);

CREATE INDEX IX_SurveyQuestions_SurveyId ON SurveyQuestions(SurveyId);
```

#### **7. SurveyOptions Table**
```sql
CREATE TABLE SurveyOptions (
    SurveyOptionId UNIQUEIDENTIFIER PRIMARY KEY,
    SurveyQuestionId UNIQUEIDENTIFIER NOT NULL,
    OptionText NVARCHAR(200) NOT NULL,
    OrderIndex INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (SurveyQuestionId) REFERENCES SurveyQuestions(SurveyQuestionId) ON DELETE CASCADE
);

CREATE INDEX IX_SurveyOptions_SurveyQuestionId ON SurveyOptions(SurveyQuestionId);
```

#### **8. SurveyAnswers Table**
```sql
CREATE TABLE SurveyAnswers (
    SurveyAnswerId UNIQUEIDENTIFIER PRIMARY KEY,
    SurveyQuestionId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    SelectedOptionIds NVARCHAR(MAX), -- JSON array of GUIDs
    AnsweredAt DATETIME2 NOT NULL,
    FOREIGN KEY (SurveyQuestionId) REFERENCES SurveyQuestions(SurveyQuestionId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE INDEX IX_SurveyAnswers_SurveyQuestionId ON SurveyAnswers(SurveyQuestionId);
CREATE INDEX IX_SurveyAnswers_UserId ON SurveyAnswers(UserId);
```

#### **9. Discussions Table**
```sql
CREATE TABLE Discussions (
    DiscussionId UNIQUEIDENTIFIER PRIMARY KEY,
    EventId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    OrderIndex INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (EventId) REFERENCES Events(EventId) ON DELETE CASCADE
);

CREATE INDEX IX_Discussions_EventId ON Discussions(EventId);
```

#### **10. DiscussionReplies Table**
```sql
CREATE TABLE DiscussionReplies (
    DiscussionReplyId UNIQUEIDENTIFIER PRIMARY KEY,
    DiscussionId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    RepliedAt DATETIME2 NOT NULL,
    FOREIGN KEY (DiscussionId) REFERENCES Discussions(DiscussionId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE INDEX IX_DiscussionReplies_DiscussionId ON DiscussionReplies(DiscussionId);
CREATE INDEX IX_DiscussionReplies_UserId ON DiscussionReplies(UserId);
```

#### **11. TableBlocks Table**
```sql
CREATE TABLE TableBlocks (
    TableBlockId UNIQUEIDENTIFIER PRIMARY KEY,
    EventId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    RowsJson NVARCHAR(MAX) NOT NULL, -- JSON array of rows
    OrderIndex INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (EventId) REFERENCES Events(EventId) ON DELETE CASCADE
);

CREATE INDEX IX_TableBlocks_EventId ON TableBlocks(EventId);
```

**RowsJson Format:**
```json
[
  [
    {"content": "خلية 1", "rowSpan": 1, "colSpan": 1, "isBold": false, "isItalic": false, "align": "right"},
    {"content": "خلية 2", "rowSpan": 1, "colSpan": 1, "isBold": false, "isItalic": false, "align": "center"}
  ],
  [
    {"content": "خلية 3", "rowSpan": 1, "colSpan": 2, "isBold": true, "isItalic": false, "align": "center"}
  ]
]
```

#### **12. Attachments Table**
```sql
CREATE TABLE Attachments (
    AttachmentId UNIQUEIDENTIFIER PRIMARY KEY,
    EventId UNIQUEIDENTIFIER NOT NULL,
    FileName NVARCHAR(200) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    Type NVARCHAR(50) NOT NULL, -- 'Image' or 'PDF'
    OrderIndex INT NOT NULL,
    UploadedAt DATETIME2 NOT NULL,
    FOREIGN KEY (EventId) REFERENCES Events(EventId) ON DELETE CASCADE
);

CREATE INDEX IX_Attachments_EventId ON Attachments(EventId);
```

#### **13. UserSignatures Table**
```sql
CREATE TABLE UserSignatures (
    UserSignatureId UNIQUEIDENTIFIER PRIMARY KEY,
    EventId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    SignatureDataUrl NVARCHAR(MAX) NOT NULL, -- Base64 image
    SignedAt DATETIME2 NOT NULL,
    FOREIGN KEY (EventId) REFERENCES Events(EventId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE INDEX IX_UserSignatures_EventId ON UserSignatures(EventId);
CREATE INDEX IX_UserSignatures_UserId ON UserSignatures(UserId);
```

---

## 💼 طبقة الأعمال

### Services Overview

#### **1. MinaEventsService**

**Interface:**
```csharp
public interface IMinaEventsService
{
    Task<List<EventDto>> GetEventsByOrganizationAsync(Guid organizationId);
    Task<EventDto?> GetEventByIdAsync(Guid eventId);
    Task<EventDto> CreateEventAsync(CreateEventDto dto);
    Task<bool> UpdateEventAsync(Guid eventId, UpdateEventDto dto);
    Task<bool> DeleteEventAsync(Guid eventId);
}
```

**Implementation:**
```csharp
public class MinaEventsService : IMinaEventsService
{
    private readonly IRepository<Event, Guid> _eventRepo;
    private readonly IMapper _mapper;

    public async Task<EventDto> CreateEventAsync(CreateEventDto dto)
    {
        var eventEntity = _mapper.Map<Event>(dto);
        eventEntity.EventId = Guid.NewGuid();
        eventEntity.CreatedAt = DateTime.UtcNow;
        
        await _eventRepo.AddAsync(eventEntity);
        await _eventRepo.SaveChangesAsync();
        
        return _mapper.Map<EventDto>(eventEntity);
    }
}
```

#### **2. SectionsService**

**Methods:**
- `AddSectionAsync(CreateSectionDto dto)`
- `AddDecisionAsync(CreateDecisionDto dto)`
- `AddDecisionItemAsync(CreateDecisionItemDto dto)`
- `GetSectionsByEventIdAsync(Guid eventId)`

#### **3. SurveysService**

**Methods:**
- `CreateSurveyAsync(CreateSurveyDto dto)`
- `AddQuestionAsync(CreateSurveyQuestionDto dto)`
- `AddOptionAsync(CreateSurveyOptionDto dto)`
- `SubmitAnswerAsync(SubmitSurveyAnswerDto dto)`

#### **4. DiscussionsService**

**Methods:**
- `CreateDiscussionAsync(CreateDiscussionDto dto)`
- `AddReplyAsync(CreateDiscussionReplyDto dto)`
- `GetDiscussionsByEventIdAsync(Guid eventId)`

#### **5. TableBlocksService**

**Methods:**
- `CreateTableAsync(CreateTableBlockDto dto)`
- `UpdateTableAsync(Guid tableId, UpdateTableBlockDto dto)`
- `GetTablesByEventIdAsync(Guid eventId)`

#### **6. AttachmentsService**

**Methods:**
- `UploadAttachmentAsync(CreateAttachmentDto dto)`
- `GetAttachmentsByEventIdAsync(Guid eventId)`
- `DeleteAttachmentAsync(Guid attachmentId)`

#### **7. SignaturesService**

**Methods:**
- `SaveSignatureAsync(CreateUserSignatureDto dto)`
- `GetSignatureByUserAndEventAsync(Guid userId, Guid eventId)`

#### **8. MinaResultsService**

**Methods:**
- `GetEventResultsSummaryAsync(Guid eventId)`
- `GetSurveyResultsAsync(Guid surveyId)`
- `GetDiscussionRepliesAsync(Guid discussionId)`

---

## 🎨 طبقة العرض

### Controllers Structure

#### **Admin Area**

```
/Admin/Dashboard/Index          → لوحة التحكم
/Admin/Events/Index             → قائمة الأحداث
/Admin/Events/Create            → إنشاء حدث
/Admin/Events/Edit/{id}         → تعديل حدث
/Admin/Events/Details/{id}      → تفاصيل الحدث
/Admin/Events/Delete/{id}       → حذف حدث
/Admin/EventResults/Summary/{id} → النتائج
```

#### **UserPortal Area**

```
/UserPortal/MyEvents/Index                      → أحداثي
/UserPortal/EventParticipation/Details/{id}     → تفاصيل الحدث
/UserPortal/EventParticipation/SubmitResponses  → إرسال الإجابات
/UserPortal/EventParticipation/Confirmation     → تأكيد الإرسال
```

### ViewModels Hierarchy

```
EventDetailsViewModel
├── Sections (List<SectionViewModel>)
│   └── Decisions (List<DecisionViewModel>)
│       └── Items (List<DecisionItemViewModel>)
├── Surveys (List<SurveyViewModel>)
│   └── Questions (List<QuestionViewModel>)
│       └── Options (List<OptionViewModel>)
├── Discussions (List<DiscussionViewModel>)
├── Tables (List<TableViewModel>)
│   └── Rows (List<List<TableCellViewModel>>)
└── Attachments (List<AttachmentViewModel>)
```

---

## 🔐 الأمان

### Authentication

```csharp
// Program.cs
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
```

### Authorization

```csharp
// Controllers
[Authorize(Roles = "Admin")]
public class EventsController : Controller
{
    // Admin only
}

[Authorize(Roles = "User")]
public class MyEventsController : Controller
{
    // User only
}
```

### Organization-level Access

```csharp
private Guid GetOrganizationId()
{
    var orgIdClaim = User.FindFirst("OrganizationId")?.Value;
    return Guid.Parse(orgIdClaim ?? throw new UnauthorizedAccessException());
}
```

---

## 📡 API Reference

### Complete Endpoint List

See separate file: [API_ENDPOINTS.md](API_ENDPOINTS.md)

---

**📅 آخر تحديث:** 2025-10-06

