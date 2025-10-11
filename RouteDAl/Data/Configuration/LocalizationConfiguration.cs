using EvenDAL.Models.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Data.Configuration
{
    public class LocalizationConfiguration : IEntityTypeConfiguration<Localization>
    {
        public void Configure(EntityTypeBuilder<Localization> builder)
        {
            builder.HasKey(l => l.LocalizationId);

            builder.Property(l => l.Key)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(l => l.Value)
                .IsRequired();

            builder.Property(l => l.Language)
                .IsRequired()
                .HasMaxLength(5);

            // Relationships
            builder.HasOne(l => l.Organization)
                .WithMany(o => o.Localizations)
                .HasForeignKey(l => l.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes & Constraints
            builder.HasIndex(l => new { l.OrganizationId, l.Key, l.Language }).IsUnique();
        }
    }
}
