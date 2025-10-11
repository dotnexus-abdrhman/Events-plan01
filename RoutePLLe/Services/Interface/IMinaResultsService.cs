using EventPl.Dto.Mina;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventPl.Services.Interface
{
    /// <summary>
    /// خدمة عرض نتائج الأحداث (Surveys, Discussions, Signatures)
    /// </summary>
    public interface IMinaResultsService
    {
        /// <summary>
        /// الحصول على نتائج الاستبيانات (للأدمن)
        /// </summary>
        Task<EventResultsSummaryDto> GetEventResultsAsync(Guid eventId);
        
        /// <summary>
        /// الحصول على نتائج استبيان محدد
        /// </summary>
        Task<SurveyResultsDto> GetSurveyResultsAsync(Guid surveyId);
        
        /// <summary>
        /// الحصول على إحصائيات الحدث
        /// </summary>
        Task<EventStatisticsDto> GetEventStatisticsAsync(Guid eventId);
    }

    /// <summary>
    /// ملخص نتائج الحدث
    /// </summary>
    public class EventResultsSummaryDto
    {
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public List<SurveyResultsDto> SurveyResults { get; set; } = new();
        public int TotalParticipants { get; set; }
        public int TotalSignatures { get; set; }
        public int TotalDiscussionReplies { get; set; }
    }

    /// <summary>
    /// نتائج استبيان محدد
    /// </summary>
    public class SurveyResultsDto
    {
        public Guid SurveyId { get; set; }
        public string SurveyTitle { get; set; } = string.Empty;
        public List<QuestionResultDto> QuestionResults { get; set; } = new();
    }

    /// <summary>
    /// نتائج سؤال محدد
    /// </summary>
    public class QuestionResultDto
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "Single"; // Single, Multiple
        public int TotalAnswers { get; set; }
        public List<OptionResultDto> OptionResults { get; set; } = new();
    }

    /// <summary>
    /// نتائج خيار محدد
    /// </summary>
    public class OptionResultDto
    {
        public Guid OptionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Percentage { get; set; } = "0%";
    }

    /// <summary>
    /// إحصائيات الحدث
    /// </summary>
    public class EventStatisticsDto
    {
        public Guid EventId { get; set; }
        public string EventTitle { get; set; } = string.Empty;
        public int TotalSections { get; set; }
        public int TotalDecisions { get; set; }
        public int TotalSurveys { get; set; }
        public int TotalSurveyQuestions { get; set; }
        public int TotalSurveyAnswers { get; set; }
        public int TotalDiscussions { get; set; }
        public int TotalDiscussionReplies { get; set; }
        public int TotalTables { get; set; }
        public int TotalAttachments { get; set; }
        public int TotalSignatures { get; set; }
        public int UniqueParticipants { get; set; }
    }
}

