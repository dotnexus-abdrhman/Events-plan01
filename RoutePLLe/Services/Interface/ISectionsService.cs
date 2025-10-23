using EventPl.Dto.Mina;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventPl.Services.Interface
{
    /// <summary>
    /// خدمة إدارة البنود (Sections) والقرارات (Decisions)
    /// </summary>
    public interface ISectionsService
    {
        // ============================================
        // Section Operations
        // ============================================

        /// <summary>
        /// الحصول على جميع بنود الحدث مع القرارات
        /// </summary>
        Task<List<SectionDto>> GetEventSectionsAsync(Guid eventId);
        /// <summary>
        /// الحصول على جميع بنود الحدث مع القرارات مع تمرير نسخة الحدث (Ticks) لاستخدامها في المفتاح المؤقت
        /// </summary>
        Task<List<SectionDto>> GetEventSectionsAsync(Guid eventId, long? eventVersionTicks);


        /// <summary>
        /// الحصول على بند محدد مع قراراته
        /// </summary>
        Task<SectionDto?> GetSectionByIdAsync(Guid sectionId);

        /// <summary>
        /// إنشاء بند جديد
        /// </summary>
        Task<SectionDto> CreateSectionAsync(SectionDto dto);

        /// <summary>
        /// تحديث بند موجود
        /// </summary>
        Task<bool> UpdateSectionAsync(SectionDto dto);

        /// <summary>
        /// حذف بند (سيحذف القرارات تلقائياً - Cascade)
        /// </summary>
        Task<bool> DeleteSectionAsync(Guid sectionId);

        /// <summary>
        /// إعادة ترتيب البنود
        /// </summary>
        Task<bool> ReorderSectionsAsync(Guid eventId, List<Guid> sectionIds);

        // ============================================
        // Decision Operations
        // ============================================

        /// <summary>
        /// إضافة قرار لبند
        /// </summary>
        Task<DecisionDto> AddDecisionAsync(DecisionDto dto);

        /// <summary>
        /// تحديث قرار
        /// </summary>
        Task<bool> UpdateDecisionAsync(DecisionDto dto);

        /// <summary>
        /// حذف قرار (سيحذف العناصر تلقائياً - Cascade)
        /// </summary>
        Task<bool> DeleteDecisionAsync(Guid decisionId);

        // ============================================
        // DecisionItem Operations
        // ============================================

        /// <summary>
        /// إضافة عنصر لقرار
        /// </summary>
        Task<DecisionItemDto> AddDecisionItemAsync(DecisionItemDto dto);

        /// <summary>
        /// تحديث عنصر قرار
        /// </summary>
        Task<bool> UpdateDecisionItemAsync(DecisionItemDto dto);

        /// <summary>
        /// حذف عنصر قرار
        /// </summary>
        Task<bool> DeleteDecisionItemAsync(Guid itemId);
    }
}

