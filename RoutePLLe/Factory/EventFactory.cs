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
    public static class EventFactory
    {
        public static EventDto ToDto(this Event e) =>
            e is null ? null : new EventDto
            {
                EventId = e.EventId,
                OrganizationId = e.OrganizationId,
                CreatedById = e.CreatedById,
                Title = e.Title,
                Description = e.Description,
                Location = string.Empty, // Event لا يحتوي على Location حالياً
                TypeName = "Event", // للتوافق مع الكود القديم
                StartAt = e.StartAt,
                EndAt = e.EndAt,
                StatusName = e.Status.ToString(),
                Settings = "{}", // للتوافق مع الكود القديم
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                RequireSignature = e.RequireSignature,
                IsBroadcast = e.IsBroadcast,
                AllowProposals = false, // للتوافق مع الكود القديم
                AllowDiscussion = false // للتوافق مع الكود القديم
            };

        public static Event ToEntity(this EventDto d)
        {
            var e = new Event
            {
                EventId = d.EventId == Guid.Empty ? Guid.NewGuid() : d.EventId,
                OrganizationId = d.OrganizationId,
                CreatedById = d.CreatedById,
                Title = d.Title,
                Description = d.Description,
                StartAt = d.StartAt,
                EndAt = d.EndAt,
                CreatedAt = d.CreatedAt == default ? DateTime.UtcNow : d.CreatedAt,
                UpdatedAt = d.UpdatedAt,
                RequireSignature = d.RequireSignature,
                IsBroadcast = d.IsBroadcast
            };
            if (TryParseIgnoreCase<EventStatus>(d.StatusName, out var st)) e.Status = st;
            return e;
        }
    }
}
