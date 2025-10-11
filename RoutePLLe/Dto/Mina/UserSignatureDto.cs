using System;

namespace EventPl.Dto.Mina
{
    /// <summary>
    /// DTO لتوقيع المستخدم (UserSignature)
    /// </summary>
    public class UserSignatureDto
    {
        public Guid UserSignatureId { get; set; }
        public Guid EventId { get; set; }
        public Guid UserId { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty; // Base64 أو JSON
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO لحفظ توقيع جديد
    /// </summary>
    public class SaveSignatureRequest
    {
        public Guid EventId { get; set; }
        public Guid UserId { get; set; }
        public string SignatureData { get; set; } = string.Empty; // Base64
    }
}

