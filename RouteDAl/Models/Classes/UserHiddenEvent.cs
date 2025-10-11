using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EvenDAL.Models.Classes
{
    public class UserHiddenEvent
    {
        [Key, Column(Order = 0)]
        public Guid UserId { get; set; }

        [Key, Column(Order = 1)]
        public Guid EventId { get; set; }

        public DateTime HiddenAt { get; set; } = DateTime.UtcNow;

        // Navigation (optional)
        public virtual User? User { get; set; }
        public virtual Event? Event { get; set; }
    }
}

