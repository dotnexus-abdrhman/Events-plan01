using EvenDAL.Models.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Models.Classes
{
    public class AgendaItem
    {
        public Guid AgendaItemId { get; set; }
        public Guid EventId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }
        public int OrderIndex { get; set; }
        public int EstimatedDuration { get; set; } // minutes
        public AgendaItemType Type { get; set; }
        public bool RequiresVoting { get; set; }
        public Guid? PresenterId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Event Event { get; set; }
        public virtual User Presenter { get; set; }
        public virtual ICollection<VotingSession> VotingSessions { get; set; } = new List<VotingSession>();
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}
