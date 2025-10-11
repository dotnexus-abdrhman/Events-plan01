using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RouteDAl.Data.Configuration
{
    public class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
    {
        public void Configure(EntityTypeBuilder<Proposal> builder)
        {
            builder.HasKey(x => x.ProposalId);

            builder.Property(x => x.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.Body)
                   .HasMaxLength(2000);

            builder.Property(x => x.Status)
                   .HasMaxLength(20)
                   .HasDefaultValue("Pending");

            builder.HasOne(x => x.Event)
                   .WithMany()
                   .HasForeignKey(x => x.EventId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                   .WithMany()
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Restrict); // مهم لتفادي الـ cycles
        }
    }
}
