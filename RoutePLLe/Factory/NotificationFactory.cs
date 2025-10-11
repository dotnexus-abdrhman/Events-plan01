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
    public static class NotificationFactory
    {
        public static NotificationDto ToDto(this Notification e) =>
            e is null ? null : new NotificationDto
            {
                NotificationId = e.NotificationId,
                UserId = e.UserId,
                EventId = e.EventId,
                Title = e.Title,
                Message = e.Message,
                TypeName = e.Type.ToString(),
                IsRead = e.IsRead,
                CreatedAt = e.CreatedAt,
                ReadAt = e.ReadAt
            };

        public static Notification ToEntity(this NotificationDto d)
        {
            var e = new Notification
            {
                NotificationId = d.NotificationId == Guid.Empty ? Guid.NewGuid() : d.NotificationId,
                UserId = d.UserId,
                EventId = d.EventId,
                Title = d.Title,
                Message = d.Message,
                IsRead = d.IsRead,
                CreatedAt = d.CreatedAt == default ? DateTime.UtcNow : d.CreatedAt,
                ReadAt = d.ReadAt
            };
            if (TryParseIgnoreCase<NotificationType>(d.TypeName, out var t)) e.Type = t;
            return e;
        }
    }
}
