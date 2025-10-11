using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class NotificationDto
    {
        [Required]
        public Guid NotificationId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public Guid? EventId { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        [StringLength(50)]
        public string TypeName { get; set; }   // Email / SMS / Push / InApp

        public bool IsRead { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }

        public DateTime? ReadAt { get; set; }
    }
}
