using System.ComponentModel.DataAnnotations;

namespace EventPresentationlayer.ViewModels
{
    public class OrganizationFormVm
    {
        public Guid? OrganizationId { get; set; }   // بدل int → Guid?
        [Required(ErrorMessage = "الاسم بالعربية مطلوب")]
        [MaxLength(200, ErrorMessage = "الحد الأقصى 200 حرف")]
        [Display(Name = "الاسم بالعربية")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200, ErrorMessage = "الحد الأقصى 200 حرف")]
        [Display(Name = "الاسم بالإنجليزية")]
        public string? NameEn { get; set; }

        [Display(Name = "النوع")]
        [Range(1, 4, ErrorMessage = "نوع الجهة غير صحيح")]
        public int Type { get; set; } = 2; // افتراضي: خاص

        [Display(Name = "انتهاء الترخيص")]
        public DateTime? LicenseExpiry { get; set; }

        [Display(Name = "نشِطة؟")]
        public bool IsActive { get; set; } = true;

        public string? LicenseKey { get; set; }
        public string? Logo { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? Settings { get; set; }
    }
}
