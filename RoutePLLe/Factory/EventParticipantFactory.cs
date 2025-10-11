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
    public static class EventParticipantFactory
    {
        public static EventParticipantDto ToDto(this EventParticipant e) =>
            e is null ? null : new EventParticipantDto
            {
                EventParticipantId = e.EventParticipantId,
                EventId = e.EventId,
                UserId = e.UserId,
                RoleName = e.Role.ToString(),
                InvitedAt = e.InvitedAt,
                JoinedAt = e.JoinedAt,
                StatusName = e.Status.ToString()
            };

        public static EventParticipant ToEntity(this EventParticipantDto d)
        {
            var e = new EventParticipant
            {
                EventParticipantId = d.EventParticipantId,
                EventId = d.EventId,
                UserId = d.UserId,
                InvitedAt = d.InvitedAt == default ? DateTime.UtcNow : d.InvitedAt,
                JoinedAt = d.JoinedAt
            };
            if (TryParseIgnoreCase<ParticipantRole>(d.RoleName, out var role)) e.Role = role;
            if (TryParseIgnoreCase<ParticipantStatus>(d.StatusName, out var st)) e.Status = st;
            return e;
        }
    }
}
