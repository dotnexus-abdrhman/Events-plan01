using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// النقاش - مكون عام على مستوى الحدث
    /// </summary>
    public class Discussion
    {
        public Guid DiscussionId { get; set; }

        public Guid EventId { get; set; }

        public Guid? SectionId { get; set; } // اختياري: يربط النقاش بالبند عند الحاجة


        [Required, MaxLength(300)]
        public string Title { get; set; }

        [Required]
        public string Purpose { get; set; } // الغرض/الهدف - NVARCHAR(MAX)

        public int Order { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual Section? Section { get; set; }


        // Navigation Properties
        public virtual Event Event { get; set; }
        public virtual ICollection<DiscussionReply> Replies { get; set; } = new List<DiscussionReply>();
    }
}

