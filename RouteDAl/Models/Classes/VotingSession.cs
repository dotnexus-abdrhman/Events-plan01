using EvenDAL.Models.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Models.Classes
{
    public class VotingSession
    {
        public Guid VotingSessionId { get; set; }
        public Guid EventId { get; set; }
        public Guid? AgendaItemId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public string Question { get; set; }

        public VotingType Type { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsAnonymous { get; set; }
        public VotingStatus Status { get; set; } = VotingStatus.Pending;
        public string Settings { get; set; } // JSON
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Event Event { get; set; }
        public virtual AgendaItem AgendaItem { get; set; }
        public virtual ICollection<VotingOption> VotingOptions { get; set; } = new List<VotingOption>();
        public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }
}
