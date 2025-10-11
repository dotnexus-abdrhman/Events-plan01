using EvenDAL.Models.Classes;
using EventPl.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Factory
{
    public static class ModuleFactory
    {
        public static ModuleDto ToDto(this AppModule e) =>
            e is null ? null : new ModuleDto
            {
                ModuleId = e.ModuleId,
                Name = e.Name,
                Description = e.Description,
                IsActive = e.IsActive,
                RequiredFeatures = e.RequiredFeatures,
                CreatedAt = e.CreatedAt
            };

        public static AppModule ToEntity(this ModuleDto d) =>
            new AppModule
            {
                ModuleId = d.ModuleId == Guid.Empty ? Guid.NewGuid() : d.ModuleId,
                Name = d.Name,
                Description = d.Description,
                IsActive = d.IsActive,
                RequiredFeatures = d.RequiredFeatures,
                CreatedAt = d.CreatedAt == default ? DateTime.UtcNow : d.CreatedAt
            };
    }
}
