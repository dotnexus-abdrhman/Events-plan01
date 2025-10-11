using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RouteDAl.Data.Configuration
{
    public class DiscussionPostConfiguration : IEntityTypeConfiguration<DiscussionPost>
    {
        public void Configure(EntityTypeBuilder<DiscussionPost> builder)
        {
            builder.HasKey(x => x.DiscussionPostId);

            builder.Property(x => x.Body)
                   .IsRequired()
                   .HasMaxLength(1000);

            builder.HasOne(x => x.Event)
                   .WithMany()
                   .HasForeignKey(x => x.EventId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Parent)
                   .WithMany(x => x.Replies)
                   .HasForeignKey(x => x.ParentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.User)
                   .WithMany()
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Restrict); // عشان مايحصلش multiple cascade paths
        }
    }
}
