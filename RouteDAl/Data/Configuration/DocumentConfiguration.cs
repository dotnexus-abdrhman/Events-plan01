using EvenDAL.Models.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Data.Configuration
{
    
    public class DocumentConfiguration : IEntityTypeConfiguration<Document>
    {
        public void Configure(EntityTypeBuilder<Document> builder)
        {
            builder.HasKey(d => d.DocumentId);

            builder.Property(d => d.FileName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(d => d.FilePath)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(d => d.FileType)
                .HasMaxLength(50);

            builder.Property(d => d.UploadedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Relationships
            builder.HasOne(d => d.Event)
                .WithMany(e => e.Documents)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(d => d.AgendaItem)
                .WithMany(ai => ai.Documents)
                .HasForeignKey(d => d.AgendaItemId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(d => d.UploadedBy)
                .WithMany(u => u.UploadedDocuments)
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(d => d.EventId);
            builder.HasIndex(d => d.UploadedAt);
            builder.HasIndex(d => d.FileType);
        }
    }
}
