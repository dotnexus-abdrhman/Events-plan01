using AutoMapper;
using EvenDAL.Models.Classes;
using EvenDAL.Models.Shared.Enums;
using EvenDAL.Repositories.InterFace;
using EventPl.Dto.Mina;
using EventPl.Services.Interface;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RouteDAl.Data.Contexts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EventPl.Services.ClassServices
{
    /// <summary>
    /// خدمة إدارة المرفقات (Attachments)
    /// </summary>
    public class AttachmentsService : IAttachmentsService
    {
        private readonly AppDbContext _db;
        private readonly IRepository<Attachment, Guid> _attachmentRepo;
        private readonly IMapper _mapper;
        private readonly IHostEnvironment _env;
        private readonly ILogger<AttachmentsService> _logger;

        public AttachmentsService(
            AppDbContext db,
            IRepository<Attachment, Guid> attachmentRepo,
            IMapper mapper,
            IHostEnvironment env,
            ILogger<AttachmentsService> logger)
        {
            _db = db;
            _attachmentRepo = attachmentRepo;
            _mapper = mapper;
            _env = env;
            _logger = logger;
        }

        public async Task<List<AttachmentDto>> GetEventAttachmentsAsync(Guid eventId)
        {
            var attachments = await _db.Attachments
                .AsNoTracking()
                .Where(a => a.EventId == eventId)
                .OrderBy(a => a.Order)
                .ToListAsync();

            return _mapper.Map<List<AttachmentDto>>(attachments);
        }

        public async Task<AttachmentDto?> GetAttachmentByIdAsync(Guid attachmentId)
        {
            var attachment = await _attachmentRepo.GetByIdAsync(attachmentId);
            return attachment != null ? _mapper.Map<AttachmentDto>(attachment) : null;
        }

        public async Task<AttachmentDto> UploadAttachmentAsync(UploadAttachmentRequest request)
        {
            // Validation
            if (request.EventId == Guid.Empty)
                throw new ArgumentException("معرّف الحدث مطلوب");

            if (string.IsNullOrWhiteSpace(request.FileName))
                throw new ArgumentException("اسم الملف مطلوب");

            if (request.FileData == null || request.FileData.Length == 0)
                throw new ArgumentException("بيانات الملف مطلوبة");

            // التحقق من نوع الملف
            if (!Enum.TryParse<AttachmentType>(request.Type, true, out var attachmentType))
                throw new ArgumentException("نوع الملف غير صحيح. استخدم: Image أو Pdf");

            // التحقق من امتداد الملف
            var extension = Path.GetExtension(request.FileName).ToLowerInvariant();
            var allowedExtensions = attachmentType == AttachmentType.Image
                ? new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }
                : new[] { ".pdf" };

            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException($"امتداد الملف غير مسموح. الامتدادات المسموحة: {string.Join(", ", allowedExtensions)}");

            // إنشاء مجلد التخزين
            var uploadsFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "events", request.EventId.ToString());
            Directory.CreateDirectory(uploadsFolder);

            // إنشاء اسم ملف فريد
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // حفظ الملف
            try
            {
                await File.WriteAllBytesAsync(filePath, request.FileData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حفظ الملف");
                throw new Exception("فشل حفظ الملف");
            }

            // تحديد الترتيب التلقائي ضمن نفس النطاق (البند إن وجد، وإلا على مستوى الحدث)
            var existingAttachments = await _attachmentRepo
                .FindAsync(a => a.EventId == request.EventId && a.SectionId == request.SectionId);
            var order = existingAttachments.Any()
                ? existingAttachments.Max(a => a.Order) + 1
                : 1;

            // إنشاء السجل في قاعدة البيانات
            var attachment = new Attachment
            {
                AttachmentId = Guid.NewGuid(),
                EventId = request.EventId,
                SectionId = request.SectionId,
                Type = attachmentType,
                FileName = request.FileName,
                Path = $"/uploads/events/{request.EventId}/{uniqueFileName}",
                Size = request.FileData.Length,
                MetadataJson = "{}",
                Order = order,
                CreatedAt = DateTime.UtcNow
            };

            await _attachmentRepo.AddAsync(attachment);

            return _mapper.Map<AttachmentDto>(attachment);
        }

        public async Task<bool> DeleteAttachmentAsync(Guid attachmentId)
        {
            var attachment = await _attachmentRepo.GetByIdAsync(attachmentId);
            if (attachment == null)
                throw new KeyNotFoundException("المرفق غير موجود");

            // حذف الملف من الخادم
            try
            {
                var fullPath = Path.Combine(_env.ContentRootPath, "wwwroot", attachment.Path.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "فشل حذف الملف الفعلي: {Path}", attachment.Path);
                // نستمر في حذف السجل من قاعدة البيانات
            }

            return await _attachmentRepo.DeleteByIdAsync(attachmentId);
        }

        public async Task<bool> ReorderAttachmentsAsync(Guid eventId, List<Guid> attachmentIds)
        {
            if (attachmentIds == null || !attachmentIds.Any())
                throw new ArgumentException("قائمة المرفقات فارغة");

            var attachments = await _attachmentRepo
                .FindAsync(a => a.EventId == eventId && attachmentIds.Contains(a.AttachmentId));

            var attachmentsList = attachments.ToList();
            
            for (int i = 0; i < attachmentIds.Count; i++)
            {
                var attachment = attachmentsList.FirstOrDefault(a => a.AttachmentId == attachmentIds[i]);
                if (attachment != null)
                {
                    attachment.Order = i + 1;
                    await _attachmentRepo.UpdateAsync(attachment);
                }
            }

            return true;
        }
    }
}

