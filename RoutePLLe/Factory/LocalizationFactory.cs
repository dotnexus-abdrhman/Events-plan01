using EvenDAL.Models.Classes;
using EventPl.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Factory
{
    public static class LocalizationFactory
    {
        public static LocalizationDto ToDto(this Localization e) =>
            e is null ? null : new LocalizationDto
            {
                LocalizationId = e.LocalizationId,
                OrganizationId = e.OrganizationId,
                Key = e.Key,
                Value = e.Value,
                Language = e.Language
            };

        public static Localization ToEntity(this LocalizationDto d) =>
            new Localization
            {
                LocalizationId = d.LocalizationId == Guid.Empty ? Guid.NewGuid() : d.LocalizationId,
                OrganizationId = d.OrganizationId,
                Key = d.Key,
                Value = d.Value,
                Language = d.Language
            };
    }
}
