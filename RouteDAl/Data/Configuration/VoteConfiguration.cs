using EvenDAL.Models.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Data.Configuration
{
    public class VoteConfiguration : IEntityTypeConfiguration<Vote>
    {
        public void Configure(EntityTypeBuilder<Vote> builder)
        {
            builder.HasKey(v => v.VoteId);

            builder.Property(v => v.CustomAnswer)
                .HasMaxLength(1000);

            builder.Property(v => v.VotedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Relationships
            builder.HasOne(v => v.VotingSession)
                .WithMany(vs => vs.Votes)
                .HasForeignKey(v => v.VotingSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(v => v.User)
                .WithMany(u => u.Votes)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(v => v.VotingOption)
                .WithMany(vo => vo.Votes)
                .HasForeignKey(v => v.VotingOptionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes & Constraints
            builder.HasIndex(v => new { v.VotingSessionId, v.UserId }).IsUnique();
            builder.HasIndex(v => v.VotedAt);
        }
    }
}
