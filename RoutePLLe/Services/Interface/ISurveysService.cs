using EventPl.Dto.Mina;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventPl.Services.Interface
{
    /// <summary>
    /// خدمة إدارة الاستبيانات (Surveys)
    /// </summary>
    public interface ISurveysService
    {
        // ============================================
        // Survey Operations
        // ============================================

        /// <summary>
        /// الحصول على جميع استبيانات الحدث
        /// </summary>
        Task<List<SurveyDto>> GetEventSurveysAsync(Guid eventId);
        /// <summary>
        /// الحصول على جميع استبيانات الحدث مع تمرير نسخة الحدث (Ticks) لاستخدامها في المفتاح المؤقت
        /// </summary>
        Task<List<SurveyDto>> GetEventSurveysAsync(Guid eventId, long? eventVersionTicks);


        /// <summary>
        /// الحصول على استبيان محدد مع أسئلته وخياراته
        /// </summary>
        Task<SurveyDto?> GetSurveyByIdAsync(Guid surveyId);

        /// <summary>
        /// إنشاء استبيان جديد
        /// </summary>
        Task<SurveyDto> CreateSurveyAsync(SurveyDto dto);

        /// <summary>
        /// تحديث استبيان
        /// </summary>
        Task<bool> UpdateSurveyAsync(SurveyDto dto);

        /// <summary>
        /// حذف استبيان (سيحذف الأسئلة والخيارات تلقائياً)
        /// </summary>
        Task<bool> DeleteSurveyAsync(Guid surveyId);

        /// <summary>
        /// تفعيل/تعطيل استبيان
        /// </summary>
        Task<bool> ToggleSurveyActiveAsync(Guid surveyId, bool isActive);

        // ============================================
        // Question Operations
        // ============================================

        /// <summary>
        /// إضافة سؤال لاستبيان
        /// </summary>
        Task<SurveyQuestionDto> AddQuestionAsync(SurveyQuestionDto dto);

        /// <summary>
        /// تحديث سؤال
        /// </summary>
        Task<bool> UpdateQuestionAsync(SurveyQuestionDto dto);

        /// <summary>
        /// حذف سؤال (سيحذف الخيارات تلقائياً)
        /// </summary>
        Task<bool> DeleteQuestionAsync(Guid questionId);

        // ============================================
        // Option Operations
        // ============================================

        /// <summary>
        /// إضافة خيار لسؤال
        /// </summary>
        Task<SurveyOptionDto> AddOptionAsync(SurveyOptionDto dto);

        /// <summary>
        /// تحديث خيار
        /// </summary>
        Task<bool> UpdateOptionAsync(SurveyOptionDto dto);

        /// <summary>
        /// حذف خيار
        /// </summary>
        Task<bool> DeleteOptionAsync(Guid optionId);

        // ============================================
        // Answer Operations
        // ============================================

        /// <summary>
        /// حفظ إجابات المستخدم (مع منع التكرار)
        /// </summary>
        Task<bool> SaveUserAnswersAsync(SaveSurveyAnswersRequest request);

        /// <summary>
        /// الحصول على إجابات مستخدم محدد
        /// </summary>
        Task<List<SurveyAnswerDto>> GetUserAnswersAsync(Guid eventId, Guid userId);

        /// <summary>
        /// التحقق من إجابة المستخدم على الاستبيان
        /// </summary>
        Task<bool> HasUserAnsweredAsync(Guid eventId, Guid userId);
    }
}

