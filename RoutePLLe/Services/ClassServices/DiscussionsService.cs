using AutoMapper;
using EvenDAL.Models.Classes;
using EvenDAL.Repositories.InterFace;
using EventPl.Dto.Mina;
using EventPl.Services.Interface;
using Microsoft.EntityFrameworkCore;
using RouteDAl.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventPl.Services.ClassServices
{
    /// <summary>
    /// خدمة إدارة النقاشات (Discussions)
    /// </summary>
    public class DiscussionsService : IDiscussionsService
    {
        private readonly AppDbContext _db;
        private readonly IRepository<Discussion, Guid> _discussionRepo;
        private readonly IRepository<DiscussionReply, Guid> _replyRepo;
        private readonly IMapper _mapper;

        public DiscussionsService(
            AppDbContext db,
            IRepository<Discussion, Guid> discussionRepo,
            IRepository<DiscussionReply, Guid> replyRepo,
            IMapper mapper)
        {
            _db = db;
            _discussionRepo = discussionRepo;
            _replyRepo = replyRepo;
            _mapper = mapper;
        }

        // ============================================
        // Discussion Operations
        // ============================================

        public async Task<List<DiscussionDto>> GetEventDiscussionsAsync(Guid eventId)
        {
            var discussions = await _db.Discussions
                .AsNoTracking()
                .Where(d => d.EventId == eventId)
                .Include(d => d.Replies.OrderByDescending(r => r.CreatedAt))
                    .ThenInclude(r => r.User)
                .OrderBy(d => d.Order)
                .ToListAsync();

            return _mapper.Map<List<DiscussionDto>>(discussions);
        }

        public async Task<DiscussionDto?> GetDiscussionByIdAsync(Guid discussionId)
        {
            var discussion = await _db.Discussions
                .AsNoTracking()
                .Where(d => d.DiscussionId == discussionId)
                .Include(d => d.Replies.OrderByDescending(r => r.CreatedAt))
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync();

            return discussion != null ? _mapper.Map<DiscussionDto>(discussion) : null;
        }

        public async Task<DiscussionDto> CreateDiscussionAsync(DiscussionDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("عنوان النقاش مطلوب");

            if (string.IsNullOrWhiteSpace(dto.Purpose))
                throw new ArgumentException("هدف النقاش مطلوب");

            if (dto.EventId == Guid.Empty)
                throw new ArgumentException("معرّف الحدث مطلوب");

            // تحديد الترتيب التلقائي
            if (dto.Order == 0)
            {
                var existingDiscussions = await _discussionRepo
                    .FindAsync(d => d.EventId == dto.EventId && d.SectionId == dto.SectionId);
                dto.Order = existingDiscussions.Any()
                    ? existingDiscussions.Max(d => d.Order) + 1
                    : 1;
            }

            var discussion = _mapper.Map<Discussion>(dto);
            discussion.DiscussionId = Guid.NewGuid();
            discussion.CreatedAt = DateTime.UtcNow;
            discussion.IsActive = true;

            await _discussionRepo.AddAsync(discussion);

            return _mapper.Map<DiscussionDto>(discussion);
        }

        public async Task<bool> UpdateDiscussionAsync(DiscussionDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("عنوان النقاش مطلوب");

            if (string.IsNullOrWhiteSpace(dto.Purpose))
                throw new ArgumentException("هدف النقاش مطلوب");

            var discussion = await _discussionRepo.GetByIdAsync(dto.DiscussionId);
            if (discussion == null)
                throw new KeyNotFoundException("النقاش غير موجود");

            discussion.Title = dto.Title.Trim();
            discussion.Purpose = dto.Purpose.Trim();
            discussion.Order = dto.Order;
            discussion.IsActive = dto.IsActive;

            return await _discussionRepo.UpdateAsync(discussion);
        }

        public async Task<bool> DeleteDiscussionAsync(Guid discussionId)
        {
            var discussion = await _discussionRepo.GetByIdAsync(discussionId);
            if (discussion == null)
                throw new KeyNotFoundException("النقاش غير موجود");

            return await _discussionRepo.DeleteByIdAsync(discussionId);
        }

        public async Task<bool> ToggleDiscussionActiveAsync(Guid discussionId, bool isActive)
        {
            var discussion = await _discussionRepo.GetByIdAsync(discussionId);
            if (discussion == null)
                throw new KeyNotFoundException("النقاش غير موجود");

            discussion.IsActive = isActive;
            return await _discussionRepo.UpdateAsync(discussion);
        }

        // ============================================
        // Reply Operations
        // ============================================

        public async Task<DiscussionReplyDto> AddReplyAsync(AddDiscussionReplyRequest request)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(request.Body))
                throw new ArgumentException("نص الرد مطلوب");

            if (request.DiscussionId == Guid.Empty)
                throw new ArgumentException("معرّف النقاش مطلوب");

            if (request.UserId == Guid.Empty)
                throw new ArgumentException("معرّف المستخدم مطلوب");

            // التحقق من أن النقاش نشط
            var discussion = await _discussionRepo.GetByIdAsync(request.DiscussionId);
            if (discussion == null)
                throw new KeyNotFoundException("النقاش غير موجود");

            if (!discussion.IsActive)
                throw new InvalidOperationException("النقاش غير نشط حالياً");

            var reply = new DiscussionReply
            {
                DiscussionReplyId = Guid.NewGuid(),
                DiscussionId = request.DiscussionId,
                UserId = request.UserId,
                Body = request.Body.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _replyRepo.AddAsync(reply);

            // إعادة تحميل الرد مع بيانات المستخدم
            var replyWithUser = await _db.DiscussionReplies
                .AsNoTracking()
                .Where(r => r.DiscussionReplyId == reply.DiscussionReplyId)
                .Include(r => r.User)
                .FirstOrDefaultAsync();

            return _mapper.Map<DiscussionReplyDto>(replyWithUser);
        }

        public async Task<bool> UpdateReplyAsync(Guid replyId, Guid userId, string newBody)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(newBody))
                throw new ArgumentException("نص الرد مطلوب");

            var reply = await _replyRepo.GetByIdAsync(replyId);
            if (reply == null)
                throw new KeyNotFoundException("الرد غير موجود");

            // التحقق من أن المستخدم هو صاحب الرد
            if (reply.UserId != userId)
                throw new UnauthorizedAccessException("لا يمكنك تعديل رد شخص آخر");

            reply.Body = newBody.Trim();
            return await _replyRepo.UpdateAsync(reply);
        }

        public async Task<bool> DeleteReplyAsync(Guid replyId, Guid userId, bool isAdmin = false)
        {
            var reply = await _replyRepo.GetByIdAsync(replyId);
            if (reply == null)
                throw new KeyNotFoundException("الرد غير موجود");

            // التحقق من الصلاحية
            if (!isAdmin && reply.UserId != userId)
                throw new UnauthorizedAccessException("لا يمكنك حذف رد شخص آخر");

            return await _replyRepo.DeleteByIdAsync(replyId);
        }

        public async Task<List<DiscussionReplyDto>> GetDiscussionRepliesAsync(Guid discussionId, int skip = 0, int take = 50)
        {
            var replies = await _db.DiscussionReplies
                .AsNoTracking()
                .Where(r => r.DiscussionId == discussionId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return _mapper.Map<List<DiscussionReplyDto>>(replies);
        }
    }
}

