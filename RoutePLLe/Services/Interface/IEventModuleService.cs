using EventPl.Dto;

namespace EventPl.Services.Interface
{
    /// <summary>
    /// يدير إنشاء/تعديل/حذف الفعالية مع جلسة الاستبيان وخياراتها (إن وُجدت)
    /// أو مع غرض النقاش/الورشة. كل الإعدادات تحفظ داخليًا في Settings (JSON)
    /// بدون إظهارها في الواجهات.
    /// </summary>
    public interface IEventModuleService
    {
        Task<Guid> CreateEventAsync(
            EventDto eventDto,
            VotingSessionDto? surveySession,
            IEnumerable<VotingOptionDto>? surveyOptions,
            string? discussionPurpose = null);

        Task UpdateEventAsync(
            EventDto eventDto,
            VotingSessionDto? surveySession,
            IEnumerable<VotingOptionDto>? surveyOptions,
            string? discussionPurpose = null);

        Task DeleteEventAsync(Guid eventId);
    }
}
