using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class DiscussionConfiguration : IEntityTypeConfiguration<Discussion>
    {
        public void Configure(EntityTypeBuilder<Discussion> builder)
        {
            builder.ToTable("Discussions");
            builder.HasKey(d => d.DiscussionId);
            
            builder.Property(d => d.Title).IsRequired().HasMaxLength(300);
            builder.Property(d => d.Purpose).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(d => d.Order).IsRequired();
            builder.Property(d => d.IsActive).IsRequired();
            builder.Property(d => d.CreatedAt).IsRequired();
            
            // العلاقة مع Event
            builder.HasOne(d => d.Event)
                .WithMany(e => e.Discussions)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // فهرس على EventId و Order
            builder.HasIndex(d => new { d.EventId, d.Order });
        }
    }
}

