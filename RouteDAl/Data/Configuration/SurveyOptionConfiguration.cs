using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class SurveyOptionConfiguration : IEntityTypeConfiguration<SurveyOption>
    {
        public void Configure(EntityTypeBuilder<SurveyOption> builder)
        {
            builder.ToTable("SurveyOptions");
            builder.HasKey(so => so.SurveyOptionId);
            
            builder.Property(so => so.Text).IsRequired().HasMaxLength(500);
            builder.Property(so => so.Order).IsRequired();
            builder.Property(so => so.CreatedAt).IsRequired();
            
            // العلاقة مع SurveyQuestion
            builder.HasOne(so => so.Question)
                .WithMany(sq => sq.Options)
                .HasForeignKey(so => so.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // فهرس على QuestionId و Order
            builder.HasIndex(so => new { so.QuestionId, so.Order });
        }
    }
}

