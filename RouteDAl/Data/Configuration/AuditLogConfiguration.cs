using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(al => al.AuditLogId);
            
            builder.Property(al => al.Action).IsRequired().HasMaxLength(100);
            builder.Property(al => al.Entity).IsRequired().HasMaxLength(100);
            builder.Property(al => al.At).IsRequired();
            builder.Property(al => al.MetaJson).HasColumnType("nvarchar(max)");
            
            // فهارس للبحث السريع
            builder.HasIndex(al => al.At);
            builder.HasIndex(al => new { al.Entity, al.EntityId });
            builder.HasIndex(al => al.ActorId);
        }
    }
}

