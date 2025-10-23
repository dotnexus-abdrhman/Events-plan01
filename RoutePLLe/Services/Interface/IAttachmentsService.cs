using EventPl.Dto.Mina;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventPl.Services.Interface
{
    /// <summary>
    /// خدمة إدارة المرفقات (Attachments)
    /// </summary>
    public interface IAttachmentsService
    {
        /// <summary>
        /// الحصول على جميع مرفقات الحدث
        /// </summary>
        Task<List<AttachmentDto>> GetEventAttachmentsAsync(Guid eventId);
        /// <summary>
        /// الحصول على جميع مرفقات الحدث مع تمرير نسخة الحدث (Ticks) لاستخدامها في المفتاح المؤقت
        /// </summary>
        Task<List<AttachmentDto>> GetEventAttachmentsAsync(Guid eventId, long? eventVersionTicks);


        /// <summary>
        /// الحصول على مرفق محدد
        /// </summary>
        Task<AttachmentDto?> GetAttachmentByIdAsync(Guid attachmentId);

        /// <summary>
        /// رفع مرفق جديد (صورة أو PDF)
        /// </summary>
        Task<AttachmentDto> UploadAttachmentAsync(UploadAttachmentRequest request);

        /// <summary>
        /// حذف مرفق (مع حذف الملف من الخادم)
        /// </summary>
        Task<bool> DeleteAttachmentAsync(Guid attachmentId);

        /// <summary>
        /// تحديث ترتيب المرفقات
        /// </summary>
        Task<bool> ReorderAttachmentsAsync(Guid eventId, List<Guid> attachmentIds);

        Task RegenerateMergedCustomPdfIfAnyAsync(Guid eventId);
    }
}

