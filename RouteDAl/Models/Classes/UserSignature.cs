using System;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// توقيع المستخدم على الحدث
    /// </summary>
    public class UserSignature
    {
        public Guid UserSignatureId { get; set; }
        
        public Guid EventId { get; set; }
        
        public Guid UserId { get; set; }
        
        [MaxLength(500)]
        public string ImagePath { get; set; } // مسار الصورة
        
        public string Data { get; set; } // Base64 أو بيانات أخرى - NVARCHAR(MAX)
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation Properties
        public virtual Event Event { get; set; }
        public virtual User User { get; set; }
    }
}

