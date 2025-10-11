using System.ComponentModel.DataAnnotations;

namespace RourtPPl01.Areas.Admin.ViewModels
{
    // ============================================
    // Manage Components
    // ============================================
    public class ManageComponentsViewModel
    {
        public Guid EventId { get; set; }
        public int SurveysCount { get; set; }
        public int DiscussionsCount { get; set; }
        public int TablesCount { get; set; }
        public int AttachmentsCount { get; set; }
    }

    // ============================================
    // Add Survey
    // ============================================
    public class AddSurveyViewModel
    {
        [Required]
        public Guid EventId { get; set; }

        // اختياري: عند تمريره يتم إنشاء الاستبيان على مستوى البند
        public Guid? SectionId { get; set; }

        [Required(ErrorMessage = "عنوان الاستبيان مطلوب")]
        [StringLength(200, ErrorMessage = "العنوان يجب ألا يتجاوز 200 حرف")]
        public string Title { get; set; } = string.Empty;
    }

    // ============================================
    // Add Discussion
    // ============================================
    public class AddDiscussionViewModel
    {
        [Required]
        public Guid EventId { get; set; }

        // اختياري: عند تمريره يتم إنشاء النقاش على مستوى البند
        public Guid? SectionId { get; set; }

        [Required(ErrorMessage = "عنوان النقاش مطلوب")]
        [StringLength(200, ErrorMessage = "العنوان يجب ألا يتجاوز 200 حرف")]
        public string Title { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "الغرض يجب ألا يتجاوز 1000 حرف")]
        public string? Purpose { get; set; }
    }

    // ============================================
    // Add Table
    // ============================================
    public class AddTableViewModel
    {
        [Required]
        public Guid EventId { get; set; }

        // اختياري: عند تمريره يتم إنشاء الجدول على مستوى البند
        public Guid? SectionId { get; set; }

        [Required(ErrorMessage = "عنوان الجدول مطلوب")]
        [StringLength(200, ErrorMessage = "العنوان يجب ألا يتجاوز 200 حرف")]
        public string Title { get; set; } = string.Empty;

        public List<string>? Headers { get; set; }
    }

    // ============================================
    // Upload Attachment
    // ============================================
    public class UploadAttachmentViewModel
    {
        [Required]
        public Guid EventId { get; set; }

        // اختياري: عند تمريره يتم رفع المرفق على مستوى البند
        public Guid? SectionId { get; set; }

        [StringLength(200, ErrorMessage = "العنوان يجب ألا يتجاوز 200 حرف")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "الملف مطلوب")]
        public IFormFile File { get; set; } = null!;
    }
}

