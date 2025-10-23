using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class UserDto
    {
        [Required]
        public Guid UserId { get; set; }

        // اختيار المجموعة اختياري
        public Guid? OrganizationId { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; }

        [Required, EmailAddress, StringLength(150)]
        public string Email { get; set; }

        [Phone, StringLength(20)]
        public string Phone { get; set; }

        [StringLength(50)]
        public string RoleName { get; set; }   // Admin / Organizer / Attendee / Observer

        [StringLength(500)]
        public string ProfilePicture { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime? LastLogin { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }
    }

}
