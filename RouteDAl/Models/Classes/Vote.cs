using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Models.Classes
{
    public class Vote
    {
        public Guid VoteId { get; set; }
        public Guid VotingSessionId { get; set; }
        public Guid UserId { get; set; }
        public Guid? VotingOptionId { get; set; }

        [MaxLength(1000)]
        public string CustomAnswer { get; set; }

        public DateTime VotedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual VotingSession VotingSession { get; set; }
        public virtual User User { get; set; }
        public virtual VotingOption VotingOption { get; set; }
    }
}
