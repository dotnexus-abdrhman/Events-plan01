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
    public static class OrganizationFactory
    {
        public static OrganizationDto ToDto(this Organization e) =>
            e is null ? null : new OrganizationDto
            {
                OrganizationId = e.OrganizationId,
                Name = e.Name,
                NameEn = e.NameEn,
                TypeName = e.Type.ToString(),
                Logo = e.Logo,
                PrimaryColor = e.PrimaryColor,
                SecondaryColor = e.SecondaryColor,
                Settings = e.Settings,
                LicenseKey = e.LicenseKey,
                LicenseExpiry = e.LicenseExpiry,
                CreatedAt = e.CreatedAt,
                IsActive = e.IsActive
            };

        public static Organization ToEntity(this OrganizationDto d)
        {
            var e = new Organization
            {
                OrganizationId = d.OrganizationId == Guid.Empty ? Guid.NewGuid() : d.OrganizationId,
                Name = d.Name,
                NameEn = d.NameEn,
                Logo = d.Logo,
                PrimaryColor = d.PrimaryColor,
                SecondaryColor = d.SecondaryColor,
                Settings = d.Settings,
                LicenseKey = d.LicenseKey,
                LicenseExpiry = d.LicenseExpiry ?? default,
                CreatedAt = d.CreatedAt == default ? DateTime.UtcNow : d.CreatedAt,
                IsActive = d.IsActive
            };
            if (TryParseIgnoreCase<OrganizationType>(d.TypeName, out var t)) e.Type = t;
            return e;
        }
    }
}
