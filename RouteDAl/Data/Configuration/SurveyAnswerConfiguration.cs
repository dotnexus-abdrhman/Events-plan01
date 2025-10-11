using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class SurveyAnswerConfiguration : IEntityTypeConfiguration<SurveyAnswer>
    {
        public void Configure(EntityTypeBuilder<SurveyAnswer> builder)
        {
            builder.ToTable("SurveyAnswers");
            builder.HasKey(sa => sa.SurveyAnswerId);
            
            builder.Property(sa => sa.CreatedAt).IsRequired();
            
            // العلاقة مع Event
            builder.HasOne(sa => sa.Event)
                .WithMany(e => e.SurveyAnswers)
                .HasForeignKey(sa => sa.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // العلاقة مع SurveyQuestion
            builder.HasOne(sa => sa.Question)
                .WithMany(sq => sq.Answers)
                .HasForeignKey(sa => sa.QuestionId)
                .OnDelete(DeleteBehavior.NoAction); // تجنب Cascade Paths
            
            // العلاقة مع User
            builder.HasOne(sa => sa.User)
                .WithMany(u => u.SurveyAnswers)
                .HasForeignKey(sa => sa.UserId)
                .OnDelete(DeleteBehavior.NoAction); // تجنب Cascade Paths
            
            // فهرس فريد: كل مستخدم يجيب مرة واحدة على كل سؤال
            builder.HasIndex(sa => new { sa.QuestionId, sa.UserId })
                .IsUnique();
            
            // فهرس على EventId
            builder.HasIndex(sa => sa.EventId);
        }
    }
}

