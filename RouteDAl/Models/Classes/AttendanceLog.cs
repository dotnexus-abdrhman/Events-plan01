using EvenDAL.Models.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Models.Classes
{
    public class AttendanceLog
    {
        public Guid AttendanceId { get; set; }
        public Guid EventId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinTime { get; set; }
        public DateTime? LeaveTime { get; set; }
        public AttendanceType AttendanceType { get; set; }

        // Navigation Properties
        public virtual Event Event { get; set; }
        public virtual User User { get; set; }
    }
}
