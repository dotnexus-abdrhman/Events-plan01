using System.ComponentModel.DataAnnotations;

namespace RourtPPl01.ViewModels.Auth
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "نوع الحساب")]
        public string RoleChoice { get; set; } = "User"; // Admin | User

        [Required]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "البريد الإلكتروني")]
        [EmailAddress]
        public string? Email { get; set; }

        [Display(Name = "رقم الجوال")]
        public string? Phone { get; set; }

        // For User registration
        [Display(Name = "المجموعة")]
        public Guid OrganizationId { get; set; }

        [Display(Name = "الدور داخل المجموعة")]
        public string? RoleName { get; set; } // Attendee | Organizer | Observer
    }
}

