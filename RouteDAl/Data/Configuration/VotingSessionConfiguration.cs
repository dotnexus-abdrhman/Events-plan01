using EvenDAL.Models.Classes;
using EvenDAL.Models.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Data.Configuration
{
    public class VotingSessionConfiguration : IEntityTypeConfiguration<VotingSession>
    {
        public void Configure(EntityTypeBuilder<VotingSession> builder)
        {
            builder.HasKey(vs => vs.VotingSessionId);

            builder.Property(vs => vs.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(vs => vs.Question)
                .IsRequired();

            builder.Property(vs => vs.Status)
                .HasDefaultValue(VotingStatus.Pending);

            builder.Property(vs => vs.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Relationships
            builder.HasOne(vs => vs.Event)
                .WithMany(e => e.VotingSessions)
                .HasForeignKey(vs => vs.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(vs => vs.AgendaItem)
                .WithMany(ai => ai.VotingSessions)
                .HasForeignKey(vs => vs.AgendaItemId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            builder.HasIndex(vs => vs.Status);
            builder.HasIndex(vs => vs.StartTime);
            builder.HasIndex(vs => new { vs.EventId, vs.StartTime });
        }
    }
}
