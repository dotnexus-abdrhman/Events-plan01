using EvenDAL.Models.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Models.Classes
{
    public class EventParticipant
    {
        public Guid EventParticipantId { get; set; }

        public Guid EventId { get; set; }
        public Guid UserId { get; set; }

        public ParticipantRole Role { get; set; }
        public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
        public DateTime? JoinedAt { get; set; }
        public ParticipantStatus Status { get; set; } = ParticipantStatus.Invited;

        public virtual Event Event { get; set; }
        public virtual User User { get; set; }

    }
}
