using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class DecisionItemConfiguration : IEntityTypeConfiguration<DecisionItem>
    {
        public void Configure(EntityTypeBuilder<DecisionItem> builder)
        {
            builder.ToTable("DecisionItems");
            builder.HasKey(di => di.DecisionItemId);
            
            builder.Property(di => di.Text).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(di => di.Order).IsRequired();
            builder.Property(di => di.CreatedAt).IsRequired();
            
            // العلاقة مع Decision
            builder.HasOne(di => di.Decision)
                .WithMany(d => d.Items)
                .HasForeignKey(di => di.DecisionId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // فهرس على DecisionId و Order
            builder.HasIndex(di => new { di.DecisionId, di.Order });
        }
    }
}

