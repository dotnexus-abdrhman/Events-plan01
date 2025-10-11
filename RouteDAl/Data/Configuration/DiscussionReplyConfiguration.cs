using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class DiscussionReplyConfiguration : IEntityTypeConfiguration<DiscussionReply>
    {
        public void Configure(EntityTypeBuilder<DiscussionReply> builder)
        {
            builder.ToTable("DiscussionReplies");
            builder.HasKey(dr => dr.DiscussionReplyId);
            
            builder.Property(dr => dr.Body).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(dr => dr.CreatedAt).IsRequired();
            
            // العلاقة مع Discussion
            builder.HasOne(dr => dr.Discussion)
                .WithMany(d => d.Replies)
                .HasForeignKey(dr => dr.DiscussionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // العلاقة مع User
            builder.HasOne(dr => dr.User)
                .WithMany(u => u.DiscussionReplies)
                .HasForeignKey(dr => dr.UserId)
                .OnDelete(DeleteBehavior.NoAction); // تجنب Cascade Paths
            
            // فهرس على DiscussionId و CreatedAt (للترتيب الزمني)
            builder.HasIndex(dr => new { dr.DiscussionId, dr.CreatedAt });
        }
    }
}

