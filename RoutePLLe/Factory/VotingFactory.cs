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
    public static class VotingFactory
    {
        public static VotingSessionDto ToDto(this VotingSession e) =>
            e is null ? null : new VotingSessionDto
            {
                VotingSessionId = e.VotingSessionId,
                EventId = e.EventId,
                AgendaItemId = e.AgendaItemId,
                Title = e.Title,
                Question = e.Question,
                TypeName = e.Type.ToString(),
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                IsAnonymous = e.IsAnonymous,
                StatusName = e.Status.ToString(),
                Settings = e.Settings,
                CreatedAt = e.CreatedAt
            };

        public static VotingSession ToEntity(this VotingSessionDto d)
        {
            var e = new VotingSession
            {
                VotingSessionId = d.VotingSessionId == Guid.Empty ? Guid.NewGuid() : d.VotingSessionId,
                EventId = d.EventId,
                AgendaItemId = d.AgendaItemId,
                Title = d.Title,
                Question = d.Question,
                StartTime = d.StartTime,
                EndTime = d.EndTime,
                IsAnonymous = d.IsAnonymous,
                Settings = d.Settings,
                CreatedAt = d.CreatedAt == default ? DateTime.UtcNow : d.CreatedAt
            };
            if (TryParseIgnoreCase<VotingType>(d.TypeName, out var vt)) e.Type = vt;
            if (TryParseIgnoreCase<VotingStatus>(d.StatusName, out var vs)) e.Status = vs;
            return e;
        }

        public static VotingOptionDto ToDto(this VotingOption e) =>
            e is null ? null : new VotingOptionDto
            {
                VotingOptionId = e.VotingOptionId,
                VotingSessionId = e.VotingSessionId,
                Text = e.Text,
                OrderIndex = e.OrderIndex
            };

        public static VotingOption ToEntity(this VotingOptionDto d) =>
            new VotingOption
            {
                VotingOptionId = d.VotingOptionId == Guid.Empty ? Guid.NewGuid() : d.VotingOptionId,
                VotingSessionId = d.VotingSessionId,
                Text = d.Text,
                OrderIndex = d.OrderIndex
            };

        public static VoteDto ToDto(this Vote e) =>
            e is null ? null : new VoteDto
            {
                VoteId = e.VoteId,
                VotingSessionId = e.VotingSessionId,
                UserId = e.UserId,
                VotingOptionId = e.VotingOptionId,
                CustomAnswer = e.CustomAnswer,
                VotedAt = e.VotedAt
            };

        public static Vote ToEntity(this VoteDto d) =>
            new Vote
            {
                VoteId = d.VoteId == Guid.Empty ? Guid.NewGuid() : d.VoteId,
                VotingSessionId = d.VotingSessionId,
                UserId = d.UserId,
                VotingOptionId = d.VotingOptionId,
                CustomAnswer = d.CustomAnswer,
                VotedAt = d.VotedAt == default ? DateTime.UtcNow : d.VotedAt
            };
    }
}
