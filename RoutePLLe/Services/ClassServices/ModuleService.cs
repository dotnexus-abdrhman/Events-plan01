using EvenDAL.Models.Classes;
using EvenDAL.Repositories.InterFace;
using EventPl.Dto;
using EventPl.Factory;
using EventPl.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Services.ClassServices
{
    public class ModuleService
     : CrudService<AppModule, ModuleDto, Guid>, ICrudService<ModuleDto, Guid>
    {
        public ModuleService(IRepository<AppModule, Guid> repo)
            : base(repo, e => e.ToDto(), d => d.ToEntity()) { }
    }
}
