using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        [Range(1, 4, ErrorMessage = "نوع المجموعة غير صحيح")]
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

        // المستخدمون المضافون عند إنشاء/تعديل المجموعة
        [Display(Name = "المستخدمون")]
        public List<Guid> SelectedUserIds { get; set; } = new();

        // للواجهة القديمة (متروك للتوافق)
        public List<SelectListItem> Users { get; set; } = new();

        // مصدر بيانات واجهة "واتساب" (بحث + اختيار)
        public List<UserLiteVm> AvailableUsers { get; set; } = new();

        // لصفحة التعديل لعرض أعضاء المجموعة الحاليين
        public List<UserLiteVm> CurrentUsers { get; set; } = new();
    }

    public class UserLiteVm
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
