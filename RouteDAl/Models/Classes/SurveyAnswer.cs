using System;
using System.Collections.Generic;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// إجابة المستخدم على سؤال الاستبيان
    /// </summary>
    public class SurveyAnswer
    {
        public Guid SurveyAnswerId { get; set; }
        
        public Guid EventId { get; set; }
        
        public Guid QuestionId { get; set; }
        
        public Guid UserId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual Event Event { get; set; }
        public virtual SurveyQuestion Question { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<SurveyAnswerOption> SelectedOptions { get; set; } = new List<SurveyAnswerOption>();
    }
}

