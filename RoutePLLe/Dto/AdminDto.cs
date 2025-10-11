using System;

namespace EventPl.Dto
{
    public class AdminDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? ProfilePicture { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}
