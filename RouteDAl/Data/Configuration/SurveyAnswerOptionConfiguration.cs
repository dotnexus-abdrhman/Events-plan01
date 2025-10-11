using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class SurveyAnswerOptionConfiguration : IEntityTypeConfiguration<SurveyAnswerOption>
    {
        public void Configure(EntityTypeBuilder<SurveyAnswerOption> builder)
        {
            builder.ToTable("SurveyAnswerOptions");
            
            // Composite Primary Key
            builder.HasKey(sao => new { sao.SurveyAnswerId, sao.OptionId });
            
            builder.Property(sao => sao.CreatedAt).IsRequired();
            
            // العلاقة مع SurveyAnswer
            builder.HasOne(sao => sao.Answer)
                .WithMany(sa => sa.SelectedOptions)
                .HasForeignKey(sao => sao.SurveyAnswerId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // العلاقة مع SurveyOption
            builder.HasOne(sao => sao.Option)
                .WithMany(so => so.AnswerOptions)
                .HasForeignKey(sao => sao.OptionId)
                .OnDelete(DeleteBehavior.NoAction); // تجنب Cascade Paths
        }
    }
}

