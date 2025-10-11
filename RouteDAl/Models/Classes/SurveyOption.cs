using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// خيار السؤال في الاستبيان
    /// </summary>
    public class SurveyOption
    {
        public Guid SurveyOptionId { get; set; }
        
        public Guid QuestionId { get; set; }
        
        [Required, MaxLength(500)]
        public string Text { get; set; }
        
        public int Order { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual SurveyQuestion Question { get; set; }
        public virtual ICollection<SurveyAnswerOption> AnswerOptions { get; set; } = new List<SurveyAnswerOption>();
    }
}

