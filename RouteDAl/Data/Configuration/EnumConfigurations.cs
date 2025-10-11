using EvenDAL.Models.Classes;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Data.Configuration
{
    public static class EnumConfigurations
    {
        public static void ConfigureEnums(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Organization>()
                .Property(o => o.Type)
                .HasConversion<string>();

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<Event>()
                .Property(e => e.Status)
                .HasConversion<string>();

            modelBuilder.Entity<EventParticipant>()
                .Property(ep => ep.Role)
                .HasConversion<string>();

            modelBuilder.Entity<EventParticipant>()
                .Property(ep => ep.Status)
                .HasConversion<string>();

            modelBuilder.Entity<AgendaItem>()
                .Property(ai => ai.Type)
                .HasConversion<string>();

            modelBuilder.Entity<VotingSession>()
                .Property(vs => vs.Type)
                .HasConversion<string>();

            modelBuilder.Entity<VotingSession>()
                .Property(vs => vs.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Notification>()
                .Property(n => n.Type)
                .HasConversion<string>();

            modelBuilder.Entity<AttendanceLog>()
                .Property(al => al.AttendanceType)
                .HasConversion<string>();

            // الـEnums الجديدة
            modelBuilder.Entity<SurveyQuestion>()
                .Property(sq => sq.Type)
                .HasConversion<string>();

            modelBuilder.Entity<Attachment>()
                .Property(a => a.Type)
                .HasConversion<string>();
        }
    }
}
