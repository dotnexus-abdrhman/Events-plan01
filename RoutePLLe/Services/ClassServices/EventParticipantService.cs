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
    public class EventParticipantService
     : CrudService<EventParticipant, EventParticipantDto, Guid>, ICrudService<EventParticipantDto, Guid>
    {
        // المفتاح هنا مركب (EventId + UserId) في الموديل،
        // لو احتجت عمليات Delete/Get بالمفتاحين، اعمل methods خاصة لاحقاً.
        public EventParticipantService(IRepository<EventParticipant, Guid> repo)
            : base(repo, e => e.ToDto(), d => d.ToEntity()) { }
    }
}
