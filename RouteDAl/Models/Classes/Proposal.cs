using System;

namespace EvenDAL.Models.Classes
{
    public class Proposal
    {
        public Guid ProposalId { get; set; }
        public Guid EventId { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public int Upvotes { get; set; } = 0;
        public string Status { get; set; } = "Pending"; // Pending / Accepted / Rejected
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual Event Event { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<ProposalUpvote> UpvoteList { get; set; } = new List<ProposalUpvote>();
    }
}
