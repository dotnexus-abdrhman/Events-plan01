using EvenDAL.Models.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Data.Configuration
{
    public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.HasKey(o => o.OrganizationId);

            builder.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(o => o.NameEn)
                .HasMaxLength(200);

            builder.Property(o => o.Logo)
                .HasMaxLength(500);

            builder.Property(o => o.PrimaryColor)
                .HasMaxLength(7);

            builder.Property(o => o.SecondaryColor)
                .HasMaxLength(7);

            builder.Property(o => o.LicenseKey)
                .HasMaxLength(100);

            builder.Property(o => o.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(o => o.IsActive)
                .HasDefaultValue(true);

            // Relationships
            builder.HasMany(o => o.Users)
                .WithOne(u => u.Organization)
                .HasForeignKey(u => u.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(o => o.Events)
                .WithOne(e => e.Organization)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(o => o.Localizations)
                .WithOne(l => l.Organization)
                .HasForeignKey(l => l.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(o => o.Name);
            builder.HasIndex(o => o.LicenseKey);
            builder.HasIndex(o => o.IsActive);
        }
    }
}
