using System;
using System.ComponentModel.DataAnnotations;

namespace EventPresentationlayer.ViewModels
{
    public class SurveyOptionVm
    {
        // أزلنا Required للسماح بتجاهل الخيارات الفارغة لاحقًا في الكنترولر
        [StringLength(200, ErrorMessage = "الحد الأقصى 200 حرف")]
        [Display(Name = "الخيار")]
        public string? Text { get; set; }

        // للحفاظ على ربط الخيار عند التعديل
        public Guid? VotingOptionId { get; set; }

        public int Order { get; set; } = 0; // للترتيب في الواجهة فقط
    }
}
