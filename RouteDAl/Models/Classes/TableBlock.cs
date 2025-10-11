using System;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// جدول مرن - يُحفظ كـJSON (مثل Word) - يمكن ربطه بالبند
    /// </summary>
    public class TableBlock
    {
        public Guid TableBlockId { get; set; }

        public Guid EventId { get; set; }

        public Guid? SectionId { get; set; } // اختياري

        [Required, MaxLength(300)]
        public string Title { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public bool HasHeader { get; set; } = false;

        [Required]
        public string RowsJson { get; set; } // NVARCHAR(MAX) - JSON مرن

        public int Order { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Event Event { get; set; }
        public virtual Section? Section { get; set; }
    }
}

