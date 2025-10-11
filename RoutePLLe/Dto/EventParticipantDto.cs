using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class EventParticipantDto
    {
        public Guid EventParticipantId { get; set; }
        [Required]
        public Guid EventId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [StringLength(50)]
        public string RoleName { get; set; }      // Organizer / Attendee / Observer

        [DataType(DataType.DateTime)]
        public DateTime InvitedAt { get; set; }

        public DateTime? JoinedAt { get; set; }

        [StringLength(50)]
        public string StatusName { get; set; }    // Invited / Accepted / Declined
    }
}
