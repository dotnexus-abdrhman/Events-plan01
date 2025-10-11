using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
    {
        public void Configure(EntityTypeBuilder<Attachment> builder)
        {
            builder.ToTable("Attachments");
            builder.HasKey(a => a.AttachmentId);
            
            builder.Property(a => a.Type).IsRequired();
            builder.Property(a => a.FileName).IsRequired().HasMaxLength(300);
            builder.Property(a => a.Path).IsRequired().HasMaxLength(500);
            builder.Property(a => a.Size).IsRequired();
            builder.Property(a => a.MetadataJson).HasColumnType("nvarchar(max)");
            builder.Property(a => a.Order).IsRequired();
            builder.Property(a => a.CreatedAt).IsRequired();
            
            // العلاقة مع Event
            builder.HasOne(a => a.Event)
                .WithMany(e => e.Attachments)
                .HasForeignKey(a => a.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // فهرس على EventId و Order
            builder.HasIndex(a => new { a.EventId, a.Order });
            
            // فهرس على Type للبحث السريع
            builder.HasIndex(a => a.Type);
        }
    }
}

