using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RouteDAl.Data.Configuration
{
    public class ProposalUpvoteConfiguration : IEntityTypeConfiguration<ProposalUpvote>
    {
        public void Configure(EntityTypeBuilder<ProposalUpvote> builder)
        {
            builder.HasKey(x => x.ProposalUpvoteId);

            builder.HasOne(x => x.Proposal)
                   .WithMany(p => p.UpvoteList)
                   .HasForeignKey(x => x.ProposalId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                   .WithMany()
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Restrict); // ممنوع Cascade مع User
        }
    }
}
