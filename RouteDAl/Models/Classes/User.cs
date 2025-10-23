using EvenDAL.Models.Shared.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace EvenDAL.Models.Classes
{
    public class User
    {
        public Guid UserId { get; set; }
        public Guid? OrganizationId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        [Required, MaxLength(150)]
        public string Email { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; }

        public UserRole Role { get; set; }

        [MaxLength(500)]
        public string ProfilePicture { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Organization? Organization { get; set; }
        public virtual ICollection<Event> CreatedEvents { get; set; } = new List<Event>();

        // العلاقات الجديدة
        public virtual ICollection<SurveyAnswer> SurveyAnswers { get; set; } = new List<SurveyAnswer>();
        public virtual ICollection<DiscussionReply> DiscussionReplies { get; set; } = new List<DiscussionReply>();
        public virtual ICollection<UserSignature> Signatures { get; set; } = new List<UserSignature>();

        // العلاقات القديمة
        public virtual ICollection<EventParticipant> EventParticipants { get; set; } = new List<EventParticipant>();
        public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
        public virtual ICollection<Document> UploadedDocuments { get; set; } = new List<Document>();
        public virtual ICollection<AgendaItem> PresentedItems { get; set; } = new List<AgendaItem>();
    }
}
