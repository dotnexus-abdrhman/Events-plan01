using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class SurveyQuestionConfiguration : IEntityTypeConfiguration<SurveyQuestion>
    {
        public void Configure(EntityTypeBuilder<SurveyQuestion> builder)
        {
            builder.ToTable("SurveyQuestions");
            builder.HasKey(sq => sq.SurveyQuestionId);
            
            builder.Property(sq => sq.Text).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(sq => sq.Type).IsRequired();
            builder.Property(sq => sq.Order).IsRequired();
            builder.Property(sq => sq.IsRequired).IsRequired();
            builder.Property(sq => sq.CreatedAt).IsRequired();
            
            // العلاقة مع Survey
            builder.HasOne(sq => sq.Survey)
                .WithMany(s => s.Questions)
                .HasForeignKey(sq => sq.SurveyId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // فهرس على SurveyId و Order
            builder.HasIndex(sq => new { sq.SurveyId, sq.Order });
        }
    }
}

