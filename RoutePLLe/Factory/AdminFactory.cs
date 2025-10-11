using EvenDAL.Models.Classes;
using EventPl.Dto;
using System;

namespace EventPl.Factory
{
    public static class AdminFactory
    {
        public static AdminDto ToDto(this PlatformAdmin e) =>
            e is null ? null : new AdminDto
            {
                Id = e.Id,
                Email = e.Email,
                FullName = e.FullName,
                Phone = e.Phone,
                ProfilePicture = e.ProfilePicture,
                IsActive = e.IsActive,
                CreatedAt = e.CreatedAt,
                LastLogin = e.LastLogin
            };

        public static PlatformAdmin ToEntity(this AdminDto d)
        {
            if (d == null) return null!;
            return new PlatformAdmin
            {
                Id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id,
                Email = d.Email?.Trim() ?? string.Empty,
                FullName = string.IsNullOrWhiteSpace(d.FullName) ? d.Email : d.FullName!.Trim(),
                Phone = d.Phone,
                ProfilePicture = d.ProfilePicture,
                IsActive = d.IsActive,
                CreatedAt = d.CreatedAt == default ? DateTime.UtcNow : d.CreatedAt,
                LastLogin = d.LastLogin
            };
        }
    }
}
