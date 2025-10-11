using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EventPresentationlayer.ViewModels
{
    public class UserEventsIndexItemVm
    {
        public Guid EventId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }

    public class UserEventDetailsVm
    {
        public Guid EventId { get; set; }
        public Guid OrganizationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // عرض للقراءة فقط
        public List<TableVm> Tables { get; set; } = new();
        public List<UserDocumentVm> Documents { get; set; } = new();

        // استبيانات
        public List<UserSessionVm> Sessions { get; set; } = new();

        // نقاشات وردود المستخدم
        public List<UserDiscussionReplyVm> Discussions { get; set; } = new();

        // توقيع اختياري
        [Display(Name = "إضافة توقيع؟")] public bool AddSignature { get; set; } = false;
        [StringLength(200)] [Display(Name = "التوقيع النصّي (اختياري)")] public string? SignatureText { get; set; }
        [Display(Name = "صورة التوقيع (Base64)")] public string? SignatureImageData { get; set; }
    }

    public class UserSessionVm
    {
        public Guid VotingSessionId { get; set; }
        public string Question { get; set; } = string.Empty;
        public bool IsMultipleChoice { get; set; }
        public List<UserOptionVm> Options { get; set; } = new();
        // للربط عند الإرسال
        public List<Guid> SelectedOptionIds { get; set; } = new();
    }

    public class UserOptionVm
    {
        public Guid VotingOptionId { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public class UserDiscussionReplyVm
    {
        public Guid RootPostId { get; set; }
        [Display(Name = "الغرض/الهدف")] public string Goal { get; set; } = string.Empty;
        [Display(Name = "العنوان (اختياري)")] public string? Title { get; set; }
        [Display(Name = "ردّك")] [StringLength(1000)] public string? Reply { get; set; }
    }

    public class UserDocumentVm
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public bool IsImage => FileType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        public bool IsPdf => FileType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    }
}

