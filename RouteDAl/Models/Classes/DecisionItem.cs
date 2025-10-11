using System;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// عنصر القرار - عنصر مرقم داخل القرار
    /// </summary>
    public class DecisionItem
    {
        public Guid DecisionItemId { get; set; }
        
        public Guid DecisionId { get; set; }
        
        [Required]
        public string Text { get; set; } // NVARCHAR(MAX)
        
        public int Order { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual Decision Decision { get; set; }
    }
}

