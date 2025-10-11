using AutoMapper;
using EvenDAL.Models.Classes;
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
    /// خدمة إدارة توقيعات المستخدمين (UserSignatures)
    /// </summary>
    public class SignaturesService : ISignaturesService
    {
        private readonly AppDbContext _db;
        private readonly IRepository<UserSignature, Guid> _signatureRepo;
        private readonly IMapper _mapper;
        private readonly IHostEnvironment _env;
        private readonly ILogger<SignaturesService> _logger;

        public SignaturesService(
            AppDbContext db,
            IRepository<UserSignature, Guid> signatureRepo,
            IMapper mapper,
            IHostEnvironment env,
            ILogger<SignaturesService> logger)
        {
            _db = db;
            _signatureRepo = signatureRepo;
            _mapper = mapper;
            _env = env;
            _logger = logger;
        }

        public async Task<UserSignatureDto> SaveSignatureAsync(SaveSignatureRequest request)
        {
            // Validation
            if (request.EventId == Guid.Empty)
                throw new ArgumentException("معرّف الحدث مطلوب");

            if (request.UserId == Guid.Empty)
                throw new ArgumentException("معرّف المستخدم مطلوب");

            if (string.IsNullOrWhiteSpace(request.SignatureData))
                throw new ArgumentException("بيانات التوقيع مطلوبة");

            // التحقق من وجود توقيع سابق (Unique Constraint: EventId + UserId)
            var existingSignature = await _db.UserSignatures
                .FirstOrDefaultAsync(s => s.EventId == request.EventId && s.UserId == request.UserId);
            
            if (existingSignature != null)
            {
                // تحديث التوقيع الموجود
                existingSignature.Data = request.SignatureData;
                
                // حفظ الصورة إذا كانت Base64
                if (IsBase64String(request.SignatureData))
                {
                    try
                    {
                        var imagePath = await SaveSignatureImageAsync(request.EventId, request.UserId, request.SignatureData);
                        existingSignature.ImagePath = imagePath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "فشل حفظ صورة التوقيع");
                    }
                }

                await _signatureRepo.UpdateAsync(existingSignature);
                return _mapper.Map<UserSignatureDto>(existingSignature);
            }
            else
            {
                // إنشاء توقيع جديد
                var signature = new UserSignature
                {
                    UserSignatureId = Guid.NewGuid(),
                    EventId = request.EventId,
                    UserId = request.UserId,
                    Data = request.SignatureData,
                    ImagePath = string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                // حفظ الصورة إذا كانت Base64
                if (IsBase64String(request.SignatureData))
                {
                    try
                    {
                        var imagePath = await SaveSignatureImageAsync(request.EventId, request.UserId, request.SignatureData);
                        signature.ImagePath = imagePath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "فشل حفظ صورة التوقيع");
                    }
                }

                await _signatureRepo.AddAsync(signature);
                return _mapper.Map<UserSignatureDto>(signature);
            }
        }

        public async Task<UserSignatureDto?> GetUserSignatureAsync(Guid eventId, Guid userId)
        {
            var signature = await _db.UserSignatures
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.EventId == eventId && s.UserId == userId);

            return signature != null ? _mapper.Map<UserSignatureDto>(signature) : null;
        }

        public async Task<List<UserSignatureDto>> GetEventSignaturesAsync(Guid eventId)
        {
            var signatures = await _db.UserSignatures
                .AsNoTracking()
                .Where(s => s.EventId == eventId)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<UserSignatureDto>>(signatures);
        }

        public async Task<bool> HasUserSignedAsync(Guid eventId, Guid userId)
        {
            return await _db.UserSignatures
                .AnyAsync(s => s.EventId == eventId && s.UserId == userId);
        }

        public async Task<bool> DeleteSignatureAsync(Guid signatureId)
        {
            var signature = await _signatureRepo.GetByIdAsync(signatureId);
            if (signature == null)
                throw new KeyNotFoundException("التوقيع غير موجود");

            // حذف الصورة من الخادم
            if (!string.IsNullOrWhiteSpace(signature.ImagePath))
            {
                try
                {
                    var fullPath = Path.Combine(_env.ContentRootPath, "wwwroot", signature.ImagePath.TrimStart('/'));
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "فشل حذف صورة التوقيع: {Path}", signature.ImagePath);
                }
            }

            return await _signatureRepo.DeleteByIdAsync(signatureId);
        }

        // ============================================
        // Helper Methods
        // ============================================

        private bool IsBase64String(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return false;

            // إزالة البادئة إن وجدت (data:image/png;base64,)
            var base64Data = data.Contains(",") ? data.Split(',')[1] : data;

            try
            {
                Convert.FromBase64String(base64Data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> SaveSignatureImageAsync(Guid eventId, Guid userId, string base64Data)
        {
            // إزالة البادئة
            var base64String = base64Data.Contains(",") ? base64Data.Split(',')[1] : base64Data;
            var imageBytes = Convert.FromBase64String(base64String);

            // إنشاء مجلد التخزين
            var uploadsFolder = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "signatures", eventId.ToString());
            Directory.CreateDirectory(uploadsFolder);

            // إنشاء اسم ملف فريد
            var fileName = $"{userId}.png";
            var filePath = Path.Combine(uploadsFolder, fileName);

            // حفظ الملف
            await File.WriteAllBytesAsync(filePath, imageBytes);

            return $"/uploads/signatures/{eventId}/{fileName}";
        }
    }
}

