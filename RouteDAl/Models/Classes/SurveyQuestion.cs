using EvenDAL.Models.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// سؤال الاستبيان
    /// </summary>
    public class SurveyQuestion
    {
        public Guid SurveyQuestionId { get; set; }
        
        public Guid SurveyId { get; set; }
        
        [Required]
        public string Text { get; set; } // NVARCHAR(MAX)
        
        public SurveyQuestionType Type { get; set; } = SurveyQuestionType.Single;
        
        public int Order { get; set; }
        
        public bool IsRequired { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual Survey Survey { get; set; }
        public virtual ICollection<SurveyOption> Options { get; set; } = new List<SurveyOption>();
        public virtual ICollection<SurveyAnswer> Answers { get; set; } = new List<SurveyAnswer>();
    }
}

