using System.ComponentModel.DataAnnotations;

namespace EventPresentationlayer.ViewModels.Auth
{
    public class RegisterVm
    {
        [Required(ErrorMessage = "اختر الدور.")]
        public string RoleChoice { get; set; } = string.Empty; // Admin | User

        [Required(ErrorMessage = "الاسم الكامل مطلوب.")]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "البريد الإلكتروني")]
        public string? Email { get; set; }

        [Display(Name = "رقم الجوال")]
        public string? Phone { get; set; }
    }
}

