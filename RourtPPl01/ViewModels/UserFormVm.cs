using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace EventPresentationlayer.ViewModels
{
    public class UserFormVm
    {
        public Guid? UserId { get; set; }

        [Required(ErrorMessage = "اختيار الجهة مطلوب.")]
        [Display(Name = "الجهة")]
        public Guid OrganizationId { get; set; }

        [Required(ErrorMessage = "الاسم مطلوب.")]
        [StringLength(100, ErrorMessage = "الاسم يجب ألا يزيد عن 100 حرف.")]
        [Display(Name = "الاسم الكامل")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب.")]
        [EmailAddress(ErrorMessage = "صيغة بريد إلكتروني غير صحيحة.")]
        [StringLength(150)]
        [Display(Name = "البريد الإلكتروني")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "رقم هاتف غير صالح.")]
        [StringLength(20)]
        [Display(Name = "الهاتف")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "الدور مطلوب.")]
        [StringLength(50)]
        [Display(Name = "الدور")]
        public string? RoleName { get; set; }

        [Display(Name = "نشط؟")]
        public bool IsActive { get; set; }

        // للعرض/الحفظ غير المباشر فقط (hidden في Edit)
        [Display(Name = "آخر تسجيل دخول")]
        public DateTime? LastLogin { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime? CreatedAt { get; set; }

        // للقوائم
        public IEnumerable<SelectListItem>? Organizations { get; set; }
        public IEnumerable<SelectListItem>? Roles { get; set; }
    }
}
