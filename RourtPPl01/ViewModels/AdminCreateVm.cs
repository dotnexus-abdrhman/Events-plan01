using System.ComponentModel.DataAnnotations;

namespace EventPresentationlayer.ViewModels.Admins
{
    public class AdminCreateVm
    {
        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress(ErrorMessage = "بريد غير صالح")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "الاسم (اختياري)")]
        [StringLength(150)]
        public string? FullName { get; set; }
    }
}
