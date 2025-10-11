using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class EventDto
    {
        [Required]
        public Guid EventId { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Required]
        public Guid CreatedById { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        [Required]
        public DateTime StartAt { get; set; }

        [Required]
        public DateTime EndAt { get; set; }

        [StringLength(50)]
        public string StatusName { get; set; } = "Draft";   // Draft / Published / InProgress / Completed / Cancelled

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool RequireSignature { get; set; }

        // Legacy properties (للتوافق مع الكود القديم)
        [StringLength(50)]
        public string TypeName { get; set; } = "Meeting";     // Meeting / Survey / Workshop / ...

        [StringLength(4000)]
        public string Settings { get; set; } = "{}";         // JSON

        public bool AllowProposals { get; set; }
        public bool AllowDiscussion { get; set; }
    }
}
