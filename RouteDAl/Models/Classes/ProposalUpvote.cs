using System;

namespace EvenDAL.Models.Classes
{
    public class ProposalUpvote
    {
        public Guid ProposalUpvoteId { get; set; }
        public Guid ProposalId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Proposal Proposal { get; set; }
        public virtual User User { get; set; }
    }
}
