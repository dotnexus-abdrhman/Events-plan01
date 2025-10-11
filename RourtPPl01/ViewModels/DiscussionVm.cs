using System.ComponentModel.DataAnnotations;

namespace EventPresentationlayer.ViewModels
{
    public class DiscussionVm
    {
        [Required(ErrorMessage = "الغرض/الهدف مطلوب")]
        [StringLength(300, ErrorMessage = "الحد الأقصى 300 حرف")]
        [Display(Name = "الغرض/الهدف")]
        public string Goal { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "الحد الأقصى 200 حرف")]
        [Display(Name = "العنوان (اختياري)")]
        public string? Title { get; set; }
    }
}

