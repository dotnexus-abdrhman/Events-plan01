using System.ComponentModel.DataAnnotations;

namespace RourtPPl01.Areas.Admin.ViewModels
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "الاسم مطلوب")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "اختيار الجهة مطلوب")]
        public Guid OrganizationId { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = "Attendee"; // Attendee / Organizer / Observer

        public bool IsActive { get; set; } = true;
    }

    public class UsersCreatePageViewModel
    {
        public CreateUserViewModel Form { get; set; } = new();
        public List<OrgItem> Organizations { get; set; } = new();
        public List<string> Roles { get; set; } = new() { "Attendee", "Organizer", "Observer" };
    }

    public class OrgItem
    {
        public Guid OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UsersIndexItem
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string OrganizationName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UsersIndexViewModel
    {
        public List<UsersIndexItem> Users { get; set; } = new();
    }

}

