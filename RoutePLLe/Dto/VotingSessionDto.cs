using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class VotingSessionDto
    {
        [Required]
        public Guid VotingSessionId { get; set; }

        [Required]
        public Guid EventId { get; set; }

        public Guid? AgendaItemId { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Question { get; set; }

        [StringLength(50)]
        public string TypeName { get; set; }      // SingleChoice / MultipleChoice / Rating / OpenEnded

        [Required]
        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public bool IsAnonymous { get; set; }

        [StringLength(50)]
        public string StatusName { get; set; }    // Pending / Active / Closed

        [StringLength(4000)]
        public string Settings { get; set; }      // JSON

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }
    }
}
