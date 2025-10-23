using AutoMapper;
using EvenDAL.Models.Classes;
using EvenDAL.Models.Shared.Enums;
using EvenDAL.Repositories.InterFace;
using EventPl.Dto.Mina;
using EventPl.Services.Interface;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _cache;
        private readonly IPdfExportService _pdfExport;

        public AttachmentsService(
            AppDbContext db,
            IRepository<Attachment, Guid> attachmentRepo,
            IMapper mapper,
            IHostEnvironment env,
            ILogger<AttachmentsService> logger,
            IMemoryCache cache,
            IPdfExportService pdfExport)
        {
            _db = db;
            _attachmentRepo = attachmentRepo;
            _mapper = mapper;
            _env = env;
            _logger = logger;
            _cache = cache;
            _pdfExport = pdfExport;
        }

        public async Task<List<AttachmentDto>> GetEventAttachmentsAsync(Guid eventId)
        {
            var version = await _db.Events.AsNoTracking()
                .Where(e => e.EventId == eventId)
                .Select(e => (DateTime?)(e.UpdatedAt ?? e.CreatedAt))
                .FirstOrDefaultAsync();
            var ticks = version.HasValue ? version.Value.Ticks : 0L;
            var cacheKey = $"evt:{eventId}:v:{ticks}:attachments";
            if (_cache.TryGetValue(cacheKey, out List<AttachmentDto> cached))
                return cached;

            var attachments = await _db.Attachments
                .AsNoTracking()
                .Where(a => a.EventId == eventId)
                .OrderBy(a => a.Order)
                .ToListAsync();

            var dtos = _mapper.Map<List<AttachmentDto>>(attachments);
            _cache.Set(cacheKey, dtos, TimeSpan.FromSeconds(45));
            return dtos;
        }
        public async Task<List<AttachmentDto>> GetEventAttachmentsAsync(Guid eventId, long? eventVersionTicks)
        {
            var ticks = eventVersionTicks ?? (await _db.Events.AsNoTracking()
                .Where(e => e.EventId == eventId)
                .Select(e => (DateTime?)(e.UpdatedAt ?? e.CreatedAt))
                .FirstOrDefaultAsync())?.Ticks ?? 0L;

            var cacheKey = $"evt:{eventId}:v:{ticks}:attachments";
            if (_cache.TryGetValue(cacheKey, out List<AttachmentDto> cached))
                return cached;

            var attachments = await _db.Attachments
                .AsNoTracking()
                .Where(a => a.EventId == eventId)
                .OrderBy(a => a.Order)
                .ToListAsync();

            var dtos = _mapper.Map<List<AttachmentDto>>(attachments);
            _cache.Set(cacheKey, dtos, TimeSpan.FromSeconds(45));
            return dtos;
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
                throw new ArgumentException("نوع الملف غير صحيح. استخدم: Image أو Pdf أو CustomPdf");

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
            _logger.LogInformation("[AttachmentsService] Added attachment {AttachmentId} Type={Type} Event={EventId} Path={Path} Size={Size}", attachment.AttachmentId, attachment.Type.ToString(), attachment.EventId, attachment.Path, attachment.Size);

            // إذا كان المرفق المرفوع هو CustomPdf، قم بإعادة توليد الملف المدمج وحفظه (بعد إضافة السجل)
            if (attachmentType == AttachmentType.CustomPdf)
            {
                _logger.LogInformation("[AttachmentsService] Trigger RegenerateMergedCustomPdfAsync for Event {EventId}", request.EventId);
                await RegenerateMergedCustomPdfAsync(request.EventId);
            }

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

        public async Task RegenerateMergedCustomPdfIfAnyAsync(Guid eventId)
        {
            try
            {
                var hasCustom = await _db.Attachments.AsNoTracking()
                    .AnyAsync(a => a.EventId == eventId && a.Type == AttachmentType.CustomPdf);
                if (!hasCustom)
                {
                    _logger.LogInformation("[AttachmentsService] No CustomPdf attachments for Event {EventId}. Skip regeneration.", eventId);
                    return;
                }
                _logger.LogInformation("[AttachmentsService] RegenerateMergedCustomPdfIfAnyAsync START for Event {EventId}", eventId);
                await RegenerateMergedCustomPdfAsync(eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AttachmentsService] RegenerateMergedCustomPdfIfAnyAsync FAILED for Event {EventId}", eventId);
            }
        }


        private async Task RegenerateMergedCustomPdfAsync(Guid eventId)
        {
            try
            {
                _logger.LogInformation("[AttachmentsService] RegenerateMergedCustomPdfAsync START for Event {EventId}", eventId);
                var bytes = await _pdfExport.ExportCustomMergedWithParticipantsPdfAsync(eventId);
                _logger.LogInformation("[AttachmentsService] Merged bytes length for Event {EventId} = {Len}", eventId, bytes?.Length ?? 0);

                var uploadsFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "events", eventId.ToString());
                Directory.CreateDirectory(uploadsFolder);
                var mergedFileName = "custom-merged.pdf";
                var mergedFullPath = Path.Combine(uploadsFolder, mergedFileName);
                _logger.LogInformation("[AttachmentsService] Writing merged PDF => {Path}", mergedFullPath);
                await File.WriteAllBytesAsync(mergedFullPath, bytes);
                var fileExists = File.Exists(mergedFullPath);
                var actualSize = fileExists ? new FileInfo(mergedFullPath).Length : 0;
                _logger.LogInformation("[AttachmentsService] Merged PDF written. Exists={Exists} Size={Size}", fileExists, actualSize);
                var webPath = $"/uploads/events/{eventId}/{mergedFileName}";

                var existing = await _attachmentRepo.FindAsync(a => a.EventId == eventId && a.Type == AttachmentType.CustomPdfMerged);
                var now = DateTime.UtcNow;
                if (existing.Any())
                {
                    var att = existing.OrderByDescending(a => a.CreatedAt).First();
                    att.FileName = mergedFileName;
                    att.Path = webPath;
                    att.Size = bytes.Length;
                    att.CreatedAt = now;
                    await _attachmentRepo.UpdateAsync(att);

                    foreach (var dup in existing.Where(a => a.AttachmentId != att.AttachmentId))
                    {
                        try
                        {
                            var dupFull = Path.Combine(_env.ContentRootPath, "wwwroot", dup.Path.TrimStart('/'));
                            if (File.Exists(dupFull)) File.Delete(dupFull);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "فشل حذف نسخة قديمة من الملف المدمج للحدث {EventId}", eventId);
                        }
                        await _attachmentRepo.DeleteByIdAsync(dup.AttachmentId);
                    }
                }
                else
                {
                    var existingAll = await _attachmentRepo.FindAsync(a => a.EventId == eventId);
                    var order = existingAll.Any() ? existingAll.Max(a => a.Order) + 1 : 1;
                    var merged = new Attachment
                    {
                        AttachmentId = Guid.NewGuid(),
                        EventId = eventId,
                        Type = AttachmentType.CustomPdfMerged,
                        FileName = mergedFileName,
                        Path = webPath,
                        Size = bytes.Length,
                        MetadataJson = "{}",
                        Order = order,
                        CreatedAt = now
                    };
                    await _attachmentRepo.AddAsync(merged);
                }

                _logger.LogInformation("[AttachmentsService] RegenerateMergedCustomPdfAsync DONE for Event {EventId}. Saved attachment record and file.", eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل توليد أو حفظ ملف PDF المدمج للحدث {EventId}", eventId);
            }
        }


    }
}

