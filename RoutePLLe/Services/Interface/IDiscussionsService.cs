using EventPl.Dto.Mina;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventPl.Services.Interface
{
    /// <summary>
    /// خدمة إدارة النقاشات (Discussions)
    /// </summary>
    public interface IDiscussionsService
    {
        // ============================================
        // Discussion Operations
        // ============================================
        
        /// <summary>
        /// الحصول على جميع نقاشات الحدث مع الردود
        /// </summary>
        Task<List<DiscussionDto>> GetEventDiscussionsAsync(Guid eventId);
        
        /// <summary>
        /// الحصول على نقاش محدد مع ردوده
        /// </summary>
        Task<DiscussionDto?> GetDiscussionByIdAsync(Guid discussionId);
        
        /// <summary>
        /// إنشاء نقاش جديد
        /// </summary>
        Task<DiscussionDto> CreateDiscussionAsync(DiscussionDto dto);
        
        /// <summary>
        /// تحديث نقاش
        /// </summary>
        Task<bool> UpdateDiscussionAsync(DiscussionDto dto);
        
        /// <summary>
        /// حذف نقاش (سيحذف الردود تلقائياً)
        /// </summary>
        Task<bool> DeleteDiscussionAsync(Guid discussionId);
        
        /// <summary>
        /// تفعيل/تعطيل نقاش
        /// </summary>
        Task<bool> ToggleDiscussionActiveAsync(Guid discussionId, bool isActive);

        // ============================================
        // Reply Operations
        // ============================================
        
        /// <summary>
        /// إضافة رد على نقاش
        /// </summary>
        Task<DiscussionReplyDto> AddReplyAsync(AddDiscussionReplyRequest request);
        
        /// <summary>
        /// تحديث رد (فقط صاحب الرد)
        /// </summary>
        Task<bool> UpdateReplyAsync(Guid replyId, Guid userId, string newBody);
        
        /// <summary>
        /// حذف رد (فقط صاحب الرد أو الأدمن)
        /// </summary>
        Task<bool> DeleteReplyAsync(Guid replyId, Guid userId, bool isAdmin = false);
        
        /// <summary>
        /// الحصول على ردود نقاش محدد (مع Pagination)
        /// </summary>
        Task<List<DiscussionReplyDto>> GetDiscussionRepliesAsync(Guid discussionId, int skip = 0, int take = 50);
    }
}

