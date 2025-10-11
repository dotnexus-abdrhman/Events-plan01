using System.ComponentModel.DataAnnotations;
using EvenDAL.Models.Shared.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RourtPPl01.Areas.Admin.ViewModels
{
    // ============================================
    // Events Index
    // ============================================
    public class EventsIndexViewModel
    {
        public List<EventListItemViewModel> Events { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int? StatusFilter { get; set; }
        public DateTime? StartDateFilter { get; set; }
    }

    public class EventListItemViewModel
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public EventStatus Status { get; set; }
        public bool RequireSignature { get; set; }
    }

    // ============================================
    // Event Details
    // ============================================
    public class EventDetailsViewModel
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public EventStatus Status { get; set; }
        public bool RequireSignature { get; set; }

        public List<SectionViewModel> Sections { get; set; } = new();
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
    }

    public class DecisionViewModel
    {
        public Guid DecisionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<DecisionItemViewModel> Items { get; set; } = new();
    }

    public class DecisionItemViewModel
    {
        public Guid DecisionItemId { get; set; }
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
    }

    public class SurveyViewModel
    {
        public Guid SurveyId { get; set; }
        public Guid? SectionId { get; set; }
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
        public Guid? SectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }

    public class TableViewModel
    {
        public Guid TableBlockId { get; set; }
        public Guid? SectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool HasHeader { get; set; }
        public List<List<TableCellViewModel>>? Rows { get; set; }
    }

    public class TableCellViewModel
    {
        public string Value { get; set; } = string.Empty;
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public string Align { get; set; } = "right";
    }

    public class AttachmentViewModel
    {
        public Guid AttachmentId { get; set; }
        public Guid? SectionId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public AttachmentType Type { get; set; }
        public int Order { get; set; }
    }

    // ============================================
    // Create Event
    // ============================================
    public class CreateEventViewModel
    {
        [Required(ErrorMessage = "عنوان الحدث مطلوب")]
        [StringLength(200, ErrorMessage = "العنوان يجب ألا يتجاوز 200 حرف")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "الوصف يجب ألا يتجاوز 2000 حرف")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        public DateTime StartAt { get; set; }

        [Required(ErrorMessage = "تاريخ النهاية مطلوب")]
        public DateTime EndAt { get; set; }

        [Display(Name = "يتطلب توقيع المستخدم")]
        public bool RequireSignature { get; set; }

        public EventStatus Status { get; set; } = EventStatus.Draft;

        [Required(ErrorMessage = "الجهة مطلوبة")]
        public Guid OrganizationId { get; set; }

        public IEnumerable<SelectListItem> Organizations { get; set; } = Enumerable.Empty<SelectListItem>();

        // JSON مجمّع لمكونات البناء (بنود/استبيانات/نقاشات/جداول/مرفقات)
        public string? BuilderJson { get; set; }
    }

    // ============================================
    // Edit Event
    // ============================================
    public class EditEventViewModel
    {
        public Guid EventId { get; set; }

        [Required(ErrorMessage = "عنوان الحدث مطلوب")]
        [StringLength(200, ErrorMessage = "العنوان يجب ألا يتجاوز 200 حرف")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "الوصف يجب ألا يتجاوز 2000 حرف")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        public DateTime StartAt { get; set; }

        [Required(ErrorMessage = "تاريخ النهاية مطلوب")]
        public DateTime EndAt { get; set; }

        [Display(Name = "يتطلب توقيع المستخدم")]
        public bool RequireSignature { get; set; }

        public EventStatus Status { get; set; } = EventStatus.Draft;
    }
}

