using EvenDAL.Models.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.UserId);

            builder.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(u => u.Phone)
                .HasMaxLength(20);

            builder.Property(u => u.ProfilePicture)
                .HasMaxLength(500);

            builder.Property(u => u.IsActive)
                .HasDefaultValue(true);

            builder.Property(u => u.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Relationships
            builder.Property(u => u.OrganizationId)
                .IsRequired(false);

            builder.HasOne(u => u.Organization)
                .WithMany(o => o.Users)
                .HasForeignKey(u => u.OrganizationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);




            // Indexes
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => new { u.OrganizationId, u.Email })
                    .IsUnique()
                    .HasFilter("[OrganizationId] IS NOT NULL");
            builder.HasIndex(u => u.IsActive);
            builder.HasIndex(u => u.Role);
        }
    }

}
