using System;
using System.Collections.Generic;

namespace EventPl.Dto.Mina
{
    /// <summary>
    /// DTO للاستبيان (Survey) مع الأسئلة
    /// </summary>
    public class SurveyDto
    {
        public Guid SurveyId { get; set; }
        public Guid EventId { get; set; }
        public Guid? SectionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // الأسئلة
        public List<SurveyQuestionDto> Questions { get; set; } = new();
    }

    /// <summary>
    /// DTO لسؤال الاستبيان (SurveyQuestion) مع الخيارات
    /// </summary>
    public class SurveyQuestionDto
    {
        public Guid SurveyQuestionId { get; set; }
        public Guid SurveyId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = "Single"; // Single, Multiple
        public int Order { get; set; }
        public bool IsRequired { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // الخيارات
        public List<SurveyOptionDto> Options { get; set; } = new();
    }

    /// <summary>
    /// DTO لخيار السؤال (SurveyOption)
    /// </summary>
    public class SurveyOptionDto
    {
        public Guid SurveyOptionId { get; set; }
        public Guid QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO لإجابة المستخدم (SurveyAnswer)
    /// </summary>
    public class SurveyAnswerDto
    {
        public Guid SurveyAnswerId { get; set; }
        public Guid EventId { get; set; }
        public Guid QuestionId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // الخيارات المختارة (للأسئلة Multiple)
        public List<Guid> SelectedOptionIds { get; set; } = new();
    }

    /// <summary>
    /// DTO لحفظ إجابات المستخدم
    /// </summary>
    public class SaveSurveyAnswersRequest
    {
        public Guid EventId { get; set; }
        public Guid UserId { get; set; }
        public List<QuestionAnswerDto> Answers { get; set; } = new();
    }

    public class QuestionAnswerDto
    {
        public Guid QuestionId { get; set; }
        public List<Guid> SelectedOptionIds { get; set; } = new();
    }
}

