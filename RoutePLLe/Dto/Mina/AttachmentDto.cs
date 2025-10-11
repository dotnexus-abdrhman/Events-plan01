using System;

namespace EventPl.Dto.Mina
{
    /// <summary>
    /// DTO للمرفق (Attachment)
    /// </summary>
    public class AttachmentDto
    {
        public Guid AttachmentId { get; set; }
        public Guid EventId { get; set; }
        public Guid? SectionId { get; set; }
        public string Type { get; set; } = "Image"; // Image, Pdf
        public string FileName { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public string MetadataJson { get; set; } = "{}";
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO لرفع مرفق جديد
    /// </summary>
    public class UploadAttachmentRequest
    {
        public Guid EventId { get; set; }
        public Guid? SectionId { get; set; }
        public string Type { get; set; } = "Image";
        public string FileName { get; set; } = string.Empty;
        public byte[] FileData { get; set; } = Array.Empty<byte>();
    }
}

