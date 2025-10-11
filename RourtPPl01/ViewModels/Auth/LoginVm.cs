using System.ComponentModel.DataAnnotations;

namespace EventPresentationlayer.ViewModels.Auth
{
    public class LoginVm
    {
        [Required(ErrorMessage = "اختر الدور.")]
        public string RoleChoice { get; set; } = "Admin"; // Admin | User

        [EmailAddress(ErrorMessage = "صيغة البريد غير صحيحة.")]
        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? ReturnUrl { get; set; }
    }
}

