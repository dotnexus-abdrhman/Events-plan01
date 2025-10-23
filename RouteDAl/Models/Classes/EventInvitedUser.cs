using System;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// ربط (Many-to-Many) بين الحدث والمستخدمين المدعوين بشكل فردي.
    /// عند وجود مدعوين فرديين للحدث، يجب أن يظهر الحدث فقط لهؤلاء المدعوين
    /// ولا يظهر لبقية أعضاء المجموعة حتى وإن تطابق OrganizationId.
    /// </summary>
    public class EventInvitedUser
    {
        public Guid EventInvitedUserId { get; set; } = Guid.NewGuid();
        public Guid EventId { get; set; }
        public Guid UserId { get; set; }
        public DateTime InvitedAt { get; set; } = DateTime.UtcNow;

        // Navigation (اختياري)
        public virtual Event? Event { get; set; }
        public virtual User? User { get; set; }
    }
}

