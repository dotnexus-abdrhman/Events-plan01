using System.ComponentModel.DataAnnotations;

namespace RourtPPl01.ViewModels.Auth
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "البريد الإلكتروني أو رقم الهاتف مطلوب")]
        [Display(Name = "البريد الإلكتروني أو رقم الهاتف")]
        public string Identifier { get; set; } = string.Empty;

        [Display(Name = "تذكرني")]
        public bool RememberMe { get; set; }
    }
}

