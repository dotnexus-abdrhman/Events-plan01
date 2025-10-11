using System;
using System.Collections.Generic;

namespace EventPl.Dto.Mina
{
    /// <summary>
    /// DTO للنقاش (Discussion) مع الردود
    /// </summary>
    public class DiscussionDto
    {
        public Guid DiscussionId { get; set; }
        public Guid EventId { get; set; }
        public Guid? SectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // الردود
        public List<DiscussionReplyDto> Replies { get; set; } = new();
    }

    /// <summary>
    /// DTO لرد النقاش (DiscussionReply)
    /// </summary>
    public class DiscussionReplyDto
    {
        public Guid DiscussionReplyId { get; set; }
        public Guid DiscussionId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty; // للعرض
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO لإضافة رد جديد
    /// </summary>
    public class AddDiscussionReplyRequest
    {
        public Guid DiscussionId { get; set; }
        public Guid UserId { get; set; }
        public string Body { get; set; } = string.Empty;
    }
}

