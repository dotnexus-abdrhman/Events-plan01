using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class DocumentDto
    {
        [Required]
        public Guid DocumentId { get; set; }

        [Required]
        public Guid EventId { get; set; }

        public Guid? AgendaItemId { get; set; }

        [Required, StringLength(255)]
        public string FileName { get; set; }

        [Required, StringLength(500)]
        public string FilePath { get; set; }

        [Range(0, long.MaxValue)]
        public long FileSize { get; set; }

        [StringLength(50)]
        public string FileType { get; set; }

        [Required]
        public Guid UploadedById { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime UploadedAt { get; set; }
    }
}
