using System;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// جدول الربط بين الإجابة والخيارات المختارة (يدعم الاختيار المتعدد)
    /// </summary>
    public class SurveyAnswerOption
    {
        public Guid SurveyAnswerId { get; set; }
        
        public Guid OptionId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual SurveyAnswer Answer { get; set; }
        public virtual SurveyOption Option { get; set; }
    }
}

