using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class UserSignatureConfiguration : IEntityTypeConfiguration<UserSignature>
    {
        public void Configure(EntityTypeBuilder<UserSignature> builder)
        {
            builder.ToTable("UserSignatures");
            builder.HasKey(us => us.UserSignatureId);
            
            builder.Property(us => us.ImagePath).HasMaxLength(500);
            builder.Property(us => us.Data).HasColumnType("nvarchar(max)");
            builder.Property(us => us.CreatedAt).IsRequired();
            
            // العلاقة مع Event
            builder.HasOne(us => us.Event)
                .WithMany(e => e.UserSignatures)
                .HasForeignKey(us => us.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // العلاقة مع User
            builder.HasOne(us => us.User)
                .WithMany(u => u.Signatures)
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.NoAction); // تجنب Cascade Paths
            
            // فهرس فريد: كل مستخدم يوقع مرة واحدة على كل حدث
            builder.HasIndex(us => new { us.EventId, us.UserId })
                .IsUnique();
        }
    }
}

