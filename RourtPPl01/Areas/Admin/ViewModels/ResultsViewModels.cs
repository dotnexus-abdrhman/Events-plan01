using Microsoft.AspNetCore.Http;

namespace RourtPPl01.Areas.Admin.ViewModels
{
    // ============================================
    // Results Summary
    // ============================================
    public class ResultsSummaryViewModel
    {
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public EventStatisticsViewModel Statistics { get; set; } = new();
        public List<SurveyResultViewModel> SurveyResults { get; set; } = new();
        public List<UserResponseViewModel> UserResponses { get; set; } = new();
    }

    public class EventStatisticsViewModel
    {
        public int UniqueParticipants { get; set; }
        public int TotalSurveyAnswers { get; set; }
        public int TotalDiscussionReplies { get; set; }
        public int TotalSignatures { get; set; }
    }

    public class SurveyResultViewModel
    {
        public string SurveyTitle { get; set; } = string.Empty;
        public List<QuestionResultViewModel> Questions { get; set; } = new();
    }

    public class QuestionResultViewModel
    {
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int TotalAnswers { get; set; }
        public List<OptionResultViewModel> Options { get; set; } = new();
    }

    public class OptionResultViewModel
    {
        public string OptionText { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    // ============================================
    // Results Details
    // ============================================
    public class ResultsDetailsViewModel
    {
        public Guid EventId { get; set; }
        public List<UserResponseViewModel> UserResponses { get; set; } = new();
    }

    public class UserResponseViewModel
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public bool HasSignature { get; set; }
        public string? SignaturePath { get; set; }
        public bool HasSurveyAnswers { get; set; }
        public bool HasDiscussionReplies { get; set; }
        public bool HasTableData { get; set; }
        public bool IsGuest { get; set; }
        public string ParticipantType => IsGuest ? "ضيف عبر الرابط" : "مستخدم مسجل";
        public List<SurveyAnswerViewModel> SurveyAnswers { get; set; } = new();
        public List<DiscussionReplyViewModel> DiscussionReplies { get; set; } = new();
    }

    public class SurveyAnswerViewModel
    {
        public string QuestionText { get; set; } = string.Empty;
        public List<string> SelectedOptions { get; set; } = new();
    }

    public class DiscussionReplyViewModel
    {
        public string DiscussionTitle { get; set; } = string.Empty;
        public string ReplyText { get; set; } = string.Empty;
    }

    // ============================================
    // Export PDF Options (Admin form)
    // ============================================
    public class PdfExportOptionsVm
    {
        public Guid EventId { get; set; }
        public bool IncludeEventDetails { get; set; } = true;
        public bool IncludeSurveyAndResponses { get; set; } = true;
        public bool IncludeDiscussions { get; set; } = true;
        public bool IncludeSignatures { get; set; } = true;
        public bool IncludeSections { get; set; } = false;
        public bool IncludeAttachments { get; set; } = false;
        public bool UseOrganizationLogo { get; set; } = false;
        public IFormFile? LogoFile { get; set; }
        public string? BrandingFooterText { get; set; } = "منصة مينا لإدارة الفعاليات";
    }
}

