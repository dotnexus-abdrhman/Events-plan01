using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Dto
{
    public class AttendanceLogDto
    {
        [Required]
        public Guid AttendanceId { get; set; }

        [Required]
        public Guid EventId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime JoinTime { get; set; }

        public DateTime? LeaveTime { get; set; }

        [StringLength(50)]
        public string AttendanceTypeName { get; set; }   // Physical / Virtual / Hybrid
    }
}
