using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class DecisionConfiguration : IEntityTypeConfiguration<Decision>
    {
        public void Configure(EntityTypeBuilder<Decision> builder)
        {
            builder.ToTable("Decisions");
            builder.HasKey(d => d.DecisionId);
            
            builder.Property(d => d.Title).IsRequired().HasMaxLength(300);
            builder.Property(d => d.Order).IsRequired();
            builder.Property(d => d.CreatedAt).IsRequired();
            
            // العلاقة مع Section
            builder.HasOne(d => d.Section)
                .WithMany(s => s.Decisions)
                .HasForeignKey(d => d.SectionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // فهرس على SectionId و Order
            builder.HasIndex(d => new { d.SectionId, d.Order });
        }
    }
}

