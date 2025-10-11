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
    public static class UserFactory
    {
        public static UserDto ToDto(this User e) =>
            e is null ? null : new UserDto
            {
                UserId = e.UserId,
                OrganizationId = e.OrganizationId,
                FullName = e.FullName,
                Email = e.Email,
                Phone = e.Phone,
                RoleName = e.Role.ToString(),
                ProfilePicture = e.ProfilePicture,
                IsActive = e.IsActive,
                LastLogin = e.LastLogin,
                CreatedAt = e.CreatedAt
            };

        public static User ToEntity(this UserDto d)
        {
            var e = new User
            {
                UserId = d.UserId == Guid.Empty ? Guid.NewGuid() : d.UserId,
                OrganizationId = d.OrganizationId,
                FullName = d.FullName,
                Email = d.Email,
                Phone = d.Phone,
                ProfilePicture = d.ProfilePicture,
                IsActive = d.IsActive,
                LastLogin = d.LastLogin,
                CreatedAt = d.CreatedAt == default ? DateTime.UtcNow : d.CreatedAt
            };
            if (TryParseIgnoreCase<UserRole>(d.RoleName, out var r)) e.Role = r;
            return e;
        }
    }
}
