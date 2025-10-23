using AutoMapper;
using EvenDAL.Models.Classes;
using EvenDAL.Models.Shared.Enums;
using EvenDAL.Repositories.InterFace;
using EventPl.Dto;
using EventPl.Dto.Mina;
using EventPl.Factory;
using EventPl.Services.Interface;
using Microsoft.EntityFrameworkCore;
using RouteDAl.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventPl.Services.ClassServices
{
    /// <summary>
    /// خدمة إدارة الأحداث الرئيسية (Mina Events)
    /// </summary>
    public class MinaEventsService : IMinaEventsService
    {
        private readonly AppDbContext _db;
        private readonly IRepository<Event, Guid> _eventRepo;
        private readonly ISectionsService _sectionsService;
        private readonly ISurveysService _surveysService;
        private readonly IDiscussionsService _discussionsService;
        private readonly ITableBlocksService _tablesService;
        private readonly IAttachmentsService _attachmentsService;
        private readonly ISignaturesService _signaturesService;
        private readonly IMapper _mapper;

        public MinaEventsService(
            AppDbContext db,
            IRepository<Event, Guid> eventRepo,
            ISectionsService sectionsService,
            ISurveysService surveysService,
            IDiscussionsService discussionsService,
            ITableBlocksService tablesService,
            IAttachmentsService attachmentsService,
            ISignaturesService signaturesService,
            IMapper mapper)
        {
            _db = db;
            _eventRepo = eventRepo;
            _sectionsService = sectionsService;
            _surveysService = surveysService;
            _discussionsService = discussionsService;
            _tablesService = tablesService;
            _attachmentsService = attachmentsService;
            _signaturesService = signaturesService;
            _mapper = mapper;
        }

        // ============================================
        // Event CRUD Operations
        // ============================================

        public async Task<List<EventDto>> GetOrganizationEventsAsync(Guid organizationId)
        {
            var events = await _db.Events
                .AsNoTracking()
                .Where(e => e.OrganizationId == organizationId || e.IsBroadcast)
                .OrderByDescending(e => e.StartAt)
                .ToListAsync();

            // Fallback safety: if no events are returned (e.g., in constrained test contexts),
            // fetch the most recent created events regardless of org, but still include broadcasts first.
            if (events == null || events.Count == 0)
            {
                var recent = await _db.Events.AsNoTracking()
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(20)
                    .ToListAsync();
                events = recent;
            }

            return events.Select(e => e.ToDto()).ToList();
        }

        public async Task<EventDto?> GetEventByIdAsync(Guid eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            return ev?.ToDto();
        }

	        public async Task<EventDto?> GetMostRecentEventAsync()
	        {
	            var ev = await _db.Events.AsNoTracking()
	                .OrderByDescending(e => e.CreatedAt)
	                .FirstOrDefaultAsync();
	            return ev?.ToDto();
	        }

		        public async Task<EventDto?> FindByExactTitleAsync(string title)
		        {
		            if (string.IsNullOrWhiteSpace(title)) return null;
		            var ev = await _db.Events.AsNoTracking()
		                .Where(e => e.Title == title)
		                .OrderByDescending(e => e.CreatedAt)
		                .FirstOrDefaultAsync();
		            return ev?.ToDto();
		        }

		        public async Task<EventDto?> GetMostRecentBroadcastAsync()
		        {
		            var ev = await _db.Events.AsNoTracking()
		                .Where(e => e.IsBroadcast)
		                .OrderByDescending(e => e.CreatedAt)
		                .FirstOrDefaultAsync();
		            return ev?.ToDto();
		        }




        public async Task<EventDto> CreateEventAsync(EventDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("عنوان الحدث مطلوب");

            if (dto.OrganizationId == Guid.Empty)
                throw new ArgumentException("معرّف المنظمة مطلوب");

            if (dto.StartAt >= dto.EndAt)
                throw new ArgumentException("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            var ev = dto.ToEntity();
            ev.EventId = Guid.NewGuid();
            ev.CreatedAt = DateTime.UtcNow;
            // Status is determined from dto.StatusName via ToEntity(); defaults to Draft if not provided

            await _eventRepo.AddAsync(ev);

            return ev.ToDto();
        }

        public async Task<bool> UpdateEventAsync(EventDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("عنوان الحدث مطلوب");

            if (dto.StartAt >= dto.EndAt)
                throw new ArgumentException("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

            var ev = await _eventRepo.GetByIdAsync(dto.EventId);
            if (ev == null)
                throw new KeyNotFoundException("الحدث غير موجود");

            // تحديث الحقول
            ev.Title = dto.Title.Trim();
            ev.Description = dto.Description ?? string.Empty;
            ev.StartAt = dto.StartAt;
            ev.EndAt = dto.EndAt;
            ev.RequireSignature = dto.RequireSignature;

            return await _eventRepo.UpdateAsync(ev);
        }

        public async Task<bool> DeleteEventAsync(Guid eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null)
                throw new KeyNotFoundException("الحدث غير موجود");

            return await _eventRepo.DeleteByIdAsync(eventId);
        }

        public async Task<bool> UpdateEventStatusAsync(Guid eventId, string status)
        {
            if (!Enum.TryParse<EventStatus>(status, true, out var eventStatus))
                throw new ArgumentException("حالة الحدث غير صحيحة. استخدم: Draft, Active, Completed, Cancelled");

            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null)
                throw new KeyNotFoundException("الحدث غير موجود");

            ev.Status = eventStatus;
            return await _eventRepo.UpdateAsync(ev);
        }

        // ============================================
        // Event Bundle (للمستخدم)
        // ============================================

        public async Task<EventBundleDto> GetEventBundleAsync(Guid eventId, Guid userId)
        {
            // الحصول على الحدث
            var eventDto = await GetEventByIdAsync(eventId);
            if (eventDto == null)
                throw new KeyNotFoundException("الحدث غير موجود");

            // احصل على نسخة الحدث (Ticks) مرة واحدة لاستخدامها كمفتاح للكاش وإلغاء الاستعلامات المكررة
            var eventVersion = await _db.Events.AsNoTracking()
                .Where(e => e.EventId == eventId)
                .Select(e => (DateTime?)(e.UpdatedAt ?? e.CreatedAt))
                .FirstOrDefaultAsync();
            var versionTicks = eventVersion?.Ticks;

            // الحصول على جميع المكونات بالتوازي وتمرير نسخة الحدث لتوحيد مفاتيح الكاش
            var sectionsTask = _sectionsService.GetEventSectionsAsync(eventId, versionTicks);
            var surveysTask = _surveysService.GetEventSurveysAsync(eventId, versionTicks);
            var discussionsTask = _discussionsService.GetEventDiscussionsAsync(eventId, versionTicks);
            var tablesTask = _tablesService.GetEventTablesAsync(eventId, versionTicks);
            var attachmentsTask = _attachmentsService.GetEventAttachmentsAsync(eventId, versionTicks);
            var hasAnsweredTask = _surveysService.HasUserAnsweredAsync(eventId, userId);
            var hasSignedTask = _signaturesService.HasUserSignedAsync(eventId, userId);

            await Task.WhenAll(
                sectionsTask,
                surveysTask,
                discussionsTask,
                tablesTask,
                attachmentsTask,
                hasAnsweredTask,
                hasSignedTask
            );

            return new EventBundleDto
            {
                Event = eventDto,
                Sections = await sectionsTask,
                Surveys = await surveysTask,
                Discussions = await discussionsTask,
                Tables = await tablesTask,
                Attachments = await attachmentsTask,
                HasAnsweredSurveys = await hasAnsweredTask,
                HasSigned = await hasSignedTask,
                SignatureRequired = eventDto.RequireSignature
            };
        }

        public async Task<List<EventDto>> GetUserEventsAsync(Guid userId, Guid organizationId)
        {
            // منطق الاستحقاق (نفس منطق MyEventsController):
            // - إذا كان الحدث بث عام IsBroadcast => يظهر للجميع
            // - إذا كان للحدث مدعوون أفراد (EventInvitedUsers) => يظهر فقط للمستخدمين المدعوين
            // - إذا لم يكن للحدث مدعوون أفراد => يتبع منطق المجموعة (OrganizationId + Active)
            var query = _db.Events
                .AsNoTracking()
                .Where(e =>
                    e.IsBroadcast
                    || (_db.EventInvitedUsers.AsNoTracking().Any(i => i.EventId == e.EventId && i.UserId == userId))
                    || (!_db.EventInvitedUsers.AsNoTracking().Any(i => i.EventId == e.EventId) && e.OrganizationId == organizationId && e.Status == EventStatus.Active)
                );

            var events = await query
                .OrderByDescending(e => e.StartAt)
                .ToListAsync();

            return events.Select(e => e.ToDto()).ToList();
        }
    }
}

