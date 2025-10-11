using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class TableBlockConfiguration : IEntityTypeConfiguration<TableBlock>
    {
        public void Configure(EntityTypeBuilder<TableBlock> builder)
        {
            builder.ToTable("TableBlocks");
            builder.HasKey(tb => tb.TableBlockId);
            
            builder.Property(tb => tb.Title).IsRequired().HasMaxLength(300);
            builder.Property(tb => tb.Description).HasMaxLength(1000);
            builder.Property(tb => tb.HasHeader).IsRequired();
            builder.Property(tb => tb.RowsJson).IsRequired().HasColumnType("nvarchar(max)");
            builder.Property(tb => tb.Order).IsRequired();
            builder.Property(tb => tb.CreatedAt).IsRequired();
            
            // العلاقة مع Event
            builder.HasOne(tb => tb.Event)
                .WithMany(e => e.TableBlocks)
                .HasForeignKey(tb => tb.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // فهرس على EventId و Order
            builder.HasIndex(tb => new { tb.EventId, tb.Order });
        }
    }
}

