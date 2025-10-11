using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// القرار - داخل البند، يحتوي على عنوان وعناصر مرقمة
    /// </summary>
    public class Decision
    {
        public Guid DecisionId { get; set; }
        
        public Guid SectionId { get; set; }
        
        [Required, MaxLength(300)]
        public string Title { get; set; }
        
        public int Order { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual Section Section { get; set; }
        public virtual ICollection<DecisionItem> Items { get; set; } = new List<DecisionItem>();
    }
}

