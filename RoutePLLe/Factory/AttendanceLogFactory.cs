using EvenDAL.Models.Classes;
using EvenDAL.Models.Shared.Enums;
using EventPl.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EventPl.Factory.EnumHelper;


namespace EventPl.Factory
{
    public static class AttendanceLogFactory
    {
        public static AttendanceLogDto ToDto(this AttendanceLog e) =>
            e is null ? null : new AttendanceLogDto
            {
                AttendanceId = e.AttendanceId,
                EventId = e.EventId,
                UserId = e.UserId,
                JoinTime = e.JoinTime,
                LeaveTime = e.LeaveTime,
                AttendanceTypeName = e.AttendanceType.ToString()
            };

        public static AttendanceLog ToEntity(this AttendanceLogDto d)
        {
            var e = new AttendanceLog
            {
                AttendanceId = d.AttendanceId == Guid.Empty ? Guid.NewGuid() : d.AttendanceId,
                EventId = d.EventId,
                UserId = d.UserId,
                JoinTime = d.JoinTime == default ? DateTime.UtcNow : d.JoinTime,
                LeaveTime = d.LeaveTime
            };
            if (TryParseIgnoreCase<AttendanceType>(d.AttendanceTypeName, out var t)) e.AttendanceType = t;
            return e;
        }
    }
}
