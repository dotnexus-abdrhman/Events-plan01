using EventPl.Dto;
using EventPl.Dto.Mina;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventPl.Services.Interface
{
    /// <summary>
    /// خدمة إدارة الأحداث الرئيسية (Mina Events)
    /// </summary>
    public interface IMinaEventsService
    {
        // ============================================
        // Event CRUD Operations
        // ============================================

        /// <summary>
        /// الحصول على جميع أحداث المنظمة
        /// </summary>
        Task<List<EventDto>> GetOrganizationEventsAsync(Guid organizationId);

        /// <summary>
        /// الحصول على حدث محدد
        /// </summary>
        Task<EventDto?> GetEventByIdAsync(Guid eventId);
        /// <summary>
        /// الحصول على أحدث حدث تم إنشاؤه (بغض النظر عن المنظمة)
        /// </summary>
        Task<EventDto?> GetMostRecentEventAsync();

		/// <summary>
		/// البحث عن حدث بعنوان مطابق تماماً
		/// </summary>
		Task<EventDto?> FindByExactTitleAsync(string title);


			/// <summary>
			/// الحصول على أحدث حدث بث عام (IsBroadcast = true) وفق CreatedAt
			/// </summary>
			Task<EventDto?> GetMostRecentBroadcastAsync();



        /// <summary>
        /// إنشاء حدث جديد
        /// </summary>
        Task<EventDto> CreateEventAsync(EventDto dto);

        /// <summary>
        /// تحديث حدث
        /// </summary>
        Task<bool> UpdateEventAsync(EventDto dto);

        /// <summary>
        /// حذف حدث (سيحذف كل المكونات تلقائياً - Cascade)
        /// </summary>
        Task<bool> DeleteEventAsync(Guid eventId);

        /// <summary>
        /// تحديث حالة الحدث
        /// </summary>
        Task<bool> UpdateEventStatusAsync(Guid eventId, string status);

        // ============================================
        // Event Bundle (للمستخدم)
        // ============================================

        /// <summary>
        /// الحصول على حزمة الحدث الكاملة (للمستخدم)
        /// تشمل: البنود، الاستبيانات، النقاشات، الجداول، المرفقات
        /// </summary>
        Task<EventBundleDto> GetEventBundleAsync(Guid eventId, Guid userId);

        /// <summary>
        /// الحصول على أحداث المستخدم (حسب المنظمة)
        /// </summary>
        Task<List<EventDto>> GetUserEventsAsync(Guid userId, Guid organizationId);
    }

    /// <summary>
    /// حزمة الحدث الكاملة
    /// </summary>
    public class EventBundleDto
    {
        public EventDto Event { get; set; } = null!;
        public List<SectionDto> Sections { get; set; } = new();
        public List<SurveyDto> Surveys { get; set; } = new();
        public List<DiscussionDto> Discussions { get; set; } = new();
        public List<TableBlockDto> Tables { get; set; } = new();
        public List<AttachmentDto> Attachments { get; set; } = new();

        // حالة المستخدم
        public bool HasAnsweredSurveys { get; set; }
        public bool HasSigned { get; set; }
        public bool SignatureRequired { get; set; }
    }
}

