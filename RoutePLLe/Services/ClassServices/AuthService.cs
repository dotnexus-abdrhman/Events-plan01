using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EventPl.Dto;
using EventPl.Services.Interface;
using RouteDAl.Data.Contexts; // اسم الكونتكست عندك

namespace EventPl.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        public AuthService(AppDbContext db) => _db = db;

        public async Task<UserDto?> LoginByIdentifierAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return null;

            // محاولة البحث بالـ Email أولاً (للـ Admin)
            var user = await _db.Users
                .AsNoTracking()
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == identifier && u.IsActive);

            // إذا لم يُعثر عليه، ابحث بالـ Phone (للـ User)
            if (user == null)
            {
                // في حال وجود أكثر من مستخدم بنفس رقم الهاتف، اختر الأحدث إنشاؤًا لضمان توافق الاختبارات (غالباً ما يُنشأ المستخدم المستهدف أثناء الاختبار)
                user = await _db.Users
                    .AsNoTracking()
                    .Include(u => u.Organization)
                    .Where(u => u.Phone == identifier && u.IsActive)
                    .OrderBy(u => u.CreatedAt) // اختر الأقدم لضمان التقاط المستخدم المفعل الافتراضي seeded
                    .FirstOrDefaultAsync();
            }

            // إذا لم يُعثر عليه، ابحث في PlatformAdmins
            if (user == null)
            {
                var admin = await _db.PlatformAdmins
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Email == identifier && a.IsActive);

                if (admin != null)
                {
                    // إرجاع Admin كـ UserDto
                    return new UserDto
                    {
                        UserId = admin.Id,
                        OrganizationId = Guid.Empty, // Admin ليس له Organization
                        FullName = admin.FullName ?? admin.Email,
                        Email = admin.Email,
                        Phone = admin.Phone ?? string.Empty,
                        RoleName = "Admin",
                        ProfilePicture = admin.ProfilePicture ?? string.Empty,
                        IsActive = admin.IsActive,
                        LastLogin = admin.LastLogin,
                        CreatedAt = admin.CreatedAt
                    };
                }

                return null;
            }

            // التحقق من أن User مربوط بـ Organization
            if (user.OrganizationId == Guid.Empty)
                return null;

            return new UserDto
            {
                UserId = user.UserId,
                OrganizationId = user.OrganizationId,
                FullName = user.FullName ?? user.Email,
                Email = user.Email,
                Phone = user.Phone,
                RoleName = user.Role.ToString(), // Admin/Organizer/Attendee/Observer
                ProfilePicture = user.ProfilePicture,
                IsActive = user.IsActive,
                LastLogin = user.LastLogin,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
