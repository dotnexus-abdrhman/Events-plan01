using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EventPl.Dto;
using EventPl.Services.Interface;
using RouteDAl.Data.Contexts; // اسم الكونتكست عندك

using EvenDAL.Models.Classes;

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

            // فرّق مبكرًا لتقليل الاستعلامات: بريد إلكتروني أم هاتف؟
            bool isEmail = identifier.Contains("@");
            User? user = null;
            if (isEmail)
            {
                user = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == identifier && u.IsActive);
            }
            else
            {
                user = await _db.Users
                    .AsNoTracking()
                    .Where(u => u.Phone == identifier && u.IsActive)
                    .OrderBy(u => u.CreatedAt) // اختر الأقدم لضمان التقاط المستخدم المفعل الافتراضي seeded
                    .FirstOrDefaultAsync();
            }

            // إذا لم يُعثر عليه، ابحث في PlatformAdmins (بريد فقط)
            if (user is null && isEmail)
            {
                var admin = await _db.PlatformAdmins
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Email == identifier && a.IsActive);

                if (admin is not null)
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

            // إن لم يُعثر على مستخدم (خاصةً في مسار الهاتف)، أعد null
            if (user is null)
                return null;

            // التحقق من أن User مربوط بـ Organization
            if (user.OrganizationId == Guid.Empty)
                return null;

            return new UserDto
            {
                UserId = user.UserId,
                OrganizationId = user.OrganizationId,
                FullName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : (!string.IsNullOrWhiteSpace(user.Email) ? user.Email : user.Phone),
                Email = !string.IsNullOrWhiteSpace(user.Email) ? user.Email : user.Phone,
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
