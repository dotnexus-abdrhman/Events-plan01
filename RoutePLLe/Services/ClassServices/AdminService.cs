using EvenDAL.Models.Classes;
using EvenDAL.Repositories.InterFace;
using EventPl.Dto;
using EventPl.Services.Interface;
using EventPl.Factory;

namespace EventPl.Services.ClassServices
{
    public class AdminService
        : CrudService<PlatformAdmin, AdminDto, Guid>, ICrudService<AdminDto, Guid>
    {
        public AdminService(IRepository<PlatformAdmin, Guid> repo)
            : base(repo, e => e.ToDto(), d => d.ToEntity()) { }
    }
}
