using EvenDAL.Models.Classes;
using EvenDAL.Repositories.InterFace;
using EventPl.Dto;
using EventPl.Factory;
using EventPl.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventPl.Services.ClassServices
{
    public class OrganizationService
    : CrudService<Organization, OrganizationDto, Guid>, ICrudService<OrganizationDto, Guid>
    {
        public OrganizationService(IRepository<Organization, Guid> repo)
            : base(repo, e => e.ToDto(), d => d.ToEntity()) { }
    }
}
