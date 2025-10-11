using EvenDAL.Models.Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Models.Classes
{
    /// <summary>
    /// الحدث - يحتوي على بنود ومكونات عامة (استبيانات، نقاشات، جداول، مرفقات)
    /// </summary>
    public class Event
    {
        public Guid EventId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid CreatedById { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        public EventStatus Status { get; set; } = EventStatus.Draft;

        /// <summary>
        /// هل يتطلب توقيع المستخدم قبل الإرسال؟
        /// </summary>
        public bool RequireSignature { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Organization Organization { get; set; }
        public virtual User CreatedBy { get; set; }

        // البنود (Sections)
        public virtual ICollection<Section> Sections { get; set; } = new List<Section>();

        // المكونات العامة
        public virtual ICollection<Survey> Surveys { get; set; } = new List<Survey>();
        public virtual ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
        public virtual ICollection<TableBlock> TableBlocks { get; set; } = new List<TableBlock>();
        public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

        // الإجابات والتوقيعات
        public virtual ICollection<SurveyAnswer> SurveyAnswers { get; set; } = new List<SurveyAnswer>();
        public virtual ICollection<UserSignature> UserSignatures { get; set; } = new List<UserSignature>();

        // العلاقات القديمة (نبقيها للتوافق مع الكود الموجود)
        public virtual ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
        public virtual ICollection<AgendaItem> AgendaItems { get; set; } = new List<AgendaItem>();
        public virtual ICollection<VotingSession> VotingSessions { get; set; } = new List<VotingSession>();
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
