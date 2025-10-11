using System;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// Public share link for an event. One active link per event.
    /// </summary>
    public class EventPublicLink
    {
        [Key]
        public Guid EventPublicLinkId { get; set; }

        [Required]
        public Guid EventId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Token { get; set; } = string.Empty; // unique token used in /Public/Event/{token}

        public bool IsEnabled { get; set; } = true;

        public DateTime? ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Event? Event { get; set; }
    }
}

