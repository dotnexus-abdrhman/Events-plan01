using EvenDAL.Models.Shared.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// المرفقات (صور/PDF) - يمكن أن تكون على مستوى الحدث أو البند
    /// </summary>
    public class Attachment
    {
        public Guid AttachmentId { get; set; }

        public Guid EventId { get; set; }

        public Guid? SectionId { get; set; } // اختياري: عند التعيين يرتبط بالبند

        public AttachmentType Type { get; set; }

        [Required, MaxLength(300)]
        public string FileName { get; set; }

        [Required, MaxLength(500)]
        public string Path { get; set; }

        public long Size { get; set; } // بالبايت

        public string MetadataJson { get; set; } // JSON - معلومات إضافية

        public int Order { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Event Event { get; set; }
        public virtual Section? Section { get; set; }
    }
}

