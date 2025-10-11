using System;

namespace EvenDAL.Models.Classes
{
    public class DiscussionPost
    {
        public Guid DiscussionPostId { get; set; }
        public Guid EventId { get; set; }
        public Guid? ParentId { get; set; }
        public Guid UserId { get; set; }
        public string Body { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Event Event { get; set; }
        public virtual DiscussionPost Parent { get; set; }
        public virtual ICollection<DiscussionPost> Replies { get; set; } = new List<DiscussionPost>();
        public virtual User User { get; set; }
    }
}
