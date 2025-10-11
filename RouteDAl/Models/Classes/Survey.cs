using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// الاستبيان - مكون عام على مستوى الحدث
    /// </summary>
    public class Survey
    {
        public Guid SurveyId { get; set; }

        public Guid EventId { get; set; }

        public Guid? SectionId { get; set; } // اختياري: عند التعيين يكون الاستبيان خاصاً بالبند


        [Required, MaxLength(300)]
        public string Title { get; set; }

        public int Order { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual Section? Section { get; set; }


        // Navigation Properties
        public virtual Event Event { get; set; }
        public virtual ICollection<SurveyQuestion> Questions { get; set; } = new List<SurveyQuestion>();
    }
}

