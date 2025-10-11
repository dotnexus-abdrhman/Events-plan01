using EvenDAL.Models.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Data.Configuration
{
    public class AttendanceLogConfiguration : IEntityTypeConfiguration<AttendanceLog>
    {
        public void Configure(EntityTypeBuilder<AttendanceLog> builder)
        {
            builder.HasKey(al => al.AttendanceId);

            // Relationships
            builder.HasOne(al => al.Event)
                .WithMany(e => e.AttendanceLogs)
                .HasForeignKey(al => al.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(al => al.User)
                .WithMany(u => u.AttendanceLogs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);




            // Indexes
            builder.HasIndex(al => new { al.EventId, al.UserId, al.JoinTime });
            builder.HasIndex(al => al.AttendanceType);
        }
    }

}
