using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Data.Configuration
{
    public class AgendaItemConfiguration : IEntityTypeConfiguration<AgendaItem>
    {
        public void Configure(EntityTypeBuilder<AgendaItem> builder)
        {
            builder.HasKey(ai => ai.AgendaItemId);

            builder.Property(ai => ai.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(ai => ai.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Relationships
            builder.HasOne(ai => ai.Event)
                .WithMany(e => e.AgendaItems)
                .HasForeignKey(ai => ai.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ai => ai.Presenter)
                .WithMany(u => u.PresentedItems)
                .HasForeignKey(ai => ai.PresenterId)
                .OnDelete(DeleteBehavior.SetNull);




            // Indexes
            builder.HasIndex(ai => new { ai.EventId, ai.OrderIndex });
            builder.HasIndex(ai => ai.Type);
        }
    }
}
