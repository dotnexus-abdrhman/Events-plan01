using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class SurveyConfiguration : IEntityTypeConfiguration<Survey>
    {
        public void Configure(EntityTypeBuilder<Survey> builder)
        {
            builder.ToTable("Surveys");
            builder.HasKey(s => s.SurveyId);
            
            builder.Property(s => s.Title).IsRequired().HasMaxLength(300);
            builder.Property(s => s.Order).IsRequired();
            builder.Property(s => s.IsActive).IsRequired();
            builder.Property(s => s.CreatedAt).IsRequired();
            
            // العلاقة مع Event
            builder.HasOne(s => s.Event)
                .WithMany(e => e.Surveys)
                .HasForeignKey(s => s.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // فهرس على EventId و Order
            builder.HasIndex(s => new { s.EventId, s.Order });
        }
    }
}

