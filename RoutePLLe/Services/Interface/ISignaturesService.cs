using EventPl.Dto.Mina;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventPl.Services.Interface
{
    /// <summary>
    /// خدمة إدارة توقيعات المستخدمين (UserSignatures)
    /// </summary>
    public interface ISignaturesService
    {
        /// <summary>
        /// حفظ توقيع المستخدم (مع منع التكرار)
        /// </summary>
        Task<UserSignatureDto> SaveSignatureAsync(SaveSignatureRequest request);
        
        /// <summary>
        /// الحصول على توقيع مستخدم محدد
        /// </summary>
        Task<UserSignatureDto?> GetUserSignatureAsync(Guid eventId, Guid userId);
        
        /// <summary>
        /// الحصول على جميع توقيعات الحدث
        /// </summary>
        Task<List<UserSignatureDto>> GetEventSignaturesAsync(Guid eventId);
        
        /// <summary>
        /// التحقق من توقيع المستخدم
        /// </summary>
        Task<bool> HasUserSignedAsync(Guid eventId, Guid userId);
        
        /// <summary>
        /// حذف توقيع (للأدمن فقط)
        /// </summary>
        Task<bool> DeleteSignatureAsync(Guid signatureId);
    }
}

