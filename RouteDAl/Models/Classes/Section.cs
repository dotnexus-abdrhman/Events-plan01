using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// البند - يحتوي على عنوان ونص حر وقرارات مرتبة
    /// </summary>
    public class Section
    {
        public Guid SectionId { get; set; }

        public Guid EventId { get; set; }

        [Required, MaxLength(300)]
        public string Title { get; set; }

        public string Body { get; set; } // نص حر - NVARCHAR(MAX)

        public int Order { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Event Event { get; set; }
        public virtual ICollection<Decision> Decisions { get; set; } = new List<Decision>();
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
        public virtual ICollection<TableBlock> Tables { get; set; } = new List<TableBlock>();
        public virtual ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
        public virtual ICollection<Survey> Surveys { get; set; } = new List<Survey>();

    }
}

