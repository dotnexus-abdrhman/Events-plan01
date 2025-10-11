using System.ComponentModel.DataAnnotations;
using EvenDAL.Models.Shared.Enums;

namespace RourtPPl01.Areas.UserPortal.ViewModels
{
    // ============================================
    // My Events Index
    // ============================================
    public class MyEventsIndexViewModel
    {
        public List<MyEventItemViewModel> Events { get; set; } = new();
        public string? SearchTerm { get; set; }
    }

    public class MyEventItemViewModel
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public EventStatus Status { get; set; }
        public bool RequireSignature { get; set; }
        public int SectionsCount { get; set; }
        public int SurveysCount { get; set; }
        public int DiscussionsCount { get; set; }
    }

    // ============================================
    // Event Details
    // ============================================
    public class EventDetailsViewModel
    {
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public string EventDescription { get; set; } = string.Empty;
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public bool RequireSignature { get; set; }
        public bool HasUserSigned { get; set; }
        public bool HasUserParticipated { get; set; }

        // البنود
        public List<SectionViewModel> Sections { get; set; } = new();

        // المكونات العامة (على مستوى الحدث)
        public List<SurveyViewModel> Surveys { get; set; } = new();
        public List<DiscussionViewModel> Discussions { get; set; } = new();
        public List<TableViewModel> Tables { get; set; } = new();
        public List<AttachmentViewModel> Attachments { get; set; } = new();
    }

    public class SectionViewModel
    {
        public Guid SectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Body { get; set; }
        public int Order { get; set; }
        public List<DecisionViewModel> Decisions { get; set; } = new();

        // 45434846272a 274428462f
        public List<SurveyViewModel> Surveys { get; set; } = new();
        public List<DiscussionViewModel> Discussions { get; set; } = new();
        public List<TableViewModel> Tables { get; set; } = new();
        public List<AttachmentViewModel> Attachments { get; set; } = new();
    }

    public class DecisionViewModel
    {
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<DecisionItemViewModel> Items { get; set; } = new();
    }

    public class DecisionItemViewModel
    {
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class SurveyViewModel
    {
        public Guid SurveyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<QuestionViewModel> Questions { get; set; } = new();
    }

    public class QuestionViewModel
    {
        public Guid SurveyQuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public SurveyQuestionType Type { get; set; }
        public bool IsRequired { get; set; }
        public List<OptionViewModel> Options { get; set; } = new();
    }

    public class OptionViewModel
    {
        public Guid SurveyOptionId { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class DiscussionViewModel
    {
        public Guid DiscussionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }

    public class TableViewModel
    {
        public string Title { get; set; } = string.Empty;
        public bool HasHeader { get; set; }
        public List<List<TableCellViewModel>>? Rows { get; set; }
    }

    public class TableCellViewModel
    {
        public string Value { get; set; } = string.Empty;
    }

    public class AttachmentViewModel
    {
        public Guid AttachmentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public AttachmentType Type { get; set; }
        public int Order { get; set; }
    }

    // ============================================
    // Submit Response
    // ============================================
    public class SubmitResponseViewModel
    {
        [Required]
        public Guid EventId { get; set; }

        public List<SurveyAnswerViewModel>? SurveyAnswers { get; set; }
        public List<DiscussionReplyViewModel>? DiscussionReplies { get; set; }
        public string? SignatureData { get; set; } // Base64 image
    }

    public class SurveyAnswerViewModel
    {
        public Guid SurveyId { get; set; }
        public List<QuestionAnswerViewModel> QuestionAnswers { get; set; } = new();
    }

    public class QuestionAnswerViewModel
    {
        public Guid QuestionId { get; set; }
        public List<Guid>? SelectedOptionIds { get; set; }
    }

    public class DiscussionReplyViewModel
    {
        public Guid DiscussionId { get; set; }
        public string Body { get; set; } = string.Empty;
    }

    // ============================================
    // Confirmation
    // ============================================
    public class ConfirmationViewModel
    {
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}

