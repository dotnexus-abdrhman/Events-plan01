using System;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// Guest participant created via public event link. Links to a synthetic User for reuse of existing flow.
    /// </summary>
    public class PublicEventGuest
    {
        [Key]
        public Guid GuestId { get; set; }

        [Required]
        public Guid EventId { get; set; }

        [Required]
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required]
        [MaxLength(100)]
        public string UniqueToken { get; set; } = string.Empty; // token used to access

        [Required]
        public bool IsGuest { get; set; } = true;

        // Synthetic user created to reuse survey/discussion/signature flows
        [Required]
        public Guid UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Event? Event { get; set; }
        public virtual User? User { get; set; }
    }
}

