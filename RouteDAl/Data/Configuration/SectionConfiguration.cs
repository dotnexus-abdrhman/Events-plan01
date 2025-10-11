using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class SectionConfiguration : IEntityTypeConfiguration<Section>
    {
        public void Configure(EntityTypeBuilder<Section> builder)
        {
            builder.ToTable("Sections");
            builder.HasKey(s => s.SectionId);
            
            builder.Property(s => s.Title).IsRequired().HasMaxLength(300);
            builder.Property(s => s.Body).HasColumnType("nvarchar(max)");
            builder.Property(s => s.Order).IsRequired();
            builder.Property(s => s.CreatedAt).IsRequired();
            
            // العلاقة مع Event
            builder.HasOne(s => s.Event)
                .WithMany(e => e.Sections)
                .HasForeignKey(s => s.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // فهرس على EventId و Order
            builder.HasIndex(s => new { s.EventId, s.Order });
        }
    }
}

