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
    public static class AgendaItemFactory
    {
        public static AgendaItemDto ToDto(this AgendaItem e) =>
            e is null ? null : new AgendaItemDto
            {
                AgendaItemId = e.AgendaItemId,
                EventId = e.EventId,
                Title = e.Title,
                Description = e.Description,
                OrderIndex = e.OrderIndex,
                EstimatedDuration = e.EstimatedDuration,
                TypeName = e.Type.ToString(),
                RequiresVoting = e.RequiresVoting,
                PresenterId = e.PresenterId,
                CreatedAt = e.CreatedAt
            };

        public static AgendaItem ToEntity(this AgendaItemDto d)
        {
            var e = new AgendaItem
            {
                AgendaItemId = d.AgendaItemId == Guid.Empty ? Guid.NewGuid() : d.AgendaItemId,
                EventId = d.EventId,
                Title = d.Title,
                Description = d.Description,
                OrderIndex = d.OrderIndex,
                EstimatedDuration = d.EstimatedDuration,
                RequiresVoting = d.RequiresVoting,
                PresenterId = d.PresenterId,
                CreatedAt = d.CreatedAt == default ? DateTime.UtcNow : d.CreatedAt
            };
            if (TryParseIgnoreCase<AgendaItemType>(d.TypeName, out var t)) e.Type = t;
            return e;
        }
    }
}
