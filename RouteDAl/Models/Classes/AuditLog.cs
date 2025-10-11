using System;
using System.ComponentModel.DataAnnotations;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// سجل التدقيق - لتتبع العمليات الحساسة
    /// </summary>
    public class AuditLog
    {
        public Guid AuditLogId { get; set; }
        
        public Guid? ActorId { get; set; } // المستخدم أو الأدمن الذي قام بالعملية
        
        [Required, MaxLength(100)]
        public string Action { get; set; } // مثل: Create, Update, Delete
        
        [Required, MaxLength(100)]
        public string Entity { get; set; } // اسم الكيان: Event, Survey, etc.
        
        public Guid? EntityId { get; set; }
        
        public DateTime At { get; set; } = DateTime.UtcNow;
        
        public string MetaJson { get; set; } // معلومات إضافية - JSON
        
        // لا نحتاج Navigation Properties هنا لتجنب التعقيد
    }
}

