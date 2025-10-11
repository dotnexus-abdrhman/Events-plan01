using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Models.Classes
{
    public class Document
    {
        public Guid DocumentId { get; set; }
        public Guid EventId { get; set; }
        public Guid? AgendaItemId { get; set; }

        [Required, MaxLength(255)]
        public string FileName { get; set; }

        [Required, MaxLength(500)]
        public string FilePath { get; set; }

        public long FileSize { get; set; }

        [MaxLength(50)]
        public string FileType { get; set; }

        public Guid UploadedById { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Event Event { get; set; }
        public virtual AgendaItem AgendaItem { get; set; }
        public virtual User UploadedBy { get; set; }
    }
}
