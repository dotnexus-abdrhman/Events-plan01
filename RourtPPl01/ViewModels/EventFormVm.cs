using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EventPresentationlayer.ViewModels
{
    public class EventFormVm
    {
        public Guid? EventId { get; set; }

        [Required(ErrorMessage = "المجموعة مطلوبة")]
        public Guid OrganizationId { get; set; }

        // يتعبّى من المستخدم الحالي في الكنترولر
        public Guid CreatedById { get; set; }

        [Required(ErrorMessage = "عنوان الفعالية مطلوب")]
        [StringLength(200, ErrorMessage = "الحد الأقصى 200 حرف")]
        [Display(Name = "عنوان الفعالية")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "الوصف")]
        [StringLength(2000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "الحالة مطلوبة")]
        [Display(Name = "الحالة")]
        public string StatusName { get; set; } = string.Empty; // "مسودة" / "Active" ... (العرض بالعربي)

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        [Display(Name = "تاريخ البداية")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "تاريخ النهاية مطلوب")]
        [Display(Name = "تاريخ النهاية")]
        public DateTime EndDate { get; set; }

        [Display(Name = "السماح بالنقاش")]
        public bool AllowDiscussion { get; set; }

        [Display(Name = "السماح بالمقترحات")]
        public bool AllowProposals { get; set; }

        // استبيانات متعددة
        [MinLength(0)]
        public List<SurveyVm> Polls { get; set; } = new();

        // نقاشات متعددة
        [MinLength(0)]
        public List<DiscussionVm> Discussions { get; set; } = new();

        // جداول متعددة (اختيارية)
        [MinLength(0)]
        public List<TableVm> Tables { get; set; } = new();

        // الملفات والصور
        [Display(Name = "صور الحدث")]
        public IFormFile[]? Images { get; set; }

        [Display(Name = "مرفقات توثيق")]
        public IFormFile[]? Attachments { get; set; }
    }
}
