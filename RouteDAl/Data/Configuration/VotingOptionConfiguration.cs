using EvenDAL.Models.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Data.Configuration
{
    public class VotingOptionConfiguration : IEntityTypeConfiguration<VotingOption>
    {
        public void Configure(EntityTypeBuilder<VotingOption> builder)
        {
            builder.HasKey(vo => vo.VotingOptionId);

            builder.Property(vo => vo.Text)
                .IsRequired()
                .HasMaxLength(500);

            // Relationships
            builder.HasOne(vo => vo.VotingSession)
                .WithMany(vs => vs.VotingOptions)
                .HasForeignKey(vo => vo.VotingSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(vo => new { vo.VotingSessionId, vo.OrderIndex });
        }
    }
}
