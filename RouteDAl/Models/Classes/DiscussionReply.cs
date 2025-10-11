using System;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// رد المستخدم في النقاش
    /// </summary>
    public class DiscussionReply
    {
        public Guid DiscussionReplyId { get; set; }
        
        public Guid DiscussionId { get; set; }
        
        public Guid UserId { get; set; }
        
        [Required]
        public string Body { get; set; } // NVARCHAR(MAX)
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual Discussion Discussion { get; set; }
        public virtual User User { get; set; }
    }
}

