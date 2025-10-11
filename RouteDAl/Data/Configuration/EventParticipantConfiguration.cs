using EvenDAL.Models.Classes;
using EvenDAL.Models.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Data.Configuration
{
    public class EventParticipantConfiguration : IEntityTypeConfiguration<EventParticipant>
    {
        public void Configure(EntityTypeBuilder<EventParticipant> builder)
        {
            builder.HasKey(ep => ep.EventParticipantId);
            builder.HasIndex(ep => new { ep.EventId, ep.UserId }).IsUnique();



            builder.Property(ep => ep.Status)
                .HasDefaultValue(ParticipantStatus.Invited);

            // Role: no explicit max length for SQL Server; provider will size appropriately for indexes

            builder.Property(ep => ep.InvitedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Relationships
            builder.HasOne(ep => ep.Event)
                .WithMany(e => e.Participants)
                .HasForeignKey(ep => ep.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ep => ep.User)
                .WithMany(u => u.EventParticipants)
                .HasForeignKey(ep => ep.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(ep => ep.Status);
            builder.HasIndex(ep => ep.Role);
        }
    }
   
}
