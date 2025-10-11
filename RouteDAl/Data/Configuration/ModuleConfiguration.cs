using EvenDAL.Models.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Data.Configuration
{
    public class ModuleConfiguration : IEntityTypeConfiguration<AppModule>
    {
        public void Configure(EntityTypeBuilder<AppModule> builder)
        {
            builder.HasKey(m => m.ModuleId);

            builder.Property(m => m.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(m => m.IsActive)
                .HasDefaultValue(true);

            builder.Property(m => m.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            // Indexes
            builder.HasIndex(m => m.Name).IsUnique();
            builder.HasIndex(m => m.IsActive);
        }
    }
}
