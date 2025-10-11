using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Models.Classes
{
    public class VotingOption
    {
        public Guid VotingOptionId { get; set; }
        public Guid VotingSessionId { get; set; }

        [Required, MaxLength(500)]
        public string Text { get; set; }

        public int OrderIndex { get; set; }

        // Navigation Properties
        public virtual VotingSession VotingSession { get; set; }
        public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }
}
