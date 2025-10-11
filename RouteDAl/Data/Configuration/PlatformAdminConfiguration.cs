using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EvenDAL.Data.Configuration
{
    public class PlatformAdminConfiguration : IEntityTypeConfiguration<PlatformAdmin>
    {
        public void Configure(EntityTypeBuilder<PlatformAdmin> b)
        {
            b.ToTable("PlatformAdmins");
            b.HasKey(x => x.Id);

            b.Property(x => x.Email).IsRequired().HasMaxLength(150);
            b.HasIndex(x => x.Email).IsUnique();

            b.Property(x => x.FullName).HasMaxLength(150);
            b.Property(x => x.Phone).HasMaxLength(30);
            b.Property(x => x.ProfilePicture).HasMaxLength(500);

            b.Property(x => x.IsActive).HasDefaultValue(true);
            b.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        }
    }
}
