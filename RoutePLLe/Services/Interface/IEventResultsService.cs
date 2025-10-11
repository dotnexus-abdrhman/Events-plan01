using EventPl.Dto;

namespace EventPl.Services.Interface
{
    public interface IEventResultsService
    {
        Task<EventResultsDto> GetAdminResultsAsync(Guid eventId); // كل شيء
        Task<EventResultsDto> GetUserResultsAsync(Guid eventId);  // ملخص مفلتر
    }
}
