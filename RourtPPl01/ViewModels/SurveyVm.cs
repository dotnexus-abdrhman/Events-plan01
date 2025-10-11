using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventPresentationlayer.ViewModels
{
    public class SurveyVm
    {
        // ملاحظة: أزلنا Required/MinLength للسماح بتجاهل الاستبيانات الفارغة في الكنترولر
        [Display(Name = "السؤال")]
        [StringLength(500)]
        public string? Question { get; set; }

        [Display(Name = "سماح باختيار عدة خيارات")]
        public bool IsMultipleChoice { get; set; }

        // للحفاظ على ربط الجلسات عند التعديل
        public Guid? VotingSessionId { get; set; }

        [Display(Name = "الخيارات")]
        public List<SurveyOptionVm> Options { get; set; } = new();
    }
}
